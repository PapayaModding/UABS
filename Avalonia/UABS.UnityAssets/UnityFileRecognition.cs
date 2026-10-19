namespace UABS.UnityAssets;

public enum UnityFileRecognitionStatus
{
    Supported,
    RecognizedUnsupported,
    Invalid,
    Unreadable
}

public sealed record UnityFileRecognition(
    UnityFileRecognitionStatus Status,
    UnityFileKind Kind,
    Exception? Error = null)
{
    public bool IsSupported => Status == UnityFileRecognitionStatus.Supported;
}
