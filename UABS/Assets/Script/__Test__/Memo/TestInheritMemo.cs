using System.IO;
using UnityEngine;
using UABS.Assets.Script.Writer.UserPackage;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.__Test__.Memo
{
    public class TestInheritMemo : MonoBehaviour
    {
        private void Start()
        {
            Test();
        }

        private void Test()
        {
            AppEnvironment appEnvironment = new();
            InheritMemoWriter writer = new(appEnvironment);
            string FROM_PATH = Path.Combine(PredefinedPaths.ExternalUserPackages, "战魂铭人2.10.0.4");
            string TO_PATH = Path.Combine(PredefinedPaths.ExternalUserPackages, "战魂铭人2.3.4");
            writer.InheritMemoPackage(FROM_PATH, TO_PATH, DataStruct.MemoInheritMode.Safe);
        }
    }
}