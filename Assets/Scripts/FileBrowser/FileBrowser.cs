using NodeGraph;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FileBrowser : MonoBehaviour
{
    [SerializeField] private FileOpenBrowser fileOpenBrowser;
    [SerializeField] private FileSaveBrowser fileSaveBrowser;

    public void Open<T>(FileBrowserDataSource<T> dataSource, string rootPath, System.Action<T> onConfirm, System.Action onCancel)
    {
        fileOpenBrowser.Open(dataSource, rootPath, onConfirm, onCancel);
    }

    public void OpenFile(System.Action<string> onConfirm, System.Action onCancel)
    {
        var documentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        Open(new FileBrowserFileDateSource(), documentsFolder, onConfirm, onCancel);
    }

    public void OpenGraph(System.Action<DTO.GraphID> onConfirm, System.Action onCancel)
    {
        Open(new FileBrowserGraphDateSource(Globals<GraphDatabase>.Instance), "", onConfirm, onCancel);
    }

    public void Save<T>(FileBrowserDataSource<T> dataSource, string rootPath, System.Action<string, string> onConfirm, System.Action onCancel)
    {
        fileSaveBrowser.Open(dataSource, rootPath, onConfirm, onCancel);
    }

    public void SaveFile(System.Action<string> onConfirm, System.Action onCancel)
    {
        var documentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        Save(new FileBrowserFileDateSource(), documentsFolder, (pwd, name) => onConfirm(System.IO.Path.Combine(pwd, name)), onCancel);
    }

    public void SaveGraph(System.Action<DTO.GraphID> onConfirm, System.Action onCancel)
    {
        Save(new FileBrowserGraphDateSource(Globals<GraphDatabase>.Instance), "", (_, name) => onConfirm(new(name)), onCancel);
    }
}

public struct FileBrowserDataEntry<T>
{
    public string name;
    public string path;
    public T value;
    public bool isDirectory;

    public FileBrowserDataEntry(string name, string path, T value, bool isDirectory)
    {
        this.name = name;
        this.path = path;
        this.value = value;
        this.isDirectory = isDirectory;
    }
}

public abstract class FileBrowserDataSource<T>
{
    public abstract List<FileBrowserDataEntry<T>> GetFiles(string path);
    public abstract List<FileBrowserDataEntry<T>> GetDirectories(string path);
}

public class FileBrowserFileDateSource : FileBrowserDataSource<string>
{
    public override List<FileBrowserDataEntry<string>> GetFiles(string path)
    {
        return System.IO.Directory.GetFiles(path)
            .Select(file =>
            {
                var fullPath = System.IO.Path.GetFullPath(file);
                var name = System.IO.Path.GetFileName(fullPath);
                return new FileBrowserDataEntry<string>(name, fullPath, fullPath, false);
            })
            .ToList();
    }

    public override List<FileBrowserDataEntry<string>> GetDirectories(string path)
    {
        var dirs = System.IO.Directory.GetDirectories(path)
            .Select(file =>
            {
                var fullPath = System.IO.Path.GetFullPath(file);
                var name = System.IO.Path.GetFileName(fullPath);
                return new FileBrowserDataEntry<string>(name, fullPath, fullPath, true);
            })
            .ToList();
        var dirList = new List<FileBrowserDataEntry<string>>(dirs.Count + 1);
        var backDir = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(path));
        dirList.Add(new FileBrowserDataEntry<string>("..", backDir, backDir, true));
        dirList.AddRange(dirs);
        return dirList;
    }
}

public class FileBrowserGraphDateSource : FileBrowserDataSource<DTO.GraphID>
{
    private readonly GraphDatabase database;

    public FileBrowserGraphDateSource(GraphDatabase database)
    {
        this.database = database;
    }

    public override List<FileBrowserDataEntry<DTO.GraphID>> GetFiles(string path)
    {
        return database.GetGraphs().Select(file =>
            {
                return new FileBrowserDataEntry<DTO.GraphID>(file.Key.path, "", file.Key, false);
            })
            .ToList();
    }

    public override List<FileBrowserDataEntry<DTO.GraphID>> GetDirectories(string path)
    {
        return new();
    }
}
