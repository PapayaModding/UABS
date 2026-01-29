using UABS.Data;
using UABS.Util;

namespace UABS.__Test__
{
    public class FileNavigationTester
    {
        public static void Test1()
        {
            // Build folders
            var A = new FsNode("A");
            var B = new FsNode("B", A);
            var C = new FsNode("C", B);

            // Build cached folders
            var Search1 = new FsNode("Search1");
            var Search2 = new FsNode("Search2");

            var navigator = new Navigator();

            // Push normal folders
            navigator.Push(new FolderLocation(A));
            navigator.Push(new FolderLocation(B));
            navigator.Push(new FolderLocation(C));

            // Push cached folders
            navigator.Push(new CachedLocation(Search1, C));
            navigator.Push(new CachedLocation(Search2, Search1));

            navigator.PrintStack();

            Log.Info("Test 1");
            Log.Info("Up from Search2 (leaf cached) → no effect");
            navigator.Up();
            navigator.PrintStack();

            Log.Info("Back → moves to Search1");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Up from Search1 → moves to Search2");
            navigator.Up();
            navigator.PrintStack();

            Log.Info("Back from Search2 → moves to Search1");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Back from Search1 → moves to C");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Back from C → moves to B");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Back from B → moves to A");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Back from A (root) → no effect");
            navigator.Back();
            navigator.PrintStack();
        }
    }
}