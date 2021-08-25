using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class ImageReader
{

    /// <summary>
    /// Reads the file.
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>A Texture2D.</returns>
    public static Texture2D ReadFile(string path)
    {
        if (File.Exists(path))
        {
            byte[] data = File.ReadAllBytes(path);
            Texture2D image = new Texture2D(1024, 1024, TextureFormat.DXT1, false);
            image.LoadImage(data);
            return image;
        }
        else
        {
            Debug.Log("File does not exist.");
            return null;
        }  
    }

    /// <summary>
    /// Reads the file.
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="height">Height dimension of the image</param>
    /// <param name="width">Width dimension of the image</param>
    /// <returns>A Texture2D.</returns>
    private static Texture2D ReadFile(string path, int height, int width)
    {
        return null;
    }
}
