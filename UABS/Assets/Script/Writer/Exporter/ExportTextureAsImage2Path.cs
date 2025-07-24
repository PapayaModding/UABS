using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Wrapper.TextureDecoder;
using UABS.Assets.Script.Reader.ImageDecoder;

namespace UABS.Assets.Script.Writer.Exporter
{
    public class ExportTextureAsImage2Path
    {
        private readonly ImageReader _imageReader;

        public ExportTextureAsImage2Path(AssetsManager assetsManager, ITextureDecoder textureDecoder)
        {
            _imageReader = new(assetsManager, textureDecoder);
        }

        public void ExportAssetsToPath(ExportMethod exportMethod,
                                            List<ParsedAssetAndEntry> entryInfos)
        { 
            string path = exportMethod.destination;
            string exportPath = Path.Combine(path, PredefinedPaths.ExportFolderName);
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);

            foreach (ParsedAssetAndEntry entryInfo in entryInfos)
            {
                AssetClassID type = entryInfo.assetEntryInfo.classID;
                string typePath = Path.Combine(exportPath, type.ToString());
                if (!Directory.Exists(typePath))
                    Directory.CreateDirectory(typePath);
                
                string imageName = entryInfo.assetEntryInfo.name + ".png";
                string imagePath = Path.Combine(typePath, imageName);
                long pathID = entryInfo.assetEntryInfo.pathID;
                AssetImageInfo? _textureWithMeta = _imageReader.SpriteToImage(entryInfo);
                _textureWithMeta ??= _imageReader.Texture2DToImage(entryInfo);
                if (_textureWithMeta == null)
                {
                    Debug.LogWarning($"{imageName} with path id {pathID} couldn't be saved because no texture is found.");
                    continue;
                }
                AssetImageInfo textureWithMeta = (AssetImageInfo)_textureWithMeta;
                byte[] imageData = textureWithMeta.texture2D.EncodeToPNG();
                File.WriteAllBytes(imagePath, imageData);
            }
        }
    }
}