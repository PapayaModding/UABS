using System.Collections.Generic;
using System.Linq;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Wrapper.Json;
using UnityEngine;

namespace UABS.Assets.Script.Reader
{
    public struct AtlasDumpProcessor
    {
        public readonly struct RenderDataKey
        {
            readonly uint firstData0;
            readonly uint firstData1;
            readonly uint firstData2;
            readonly uint firstData3;
            readonly long second;
            public RenderDataKey(uint firstData0, uint firstData1, uint firstData2, uint firstData3, long second)
            {
                this.firstData0 = firstData0;
                this.firstData1 = firstData1;
                this.firstData2 = firstData2;
                this.firstData3 = firstData3;
                this.second = second;
            }

            public bool Compare(RenderDataKey other)
            {
                return firstData0 == other.firstData0 &&
                    firstData1 == other.firstData1 &&
                    firstData2 == other.firstData2 &&
                    firstData3 == other.firstData3 &&
                    second == other.second;
            }

            public override string ToString()
            {
                return $"\"first\"[{firstData0}, {firstData1}, {firstData2}, {firstData3}], \"second\": {second}";
            }
        }

        public DumpInfo atlasDumpInfo;
        public List<DumpInfo> spriteDumpInfos;
        public readonly IJsonObject AtlasJson
        {
            get => atlasDumpInfo.dumpJson;
        }

        public readonly Dictionary<int, long> GetIndex2PathID()
        {
            Dictionary<int, long> result = new();
            IJsonObject packedSprites = AtlasJson.GetObject("m_PackedSprites");
            List<IJsonObject> array = packedSprites.GetArray("Array");
            
            for (int i = 0; i < array.ToList().Count; i++)
            {
                // var pathID = long.Parse(array[i].GetObject("m_PathID").ToString());
                var pathID = array[i].GetLong("m_PathID");
                result[i] = pathID;
            }
            return result;
        }

        public readonly Dictionary<long, int> GetPathID2Index()
        {
            Dictionary<int, long> index2PathID = GetIndex2PathID();
            return index2PathID.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        public readonly Dictionary<int, RenderDataKey> GetIndex2RenderDataKey()
        {
            Dictionary<long, int> pathID2Index = GetPathID2Index();
            Dictionary<int, RenderDataKey> result = new();
            foreach (DumpInfo spriteDumpInfo in spriteDumpInfos)
            {
                IJsonObject spriteDumpJson = spriteDumpInfo.dumpJson;
                long spritePathID = spriteDumpInfo.pathID;
                IJsonObject rdk = spriteDumpJson.GetObject("m_RenderDataKey");
                IJsonObject first = rdk.GetObject("first");
                uint firstData0 = first.GetUInt("data[0]");
                uint firstData1 = first.GetUInt("data[1]");
                uint firstData2 = first.GetUInt("data[2]");
                uint firstData3 = first.GetUInt("data[3]");

                long second = rdk.GetLong("second");
                RenderDataKey renderDataKey = new(firstData0, firstData1, firstData2, firstData3, second);
                result[pathID2Index[spritePathID]] = renderDataKey;
            }

            return result;
        }

        public readonly int SearchIndexOfRenderDataKey(List<RenderDataKey> lst, RenderDataKey target)
        {
            int counter = 0;
            foreach (RenderDataKey rdk in lst)
            {
                if (rdk.Compare(target))
                {
                    return counter;
                }
                counter++;
            }
            return -1;
        }

        public readonly List<RenderDataKey> GetRenderDataKeysFromJObject(IJsonObject jObject)
        {
            IJsonObject renderDataMap = AtlasJson.GetObject("m_RenderDataMap");
            List<IJsonObject> array = renderDataMap.GetArray("Array");

            // var renderDataMap = jObject["m_RenderDataMap"]["Array"];
            
            List<RenderDataKey> renderDataKeys = new();
            for (int i = 0; i < array.Count(); i++)
            {
                var rdk = array[i];
                IJsonObject outerFirst = rdk.GetObject("first");
                IJsonObject innerFirst = outerFirst.GetObject("first");
                uint firstData0 = innerFirst.GetUInt("data[0]");
                uint firstData1 = innerFirst.GetUInt("data[1]");
                uint firstData2 = innerFirst.GetUInt("data[2]");
                uint firstData3 = innerFirst.GetUInt("data[3]");

                long second = outerFirst.GetLong("second");

                // var rdk = renderDataMap[i];
                // uint firstData0 = uint.Parse(rdk["first"]["first"]["data[0]"].ToString());
                // uint firstData1 = uint.Parse(rdk["first"]["first"]["data[1]"].ToString());
                // uint firstData2 = uint.Parse(rdk["first"]["first"]["data[2]"].ToString());
                // uint firstData3 = uint.Parse(rdk["first"]["first"]["data[3]"].ToString());
                // long second = long.Parse(rdk["first"]["second"].ToString());
                RenderDataKey renderDataKey = new(firstData0, firstData1, firstData2, firstData3, second);
                renderDataKeys.Add(renderDataKey);
            }
            return renderDataKeys;
        }

        public readonly Dictionary<int, int> GetIndex2ActualRenderDataKeyIndex()
        {
            Dictionary<int, int> result = new();
            Dictionary<int, RenderDataKey> index2RenderDataKey = GetIndex2RenderDataKey();
            List<RenderDataKey> renderDataKeys = GetRenderDataKeysFromJObject(AtlasJson);

            foreach (int index in index2RenderDataKey.Keys)
            {
                if (index2RenderDataKey.TryGetValue(index, out RenderDataKey rdk))
                {
                    result[index] = SearchIndexOfRenderDataKey(renderDataKeys, rdk);
                }
            }

            return result;
        }

        public readonly Rect GetRectAtActualIndex(int actualIndex)
        {
            IJsonObject renderDataMap = AtlasJson.GetObject("m_RenderDataMap");
            List<IJsonObject> array = renderDataMap.GetArray("Array");
            IJsonObject actual = array[actualIndex];
            IJsonObject second = actual.GetObject("second");
            IJsonObject textureRect = second.GetObject("textureRect");

            // var texRect = array[actualIndex]["second"]["textureRect"];
            return new(
                textureRect.GetFloat("x"),
                textureRect.GetFloat("y"),
                textureRect.GetFloat("width"),
                textureRect.GetFloat("height")
                // float.Parse(texRect["x"].ToString()),
                // float.Parse(texRect["y"].ToString()),
                // float.Parse(texRect["width"].ToString()),
                // float.Parse(texRect["height"].ToString())
            );
        }

        public static List<AtlasDumpProcessor> DistributeProcessors(List<DumpInfo> atlasDumpInfos,
                                                                    List<DumpInfo> spriteDumpInfos)
        {
            List<AtlasDumpProcessor> result = new();
            foreach (DumpInfo atlasDumpInfo in atlasDumpInfos)
            {
                long atlasPathID = atlasDumpInfo.pathID;
                List<DumpInfo> spriteDumpInfosInAtlas = new();
                foreach (DumpInfo spriteDumpInfo in spriteDumpInfos)
                {
                    IJsonObject spriteJson = spriteDumpInfo.dumpJson;
                    IJsonObject spriteAtlas = spriteJson.GetObject("m_SpriteAtlas");
                    long pathID = spriteAtlas.GetLong("m_PathID");
                    long atlasPathIDInSprite = pathID;
                    // long atlasPathIDInSprite = long.Parse(spriteJson["m_SpriteAtlas"]["m_PathID"].ToString());
                    if (atlasPathIDInSprite == atlasPathID)
                    {
                        spriteDumpInfosInAtlas.Add(spriteDumpInfo);
                    }
                }
                result.Add(new()
                {
                    atlasDumpInfo = atlasDumpInfo,
                    spriteDumpInfos = spriteDumpInfosInAtlas
                });
            }

            return result;
        }
    }
}