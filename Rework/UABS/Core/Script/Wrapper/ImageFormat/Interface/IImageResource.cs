namespace UABS.Wrapper
{
    public interface IImageResource
    {
        int Width { get; }
        int Height { get; }
        ImagePixelFormat ImagePixelFormat { get; }
    }
}