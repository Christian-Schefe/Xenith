using ActionMenu;
using DSP;
using ReactiveData.App;
using ReactiveData.Core;
using System.Linq;
using UnityEngine;

public class Main : MonoBehaviour
{
    public GameObject exportingModal;

    public ReactiveApp app = ReactiveApp.Default;

    private IReactiveEnumerable<IReactiveTab> tabs;
    private ReactiveTrackingDict<ReactiveSong, IReactiveTab> songTabs;
    private ReactiveTrackingDict<ReactiveGraph, IReactiveTab> graphTabs;
    private IReactive<IReactiveTab> openTab;

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
        openTab = new DerivedReactive<Nand<ReactiveSong, ReactiveGraph>, IReactiveTab>(app.openElement, element =>
        {
            if (element.TryGet(out ReactiveSong song)) return songTabs[song];
            if (element.TryGet(out ReactiveGraph graph)) return graphTabs[graph];
            return null;
        });
        tabs = new ReactiveChainedEnumerable<IReactiveTab>(new IReactiveEnumerable<IReactiveTab>[] { songTabs, graphTabs });
        var actionBar = Globals<ActionBar>.Instance;

        actionBar.AddActions(new()
        {
            new("Editor", 0, new() {
                new ActionType.Button("Quit", OnQuit)
            }),

            new("Song", 0, new() {
                new ActionType.Button("New", NewSong),
                new ActionType.Button("Open", OpenSong),
                new ActionType.Button("Save", () => SaveOpenSong(false)),
                new ActionType.Button("Save As", () => SaveOpenSong(true)),
                new ActionType.Button("Export", () => ExportOpenSong()),
            }),

            new("Graph", 0, new() {
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
        bool changeOpenTab = openTab.Value == tab;
        var tabList = tabs.ToList();
        var index = tabList.IndexOf(tab);
        void ChangeOpenTab()
        {
            if (!changeOpenTab) return;

            if (tabList.Count <= 1) app.openElement.Value = new();
            else
            {
                IReactiveTab newTab;
                if (index > 0) newTab = tabList[index - 1];
                else newTab = tabList[index + 1];
                if (newTab is SongTab newSongTab) app.openElement.Value = new(newSongTab.song);
                else if (newTab is GraphTab graphTab) app.openElement.Value = new(graphTab.graph);
                else throw new System.ArgumentException($"Unknown tab type: {newTab.GetType()}");
            }
        }

        if (tab is SongTab songTab)
        {
            void OnFinishSave()
            {
                ChangeOpenTab();
                app.songs.Remove(songTab.song);
            }
            OnSongClose(songTab.song, OnFinishSave);
        }
        else if (tab is GraphTab graphTab)
        {
            void OnFinishSave()
            {
                ChangeOpenTab();
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
        var newSong = ReactiveSong.Default;
        app.songs.Add(newSong);
        app.openElement.Value = new(newSong);
    }

    private void OpenSong()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenFile(path =>
        {
            if (app.TryLoadSong(path, out var song))
            {
                app.openElement.Value = new(song);
            }
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
        if (song.path.Value == null && song.IsEmpty() && !alwaysAsk)
        {
            onFinishSave?.Invoke();
        }
        else if (song.path.Value != null && !alwaysAsk)
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

    private void OnGraphClose(ReactiveGraph graph, System.Action callback)
    {
        SaveGraph(graph, false, () =>
        {
            callback();
        });
    }

    private void SaveGraph(ReactiveGraph graph, bool alwaysAsk, System.Action onFinishSave)
    {
        if (graph.path.Value == null && graph.IsEmpty() && !alwaysAsk)
        {
            onFinishSave?.Invoke();
        }
        else if (graph.path.Value != null && !alwaysAsk)
        {
            app.SaveGraph(graph, graph.path.Value);
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.SaveGraph(newId =>
            {
                app.SaveGraph(graph, newId);
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }
}
