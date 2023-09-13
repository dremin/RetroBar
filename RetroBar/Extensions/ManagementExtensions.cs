using System;
using System.Collections.Generic;
using System.IO;

internal static class ManagementExtensions
{
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