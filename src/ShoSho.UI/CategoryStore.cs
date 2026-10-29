using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ShoSho.UI.Services;

public record CustomCategory(string Name, string Icon);

public static class CategoryStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ShoSho", "categories.json");

    public static List<CustomCategory> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            return JsonSerializer.Deserialize<List<CustomCategory>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void Save(IEnumerable<CustomCategory> categories)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(categories));
    }
}