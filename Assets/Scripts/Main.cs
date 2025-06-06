using ActionMenu;
using DSP;
using DTO;
using ReactiveData.App;
using ReactiveData.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Main : MonoBehaviour
{
    public GameObject exportingModal;

    public ReactiveApp app = ReactiveApp.Default;

    private ReactiveTrackingDict<ReactiveSong, IReactiveTab> songTabs;
    private ReactiveTrackingDict<ReactiveGraph, IReactiveTab> graphTabs;

    private void Start()
    {
        songTabs = new(app.songs, song =>
        {
            return new SongTab(song);
        });
        graphTabs = new(app.graphs, graph =>
        {
            return new GraphTab(graph);
        });
        var openTab = new DerivedReactive<Nand<ReactiveSong, ReactiveGraph>, IReactiveTab>(app.openElement, element =>
        {
            if (element.TryGet(out ReactiveSong song)) return songTabs[song];
            if (element.TryGet(out ReactiveGraph graph)) return graphTabs[graph];
            return null;
        });
        var tabs = new ReactiveChainedEnumerable<IReactiveTab>(new IReactiveEnumerable<IReactiveTab>[] { songTabs, graphTabs });
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

        actionBar.BindTabs(tabs, openTab);
        actionBar.onTabClick += OnTabClick;
        actionBar.onTabClose += OnTabClose;
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
        var songEntries = app.songs.ToList();
        var graphEntries = app.graphs.ToList();
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

    private void OnTabClick(IReactiveTab tab)
    {
        if (tab is SongTab songTab)
        {
            app.openElement.Value = new(songTab.song);
        }
        else if (tab is GraphTab graphTab)
        {
            app.openElement.Value = new(graphTab.graph);
        }
        else
        {
            throw new System.ArgumentException($"Unknown tab type: {tab.GetType()}");
        }
    }

    private void OnTabClose(IReactiveTab tab)
    {

        if (tab is SongTab songTab)
        {
            void OnFinishSave()
            {
                app.songs.Remove(songTab.song);
            }
            OnSongClose(songTab.song, OnFinishSave);
        }
        else if (tab is GraphTab graphTab)
        {
            void OnFinishSave()
            {
                app.graphs.Remove(graphTab.graph);
            }
            OnGraphClose(graphTab.graph, OnFinishSave);
        }
        else
        {
            throw new System.ArgumentException($"Unknown tab type: {tab.GetType()}");
        }
    }

    private void NewSong()
    {
        app.songs.Add(ReactiveSong.Default);
    }

    private void OpenSong()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenFile(path =>
        {
            app.TryLoadSong(path, out var id);
        }, () => { });
    }

    private void SaveOpenSong(bool alwaysAsk)
    {
        if (!app.openElement.Value.TryGet(out ReactiveSong song))
        {
            return;
        }
        SaveSong(song, alwaysAsk, null);
    }

    private void ExportOpenSong()
    {
        if (!app.openElement.Value.TryGet(out ReactiveSong _))
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

    private void OnSongClose(ReactiveSong song, System.Action callback)
    {
        SaveSong(song, false, () =>
        {
            app.songs.Remove(song);
            callback();
        });
    }

    private void SaveSong(ReactiveSong song, bool alwaysAsk, System.Action onFinishSave)
    {
        if (!song.IsEmpty() && !alwaysAsk)
        {
            onFinishSave?.Invoke();
        }
        else if (song.path != null && !alwaysAsk)
        {
            app.SaveSong(song, song.path.Value);
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.SaveFile(path =>
            {
                app.SaveSong(song, path);
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }

    private void NewGraph()
    {
        app.graphs.Add(ReactiveGraph.Default);
    }

    private void OpenGraph()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenGraph(path =>
        {
            app.TryLoadGraph(path);
        }, () => { });
    }

    private void SaveOpenGraph(bool alwaysAsk)
    {
        if (!app.openElement.Value.TryGet(out ReactiveGraph graph))
        {
            return;
        }
        SaveGraph(graph, alwaysAsk, null);
    }

    private void DeleteOpenGraph()
    {
        if (!app.openElement.Value.TryGet(out ReactiveGraph graph))
        {
            return;
        }
        app.DeleteGraph(graph);
    }

    private void OnGraphClose(ReactiveGraph id, System.Action callback)
    {
        SaveGraph(id, false, () =>
        {
            callback();
        });
    }

    private void SaveGraph(ReactiveGraph id, bool alwaysAsk, System.Action onFinishSave)
    {
        if (id.path != null && !alwaysAsk)
        {
            app.SaveGraph(id, id.path.Value);
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.SaveGraph(newId =>
            {
                app.SaveGraph(id, newId);
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }
}
