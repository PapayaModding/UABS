using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AssetsTools.NET.Extra;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UABS.Util;

namespace UABS.AvaloniaUI
{
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

        private static string ClassId2IconName(AssetClassID classID)
        {
            string input = classID.ToString();
            string result = MyRegex().Replace(input, "-").ToLower(); 
            return "asset-" + result + ".png";
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AssetClassID classID)
            {
                if ((int)classID < 0)
                    return GetBitMap(Path.Combine(PredefinedPaths.Icons_Path, ClassId2IconName(AssetClassID.MonoBehaviour)));

                return GetBitMap(LoadIconPath(ClassId2IconName(classID)));
            }

            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public string LoadIconPath(string iconName)
        {
            var assembly = typeof(IconsLoader).Assembly;
            string resourceName = $"UABS.Resources.Icons.{iconName}";
            Log.Info(resourceName);

            Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                // fallback
                resourceName = "UABS.Resources.Icons.asset-unknown.png";
                stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return Path.Combine(PredefinedPaths.Icons_Path, "asset-unknown.png");
                }
                else
                {
                    Log.Error("Default icon missing.", file: "AssetTypeIconConvertor.cs");
                    throw new InvalidOperationException("Default icon missing");
                }
            }

            return Path.Combine(PredefinedPaths.Icons_Path, iconName);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}