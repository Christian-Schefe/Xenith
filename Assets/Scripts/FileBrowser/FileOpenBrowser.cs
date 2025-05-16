using System.Collections.Generic;
using UnityEngine;

public class FileOpenBrowser : MonoBehaviour
{
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private FileTreeEntry fileEntryPrefab, folderEntryPrefab;

    private bool isOpen;
    private string pwd;
    private System.Action onCancel;
    private readonly List<FileTreeEntry> entries = new();

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            onCancel?.Invoke();
            gameObject.SetActive(false);
            CleanEntries();
            isOpen = false;
        }
    }

    public void Open<T>(FileBrowserDataSource<T> dataSource, string pwd, System.Action<T> onConfirm, System.Action onCancel)
    {
        isOpen = true;
        this.onCancel = onCancel;
        this.pwd = pwd;
        gameObject.SetActive(true);
        UpdateEntries(dataSource, onConfirm);
    }

    private void CleanEntries()
    {
        foreach (var entry in entries)
        {
            Destroy(entry.gameObject);
        }
        entries.Clear();
    }

    public void UpdateEntries<T>(FileBrowserDataSource<T> dataSource, System.Action<T> onConfirm)
    {
        CleanEntries();

        var data = new List<FileBrowserDataEntry<T>>();
        data.AddRange(dataSource.GetDirectories(pwd));
        data.AddRange(dataSource.GetFiles(pwd));

        foreach (var entry in data)
        {
            var prefab = entry.isDirectory ? folderEntryPrefab : fileEntryPrefab;
            var entryObj = Instantiate(prefab, contentArea);
            if (entry.isDirectory)
            {
                entryObj.SetData(entry.name, () => OnClickDirectory(dataSource, onConfirm, entry.path));
            }
            else
            {
                entryObj.SetData(entry.name, () => OnClickFile(onConfirm, entry.value));
            }
            entries.Add(entryObj);
        }
    }

    private void OnClickDirectory<T>(FileBrowserDataSource<T> dataSource, System.Action<T> onConfirm, string directory)
    {
        pwd = directory;
        UpdateEntries(dataSource, onConfirm);
    }

    private void OnClickFile<T>(System.Action<T> onConfirm, T value)
    {
        onConfirm?.Invoke(value);
        gameObject.SetActive(false);
        CleanEntries();
        isOpen = false;
    }
}