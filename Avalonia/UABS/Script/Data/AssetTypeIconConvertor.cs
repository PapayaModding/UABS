using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UABS;

public partial class AssetTypeIconConvertor : IValueConverter
{
    private readonly Dictionary<string, Bitmap> cache = [];
    
    [GeneratedRegex("(?<!^)(?=[A-Z][a-z])")]
    private static partial Regex MyRegex();

    private Bitmap GetBitMap(string path)
    {
        if (cache.TryGetValue(path, out Bitmap? bitmap))
        {
            return bitmap;
        }
        else
        {
            bitmap = new Bitmap(AssetLoader.Open(new Uri($"avares://{path}")));
            cache[path] = bitmap;
            return bitmap;
        }
    }

    private static string TypeNameToIconName(string typeName)
    {
        string result = MyRegex().Replace(typeName, "-").ToLowerInvariant();
        return "asset-" + result + ".png";
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string typeName && !string.IsNullOrWhiteSpace(typeName))
        {
            string iconPath = Path.Combine(PredefinedPaths.Icons_Path, TypeNameToIconName(typeName));
            try
            {
                return GetBitMap(iconPath);
            }
            catch (FileNotFoundException)
            {
                return GetBitMap(Path.Combine(PredefinedPaths.Icons_Path, "asset-unknown.png"));
            }
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
