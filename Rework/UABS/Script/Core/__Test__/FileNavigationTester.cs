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
            var B = new FsNode("B");
            var C = new FsNode("C");

            var navigator = new Navigator();

            // Push normal folders
            navigator.Push(new FolderLocation(A));
            navigator.Push(new FolderLocation(B));
            navigator.Push(new FolderLocation(C));

            Log.Info("Test 1");
            navigator.PrintStack();

            Log.Info("C → B");
            navigator.Back();
            navigator.PrintStack();
        }

        public static void Test2()
        {
            // Build folders
            var A = new FsNode("A");
            var B = new FsNode("B");
            var C = new FsNode("C");
            var Search1 = new FsNode("Search1");
            var Search2 = new FsNode("Search2");

            var navigator = new Navigator();

            // Push normal folders
            navigator.Push(new FolderLocation(A));
            navigator.Push(new FolderLocation(B));
            navigator.Push(new FolderLocation(C));

            navigator.Push(new CachedLocation(Search1));
            navigator.Push(new CachedLocation(Search2));

            Log.Info("Test 2");
            navigator.PrintStack();

            Log.Info("Search2 → Search1");
            navigator.Back();
            navigator.PrintStack();
        }

        public static void Test3()
        {
            // Build folders
            var A = new FsNode("A");
            var B = new FsNode("B");
            var C = new FsNode("C");
            var Search1 = new FsNode("Search1");
            var Search2 = new FsNode("Search2");

            var navigator = new Navigator();

            // Push normal folders
            navigator.Push(new FolderLocation(A));

            navigator.Push(new CachedLocation(Search1));
            navigator.Push(new CachedLocation(Search2));

            Log.Info("Test 2");
            navigator.PrintStack();

            Log.Info("Search2 → Search1");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Added B, C under Search1 (Search2 is gone)");
            navigator.Push(new FolderLocation(B));
            navigator.Push(new FolderLocation(C));
            navigator.PrintStack();

            Log.Info("C -> B");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("B -> Search1");
            navigator.Back();
            navigator.PrintStack();

            Log.Info("Search1 -> A");
            navigator.Back();
            navigator.PrintStack();
        }
    }
}