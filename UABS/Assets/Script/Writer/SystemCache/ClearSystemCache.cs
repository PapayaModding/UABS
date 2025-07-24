using System.IO;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.Writer.SystemCache
{
    public class ClearSystemCache
    {
        public void ClearSearchAndDependencyCache()
        {
            string searchCachePath = PredefinedPaths.ExternalSystemSearchCache;
            string dependencyCachePath = PredefinedPaths.ExternalSystemDependencyCache;

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