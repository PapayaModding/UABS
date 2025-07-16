using System.IO;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Writer;
using UnityEngine;

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
            string FROM_PATH = Path.Combine(PredefinedPaths.ExternalCache, "战魂铭人2.10.0.4");
            string TO_PATH = Path.Combine(PredefinedPaths.ExternalCache, "战魂铭人2.3.4");
            writer.InheritMemoCache(FROM_PATH, TO_PATH, safeCopy: false);
        }
    }
}