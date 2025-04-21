
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FileSaveBrowser : MonoBehaviour
{
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private FileTreeEntry folderEntryPrefab;
    [SerializeField] private TMPro.TMP_InputField fileNameInputField;
    [SerializeField] private Button cancelButton, confirmButton;

    private string pwd;
    private System.Action<string> onConfirm;
    private System.Action onCancel;
    private readonly List<FileTreeEntry> entries = new();

    private void OnEnable()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
        cancelButton.onClick.RemoveListener(OnCancel);
    }

    private void OnCancel()
    {
        onCancel?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnConfirm()
    {
        if (!string.IsNullOrEmpty(fileNameInputField.text))
        {
            var fileName = fileNameInputField.text;
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            var path = System.IO.Path.Combine(pwd, fileNameInputField.text);
            onConfirm?.Invoke(path);
            gameObject.SetActive(false);
        }
    }

    public void Open(string pwd, System.Action<string> onConfirm, System.Action onCancel)
    {
        this.pwd = pwd;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        fileNameInputField.text = "";
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
    }

    private void OnClickDirectory(string directory)
    {
        pwd = directory;
        UpdateEntries();
    }
}