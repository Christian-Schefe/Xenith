using System;
using System.Collections.Generic;
using Persistence;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private Dictionary<string, string> saveData;
    private readonly Dictionary<string, IPersistentBox> boxes = new();

    private bool isLoaded = false;

    private void Awake()
    {
        if (!Globals<SaveSystem>.RegisterOrDestroy(this)) return;
        DontDestroyOnLoad(gameObject);

        SceneSystem.AddListener(SceneSystem.EventType.ApplicationQuit, () => Save(), true);
        Load();
    }

    private string GetPath()
    {
        return "save.json";
    }

    public void AttachBox(string key, IPersistentBox box)
    {
        boxes.Add(key, box);
    }

    public void DetachBox(string key)
    {
        if (boxes.TryGetValue(key, out var box))
        {
            saveData[key] = box.Serialize();
            boxes.Remove(key);
        }
    }

    public bool TryGetValue(string key, out string value)
    {
        if (!isLoaded) Load();
        return saveData.TryGetValue(key, out value);
    }

    private void Save()
    {
        var path = GetPath();
        if (!isLoaded) throw new Exception($"SaveState '{path}' has not loaded yet.");

        foreach (var (key, box) in boxes)
        {
            saveData[key] = box.Serialize();
        }

        //Debug.Log($"Saving to '{path}'");
        if (saveData.Count == 0) return;

        JsonPersistence.Save(path, saveData);
    }

    private void Load()
    {
        if (isLoaded) return;
        var path = GetPath();
        //Debug.Log($"Loading from '{path}'");

        saveData = JsonPersistence.LoadDefault(path, new Dictionary<string, string>());

        isLoaded = true;
    }

    public void Delete()
    {
        var path = GetPath();
        JsonPersistence.Delete(path);

        saveData.Clear();
    }
}
