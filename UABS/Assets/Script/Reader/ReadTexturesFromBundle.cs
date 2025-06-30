using System;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using CommunityToolkit.HighPerformance;
using System.Runtime.InteropServices;
using System.Text;
using static UABS.Assets.Script.Reader.DumpReader;
using static UABS.Assets.Script.Reader.AtlasDumpProcessor;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Reader
{
    public class ReadTexturesFromBundle
    {
        public struct AssetExternal
        {
            public AssetsFileInstance file;
            public AssetFileInfo info;
            public AssetTypeValueField instance;
        }

        private AssetsManager AssetsManager { get; }

        private DumpReader _dumpReader;

        private BundleFileInstance _currBunInst = null;

        private List<DumpInfo> _currSpriteDumps = null;

        private List<AtlasDumpProcessor> _currAtlasDumpProcessors = null;

        private List<AssetFileInfo> _currAssetInfos = null;

        private AssetsFileInstance _currFileInst = null;

        private AssetClassID _lastReadType = AssetClassID.@void;

        public ReadTexturesFromBundle(AssetsManager am)
        {
            AssetsManager = am;
            _dumpReader = new(AssetsManager);
        }

        public Texture2DWithMeta? ReadSpriteByPathID(BundleFileInstance bunInst, long pathID)
        {
            AtlasDumpProcessor? GetAtlasDumpProcessorForSpriteDump(DumpInfo spriteDump)
            {
                foreach (AtlasDumpProcessor atlasDumpProcessor in _currAtlasDumpProcessors)
                {
                    if (atlasDumpProcessor.spriteDumpInfos.Contains(spriteDump))
                    {
                        return atlasDumpProcessor;
                    }
                }
                return null;
            }

            int GetIndexInAssets()
            {

                for (int i = 0; i < _currAssetInfos.Count; i++)
                {
                    long infoPathID = _currAssetInfos[i].PathId;
                    if (infoPathID == pathID)
                        return i;
                }
                return -1;
            }

            if (_currBunInst != bunInst || _lastReadType != AssetClassID.Sprite)
            {
                _currBunInst = bunInst;
                List<DumpInfo> atlasDumps = _dumpReader.ReadSpriteAtlasDumps(bunInst);
                _currSpriteDumps = _dumpReader.ReadSpriteDumps(bunInst);
                _currAtlasDumpProcessors = DistributeProcessors(atlasDumps, _currSpriteDumps);
                _currFileInst = AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
                _currAssetInfos = _currFileInst.file.GetAssetsOfType(AssetClassID.Sprite);
                _lastReadType = AssetClassID.Sprite;
            }

            int indexInAssets = GetIndexInAssets();
            if (indexInAssets == -1)
            {
                // Debug.LogWarning($"The given path id {pathID} is not found in sprites."); // * Silent Warning
                return null;
            }

            AssetFileInfo targetAsset = _currAssetInfos[indexInAssets];
            DumpInfo targetDump = _currSpriteDumps[indexInAssets];
            AtlasDumpProcessor? _atlasDumpInfoForSprite = GetAtlasDumpProcessorForSpriteDump(targetDump);
            if (_atlasDumpInfoForSprite != null) // Has Atlas
            {
                AtlasDumpProcessor atlasDumpInfoForSprite = (AtlasDumpProcessor)_atlasDumpInfoForSprite;
                Dictionary<int, int> index2RenderDataKey = atlasDumpInfoForSprite.GetIndex2ActualRenderDataKeyIndex();
                Dictionary<long, int> pathID2Index = atlasDumpInfoForSprite.GetPathID2Index();
                // foreach (var item in index2RenderDataKey)
                // {
                //     Debug.Log($"{item.Key}, {item.Value}");
                // }
                // Debug.Log($"{pathID}, {pathID2Index[pathID]}, {index2RenderDataKey[pathID2Index[pathID]]}");
                AssetTypeValueField spriteBase = AssetsManager.GetBaseField(_currFileInst, targetAsset);
                AssetTypeValueField atlasRefField = spriteBase["m_SpriteAtlas"];
                AssetExternal atlasAsset = GetExternalAsset(AssetsManager, _currFileInst, bunInst, atlasRefField);
                AssetTypeValueField atlasBase = AssetsManager.GetBaseField(atlasAsset.file, atlasAsset.info);
                AssetTypeValueField renderDataMap = atlasBase["m_RenderDataMap"];
                AssetTypeValueField dataArray = renderDataMap["Array"][index2RenderDataKey[pathID2Index[pathID]]]; // The true index in dict
                AssetTypeValueField firstEntry = dataArray["second"];
                AssetTypeValueField texturePtr = firstEntry["texture"];
                AssetExternal texAsset = GetExternalAsset(AssetsManager, _currFileInst, bunInst, texturePtr);
                AssetTypeValueField texBase = AssetsManager.GetBaseField(atlasAsset.file, texAsset.info);

                Rect spriteRect = atlasDumpInfoForSprite.GetRectAtActualIndex(index2RenderDataKey[pathID2Index[pathID]]);

                int textureWidth = texBase["m_Width"].AsInt;
                int textureHeight = texBase["m_Height"].AsInt;
                int textureFormat = texBase["m_TextureFormat"].AsInt;
                byte[] imageBytes = GetImageData(texBase, _currFileInst, bunInst);

                if (IsSupportedBCnFormat((TextureFormat)textureFormat, out CompressionFormat bcnFormat))
                {
                    var decoder = new BcDecoder();
                    ColorRgba32[] decoded = decoder.DecodeRaw(imageBytes, textureWidth, textureHeight, bcnFormat);

                    byte[] rgbaBytes = new byte[decoded.Length * 4];
                    MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);

                    Texture2D texture = new(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                    texture.LoadRawTextureData(rgbaBytes);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();

                    // Now you can assign tex to a material or use it however you need
                    // Debug.Log($"Decoded BC7 texture: {width}x{height}");
                    texture = CropTexture(texture, spriteRect);
                    return new()
                    {
                        texture2D = PadToSquare(texture),
                        rect = spriteRect,
                        compressionFormat = (TextureFormat)textureFormat
                    };
                }
                else if (imageBytes.Length == textureWidth * textureHeight * textureFormat)
                {
                    TextureFormat unityFormat = (TextureFormat) textureFormat;
                    Texture2D texture = new(textureWidth, textureHeight, unityFormat, false);
                    texture.LoadRawTextureData(imageBytes);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();
                    texture = CropTexture(texture, spriteRect);
                    return new()
                    {
                        texture2D = PadToSquare(texture),
                        rect = spriteRect,
                        compressionFormat = (TextureFormat)textureFormat
                    };
                }
                else
                {
                    Debug.LogError($"Expected {textureWidth * textureHeight * textureFormat} bytes, got {imageBytes.Length}");
                }
            }
            else // No Atlas
            {
                AssetTypeValueField spriteBase = AssetsManager.GetBaseField(_currFileInst, targetAsset);
                Rect spriteRect = new(
                    spriteBase["m_Rect"]["x"].AsFloat,
                    spriteBase["m_Rect"]["y"].AsFloat,
                    spriteBase["m_Rect"]["width"].AsFloat,
                    spriteBase["m_Rect"]["height"].AsFloat
                );

                // No SpriteAtlas file found in bundle but Sprite has m_SpriteAtlas field. Skip.
                if (spriteBase.Get("m_SpriteAtlas") != null)
                {
                    return null;
                }

                AssetTypeValueField texRefField = spriteBase["m_RD"]["texture"];
                AssetExternal texAsset = GetExternalAsset(AssetsManager, _currFileInst, bunInst, texRefField);
                AssetTypeValueField texBase = AssetsManager.GetBaseField(texAsset.file, texAsset.info);

                int textureWidth = texBase["m_Width"].AsInt;
                int textureHeight = texBase["m_Height"].AsInt;
                int textureFormat = texBase["m_TextureFormat"].AsInt;
                byte[] imageBytes = GetImageData(texBase, _currFileInst, bunInst);

                if (IsSupportedBCnFormat((TextureFormat)textureFormat, out CompressionFormat bcnFormat))
                {
                    var decoder = new BcDecoder();
                    ColorRgba32[] decoded = decoder.DecodeRaw(imageBytes, textureWidth, textureHeight, bcnFormat);

                    byte[] rgbaBytes = new byte[decoded.Length * 4];
                    MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);

                    Texture2D texture = new(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                    texture.LoadRawTextureData(rgbaBytes);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();

                    // Now you can assign tex to a material or use it however you need
                    // Debug.Log($"Decoded BC7 texture: {width}x{height}");
                    texture = CropTexture(texture, spriteRect);
                    return new()
                    {
                        texture2D = PadToSquare(texture),
                        rect = spriteRect,
                        compressionFormat = (TextureFormat)textureFormat
                    };
                }
                else if (imageBytes.Length == textureWidth * textureHeight * textureFormat)
                {
                    TextureFormat unityFormat = (TextureFormat)textureFormat;
                    Texture2D texture = new(textureWidth, textureHeight, unityFormat, false);
                    texture.LoadRawTextureData(imageBytes);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();
                    texture = CropTexture(texture, spriteRect);
                    return new()
                    {
                        texture2D = PadToSquare(texture),
                        rect = spriteRect,
                        compressionFormat = (TextureFormat)textureFormat
                    };
                }
                else
                {
                    Debug.LogError($"Expected {textureWidth * textureHeight * textureFormat} bytes, got {imageBytes.Length}");
                }
            }

            return null;
        }

        public Texture2DWithMeta? ReadTexture2DByPathID(BundleFileInstance bunInst, long pathID)
        {
            int GetIndexInAssets()
            {

                for (int i = 0; i < _currAssetInfos.Count; i++)
                {
                    long infoPathID = _currAssetInfos[i].PathId;
                    if (infoPathID == pathID)
                        return i;
                }
                return -1;
            }

            if (_currBunInst != bunInst || _lastReadType != AssetClassID.Texture2D)
            {
                _currBunInst = bunInst;
                _currFileInst = AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
                _currAssetInfos = _currFileInst.file.GetAssetsOfType(AssetClassID.Texture2D);
                _lastReadType = AssetClassID.Texture2D;
            }

            int indexInAssets = GetIndexInAssets();
            if (indexInAssets == -1)
            {
                Debug.LogWarning($"The given path id {pathID} is not found in textures.");
                return null;
            }

            AssetFileInfo targetAsset = _currAssetInfos[indexInAssets];
            AssetTypeValueField texBase = AssetsManager.GetBaseField(_currFileInst, targetAsset);
            int width = texBase["m_Width"].AsInt;
            int height = texBase["m_Height"].AsInt;
            int format = texBase["m_TextureFormat"].AsInt;
            byte[] imageBytes = GetImageData(texBase, _currFileInst, bunInst);

            if (IsSupportedBCnFormat((TextureFormat)format, out CompressionFormat bcnFormat))
            {
                var decoder = new BcDecoder();
                ColorRgba32[] decoded = decoder.DecodeRaw(imageBytes, width, height, bcnFormat);

                byte[] rgbaBytes = new byte[decoded.Length * 4];
                MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);

                Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(rgbaBytes);
                texture.filterMode = FilterMode.Point;
                texture.Apply();

                // Now you can assign tex to a material or use it however you need
                // Debug.Log($"Decoded BC7 texture: {width}x{height}");
                return new()
                {
                    texture2D = PadToSquare(texture),
                    rect = new(0, 0, width, height),
                    compressionFormat = (TextureFormat)format
                };
            }
            else if (imageBytes.Length == width * height * format)
            {
                TextureFormat unityFormat = (TextureFormat)format;
                Texture2D texture = new(width, height, unityFormat, false);
                texture.LoadRawTextureData(imageBytes);
                texture.filterMode = FilterMode.Point;
                texture.Apply();
                return new()
                {
                    texture2D = PadToSquare(texture),
                    rect = new(0, 0, width, height),
                    compressionFormat = (TextureFormat)format
                };
            }
            else
            {
                Debug.LogError($"Expected {width * height * format} bytes, got {imageBytes.Length}");
            }

            return null;
        }

        public List<Texture2D> ReadSpritesInAtlas(BundleFileInstance bunInst)
        {
            static AtlasDumpProcessor? GetAtlasDumpProcessorForSpriteDump(DumpInfo spriteDump,
                                                                    List<AtlasDumpProcessor> atlasDumpProcessors)
            {
                foreach (AtlasDumpProcessor atlasDumpProcessor in atlasDumpProcessors)
                {
                    if (atlasDumpProcessor.spriteDumpInfos.Contains(spriteDump))
                    {
                        return atlasDumpProcessor;
                    }
                }
                return null;
            }

            List<Texture2D> result = new();
            AssetsFileInstance fileInst = AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
            List<AssetFileInfo> spriteInfos = fileInst.file.GetAssetsOfType(AssetClassID.Sprite);

            List<DumpInfo> atlasDumps = _dumpReader.ReadSpriteAtlasDumps(bunInst);
            List<DumpInfo> spriteDumps = _dumpReader.ReadSpriteDumps(bunInst);
            List<AtlasDumpProcessor> atlasDumpProcessors = DistributeProcessors(atlasDumps, spriteDumps);

            // foreach (JObject obj in atlasDumps)
            // {
            //     Debug.Log(obj.ToString(Newtonsoft.Json.Formatting.Indented));
            // }

            if (spriteInfos.Count == 0)
                return result;

            for (int i = 0; i < spriteInfos.Count; i++)
            {
                DumpInfo spriteDump = spriteDumps[i];
                AtlasDumpProcessor? _atlasDumpInfoForSprite = GetAtlasDumpProcessorForSpriteDump(spriteDump, atlasDumpProcessors);
                if (_atlasDumpInfoForSprite != null) // Has Atlas
                {
                    AtlasDumpProcessor atlasDumpInfoForSprite = (AtlasDumpProcessor)_atlasDumpInfoForSprite;
                    Dictionary<int, int> index2RenderDataKey = atlasDumpInfoForSprite.GetIndex2ActualRenderDataKeyIndex();
                    AssetTypeValueField spriteBase = AssetsManager.GetBaseField(fileInst, spriteInfos[i]);
                    AssetTypeValueField atlasRefField = spriteBase["m_SpriteAtlas"];
                    AssetExternal atlasAsset = GetExternalAsset(AssetsManager, fileInst, bunInst, atlasRefField);
                    AssetTypeValueField atlasBase = AssetsManager.GetBaseField(atlasAsset.file, atlasAsset.info);
                    AssetTypeValueField renderDataMap = atlasBase["m_RenderDataMap"];
                    AssetTypeValueField dataArray = renderDataMap["Array"][index2RenderDataKey[i]]; // The true index in dict
                    AssetTypeValueField firstEntry = dataArray["second"];
                    AssetTypeValueField texturePtr = firstEntry["texture"];
                    AssetExternal texAsset = GetExternalAsset(AssetsManager, fileInst, bunInst, texturePtr);
                    AssetTypeValueField texBase = AssetsManager.GetBaseField(atlasAsset.file, texAsset.info);

                    Rect spriteRect = atlasDumpInfoForSprite.GetRectAtActualIndex(index2RenderDataKey[i]);

                    int textureWidth = texBase["m_Width"].AsInt;
                    int textureHeight = texBase["m_Height"].AsInt;
                    int textureFormat = texBase["m_TextureFormat"].AsInt;
                    byte[] imageBytes = GetImageData(texBase, fileInst, bunInst);

                    if (IsSupportedBCnFormat((TextureFormat)textureFormat, out CompressionFormat bcnFormat))
                    {
                        var decoder = new BcDecoder();
                        ColorRgba32[] decoded = decoder.DecodeRaw(imageBytes, textureWidth, textureHeight, CompressionFormat.Bc7);

                        byte[] rgbaBytes = new byte[decoded.Length * 4];
                        MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);

                        Texture2D texture = new(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                        texture.LoadRawTextureData(rgbaBytes);
                        texture.filterMode = FilterMode.Point;
                        texture.Apply();

                        // Now you can assign tex to a material or use it however you need
                        // Debug.Log($"Decoded BC7 texture: {width}x{height}");
                        texture = CropTexture(texture, spriteRect);
                        result.Add(PadToSquare(texture));
                    }
                    else if (imageBytes.Length == textureWidth * textureHeight * textureFormat)
                    {
                        TextureFormat unityFormat = TextureFormat.RGBA32;
                        Texture2D texture = new(textureWidth, textureHeight, unityFormat, false);
                        texture.LoadRawTextureData(imageBytes);
                        texture.filterMode = FilterMode.Point;
                        texture.Apply();
                        texture = CropTexture(texture, spriteRect);
                        result.Add(PadToSquare(texture));
                    }
                    else
                    {
                        Debug.LogError($"Expected {textureWidth * textureHeight * textureFormat} bytes, got {imageBytes.Length}");
                    }
                }
                else // No Atlas
                {
                    AssetTypeValueField spriteBase = AssetsManager.GetBaseField(fileInst, spriteInfos[i]);
                    Rect spriteRect = new(
                        spriteBase["m_Rect"]["x"].AsFloat,
                        spriteBase["m_Rect"]["y"].AsFloat,
                        spriteBase["m_Rect"]["width"].AsFloat,
                        spriteBase["m_Rect"]["height"].AsFloat
                    );
                    AssetTypeValueField texRefField = spriteBase["m_RD"]["texture"];
                    AssetExternal texAsset = GetExternalAsset(AssetsManager, fileInst, bunInst, texRefField);
                    AssetTypeValueField texBase = AssetsManager.GetBaseField(texAsset.file, texAsset.info);

                    int textureWidth = texBase["m_Width"].AsInt;
                    int textureHeight = texBase["m_Height"].AsInt;
                    int textureFormat = texBase["m_TextureFormat"].AsInt;
                    byte[] imageBytes = GetImageData(texBase, fileInst, bunInst);

                    if (IsSupportedBCnFormat((TextureFormat)textureFormat, out CompressionFormat bcnFormat))
                    {
                        var decoder = new BcDecoder();
                        ColorRgba32[] decoded = decoder.DecodeRaw(imageBytes, textureWidth, textureHeight, CompressionFormat.Bc7);

                        byte[] rgbaBytes = new byte[decoded.Length * 4];
                        MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);

                        Texture2D texture = new(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                        texture.LoadRawTextureData(rgbaBytes);
                        texture.filterMode = FilterMode.Point;
                        texture.Apply();

                        // Now you can assign tex to a material or use it however you need
                        // Debug.Log($"Decoded BC7 texture: {width}x{height}");
                        texture = CropTexture(texture, spriteRect);
                        result.Add(PadToSquare(texture));
                    }
                    else if (imageBytes.Length == textureWidth * textureHeight * textureFormat)
                    {
                        TextureFormat unityFormat = TextureFormat.RGBA32;
                        Texture2D texture = new(textureWidth, textureHeight, unityFormat, false);
                        texture.LoadRawTextureData(imageBytes);
                        texture.filterMode = FilterMode.Point;
                        texture.Apply();
                        texture = CropTexture(texture, spriteRect);
                        result.Add(PadToSquare(texture));
                    }
                    else
                    {
                        Debug.LogError($"Expected {textureWidth * textureHeight * textureFormat} bytes, got {imageBytes.Length}");
                    }
                }
            }

            return result;
        }

        public List<Texture2D> ReadTextures(BundleFileInstance bunInst)
        {
            List<Texture2D> result = new();
            AssetsFileInstance fileInst = AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
            List<AssetFileInfo> texInfos = fileInst.file.GetAssetsOfType(AssetClassID.Texture2D);

            foreach (AssetFileInfo texInfo in texInfos)
            {
                AssetTypeValueField texBase = AssetsManager.GetBaseField(fileInst, texInfo);
                int width = texBase["m_Width"].AsInt;
                int height = texBase["m_Height"].AsInt;
                int format = texBase["m_TextureFormat"].AsInt;
                byte[] imageBytes = GetImageData(texBase, fileInst, bunInst);

                if (IsSupportedBCnFormat((TextureFormat)format, out CompressionFormat bcnFormat))
                {
                    var decoder = new BcDecoder();
                    ColorRgba32[] decoded = decoder.DecodeRaw(imageBytes, width, height, CompressionFormat.Bc7);

                    byte[] rgbaBytes = new byte[decoded.Length * 4];
                    MemoryMarshal.Cast<ColorRgba32, byte>(decoded.AsSpan()).CopyTo(rgbaBytes);

                    Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
                    texture.LoadRawTextureData(rgbaBytes);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();

                    // Now you can assign tex to a material or use it however you need
                    // Debug.Log($"Decoded BC7 texture: {width}x{height}");
                    result.Add(PadToSquare(texture));
                }
                else if (imageBytes.Length == width * height * format)
                {
                    TextureFormat unityFormat = TextureFormat.RGBA32;
                    Texture2D texture = new(width, height, unityFormat, false);
                    texture.LoadRawTextureData(imageBytes);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();
                    result.Add(PadToSquare(texture));
                }
                else
                {
                    Debug.LogError($"Expected {width * height * format} bytes, got {imageBytes.Length}");
                }
            }

            return result;
        }

        private byte[] GetImageData(AssetTypeValueField texField, AssetsFileInstance fileInst, BundleFileInstance bunInst)
        {
            var imageDataField = texField["image data"];
            byte[] rawData = imageDataField?.Value?.AsByteArray ?? Array.Empty<byte>();

            var streamData = texField["m_StreamData"];
            uint offset = streamData["offset"].AsUInt;
            uint size = streamData["size"].AsUInt;
            string path = streamData["path"].AsString;

            if (!string.IsNullOrEmpty(path) && size > 0)
            {
                if (path.StartsWith("archive:/"))
                {
                    // Extract internal stream file from bundle
                    string internalFileName = Path.GetFileName(path); // e.g. "CAB-c8b157fca857626dbba75589e140a72a.resS"
                    string bundleFilePath = fileInst.path; // path to the .bundle file

                    byte[] internalFileData = ExtractFileManually(bunInst, internalFileName);

                    // Read the stream segment from the extracted bytes
                    byte[] buffer = new byte[size];
                    Array.Copy(internalFileData, offset, buffer, 0, size);
                    return buffer;
                }
                else
                {
                    // External file on disk (normal case)
                    string baseDir = Path.GetDirectoryName(fileInst.path);
                    string fullPath = Path.Combine(baseDir, path);

                    if (!File.Exists(fullPath))
                        throw new FileNotFoundException("Stream data file not found", fullPath);

                    byte[] buffer = new byte[size];
                    using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);

                        int totalRead = 0;
                        while (totalRead < size)
                        {
                            int read = fs.Read(buffer, totalRead, (int)(size - totalRead));
                            if (read == 0)
                                throw new IOException("Unexpected end of stream while reading external data.");
                            totalRead += read;
                        }
                    }

                    return buffer;
                }
            }

            // No external stream, return rawData
            return rawData;
        }

        private byte[] ExtractFileManually(BundleFileInstance bundle, string internalFileName)
        {
            var dirInfos = bundle.file.BlockAndDirInfo.DirectoryInfos;

            foreach (var dir in dirInfos)
            {
                if (dir.Name.Equals(internalFileName, StringComparison.OrdinalIgnoreCase))
                {
                    long offset = dir.Offset; // or use dir.OffsetInBundle if that’s the actual name
                    long size = dir.DecompressedSize; // or dir.Size or similar

                    // Make sure the stream is at the beginning of decompressed data
                    var stream = bundle.DataStream;
                    stream.Seek(offset, SeekOrigin.Begin);

                    byte[] buffer = new byte[size];
                    int totalRead = 0;

                    while (totalRead < size)
                    {
                        int read = stream.Read(buffer, totalRead, (int)(size - totalRead));
                        if (read == 0)
                            throw new IOException("Unexpected end of stream while reading internal bundle file.");
                        totalRead += read;
                    }

                    return buffer;
                }
            }

            throw new FileNotFoundException($"File '{internalFileName}' not found in bundle '{bundle.path}'");
        }

        private bool IsSupportedBCnFormat(TextureFormat unityFormat, out CompressionFormat format)
        {
            switch (unityFormat)
            {
                // Uncompressed 8-bit/channel formats
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    format = CompressionFormat.Rgba;
                    return true;

                case TextureFormat.RGB24:
                    format = CompressionFormat.Rgb;
                    return true;

                case TextureFormat.Alpha8:
                    format = CompressionFormat.Bc1WithAlpha;
                    return true;

                case TextureFormat.R8:
                    format = CompressionFormat.R;
                    return true;

                case TextureFormat.R16:
                    format = CompressionFormat.Rg;
                    return true;

                // BCn (DXT) compressed formats
                case TextureFormat.DXT1:
                case TextureFormat.BC4:
                    format = CompressionFormat.Bc1;
                    return true;

                case TextureFormat.DXT5:
                    format = CompressionFormat.Bc3;
                    return true;

                case TextureFormat.BC5:
                    format = CompressionFormat.Bc5;
                    return true;

                case TextureFormat.BC6H:
                    format = CompressionFormat.Bc6U;
                    return true;

                case TextureFormat.BC7:
                    format = CompressionFormat.Bc7;
                    return true;

                // Fallback/default
                default:
                    format = default;
                    return false;
            }
        }

        private Texture2D PadToSquare(Texture2D original)
        {
            int size = Mathf.Max(original.width, original.height); // square dimension
            Texture2D square = new(size, size, TextureFormat.RGBA32, false);

            // Fill background with transparent or black
            Color32[] fill = new Color32[size * size];
            square.SetPixels32(fill);

            // Center original image
            int xOffset = (size - original.width) / 2;
            int yOffset = (size - original.height) / 2;
            square.SetPixels(xOffset, yOffset, original.width, original.height, original.GetPixels());

            square.Apply();
            return square;
        }

        private AssetExternal GetExternalAsset(AssetsManager am, AssetsFileInstance currentFile, BundleFileInstance bundleFile, AssetTypeValueField pptr)
        {
            int fileId = pptr["m_FileID"].AsInt;
            long pathId = pptr["m_PathID"].AsLong;

            AssetsFileInstance targetFile = (fileId == 0)
                ? currentFile
                : am.LoadAssetsFileFromBundle(bundleFile, fileId, false);  // use fileId as index

            AssetFileInfo targetInfo = targetFile.file.GetAssetInfo(pathId);
            if (targetFile == null)
                throw new Exception("targetFile is null. Failed to resolve fileId.");

            if (targetInfo == null)
                throw new Exception($"Asset with pathId {pathId} not found in file.");

            var baseField = am.GetBaseField(targetFile, targetInfo);
            if (baseField == null)
                throw new Exception("GetBaseField returned null.");

            return new AssetExternal
            {
                file = targetFile,
                info = targetInfo,
                instance = am.GetBaseField(targetFile, targetInfo)
            };
        }

        private Texture2D CropTexture(Texture2D source, Rect rect)
        {
            int x = Mathf.FloorToInt(rect.x);
            int y = Mathf.FloorToInt(rect.y);
            int width = Mathf.FloorToInt(rect.width);
            int height = Mathf.FloorToInt(rect.height);

            // Clamp to source texture bounds
            x = Mathf.Clamp(x, 0, source.width - 1);
            y = Mathf.Clamp(y, 0, source.height - 1);
            width = Mathf.Clamp(width, 1, source.width - x);
            height = Mathf.Clamp(height, 1, source.height - y);

            // Get pixels from the specified rect
            Color[] pixels = source.GetPixels(x, y, width, height);

            // Create new texture and apply pixels
            Texture2D cropped = new Texture2D(width, height, source.format, false);
            cropped.SetPixels(pixels);
            cropped.Apply();

            return cropped;
        }

        public static string GetAssetTypeValueFieldString(AssetTypeValueField field, int indentLevel = 0)
        {
            if (field == null) return "<null>";

            StringBuilder sb = new StringBuilder();
            string indent = new string(' ', indentLevel * 2);

            // Field name and type
            sb.Append(indent);
            sb.Append(field.FieldName);
            sb.Append(" (");
            sb.Append(field.TypeName);
            sb.Append(")");

            // Field value (if any)
            if (field.Value != null)
            {
                sb.Append(" : ");
                sb.Append(field.Value);
            }
            sb.AppendLine();

            // Recursively append children
            if (field.Children != null)
            {
                foreach (var child in field.Children)
                {
                    sb.Append(GetAssetTypeValueFieldString(child, indentLevel + 1));
                }
            }

            return sb.ToString();
        }
    }
}