using System.Collections.Generic;
using UnityEngine;

public class FileOpenBrowser : MonoBehaviour
{
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private FileTreeEntry fileEntryPrefab, folderEntryPrefab;

    private string pwd;
    private System.Action<string> onConfirm;
    private readonly List<FileTreeEntry> entries = new();

    public void Open(string pwd, System.Action<string> onConfirm)
    {
        this.pwd = pwd;
        this.onConfirm = onConfirm;
        gameObject.SetActive(true);
        UpdateEntries();
    }

    public void UpdateEntries()
    {
        foreach (var entry in entries)
        {
            Destroy(entry.gameObject);
        }
        entries.Clear();

        var parent = System.IO.Path.GetDirectoryName(pwd);
        if (parent != null)
        {
            var parentName = System.IO.Path.GetFileName(parent);
            var entry = Instantiate(folderEntryPrefab, contentArea);
            entry.SetData("..", () => OnClickDirectory(parent));
            entries.Add(entry);
        }

        var directories = System.IO.Directory.GetDirectories(pwd);
        foreach (var directory in directories)
        {
            var fullPath = System.IO.Path.GetFullPath(directory);
            var name = System.IO.Path.GetFileName(fullPath);
            var entry = Instantiate(folderEntryPrefab, contentArea);
            entry.SetData(name, () => OnClickDirectory(fullPath));
            entries.Add(entry);
        }
        var files = System.IO.Directory.GetFiles(pwd);
        foreach (var file in files)
        {
            var fullPath = System.IO.Path.GetFullPath(file);
            var name = System.IO.Path.GetFileName(fullPath);
            var entry = Instantiate(fileEntryPrefab, contentArea);
            entry.SetData(name, () => OnClickFile(fullPath));
            entries.Add(entry);
        }
    }

    private void OnClickDirectory(string directory)
    {
        pwd = directory;
        UpdateEntries();
    }

    private void OnClickFile(string file)
    {
        onConfirm?.Invoke(file);
        gameObject.SetActive(false);
    }
}