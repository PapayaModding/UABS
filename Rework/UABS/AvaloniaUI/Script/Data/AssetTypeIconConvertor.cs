using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UABS.Service;
using UABS.Util;

namespace UABS.AvaloniaUI
{
    public partial class AssetTypeIconConvertor : IValueConverter
    {
        private readonly Dictionary<string, Bitmap> cache = new();
        
        [GeneratedRegex("(?<!^)(?=[A-Z][a-z])")]
        private static partial Regex MyRegex();

        private Bitmap GetBitMap(string path, Stream stream)
        {
            if (cache.TryGetValue(path, out Bitmap? bitmap))
            {
                return bitmap;
            }
            else
            {
                bitmap = new Bitmap(stream);
                cache[path] = bitmap;
                return bitmap;
            }
        }

        private static string ClassId2IconName(AssetClassIDService classID)
        {
            string input = classID.ToString();
            string result = MyRegex().Replace(input, "-").ToLower(); 
            return "asset-" + result + ".png";
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AssetClassIDService classID)
            {
                string path = LoadIconPath(ClassId2IconName(classID));

                const string assemblyName = "AvaloniaUI";
                string relativePath = path;
                var uri = new Uri($"avares://{assemblyName}/{relativePath}");

                using var stream = AssetLoader.Open(uri);
                if (stream != null)
                {
                    Log.Error($"Resource not found: avares://{assemblyName}/{relativePath}");
                }
                // Log.Info($"avares://{assemblyName}/{relativePath}");
                if (classID.ToInt() < 0)
                    return GetBitMap(Path.Combine(PredefinedPaths.Icons_Path, "asset-mono-behaviour.png"), stream!);

                return GetBitMap(relativePath, stream!);
            }

            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public string LoadIconPath(string iconName)
        {
            string iconPath = Path.Combine(PredefinedPaths.Icons_Path, iconName);
            string absIconPath = PathHelper.ToLongPath(iconPath);
            if (!File.Exists(absIconPath))
            {
                iconPath = Path.Combine(PredefinedPaths.Icons_Path, "asset-unknown.png");
                absIconPath = PathHelper.ToLongPath(iconPath);
                if (!File.Exists(absIconPath))
                {
                    Log.Error("Default icon missing.", file: "AssetTypeIconConvertor.cs");
                    throw new InvalidOperationException("Default icon missing");
                }
                return iconPath;
            }

            return iconPath;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}