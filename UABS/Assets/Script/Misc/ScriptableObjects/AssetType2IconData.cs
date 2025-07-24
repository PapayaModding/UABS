using System.Collections.Generic;
using UnityEngine;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Misc.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AssetType2IconData", menuName = "Custom/AssetType2IconData", order = 1)]
    public class AssetType2IconData : ScriptableObject
    {
        [System.Serializable]
        public class AssetIconEntry
        {
            public AssetClassID assetType;
            public Sprite icon;
        }

        public List<AssetIconEntry> iconEntries = new();

        private Dictionary<AssetClassID, Sprite> lookup;

        public Sprite _unknownTypeSprite;

        public Sprite GetIcon(AssetClassID assetType)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<AssetClassID, Sprite>();
                foreach (var entry in iconEntries)
                {
                    if (!lookup.ContainsKey(entry.assetType))
                        lookup.Add(entry.assetType, entry.icon);
                    else
                        lookup.Add(entry.assetType, _unknownTypeSprite);
                }
            }

            lookup.TryGetValue(assetType, out Sprite result);
            if (result == null)
                return _unknownTypeSprite;
            return result;
        }
    }
}