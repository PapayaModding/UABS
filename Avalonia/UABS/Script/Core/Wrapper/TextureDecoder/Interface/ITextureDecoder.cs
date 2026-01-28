namespace UABS.Script.Wrapper.TextureDecoder.Interface
{
    public interface ITextureDecoder
    {
        public IColor[] DecodeRaw(byte[] imageBytes, int width, int height, TextureCompressionFormat format);
        byte[] DecodeToBytes(byte[] imageBytes, int width, int height, TextureCompressionFormat format);
    }
}