using UnityEngine;
using UABS.Assets.Script.View;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Misc;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;
using UnityEditor.Experimental.GraphView;
using System.IO;

namespace UABS.Assets.Script.Controller
{
    public class TextureDisplayForBundle : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private TextureView _textureView;
        private ReadTexturesFromBundle _readTexturesFromBundle;
        private BundleFileInstance _currBunInst;
        private Dictionary<long, AssetImageInfo> _cacheTextureByPathID = new();

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _textureView.dispatcher = appEnvironment.Dispatcher;
            _readTexturesFromBundle = new(appEnvironment.AssetsManager);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is BundleReadEvent bre)
            {
                _currBunInst = bre.Bundle;
                _cacheTextureByPathID = new();
                _textureView.AssignSizeText("");
                _textureView.Render(null);
                _textureView.AssignIndexText("");
            }
            else if (e is AssetSelectionEvent ase)
            {
                AssetImageInfo textureWithMeta = GetTextureByPathID(ase.PathID);
                _textureView.AssignSizeText($"{textureWithMeta.rect.width}x{textureWithMeta.rect.height} ({textureWithMeta.compressionFormat})");
                _textureView.Render(textureWithMeta.texture2D);
                _textureView.AssignIndexText($"{ase.CurrIndex + 1} / {ase.TotalNumOfAssets}");
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
            else if (e is BundleRead4DependencyEvent br4d)
            {
                _currBunInst = br4d.Bundle;
                _cacheTextureByPathID = new();
                _textureView.AssignSizeText("");
                _textureView.Render(null);
                _textureView.AssignIndexText("");
            }
            else if (e is FolderRead4DependencyEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _textureView.AssignSizeText("");
                    _textureView.Render(null);
                    _textureView.AssignIndexText("");
                }
            }
        }

        private AssetImageInfo GetTextureByPathID(long pathID)
        {
            if (!_cacheTextureByPathID.ContainsKey(pathID))
            {
                AssetImageInfo? _textureWithMeta = _readTexturesFromBundle.ReadSpriteByPathID(_currBunInst, pathID);
                _textureWithMeta ??= _readTexturesFromBundle.ReadTexture2DByPathID(_currBunInst, pathID);
                if (_textureWithMeta == null)
                {
                    Debug.LogWarning($"The given path id {pathID} is neither Texture2D nor Sprite.");
                    return new();
                }
                _cacheTextureByPathID[pathID] = (AssetImageInfo)_textureWithMeta;
            }
            return _cacheTextureByPathID[pathID];
        }
    }
}