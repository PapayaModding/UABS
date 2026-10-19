using System.Security;

namespace UABS.UnityAssets;

public sealed class UnityFileRecognitionService : IUnityFileRecognitionService
{
    public UnityFileRecognition Recognize(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            UnityFileKind kind = UnityFileDetector.Detect(path);
            cancellationToken.ThrowIfCancellationRequested();

            return kind switch
            {
                UnityFileKind.Bundle or UnityFileKind.SerializedFile =>
                    new UnityFileRecognition(UnityFileRecognitionStatus.Supported, kind),
                UnityFileKind.Unknown =>
                    new UnityFileRecognition(UnityFileRecognitionStatus.Invalid, kind),
                _ => new UnityFileRecognition(UnityFileRecognitionStatus.RecognizedUnsupported, kind)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or SecurityException)
        {
            return new UnityFileRecognition(
                UnityFileRecognitionStatus.Unreadable,
                UnityFileKind.Unknown,
                exception);
        }
    }
}
