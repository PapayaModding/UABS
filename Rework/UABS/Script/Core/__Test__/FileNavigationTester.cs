using System.Collections.Generic;
using UABS.Data;

namespace UABS.__Test__
{
    public class FileNavigationTester
    {
        public static void Test()
        {
            // Build static folder tree A/B/C
            var A = new FsNode("A");
            var B = new FsNode("B", A);
            var C = new FsNode("C", B);
            var file1 = new FsNode("file1.txt", C) { IsFolder = false };

            // Initialize Navigator
            var navigator = new Navigator();

            // Navigate through folders
            navigator.OpenFolder(A);
            navigator.OpenFolder(B);
            navigator.OpenFolder(C);

            // Create cached folder
            var cached1 = new CachedFolder("Search1", new List<FsNode> { C });
            navigator.OpenCachedFolder(cached1);

            var cached2 = new CachedFolder("Search2", new List<FsNode> { B });
            navigator.OpenCachedFolder(cached2);

            navigator.PrintStack();

            // Up from cached2 → should go to last real folder (C)
            navigator.Up();
            navigator.PrintStack();

            // Back → should go to cached2
            navigator.Back();
            navigator.PrintStack();

            // Up from C → parent B
            navigator.Up();
            navigator.PrintStack();

            // Open a file
            navigator.OpenFile(file1);
        }
    }
}