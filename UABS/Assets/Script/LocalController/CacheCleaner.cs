using UnityEngine;
using UABS.Assets.Script.Writer.SystemCache;

namespace UABS.Assets.Script.Misc
{
    public class CacheCleaner : MonoBehaviour
    {
        private ClearSystemCache _clearSystemCache = new();

        public void Clear()
        {
            _clearSystemCache.ClearSearchAndDependencyCache();
        }
    }
}