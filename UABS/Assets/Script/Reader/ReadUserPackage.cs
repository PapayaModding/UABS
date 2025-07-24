using System.Collections.Generic;
using System.IO;
using System.Linq;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.Reader
{
    public class ReadUserPackage
    {
        public List<string> GetPackagesInExternal()
        {
            string[] allFolders = Directory.GetDirectories(PredefinedPaths.ExternalUserPackages, "*", SearchOption.TopDirectoryOnly);
            return allFolders.ToList();
        }
    }
}