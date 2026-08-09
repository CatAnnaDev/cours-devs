using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Demos.Singletons;

public sealed class SaveService
{
    private const string SavePath = "user://save.json";

    private static readonly Lazy<SaveService> LazyInstance =
        new(() => new SaveService(), isThreadSafe: true);

    public static SaveService Instance => LazyInstance.Value;

    private readonly Dictionary<string, string> _values = new();

    private SaveService()
    {
        Load();
    }

    public string Get(string key, string fallback = "") =>
        _values.TryGetValue(key, out string value) ? value : fallback;

    public void Set(string key, string value)
    {
        _values[key] = value;
    }

    public void Save()
    {
        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
            return;

        file.StoreString(JsonSerializer.Serialize(_values));
    }

    private void Load()
    {
        if (!FileAccess.FileExists(SavePath))
            return;

        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(file.GetAsText());
        if (loaded == null)
            return;

        foreach ((string key, string value) in loaded)
            _values[key] = value;
    }
}
