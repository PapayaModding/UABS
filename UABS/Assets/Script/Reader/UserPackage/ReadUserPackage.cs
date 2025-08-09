using System.Collections.Generic;
using System.IO;
using System.Linq;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.Reader.UserPackage
{
    public class ReadUserPackage
    {
        public List<string> GetPackagesInExternal()
        {
            if (!Directory.Exists(PredefinedPaths.ExternalUserPackages))
                Directory.CreateDirectory(PredefinedPaths.ExternalUserPackages);
            string[] allFolders = Directory.GetDirectories(PredefinedPaths.ExternalUserPackages, "*", SearchOption.TopDirectoryOnly);
            return allFolders.ToList();
        }
    }
}