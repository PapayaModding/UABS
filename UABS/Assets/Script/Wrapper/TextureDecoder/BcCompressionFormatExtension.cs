using BCnEncoder.Shared;

namespace UABS.Assets.Script.Wrapper.TextureDecoder
{
    public static class BcCompressionFormatExtension
    {
        public static CompressionFormat ToLibraryFormat(this TextureCompressionFormat format)
        {
            return format switch
            {
                TextureCompressionFormat.R => CompressionFormat.R,
                TextureCompressionFormat.Rg => CompressionFormat.Rg,
                TextureCompressionFormat.Rgb => CompressionFormat.Rgb,
                TextureCompressionFormat.Rgba => CompressionFormat.Rgba,
                TextureCompressionFormat.Bgra => CompressionFormat.Bgra,
                TextureCompressionFormat.Bc1 => CompressionFormat.Bc1,
                TextureCompressionFormat.Bc1WithAlpha => CompressionFormat.Bc1WithAlpha,
                TextureCompressionFormat.Bc2 => CompressionFormat.Bc2,
                TextureCompressionFormat.Bc3 => CompressionFormat.Bc3,
                TextureCompressionFormat.Bc4 => CompressionFormat.Bc4,
                TextureCompressionFormat.Bc5 => CompressionFormat.Bc5,
                TextureCompressionFormat.Bc6U => CompressionFormat.Bc6U,
                TextureCompressionFormat.Bc6S => CompressionFormat.Bc6S,
                TextureCompressionFormat.Bc7 => CompressionFormat.Bc7,
                TextureCompressionFormat.Atc => CompressionFormat.Atc,
                TextureCompressionFormat.AtcExplicitAlpha => CompressionFormat.AtcExplicitAlpha,
                TextureCompressionFormat.AtcInterpolatedAlpha => CompressionFormat.AtcInterpolatedAlpha,
                TextureCompressionFormat.Unknown => CompressionFormat.Unknown,
                _ => CompressionFormat.Unknown,
            };
        }
    }
}