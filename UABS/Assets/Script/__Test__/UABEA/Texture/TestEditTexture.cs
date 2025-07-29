using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Reader.BundlesRead;
using UABS.Assets.Script.Wrapper.TextureDecoder;

using TextureFormat = AssetsTools.NET.Texture.TextureFormat;
using System.Windows.Forms;
using System.Globalization;
using System.Linq;

namespace UABS.Assets.Script.__Test__.UABEA
{
    public class TestEditTexture : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            TestHelper.TestOnCleanLabDesk(() =>
            {
                AppEnvironment appEnvironment = new();
                BundleReader bundleReader = new(appEnvironment.AssetsManager, appEnvironment.Dispatcher);
                string DATA_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "GameData");
                string bundlePath = Path.Combine(DATA_PATH, "spriteassetgroup_assets_assets/needdynamicloadresources/spritereference/unit_hero_gangdan.asset_266134690b1c6daffbecb67815ff8868.bundle");

                (BundleFileInstance bunInst, AssetsFileInstance assetInst) = bundleReader.ReadBundle(bundlePath);
                long texturePathID = -5051031092977008815;
                string ASSET_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "Asset");
                string texture4ReplacePath = Path.Combine(ASSET_PATH, "spriteassetgroup_assets_assets/needdynamicloadresources/spritereference/unit_hero_gangdan-CAB-7570f5ae7807c50c425af095d0113220--5051031092977008815.png");
                ReplaceTexture(appEnvironment, appEnvironment.AssetsManager, assetInst, bunInst, texturePathID, texture4ReplacePath, bundlePath);
                onComplete?.Invoke();
            });
        }

        private void ReplaceTexture(AppEnvironment appEnvironment,
                                    AssetsManager am,
                                    AssetsFileInstance assetInst,
                                    BundleFileInstance bunInst,
                                    long pathID,
                                    string textureImagePath,
                                    string bundlePath)
        {
            // EditTextureOption.cs - ExecutePlugin
            AssetFileInfo info = assetInst.file.GetAssetInfo(pathID);
            TextureHelper textureHelper = new(am);

            AssetTypeValueField texBase = textureHelper.GetByteArrayTexture(assetInst, info);
            // TextureFile texFile = TextureFile.ReadTextureFile(texBase);
            // bool hasMipMaps = texFile.m_MipMap;
            // bool readable = texFile.m_IsReadable;
            // int filterMode = texFile.m_TextureSettings.m_FilterMode;
            // string anisotFilter = texFile.m_TextureSettings.m_Aniso.ToString();
            // string mipmapBias = texFile.m_TextureSettings.m_MipBias.ToString();
            // int wrapmodeU = texFile.m_TextureSettings.m_WrapU;
            // int wrapmodeV = texFile.m_TextureSettings.m_WrapV;
            // string lightmapFormat = "0x" + texFile.m_LightmapFormat.ToString("X2");
            // int colorSpace = texFile.m_ColorSpace;

            // EditDialog.axaml.cs - BtnSave_Click
            uint platform = assetInst.file.Metadata.TargetPlatform;
            byte[] platformBlob = textureHelper.GetPlatformBlob(texBase);
            Image<Rgba32> imgToImport = Image.Load<Rgba32>(textureImagePath);
            int format = texBase["m_TextureFormat"].AsInt;

            int mips = 1;
            if (!texBase["m_MipCount"].IsDummy)
                mips = texBase["m_MipCount"].AsInt;
            else if (textureHelper.IsPo2(imgToImport.Width) && textureHelper.IsPo2(imgToImport.Height))
                mips = textureHelper.GetMaxMipCount(imgToImport.Width, imgToImport.Height);

            int width = imgToImport.Width, height = imgToImport.Height;
            byte[] encImageBytes = null;

            // Try with BCnEncoder
            ITextureDecoder textureDecoder = appEnvironment.Wrapper.TextureDecoder;
            byte[] rgbaBytes = new byte[width * height * 4];
            imgToImport.CopyPixelDataTo(rgbaBytes);
            byte[] imageBytes = rgbaBytes;
            if (IsSupportedFormat((TextureFormat)format, out TextureCompressionFormat compressFormat))
            {
                encImageBytes = textureDecoder.DecodeToBytes(imageBytes, width, height, compressFormat);
            }

            if (encImageBytes == null)
            {
                Debug.LogError($"Failed to encode texture format {(TextureFormat)format}!");
                return;
            }

            texBase["m_StreamData"]["offset"].AsInt = 0;
            // texBase["m_StreamData"]["size"].AsInt = 0;

            // if (!texBase["m_MipMap"].IsDummy)
            //     texBase["m_MipMap"].AsBool = hasMipMaps;

            // if (!texBase["m_MipCount"].IsDummy)
            //     texBase["m_MipCount"].AsInt = mips;

            // if (!texBase["m_ReadAllowed"].IsDummy)
            //     texBase["m_ReadAllowed"].AsBool = readable;

            // texBase["m_TextureSettings"]["m_FilterMode"].AsInt = filterMode;
            // texBase["m_TextureSettings"]["m_Aniso"].AsInt = int.Parse(anisotFilter);
            // texBase["m_TextureSettings"]["m_MipBias"].AsInt = int.Parse(mipmapBias);

            // if (!texBase["m_TextureSettings"]["m_WrapU"].IsDummy)
            //     texBase["m_TextureSettings"]["m_WrapU"].AsInt = wrapmodeU;

            // if (!texBase["m_TextureSettings"]["m_WrapV"].IsDummy)
            //     texBase["m_TextureSettings"]["m_WrapV"].AsInt = wrapmodeV;

            // if (lightmapFormat.StartsWith("0x"))
            // {
            //     if (int.TryParse(lightmapFormat, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int lightFmt))
            //         texBase["m_LightmapFormat"].AsInt = lightFmt;
            // }
            // else
            // {
            //     if (int.TryParse(lightmapFormat, out int lightFmt))
            //         texBase["m_LightmapFormat"].AsInt = lightFmt;
            // }

            // if (!texBase["m_ColorSpace"].IsDummy)
            //     texBase["m_ColorSpace"].AsInt = colorSpace;

            // texBase["m_TextureFormat"].AsInt = format;

            if (!texBase["m_CompleteImageSize"].IsDummy)
                texBase["m_CompleteImageSize"].AsInt = encImageBytes.Length;

            texBase["m_StreamData"]["size"].AsInt = encImageBytes.Length;

            texBase["m_Width"].AsInt = width;
            texBase["m_Height"].AsInt = height;

            /// 1. Write patched asset bytes to a byte array
            byte[] savedAsset = texBase.WriteToByteArray();
            AssetsReplacerFromMemory replacer = new(
                info.PathId, info.TypeId, assetInst.file.GetScriptIndex(info), savedAsset);

            List<AssetsReplacer> replacers = new() { replacer };

            // 2. Write patched assets file back to disk 
            string tempAssetPath = Path.Combine(Path.GetDirectoryName(bundlePath), "temp_assets.assets");
            using (FileStream fs = File.Create(tempAssetPath))
            using (AssetsFileWriter writer = new(fs))
            {
                assetInst.file.Write(writer, 0, replacers, am.ClassDatabase);
            }

            var streamPath = texBase["m_StreamData"]["path"].AsString;
            string searchPath = streamPath.StartsWith("archive:/") ? streamPath.Substring(9) : streamPath;
            searchPath = Path.GetFileName(searchPath);

            // Find the directory info inside the bundle file
            var dirInfo = bunInst.file.BlockAndDirInfo.DirectoryInfos.FirstOrDefault(d => d.Name == searchPath);
            if (dirInfo == null)
            {
                Debug.LogError("Could not find .resS entry in bundle directory.");
                return;
            }

            am.UnloadAllBundleFiles();

            // Write the raw .resS bytes at the correct position inside the bundle file
            using (var fs = new FileStream(bundlePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                long writePos = dirInfo.Offset + texBase["m_StreamData"]["offset"].AsLong;
                fs.Position = writePos;
                fs.Write(encImageBytes, 0, encImageBytes.Length);
                fs.Flush();
            }

            if (File.Exists(tempAssetPath))
            {
                File.Delete(tempAssetPath);
            }
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
                    using (FileStream fs = new(fullPath, FileMode.Open, FileAccess.Read))
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

        private bool IsSupportedFormat(TextureFormat unityFormat, out TextureCompressionFormat compressFormat)
        {
            switch (unityFormat)
            {
                // Uncompressed 8-bit/channel formats
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    compressFormat = TextureCompressionFormat.Rgba;
                    return true;

                case TextureFormat.RGB24:
                    compressFormat = TextureCompressionFormat.Rgb;
                    return true;

                case TextureFormat.R8:
                    compressFormat = TextureCompressionFormat.R;
                    return true;

                case TextureFormat.R16:
                    compressFormat = TextureCompressionFormat.Rg;
                    return true;

                // BCn (DXT) compressed formats
                case TextureFormat.DXT1:
                case TextureFormat.BC4:
                    compressFormat = TextureCompressionFormat.Bc1;
                    return true;

                case TextureFormat.DXT5:
                    compressFormat = TextureCompressionFormat.Bc3;
                    return true;

                case TextureFormat.BC5:
                    compressFormat = TextureCompressionFormat.Bc5;
                    return true;

                case TextureFormat.BC6H:
                    compressFormat = TextureCompressionFormat.Bc6U;
                    return true;

                case TextureFormat.BC7:
                    compressFormat = TextureCompressionFormat.Bc7;
                    return true;

                // Fallback/default
                default:
                    compressFormat = default;
                    return false;
            }
        }
    }
}