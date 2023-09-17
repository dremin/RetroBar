using System;
using System.Collections.Generic;
using System.IO;

internal static class ManagementExtensions
{
    public static string InLocalAppData(this string path1, string path2 = "")
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroBar", path1, path2);
    }

    public static void AddFrom(this List<string> list, string path, string extension, bool skipExisting = false)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(path, $"*.{extension}"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (skipExisting && list.Contains(fileName))
            {
                continue;
            }
            list.Add(fileName);
        }
    }
}