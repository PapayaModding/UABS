using System.IO;
using UnityEngine;

public static class ExternalImageLoader
{
    public static byte[]? LoadBytes(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;
        }

        return File.ReadAllBytes(filePath);
    }

    public static Texture2D? LoadPng(string filePath)
    {
        byte[]? bytes = LoadBytes(filePath);
        if (bytes is byte[] foundBytes)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(foundBytes);
            tex.Apply();
            return tex;
        }
        else
        {
            return null;
        }
    }
}