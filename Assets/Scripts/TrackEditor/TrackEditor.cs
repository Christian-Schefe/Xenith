using ActionMenu;
using DSP;
using Mono.Cecil.Cil;
using Persistence;
using PianoRoll;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Yeast;

public class TrackEditor : MonoBehaviour
{
    [SerializeField] private RectTransform trackContainer;
    [SerializeField] private SceneReference pianoRollScene;

    [SerializeField] private Track trackPrefab;

    private readonly List<Track> tracks = new();

    private void Start()
    {
        var actionBar = Globals<ActionBar>.Instance;
        actionBar.SetActions(new()
        {
            new("File", new() {
                new ActionType.Button("New", NewSong),
                new ActionType.Button("Open", OpenSong),
                new ActionType.Button("Save", () => SaveSong(false, null)),
                new ActionType.Button("Save As", () => SaveSong(true, null)),
            }),
            new("Tracks", new() {
                new ActionType.Button("New", AddTrack),
            }),
        });

        var main = Globals<Main>.Instance;
        if (main.OpenSong != null)
        {
            Deserialize(main.OpenSong.Value);
            main.OpenSong = null;
        }
    }

    private void Update()
    {
        var main = Globals<Main>.Instance;
        var songName = main.SongPath != null ? System.IO.Path.GetFileNameWithoutExtension(main.SongPath) : "Untitled";
        var actionBar = Globals<ActionBar>.Instance;
        actionBar.SetTitle(songName);
    }

    private void NewSong()
    {
        SaveSong(false, () =>
        {
            Close();
            AddTrack();
        });
    }

    private void OpenSong()
    {
        SaveSong(false, () =>
        {
            var main = Globals<Main>.Instance;
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.Open(path =>
            {
                if (FilePersistence.TryLoadFullPath(path, out var song))
                {
                    if (song.TryFromJson(out SerializedSong serializedSong))
                    {
                        Close();
                        Debug.Log($"Loaded song from {path}");
                        main.SongPath = path;
                        Deserialize(serializedSong);
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

    private void SaveSong(bool alwaysAsk, System.Action onFinishSave)
    {
        var main = Globals<Main>.Instance;
        if (main.SongPath != null && !alwaysAsk)
        {
            FilePersistence.SaveFullPath(main.SongPath, Serialize().ToJson());
            onFinishSave?.Invoke();
        }
        else if (tracks.Count == 0 && !alwaysAsk)
        {
            onFinishSave?.Invoke();
        }
        else
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.Save(path =>
            {
                main.SongPath = path;
                FilePersistence.SaveFullPath(main.SongPath, Serialize().ToJson());
                onFinishSave?.Invoke();
            }, () =>
            {
                onFinishSave?.Invoke();
            });
        }
    }

    private void Close()
    {
        foreach (var track in tracks)
        {
            Destroy(track.gameObject);
        }
        tracks.Clear();
        var main = Globals<Main>.Instance;
        main.SongPath = null;
    }

    private void AddTrack()
    {
        var track = Instantiate(trackPrefab, trackContainer);
        track.Initialize(tracks.Count);
        tracks.Add(track);
    }

    public void OpenTrack(int trackIndex)
    {
        var main = Globals<Main>.Instance;
        main.OpenSong = Serialize();
        main.OpenTrack = trackIndex;
        SceneSystem.LoadScene(pianoRollScene);
    }

    public SerializedSong Serialize()
    {
        return new SerializedSong()
        {
            tracks = tracks.ConvertAll(track => track.Serialize()),
            tempoEvents = new List<TempoEvent>() { new(0, 2), new(8, 3) }
        };
    }

    public void Deserialize(SerializedSong song)
    {
        for (int i = 0; i < song.tracks.Count; i++)
        {
            var trackInstance = Instantiate(trackPrefab, trackContainer);
            trackInstance.Deserialize(i, song.tracks[i]);
            tracks.Add(trackInstance);
        }
    }
}

public struct SerializedSong
{
    public List<SerializedTrack> tracks;
    public List<TempoEvent> tempoEvents;

    public readonly AudioNode BuildAudioNode(float startTime)
    {
        var graph = new DSP.NodeGraph();
        int outLeft = graph.AddOutput<FloatValue>("Left", 0);
        int outRight = graph.AddOutput<FloatValue>("Right", 1);
        int addLeft = graph.AddNode(Prelude.Add(tracks.Count));
        int addRight = graph.AddNode(Prelude.Add(tracks.Count));
        graph.AddConnection(new(addLeft, 0, outLeft, 0));
        graph.AddConnection(new(addRight, 0, outRight, 0));

        for (int i = 0; i < tracks.Count; i++)
        {
            var node = tracks[i].BuildAudioNode(startTime, tempoEvents);
            int nodeIndex = graph.AddNode(node);
            graph.AddConnection(new(nodeIndex, 0, addLeft, i));
            graph.AddConnection(new(nodeIndex, 1, addRight, i));
        }
        return graph;
    }
}
