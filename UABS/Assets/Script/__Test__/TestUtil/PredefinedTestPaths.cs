using System.IO;

namespace UABS.Assets.Script.__Test__.TestUtil
{
    public struct PredefinedTestPaths
    {
        public const string TestResPath = "External_Test/UABS_TestResources";
        public static readonly string DoNotOverridePath = Path.Combine(TestResPath, "__DoNotOverwrite__");
        public static readonly string LabDeskPath = Path.Combine(TestResPath, "__LabDesk__");
        public static readonly string DNO_AssetsFolder = Path.Combine(DoNotOverridePath, "Assets");
    }
}