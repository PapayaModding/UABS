namespace UABS.Assets.Script.Wrapper.TextureDecoder
{
    public interface ITextureDecoder
    {
        public IColor[] DecodeRaw(byte[] imageBytes, int width, int height, TextureCompressionFormat format);
        byte[] DecodeToBytes(byte[] imageBytes, int width, int height, TextureCompressionFormat format);
    }
}