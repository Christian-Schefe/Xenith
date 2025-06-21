using ActionMenu;
using FileFormat;
using PianoRoll;
using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;
using UnityEngine.UI;

public class TrackEditor : MonoBehaviour
{
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private RectTransform trackContainer;
    [SerializeField] private TrackUI trackPrefab;
    [SerializeField] private ReactiveButton addTrackButton;
    [SerializeField] private MasterTrackUI masterTrackUI;

    private TopLevelAction trackAction = null;

    private ReactiveSong song;
    private ReactiveUIBinder<ReactiveTrack, TrackUI> trackBinder = null;

    public ReactiveSong Song => song;

    private bool isVisible;

    private void Awake()
    {
        addTrackButton.OnClick += AddTrack;
    }

    private void Start()
    {
        var main = Globals<Main>.Instance;
        main.app.openSong.AddAndCall(OnOpenSongChanged);
    }

    private void InitializeBinder()
    {
        trackBinder ??= new(null, _ =>
        {
            var instance = Instantiate(trackPrefab, trackContainer);
            addTrackButton.transform.SetAsLastSibling();
            return instance;
        }, track => Destroy(track.gameObject));
    }

    private void BindSong(ReactiveSong song)
    {
        this.song = song;
        InitializeBinder();
        trackBinder.ChangeSource(song.tracks);
        masterTrackUI.Bind(song.master.Value);
    }

    private void UnbindSong()
    {
        song = null;
        InitializeBinder();
        trackBinder.ChangeSource(null);
        masterTrackUI.Unbind();
    }

    private TopLevelAction BuildAction()
    {
        return new("Tracks", 1, new() {
            new ActionType.Button("Import Midi", () => ImportMidi()),
        });
    }

    private void OnOpenSongChanged(ReactiveSong song)
    {
        bool visible = song != null;
        uiRoot.gameObject.SetActive(visible);
        trackAction ??= BuildAction();
        var actionBar = Globals<ActionBar>.Instance;

        if (visible)
        {
            if (!isVisible)
            {
                actionBar.AddActions(new() { trackAction });
            }
            BindSong(song);
        }
        else
        {
            actionBar.RemoveAction(trackAction);
            UnbindSong();
        }
        isVisible = visible;
    }

    private void AddTrack()
    {
        var newTrack = ReactiveTrack.Default;
        song.tracks.Add(newTrack);
        song.activeTrack.Value = newTrack;
    }

    private void ImportMidi()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenFile(path =>
        {
            try
            {
                var midiFile = SimpleMidi.ReadFromFile(path);
                AddMidiData(midiFile);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to read midi file: {e}");
            }
        }, () => { });
    }

    private void AddMidiData(SimpleMidi midi)
    {
        foreach (var midiTrack in midi.tracks)
        {
            var track = ReactiveTrack.Default;
            track.name.Value = midiTrack.name;
            track.keySignature.Value = MusicKey.FromMidi(midiTrack.keySignature.key, midiTrack.keySignature.isMajor);
            foreach (var midiNote in midiTrack.notes)
            {
                track.notes.Add(new ReactiveNote(midiNote.start, midiNote.pitch, midiNote.velocity, midiNote.duration));
            }
            song.tracks.Add(track);
        }

        song.tempoEvents.Clear();
        foreach (var tempo in midi.tempoEvents)
        {
            song.tempoEvents.Add(new ReactiveTempoEvent(tempo.time, tempo.bpm / 60f));
        }
    }
}
