using ActionMenu;
using Persistence;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yeast;

public class Main : MonoBehaviour
{
    private List<SongTab> openSongs = new();
    private SongTab openTab;

    private void Start()
    {
        var actionBar = Globals<ActionBar>.Instance;

        actionBar.AddActions(new()
        {
            new("Song", new() {
                new ActionType.Button("New", NewSong),
                new ActionType.Button("Open", OpenSong),
                new ActionType.Button("Save", () => SaveSong(false, null)),
                new ActionType.Button("Save As", () => SaveSong(true, null)),
            }),

            new("Graph", new() {
                new ActionType.Button("New", NewGraph),
                new ActionType.Button("Open", OpenGraph),
                new ActionType.Button("Save", () => SaveGraph(false)),
                new ActionType.Button("Save As", () => SaveGraph(true)),
            }),
        });

        actionBar.SetTitle("Xenith");

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.SetVisible(false);
    }

    private void NewSong()
    {
        AddSongTab(null);
    }

    private void OpenSong()
    {
        SaveSong(false, () =>
        {
            var trackEditor = Globals<TrackEditor>.Instance;
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.Open(path =>
            {
                if (FilePersistence.TryLoadFullPath(path, out var song))
                {
                    if (song.TryFromJson(out SerializedSong serializedSong))
                    {
                        Debug.Log($"Loaded song from {path}");
                        AddSongTab(path);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load song from {path}");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load song from {path}");
                }
            });
        });
    }

    private void AddSongTab(string path)
    {
        var actionBar = Globals<ActionBar>.Instance;
        var name = path == null ? "New Song" : Path.GetFileName(path);
        var songTab = new SongTab(path, SerializedSong.Default());
        openSongs.Add(songTab);
        var tab = new ActionTab(name, () => OnSongOpen(songTab), () => OnSongClose(songTab), () => { });
        actionBar.AddTab(tab, true);
    }

    private void SaveSong(bool alwaysAsk, System.Action onFinishSave)
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Save();

        if (openTab.path != null && !alwaysAsk)
        {
            FilePersistence.SaveFullPath(openTab.path, trackEditor.Serialize().ToJson());
            onFinishSave?.Invoke();
        }
        else if (trackEditor.IsEmptySong() && !alwaysAsk)
        {
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.Save(path =>
            {
                openTab.path = path;
                FilePersistence.SaveFullPath(openTab.path, trackEditor.Serialize().ToJson());
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }

    private void OnSongOpen(SongTab song)
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Deserialize(song.song);
        trackEditor.SetVisible(true);
        Debug.Log($"Opened song {song.path}");
    }

    private void OnSongClose(SongTab song)
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Save();
        song.song = trackEditor.Serialize();
        trackEditor.DiscardAll();
        trackEditor.SetVisible(false);
        Debug.Log($"Closed song {song.path}");
    }

    private void NewGraph()
    {

    }

    private void OpenGraph()
    {

    }

    private void SaveGraph(bool alwaysAsk)
    {

    }

    public class SongTab
    {
        public string path;
        public SerializedSong song;

        public SongTab(string path, SerializedSong song)
        {
            this.path = path;
            this.song = song;
        }
    }
}
