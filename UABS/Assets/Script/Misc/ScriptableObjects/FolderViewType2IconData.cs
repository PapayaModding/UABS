using System.Collections.Generic;
using UnityEngine;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Misc.ScriptableObjects
{
    [CreateAssetMenu(fileName = "FolderViewType2IconData", menuName = "Custom/FolderViewType2IconData", order = 1)]
    public class FolderViewType2IconData : ScriptableObject
    {
        [System.Serializable]
        public class FolderViewIconEntry
        {
            public FolderViewType folderViewType;
            public Sprite icon;
        }

        public List<FolderViewIconEntry> iconEntries = new();

        private Dictionary<FolderViewType, Sprite> lookup;

        public Sprite _unknownTypeSprite;

        public Sprite GetIcon(FolderViewType folderViewType)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<FolderViewType, Sprite>();
                foreach (var entry in iconEntries)
                {
                    if (!lookup.ContainsKey(entry.folderViewType))
                        lookup.Add(entry.folderViewType, entry.icon);
                    else
                        lookup.Add(entry.folderViewType, _unknownTypeSprite);
                }
            }

            lookup.TryGetValue(folderViewType, out Sprite result);
            if (result == null)
                return _unknownTypeSprite;
            return result;
        }
    }
}