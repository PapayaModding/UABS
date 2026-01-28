using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UABS.Script.Core.Data.DataStruct;

namespace UABS.Script.Avalonia.Data
{
    public partial class FolderViewIconConvertor : IValueConverter
    {
        private readonly Dictionary<string, Bitmap> cache = [];

        private Bitmap GetBitmap(string path)
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

        private static string FolderViewType2Name(FolderViewType folderViewType)
        {
            return folderViewType switch
            {
                FolderViewType.Folder => "folder.png",
                _ => "asset-unknown.png",
            };
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FolderViewType folderViewType)
            {
                return GetBitmap(Path.Combine(PredefinedPaths.Icons_Path, FolderViewType2Name(folderViewType)));
            }

            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}