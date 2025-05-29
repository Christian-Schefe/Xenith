using ActionMenu;
using DSP;
using DTO;
using ReactiveData.App;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Main : MonoBehaviour
{
    public SongController songController;
    public GraphController graphController;
    public GameObject exportingModal;

    private readonly Dictionary<SongID, ActionTab> songTabs = new();
    private readonly Dictionary<GraphID, ActionTab> graphTabs = new();
    private SongID openSong;
    private GraphID openGraph;

    public GraphID CurrentGraphId => openGraph;
    public SongID CurrentSongId => openSong;
    public Graph CurrentGraph => graphController.GetGraph(openGraph);
    public ReactiveSong CurrentSong => songController.GetSong(openSong);

    private void Start()
    {
        var actionBar = Globals<ActionBar>.Instance;

        actionBar.AddActions(new()
        {
            new("Editor", new() {
                new ActionType.Button("Quit", OnQuit)
            }),

            new("Song", new() {
                new ActionType.Button("New", NewSong),
                new ActionType.Button("Open", OpenSong),
                new ActionType.Button("Save", () => SaveOpenSong(false)),
                new ActionType.Button("Save As", () => SaveOpenSong(true)),
                new ActionType.Button("Export", () => ExportOpenSong()),
            }),

            new("Graph", new() {
                new ActionType.Button("New", NewGraph),
                new ActionType.Button("Open", OpenGraph),
                new ActionType.Button("Save", () => SaveOpenGraph(false)),
                new ActionType.Button("Save As", () => SaveOpenGraph(true)),
                new ActionType.Button("Delete", () => DeleteOpenGraph())
            }),
        });

        actionBar.SetTitle("Xenith");

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
        {
            Debug.Log("Saving");
            SaveOpenSong(false);
            SaveOpenGraph(false);
        }
    }

    private void OnQuit()
    {
        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
        var songEntries = songTabs.Keys.ToList();
        var graphEntries = graphTabs.Keys.ToList();
        void SaveSongEntry(int i)
        {
            Debug.Log($"Saving song {i}/{songEntries.Count}");
            if (i >= songEntries.Count)
            {
                SaveGraphEntry(0);
                return;
            }
            SaveSong(songEntries[i], false, () => SaveSongEntry(i + 1));
        }
        void SaveGraphEntry(int i)
        {
            Debug.Log($"Saving graph {i}/{graphEntries.Count}");
            if (i >= graphEntries.Count)
            {
                Quit();
                return;
            }
            SaveGraph(graphEntries[i], false, () => SaveGraphEntry(i + 1));
        }
        SaveSongEntry(0);
    }

    private void NewSong()
    {
        var id = songController.AddSong();
        AddSongTab(id);
    }

    private void OpenSong()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenFile(path =>
        {
            if (songController.TryLoadSong(path, out var id))
            {
                AddSongTab(id);
            }
        }, () => { });
    }

    private void SaveOpenSong(bool alwaysAsk)
    {
        if (openSong == null)
        {
            return;
        }
        SaveSong(openSong, alwaysAsk, null);
    }

    private void ExportOpenSong()
    {
        if (openSong == null)
        {
            return;
        }

        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.SaveFile(path =>
        {
            var dsp = Globals<DPSTest>.Instance;
            exportingModal.SetActive(true);
            float exportStartTime = Time.time; ;
            dsp.Render(wavFile =>
            {
                wavFile.WriteToFile(path);
                exportingModal.SetActive(false);
                Debug.Log($"Exported in {Time.time - exportStartTime} seconds");
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

    private void AddGraphTab(GraphID id)
    {
        var actionBar = Globals<ActionBar>.Instance;
        var name = id.GetName();
        var tab = new ActionTab(name, () => OnGraphShow(id), () => OnGraphHide(), (callback) => OnGraphClose(id, callback));
        graphTabs.Add(id, tab);
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
            var trackEditor = Globals<TrackEditor>.Instance;
            trackEditor.Hide();
            openSong = newId;
            trackEditor.Show();
        }
        actionBar.UpdateTab(tab);
    }

    private void ChangeGraphId(GraphID oldId, GraphID newId)
    {
        var actionBar = Globals<ActionBar>.Instance;
        var tab = graphTabs[oldId];
        graphTabs.Remove(oldId);
        graphTabs.Add(newId, tab);
        tab.name = newId.GetName();
        tab.onSelect = () => OnGraphShow(newId);
        tab.onDeselect = () => OnGraphHide();
        tab.onTryClose = (callback) => OnGraphClose(newId, callback);
        if (openGraph == oldId)
        {
            Debug.Log("Changing graph id");
            var graphEditor = Globals<NodeGraph.GraphEditor>.Instance;
            graphEditor.Hide();
            openGraph = newId;
            graphEditor.Show();
        }
        actionBar.UpdateTab(tab);
    }

    private void OnSongClose(SongID id, System.Action callback)
    {
        SaveSong(id, false, () =>
        {
            songController.UnloadSong(id);
            songTabs.Remove(id);
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
            fileBrowser.SaveFile(path =>
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
        var id = graphController.AddGraph();
        AddGraphTab(id);
    }

    private void OpenGraph()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenGraph(path =>
        {
            if (graphController.TryLoadGraph(path))
            {
                AddGraphTab(path);
            }
        }, () => { });
    }

    private void SaveOpenGraph(bool alwaysAsk)
    {
        if (openGraph == null)
        {
            return;
        }
        SaveGraph(openGraph, alwaysAsk, null);
    }

    private void DeleteOpenGraph()
    {
        if (openGraph == null)
        {
            return;
        }
        var idToDelete = openGraph;
        var actionBar = Globals<ActionBar>.Instance;
        var tab = graphTabs[openGraph];
        tab.onTryClose = callback => callback();
        graphTabs.Remove(openGraph);
        actionBar.CloseTab(tab);

        graphController.DeleteGraph(idToDelete);
    }

    private void OnGraphShow(GraphID id)
    {
        openGraph = id;

        var graphEditor = Globals<NodeGraph.GraphEditor>.Instance;
        graphEditor.Show();
    }

    private void OnGraphHide()
    {
        openGraph = null;

        var graphEditor = Globals<NodeGraph.GraphEditor>.Instance;
        graphEditor.Hide();
    }

    private void OnGraphClose(GraphID id, System.Action callback)
    {
        SaveGraph(id, false, () =>
        {
            graphController.UnloadGraph(id);
            graphTabs.Remove(id);
            callback();
        });
    }

    private void SaveGraph(GraphID id, bool alwaysAsk, System.Action onFinishSave)
    {
        if (id.path != null && !alwaysAsk)
        {
            graphController.SaveGraph(id, id);
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.SaveGraph(newId =>
            {
                if (graphController.SaveGraph(id, newId))
                {
                    ChangeGraphId(id, newId);
                }
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }
}
