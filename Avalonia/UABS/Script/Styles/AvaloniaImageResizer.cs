using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;

public static class AvaloniaImageResizer
{
    public static Bitmap ResizeBilinear(Bitmap source, int width, int height)
    {
        using var target = new RenderTargetBitmap(new PixelSize(width, height));

        using (var ctx = target.CreateDrawingContext(true))
        {
            ctx.DrawImage(
                source,
                new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height),
                new Rect(0, 0, width, height)
            );
        }

        using var ms = new MemoryStream();
        target.Save(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }
}
