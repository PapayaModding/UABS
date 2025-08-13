using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using AssetsTools.NET.Extra;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UABS;

public partial class AssetTypeIconConvertor : IValueConverter
{
    private readonly Dictionary<string, Bitmap> cache = [];
    
    [GeneratedRegex("([A-Z])")]
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

    private static string ClassId2IconName(AssetClassID classID)
    {
        string input = classID.ToString();
        string result = MyRegex().Replace(input, "-$1").TrimStart('-').ToLower();
        return "asset-" + result + ".png";
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AssetClassID classID)
        {
            if ((int)classID < 0)
                return GetBitMap(Path.Combine(PredefinedPaths.Icons_Path, ClassId2IconName(AssetClassID.MonoBehaviour)));

            return GetBitMap(Path.Combine(PredefinedPaths.Icons_Path, ClassId2IconName(classID)));
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
