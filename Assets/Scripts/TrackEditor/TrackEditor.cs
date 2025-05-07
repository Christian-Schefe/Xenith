using ActionMenu;
using DSP;
using Persistence;
using PianoRoll;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yeast;

public class TrackEditor : MonoBehaviour
{
    [SerializeField] private RectTransform trackContainer;

    [SerializeField] private Track trackPrefab;

    [System.NonSerialized] public List<TempoEvent> tempoEvents = new() { new(0, 2), new(8, 3) };

    private readonly List<Track> tracks = new();

    private string songPath;

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

        FillEmptySong();
    }

    private void Update()
    {
        var songName = songPath != null ? System.IO.Path.GetFileNameWithoutExtension(songPath) : "Untitled";
        var actionBar = Globals<ActionBar>.Instance;
        actionBar.SetTitle(songName);
    }

    private void FillEmptySong()
    {
        if (tracks.Count == 0)
        {
            AddTrack();
            Globals<NoteEditor>.Instance.SetActiveTrack(0);
        }
    }

    private void NewSong()
    {
        SaveSong(false, () =>
        {
            Close();
            FillEmptySong();
        });
    }

    private void OpenSong()
    {
        SaveSong(false, () =>
        {
            var fileBrowser = Globals<FileBrowser>.Instance;
            fileBrowser.Open(path =>
            {
                if (FilePersistence.TryLoadFullPath(path, out var song))
                {
                    if (song.TryFromJson(out SerializedSong serializedSong))
                    {
                        Close();
                        Debug.Log($"Loaded song from {path}");
                        songPath = path;
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
        if (songPath != null && !alwaysAsk)
        {
            FilePersistence.SaveFullPath(songPath, Serialize().ToJson());
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
                songPath = path;
                FilePersistence.SaveFullPath(songPath, Serialize().ToJson());
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
        songPath = null;
        var noteEditor = Globals<NoteEditor>.Instance;
        noteEditor.OnClose();
    }

    private void AddTrack()
    {
        var track = Instantiate(trackPrefab, trackContainer);
        track.Initialize(tracks.Count);
        tracks.Add(track);
    }

    public Track GetTrack(int index) => tracks[index];

    public SerializedSong Serialize()
    {
        return new SerializedSong()
        {
            tracks = tracks.ConvertAll(track => track.Serialize()),
            tempoEvents = tempoEvents
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
        var hasSoloTracks = tracks.Any(t => t.isSoloed);
        var filteredTracks = tracks.Where(t => (!hasSoloTracks || t.isSoloed) && !t.isMuted).ToList();

        var graph = new DSP.NodeGraph();
        int outLeft = graph.AddOutput<FloatValue>("Left", 0);
        int outRight = graph.AddOutput<FloatValue>("Right", 1);
        int addLeft = graph.AddNode(Prelude.Add(filteredTracks.Count));
        int addRight = graph.AddNode(Prelude.Add(filteredTracks.Count));
        graph.AddConnection(new(addLeft, 0, outLeft, 0));
        graph.AddConnection(new(addRight, 0, outRight, 0));

        for (int i = 0; i < filteredTracks.Count; i++)
        {
            var node = filteredTracks[i].BuildAudioNode(startTime, tempoEvents);
            int nodeIndex = graph.AddNode(node);
            graph.AddConnection(new(nodeIndex, 0, addLeft, i));
            graph.AddConnection(new(nodeIndex, 1, addRight, i));
        }
        return graph;
    }
}
