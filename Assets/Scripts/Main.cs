using ActionMenu;
using DSP;
using DTO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public SongController songController;

    private readonly Dictionary<SongID, ActionTab> songTabs = new();
    private SongID openSong;

    public Song CurrentSong => songController.GetSong(openSong);

    private void Start()
    {
        var actionBar = Globals<ActionBar>.Instance;

        actionBar.AddActions(new()
        {
            new("Song", new() {
                new ActionType.Button("New", NewSong),
                new ActionType.Button("Open", OpenSong),
                new ActionType.Button("Save", () => SaveOpenSong(false, null)),
                new ActionType.Button("Save As", () => SaveOpenSong(true, null)),
                new ActionType.Button("Export", () => ExportOpenSong()),
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
        trackEditor.Hide();
    }

    private void NewSong()
    {
        var id = songController.AddSong();
        AddSongTab(id);
    }

    private void OpenSong()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.Open(path =>
        {
            if (songController.TryLoadSong(path, out var id))
            {
                AddSongTab(id);
            }
        });
    }

    private void SaveOpenSong(bool alwaysAsk, System.Action onFinishSave)
    {
        if (openSong == null)
        {
            return;
        }
        SaveSong(openSong, alwaysAsk, onFinishSave);
    }

    private void ExportOpenSong()
    {
        if (openSong == null)
        {
            return;
        }

        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.Save(path =>
        {
            var dsp = Globals<DPSTest>.Instance;
            dsp.Render(wavFile =>
            {
                wavFile.WriteToFile(path);
            });
        }, () =>
        {
            Debug.Log("Export cancelled");
        });
    }

    private void AddSongTab(SongID id)
    {
        var actionBar = Globals<ActionBar>.Instance;
        var name = id.GetName();
        var tab = new ActionTab(name, () => OnSongShow(id), () => OnSongHide(), (callback) => OnSongClose(id, callback));
        songTabs.Add(id, tab);
        actionBar.AddTab(tab, true);
    }

    private void ChangeSongId(SongID oldId, SongID newId)
    {
        var actionBar = Globals<ActionBar>.Instance;
        var tab = songTabs[oldId];
        songTabs.Remove(oldId);
        songTabs.Add(newId, tab);
        tab.name = newId.GetName();
        tab.onSelect = () => OnSongShow(newId);
        tab.onDeselect = () => OnSongHide();
        tab.onTryClose = (callback) => OnSongClose(newId, callback);
        if (openSong == oldId)
        {
            openSong = newId;
        }
        actionBar.UpdateTab(tab);
    }

    private void OnSongClose(SongID id, System.Action callback)
    {
        SaveSong(id, false, () =>
        {
            callback();
        });
    }

    private void SaveSong(SongID id, bool alwaysAsk, System.Action onFinishSave)
    {
        if (!songController.HasUnsavedChanges(id) && !alwaysAsk)
        {
            onFinishSave?.Invoke();
        }
        else if (id.path != null && !alwaysAsk)
        {
            songController.SaveSong(id, id.path, out _);
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.Save(path =>
            {
                if (songController.SaveSong(id, path, out var newId))
                {
                    ChangeSongId(id, newId);
                }
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }

    private void OnSongShow(SongID id)
    {
        openSong = id;

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Show();
    }

    private void OnSongHide()
    {
        openSong = null;

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Hide();
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
}
