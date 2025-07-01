using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UABS.Assets.Script.DataStruct;
using UnityEngine;
using static UABS.Assets.Script.Reader.DumpReader;

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
        public readonly JObject AtlasJson
        {
            get => atlasDumpInfo.dumpJson;
        }

        public readonly Dictionary<int, long> GetIndex2PathID()
        {
            Dictionary<int, long> result = new();
            var packedSpritesSource = AtlasJson["m_PackedSprites"]["Array"];
            for (int i = 0; i < packedSpritesSource.ToList().Count; i++)
            {
                var pathID = long.Parse(packedSpritesSource[i]["m_PathID"].ToString());
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
                JObject spriteDumpJson = spriteDumpInfo.dumpJson;
                long spritePathID = spriteDumpInfo.pathID;
                var rdk = spriteDumpJson["m_RenderDataKey"];
                uint firstData0 = uint.Parse(rdk["first"]["data[0]"].ToString());
                uint firstData1 = uint.Parse(rdk["first"]["data[1]"].ToString());
                uint firstData2 = uint.Parse(rdk["first"]["data[2]"].ToString());
                uint firstData3 = uint.Parse(rdk["first"]["data[3]"].ToString());
                long second = long.Parse(rdk["second"].ToString());
                RenderDataKey renderDataKey = new(firstData0, firstData1, firstData2, firstData3, second);
                result[pathID2Index[spritePathID]] = renderDataKey;
            }

            return result;
        }

        public int SearchIndexOfRenderDataKey(List<RenderDataKey> lst, RenderDataKey target)
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

        public List<RenderDataKey> GetRenderDataKeysFromJObject(JObject jObject)
        {
            var renderDataMap = jObject["m_RenderDataMap"]["Array"];
            List<RenderDataKey> renderDataKeys = new();
            for (int i = 0; i < renderDataMap.Count(); i++)
            {
                var rdk = renderDataMap[i];
                uint firstData0 = uint.Parse(rdk["first"]["first"]["data[0]"].ToString());
                uint firstData1 = uint.Parse(rdk["first"]["first"]["data[1]"].ToString());
                uint firstData2 = uint.Parse(rdk["first"]["first"]["data[2]"].ToString());
                uint firstData3 = uint.Parse(rdk["first"]["first"]["data[3]"].ToString());
                long second = long.Parse(rdk["first"]["second"].ToString());
                RenderDataKey renderDataKey = new(firstData0, firstData1, firstData2, firstData3, second);
                renderDataKeys.Add(renderDataKey);
            }
            return renderDataKeys;
        }

        public Dictionary<int, int> GetIndex2ActualRenderDataKeyIndex()
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

        public Rect GetRectAtActualIndex(int actualIndex)
        {
            var renderDataMap = AtlasJson["m_RenderDataMap"]["Array"];
            var texRect = renderDataMap[actualIndex]["second"]["textureRect"];
            return new(
                float.Parse(texRect["x"].ToString()),
                float.Parse(texRect["y"].ToString()),
                float.Parse(texRect["width"].ToString()),
                float.Parse(texRect["height"].ToString())
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
                    JObject spriteJson = spriteDumpInfo.dumpJson;
                    long atlasPathIDInSprite = long.Parse(spriteJson["m_SpriteAtlas"]["m_PathID"].ToString());
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