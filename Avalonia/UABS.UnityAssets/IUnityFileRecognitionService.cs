namespace UABS.UnityAssets;

public interface IUnityFileRecognitionService
{
    UnityFileRecognition Recognize(string path, CancellationToken cancellationToken = default);
}
