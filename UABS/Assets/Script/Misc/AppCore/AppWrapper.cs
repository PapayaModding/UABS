using UABS.Assets.Script.Wrapper.FileBrowser;
using UABS.Assets.Script.Wrapper.Json;
using UABS.Assets.Script.Wrapper.TextureDecoder;

namespace UABS.Assets.Script.Misc.AppCore
{
    public class AppWrapper
    {
        public NewtonsoftJsonSerializer JsonSerializer { get; } = new();
        public BcDecoderWrapper TextureDecoder { get; } = new();
        public StandaloneFileBrowserWrapper FileBrowser { get; } = new();
    }
}