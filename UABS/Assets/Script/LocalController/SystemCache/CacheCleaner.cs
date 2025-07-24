using UnityEngine;
using UABS.Assets.Script.Writer.SystemCache;

namespace UABS.Assets.Script.LocalController.SystemCache
{
    public class CacheCleaner : MonoBehaviour
    {
        private readonly ClearSystemCache _clearSystemCache = new();

        public void Clear()
        {
            _clearSystemCache.ClearSearchAndDependencyCache();
        }
    }
}