namespace UABS.UnityAssets.Packages;

public static class PackagePathComparer
{
    public static bool Equals(string? left, string? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        return string.Equals(GetIdentity(left), GetIdentity(right), StringComparison.OrdinalIgnoreCase);
    }

    public static string GetIdentity(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return path ?? string.Empty;

        string normalized = NormalizeExtendedWindowsPrefix(path);
        normalized = normalized.Replace('/', '\\');

        try
        {
            if (Path.IsPathFullyQualified(normalized))
                normalized = Path.GetFullPath(normalized);
        }
        catch (Exception exception) when (exception is ArgumentException
                                           or NotSupportedException
                                           or PathTooLongException)
        {
            // Invalid paths still need stable, non-throwing comparison without filesystem access.
        }

        return TrimTrailingSeparators(normalized);
    }

    private static string NormalizeExtendedWindowsPrefix(string path)
    {
        const string extendedUnc = @"\\?\UNC\";
        if (path.StartsWith(extendedUnc, StringComparison.OrdinalIgnoreCase))
            return @"\\" + path[extendedUnc.Length..];

        const string extended = @"\\?\";
        if (path.Length >= extended.Length + 3
            && path.StartsWith(extended, StringComparison.Ordinal)
            && char.IsAsciiLetter(path[extended.Length])
            && path[extended.Length + 1] == ':'
            && IsDirectorySeparator(path[extended.Length + 2]))
        {
            return path[extended.Length..];
        }

        return path;
    }

    private static string TrimTrailingSeparators(string path)
    {
        string root;
        try
        {
            root = Path.GetPathRoot(path) ?? string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            root = string.Empty;
        }

        int minimumLength = root.Length;
        int length = path.Length;
        while (length > minimumLength && IsDirectorySeparator(path[length - 1]))
            length--;
        return length == path.Length ? path : path[..length];
    }

    private static bool IsDirectorySeparator(char value) => value is '\\' or '/';
}
