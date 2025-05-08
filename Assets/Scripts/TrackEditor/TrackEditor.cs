using ActionMenu;
using DSP;
using PianoRoll;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackEditor : MonoBehaviour
{
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private RectTransform trackContainer;

    [SerializeField] private Track trackPrefab;

    [System.NonSerialized] public List<TempoEvent> tempoEvents = new() { new(0, 2), new(8, 3) };

    private readonly List<Track> tracks = new();

    private TopLevelAction trackAction = null;

    private TopLevelAction BuildAction()
    {
        return new("Track", new() { new ActionType.Button("", () => AddTrack()) });
    }

    public void SetVisible(bool visible)
    {
        uiRoot.gameObject.SetActive(visible);

        var actionBar = Globals<ActionBar>.Instance;
        var noteEditor = Globals<NoteEditor>.Instance;

        trackAction ??= BuildAction();
        if (visible)
        {
            actionBar.AddActions(new() { trackAction });
            noteEditor.SetActiveTrack(0);
        }
        else
        {
            actionBar.RemoveAction(trackAction);
            noteEditor.ClearAll();
            noteEditor.SetActiveTrack(-1);
        }
    }

    public bool IsEmptySong()
    {
        if (tracks.Count == 0) return true;
        if (tracks.Count > 1) return false;
        var track = tracks[0];
        return track.PianoRoll.notes.Count == 0;
    }

    public void DiscardAll()
    {
        foreach (var track in tracks)
        {
            Destroy(track.gameObject);
        }
        tracks.Clear();
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

    public void Save()
    {
        var noteEditor = Globals<NoteEditor>.Instance;
        noteEditor.SaveNotesToTrack();
    }

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
        tempoEvents = new(song.tempoEvents);
    }
}

public struct SerializedSong
{
    public List<SerializedTrack> tracks;
    public List<TempoEvent> tempoEvents;

    public static SerializedSong Default()
    {
        return new()
        {
            tracks = new List<SerializedTrack>() {
                SerializedTrack.Default()
            },
            tempoEvents = new List<TempoEvent>() { new(0, 2), new(12, 3) }
        };
    }

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
