using UABS.Wrapper;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace UABS.AvaloniaUI
{
    public class AvaloniaImageResource : IImageResource
    {
        public Bitmap Bitmap { get; }

        public AvaloniaImageResource(Bitmap bitmap)
        {
            Bitmap = bitmap;
        }

        public int Width => Bitmap.PixelSize.Width;
        public int Height => Bitmap.PixelSize.Height;

        public ImagePixelFormat ImagePixelFormat
        {
            get
            {
                if (Bitmap == null)
                    return ImagePixelFormat.Unknown;

                if (Bitmap.Format.Equals(PixelFormat.Rgba8888))
                    return ImagePixelFormat.RGBA32;

                if (Bitmap.Format.Equals(PixelFormat.Bgra8888))
                    return ImagePixelFormat.BGRA32;

                return ImagePixelFormat.Unknown;
            }
        }
    }
}