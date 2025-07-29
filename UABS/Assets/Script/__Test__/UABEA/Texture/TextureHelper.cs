using System;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;

namespace UABS.Assets.Script.__Test__.UABEA
{
    public class TextureHelper
    {
        private AssetsManager _assetsManager;
        public TextureHelper(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public AssetTypeValueField GetByteArrayTexture(AssetsFileInstance assetInst,
                                                        AssetFileInfo info)
        {
            AssetTypeTemplateField textureTemp = GetTemplateField(assetInst, info);
            AssetTypeTemplateField image_data = textureTemp.Children.FirstOrDefault(f => f.Name == "image data");
            if (image_data == null)
                return null;
            image_data.ValueType = AssetValueType.ByteArray;
            AssetTypeTemplateField m_PlatformBlob = textureTemp.Children.FirstOrDefault(f => f.Name == "m_PlatformBlob");
            if (m_PlatformBlob != null)
            {
                AssetTypeTemplateField m_PlatformBlob_Array = m_PlatformBlob.Children[0];
                m_PlatformBlob_Array.ValueType = AssetValueType.ByteArray;
            }

            AssetTypeValueField baseField = textureTemp.MakeValue(assetInst.file.Reader, info.AbsoluteByteStart);
            return baseField;
        }

        public bool GetResSTexture(TextureFile texFile, AssetsFileInstance fileInst)
        {
            TextureFile.StreamingInfo streamInfo = texFile.m_StreamData;
            if (streamInfo.path != null && streamInfo.path != "" && fileInst.parentBundle != null)
            {
                //some versions apparently don't use archive:/
                string searchPath = streamInfo.path;
                if (searchPath.StartsWith("archive:/"))
                    searchPath = searchPath.Substring(9);

                searchPath = Path.GetFileName(searchPath);

                AssetBundleFile bundle = fileInst.parentBundle.file;

                AssetsFileReader reader = bundle.DataReader;
                AssetBundleDirectoryInfo[] dirInf = bundle.BlockAndDirInfo.DirectoryInfos;
                for (int i = 0; i < dirInf.Length; i++)
                {
                    AssetBundleDirectoryInfo info = dirInf[i];
                    if (info.Name == searchPath)
                    {
                        reader.Position = info.Offset + (long)streamInfo.offset;
                        texFile.pictureData = reader.ReadBytes((int)streamInfo.size);
                        texFile.m_StreamData.offset = 0;
                        texFile.m_StreamData.size = 0;
                        texFile.m_StreamData.path = "";
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public byte[] GetRawTextureBytes(TextureFile texFile, AssetsFileInstance inst)
        {
            string rootPath = Path.GetDirectoryName(inst.path);
            if (texFile.m_StreamData.size != 0 && texFile.m_StreamData.path != string.Empty)
            {
                string fixedStreamPath = texFile.m_StreamData.path;
                if (inst.parentBundle == null && fixedStreamPath.StartsWith("archive:/"))
                {
                    fixedStreamPath = Path.GetFileName(fixedStreamPath);
                }
                if (!Path.IsPathRooted(fixedStreamPath) && rootPath != null)
                {
                    fixedStreamPath = Path.Combine(rootPath, fixedStreamPath);
                }
                if (File.Exists(fixedStreamPath))
                {
                    Stream stream = File.OpenRead(fixedStreamPath);
                    stream.Position = (long)texFile.m_StreamData.offset;
                    texFile.pictureData = new byte[texFile.m_StreamData.size];
                    stream.Read(texFile.pictureData, 0, (int)texFile.m_StreamData.size);
                }
                else
                {
                    return null;
                }
            }
            return texFile.pictureData;
        }

        public byte[] GetPlatformBlob(AssetTypeValueField texBaseField)
        {
            AssetTypeValueField m_PlatformBlob = texBaseField["m_PlatformBlob"];
            byte[] platformBlob = null;
            if (!m_PlatformBlob.IsDummy)
            {
                platformBlob = m_PlatformBlob["Array"].AsByteArray;
            }
            return platformBlob;
        }

        public bool IsPo2(int n)
        {
            return n > 0 && ((n & (n - 1)) == 0);
        }

        public int GetMaxMipCount(int width, int height)
        {
            int IntLog2(int value)
            {
                int result = 0;
                while ((value >>= 1) != 0)
                    result++;
                return result;
            }

            int widthMipCount = IntLog2(width) + 1;
            int heightMipCount = IntLog2(height) + 1;
            // if the texture is 512x1024 for example, select the height (1024)
            // I guess the width would stay 1 while the height resizes down
            return Math.Max(widthMipCount, heightMipCount);
        }

        private AssetTypeTemplateField GetTemplateField(AssetsFileInstance assetInst,
                                                        AssetFileInfo info,
                                                        bool forceCldb = false,
                                                        bool skipMonoBehaviourFields = false)
        {
            AssetReadFlags readFlags = AssetReadFlags.None;
            if (forceCldb)
                readFlags |= AssetReadFlags.ForceFromCldb;
            if (skipMonoBehaviourFields)
                readFlags |= AssetReadFlags.SkipMonoBehaviourFields;

            return _assetsManager.GetTemplateBaseField(assetInst,
                                                        assetInst.file.Reader,
                                                        info.AbsoluteByteStart,
                                                        info.TypeId,
                                                        assetInst.file.GetScriptIndex(info),
                                                        readFlags);
        }
    }
}
