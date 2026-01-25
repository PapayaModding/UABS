using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using UABS.Script.Wrapper.TextureDecoder.Interface;

namespace UABS.Script.Wrapper.TextureDecoder
{
    public class BcDecoderWrapper : ITextureDecoder
    {
        private readonly BcDecoder _decoder = new();

        public IColor[] DecodeRaw(byte[] imageBytes, int width, int height, TextureCompressionFormat format)
        {
            var decoded = _decoder.DecodeRaw(imageBytes, width, height, format.ToLibraryFormat());
            return decoded.Select(c => new ColorRgba32Wrapper(c) as IColor).ToArray();
        }

        public byte[] DecodeToBytes(byte[] imageBytes, int width, int height, TextureCompressionFormat format)
        {
            var libFormat = format.ToLibraryFormat();
            var decoded = _decoder.DecodeRaw(imageBytes, width, height, libFormat);
            var byteCount = decoded.Length * Unsafe.SizeOf<ColorRgba32>();
            var rgbaBytes = new byte[byteCount];
            MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);
            return rgbaBytes;
        }
    }
}