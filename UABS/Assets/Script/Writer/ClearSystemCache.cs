using System.IO;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.Writer
{
    public class ClearSystemCache
    {
        public void ClearSearchAndDependencyCache()
        {
            string searchCachePath = PredefinedPaths.ExternalSystemSearchCache;
            string dependencyCachePath = PredefinedPaths.ExternalSystemDependenceCache;

            RemovePath(searchCachePath);
            RemovePath(dependencyCachePath);
        }

        private void RemovePath(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}