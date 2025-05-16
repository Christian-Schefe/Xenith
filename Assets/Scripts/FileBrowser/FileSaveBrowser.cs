
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class FileSaveBrowser : MonoBehaviour
{
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private FileTreeEntry fileEntryPrefab, folderEntryPrefab;
    [SerializeField] private TMPro.TMP_InputField fileNameInputField;
    [SerializeField] private Button cancelButton, confirmButton;

    private bool isOpen;
    private string pwd;
    private System.Action<string, string> onConfirm;
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

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
        }
    }

    private void OnCancel()
    {
        onCancel?.Invoke();
        gameObject.SetActive(false);
        CleanEntries();
        isOpen = false;
    }

    private void OnConfirm()
    {
        if (!string.IsNullOrEmpty(fileNameInputField.text))
        {
            var fileName = fileNameInputField.text;
            onConfirm?.Invoke(pwd, fileName);
            gameObject.SetActive(false);
            CleanEntries();
            isOpen = false;
        }
    }

    public void Open<T>(FileBrowserDataSource<T> dataSource, string pwd, System.Action<string, string> onConfirm, System.Action onCancel)
    {
        this.pwd = pwd;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        fileNameInputField.text = "";
        gameObject.SetActive(true);
        UpdateEntries(dataSource);
    }

    private void CleanEntries()
    {
        foreach (var entry in entries)
        {
            Destroy(entry.gameObject);
        }
        entries.Clear();
    }

    public void UpdateEntries<T>(FileBrowserDataSource<T> dataSource)
    {
        CleanEntries();

        var data = new List<FileBrowserDataEntry<T>>();
        data.AddRange(dataSource.GetDirectories(pwd));

        foreach (var entry in data)
        {
            var prefab = entry.isDirectory ? folderEntryPrefab : fileEntryPrefab;
            var entryObj = Instantiate(prefab, contentArea);
            System.Action<FileBrowserDataSource<T>, string> onClick = entry.isDirectory ? OnClickDirectory<T> : OnClickFile<T>;
            entryObj.SetData(entry.name, () => onClick(dataSource, entry.path));
            entries.Add(entryObj);
        }
    }

    private void OnClickDirectory<T>(FileBrowserDataSource<T> dataSource, string directory)
    {
        pwd = directory;
        UpdateEntries(dataSource);
    }

    private void OnClickFile<T>(FileBrowserDataSource<T> dataSource, string file)
    {
        fileNameInputField.text = System.IO.Path.GetFileName(file);
    }
}