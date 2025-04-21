using UnityEngine;

public class FileBrowser : MonoBehaviour
{
    [SerializeField] private FileOpenBrowser fileOpenBrowser;
    [SerializeField] private FileSaveBrowser fileSaveBrowser;

    public void Open(System.Action<string> onConfirm)
    {
        var documentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        fileOpenBrowser.Open(documentsFolder, onConfirm);
    }

    public void Save(System.Action<string> onConfirm, System.Action onCancel)
    {
        var documentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        fileSaveBrowser.Open(documentsFolder, onConfirm, onCancel);
    }
}
