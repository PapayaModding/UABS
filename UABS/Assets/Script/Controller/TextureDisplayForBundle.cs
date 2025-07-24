using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.View.BundleView;

namespace UABS.Assets.Script.Controller
{
    public class TextureDisplayForBundle : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private TextureView _textureView;
        private ImageReader _imageReader;
        private Dictionary<long, AssetImageInfo> _storedTextureByPathID = new();
        private Dictionary<long, ParsedAssetAndEntry> _pathID2EntryInfo;
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _textureView.dispatcher = appEnvironment.Dispatcher;
            _imageReader = new(appEnvironment.AssetsManager, appEnvironment.Wrapper.TextureDecoder);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetSelectionEvent ase)
            {
                AssetImageInfo textureWithMeta = GetTextureByPathID(ase.PathID);
                _textureView.AssignSizeText($"{textureWithMeta.rect.width}x{textureWithMeta.rect.height} ({textureWithMeta.compressionFormat})");
                _textureView.Render(textureWithMeta.texture2D);
                _textureView.AssignIndexText($"{ase.Index + 1} / {ase.TotalNumOfAssets}");
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    _textureView.AssignSizeText("");
                    _textureView.Render(null);
                    _textureView.AssignIndexText("");
                }
            }
            else if (e is FolderRead4DeriveEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _textureView.AssignSizeText("");
                    _textureView.Render(null);
                    _textureView.AssignIndexText("");
                }
            }
            else if (e is GoBundleViewEvent gbve)
            {
                _pathID2EntryInfo = gbve.PathID2EntryInfo;
                _storedTextureByPathID = new();
                _textureView.AssignSizeText("");
                _textureView.Render(null);
                _textureView.AssignIndexText("");
            }
        }

        private AssetImageInfo GetTextureByPathID(long pathID)
        {
            if (!_storedTextureByPathID.ContainsKey(pathID))
            {
                AssetImageInfo? _textureWithMeta = _imageReader.SpriteToImage(_pathID2EntryInfo[pathID]);
                _textureWithMeta ??= _imageReader.Texture2DToImage(_pathID2EntryInfo[pathID]);
                if (_textureWithMeta == null)
                {
                    Debug.LogWarning($"The given path id {pathID} is neither Texture2D nor Sprite.");
                    return new();
                }
                _storedTextureByPathID[pathID] = (AssetImageInfo)_textureWithMeta;
            }
            return _storedTextureByPathID[pathID];
        }
    }
}