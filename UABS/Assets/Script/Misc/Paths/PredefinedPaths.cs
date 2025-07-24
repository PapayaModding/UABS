using System.IO;
using UnityEngine;

namespace UABS.Assets.Script.Misc.Paths
{
    public struct PredefinedPaths
    {
        public const string ExternalUserPackages = "External/UABS_UserPackages";
        public const string ExternalSystemDependencyCache = "External/UABS_System_Cache/Dependency";
        public const string ExternalSystemDependentCache = "External/UABS_System_Cache/Dependent";
        public const string ExternalSystemSearchCache = "External/UABS_System_Cache/Search";
        public const string ExportFolderName = "UABS_Exported";
        public static readonly string ClassDataPath = Path.Combine(Application.streamingAssetsPath, "classdata.tpk");
    }
}