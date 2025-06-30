using System.Collections.Generic;
using System.IO;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Reader;
using UnityEngine;

namespace UABS.Assets.Script.Writer
{
    public class WriteTextureAsImage2Path
    {
        private ReadTexturesFromBundle _readTexturesFromBundle;

        public WriteTextureAsImage2Path(AssetsManager assetsManager)
        {
            _readTexturesFromBundle = new(assetsManager);
        }

        public void ExportAllAssetsToPath(ExportMethod exportMethod,
                                            List<AssetDisplayInfo> assetsDisplayInfo,
                                            BundleFileInstance bunInst)
        { 
            string path = exportMethod.destination;
            string exportPath = Path.Combine(path, "UABS_Exported_Assets");
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);

            foreach (AssetDisplayInfo assetDisplayInfo in assetsDisplayInfo)
            {
                AssetClassID type = assetDisplayInfo.assetTextInfo.type;
                string typePath = Path.Combine(exportPath, type.ToString());
                if (!Directory.Exists(typePath))
                    Directory.CreateDirectory(typePath);
                
                string imageName = assetDisplayInfo.assetTextInfo.name + ".png";
                string imagePath = Path.Combine(typePath, imageName);
                long pathID = assetDisplayInfo.assetTextInfo.pathID;
                Texture2DWithMeta? _textureWithMeta = _readTexturesFromBundle.ReadSpriteByPathID(bunInst, pathID);
                _textureWithMeta ??= _readTexturesFromBundle.ReadTexture2DByPathID(bunInst, pathID);
                if (_textureWithMeta == null)
                {
                    Debug.LogWarning($"{imageName} with path id {pathID} couldn't be saved because no texture is found.");
                    continue;
                }
                Texture2DWithMeta textureWithMeta = (Texture2DWithMeta)_textureWithMeta;
                byte[] imageData = textureWithMeta.texture2D.EncodeToPNG();
                File.WriteAllBytes(imagePath, imageData);
            }
        }
    }
}