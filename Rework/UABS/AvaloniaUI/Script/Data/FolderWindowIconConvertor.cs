using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UABS.Data;
using UABS.Util;

namespace UABS.AvaloniaUI
{
    public partial class FolderWindowIconConvertor : IValueConverter
    {
        private readonly Dictionary<string, Bitmap> cache = new();

        private Bitmap GetBitmap(string path, Stream stream)
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

        private static string FolderWindowType2Name(FolderWindowType FolderWindowType)
        {
            return FolderWindowType switch
            {
                FolderWindowType.Folder => "folder.png",
                _ => "asset-unknown.png",
            };
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FolderWindowType folderWindowType)
            {
                // Make sure AssemblyName matches AvaloniaUI.csproj
                const string assemblyName = "AvaloniaUI";

                // Combine the resource path inside the assembly
                string relativePath = $"{PredefinedPaths.Icons_Path}/{FolderWindowType2Name(folderWindowType)}";

                // Build the URI
                var uri = new Uri($"avares://{assemblyName}/{relativePath}");

                // Log.Info(uri.ToString());

                // Load the resource
                using var stream = AssetLoader.Open(uri);
                if (stream == null)
                {
                    Log.Error($"Resource not found: avares://{assemblyName}/{relativePath}");
                }
                else
                {
                    // Log.Info($"Resource found: avares://{assemblyName}/{relativePath}");
                }
                return GetBitmap(relativePath, stream!);
            }
            else
            {
                Log.Info("Value is not FolderWindowType, skip.");
            }

            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}