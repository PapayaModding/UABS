namespace UABS.UnityAssets;

public sealed class AssetLoadException : Exception
{
    public AssetLoadException(AssetLoadErrorCode code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public AssetLoadErrorCode Code { get; }
}

public enum AssetLoadErrorCode
{
    FileNotFound,
    UnsupportedFormat,
    InvalidData
}
