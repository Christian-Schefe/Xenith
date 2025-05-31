using ActionMenu;
using DSP;
using DTO;
using FileFormat;
using PianoRoll;
using ReactiveData.App;
using ReactiveData.Core;
using UnityEngine;
using UnityEngine.UI;

public class TrackEditor : MonoBehaviour
{
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private RectTransform trackContainer;
    [SerializeField] private TrackUI trackPrefab;
    [SerializeField] private RectTransform addTrackButton;

    private TopLevelAction trackAction = null;
    private bool isVisible = false;

    private ReactiveSong song;
    private ReactiveListBinder<ReactiveTrack, TrackUI> trackBinder = null;

    public ReactiveSong Song => song;

    private void Awake()
    {
        addTrackButton.GetComponentInChildren<Button>().onClick.AddListener(AddTrack);
    }

    private void BindSong(ReactiveSong song)
    {
        this.song = song;
        trackBinder = new(song.tracks, _ =>
        {
            var instance = Instantiate(trackPrefab, trackContainer);
            addTrackButton.SetAsLastSibling();
            return instance;
        }, track => Destroy(track.gameObject));
    }

    private void UnbindSong()
    {
        trackBinder?.Dispose();
        trackBinder = null;
        song = null;
    }

    private TopLevelAction BuildAction()
    {
        return new("Tracks", new() {
            new ActionType.Button("Import Midi", () => ImportMidi()),
        });
    }

    public void Show()
    {
        if (isVisible)
        {
            Debug.LogWarning("Track editor is already visible");
            return;
        }
        isVisible = true;
        uiRoot.gameObject.SetActive(true);

        var actionBar = Globals<ActionBar>.Instance;
        var noteEditor = Globals<NoteEditor>.Instance;
        var pianoRollVisuals = Globals<PianoRollVisuals>.Instance;
        var main = Globals<Main>.Instance;
        var song = main.CurrentSong;

        trackAction ??= BuildAction();

        actionBar.AddActions(new() { trackAction });
        noteEditor.Show(song);
        pianoRollVisuals.SetVisible(true);

        BindSong(song);
    }

    public void Hide()
    {
        isVisible = false;
        uiRoot.gameObject.SetActive(false);

        var actionBar = Globals<ActionBar>.Instance;
        var noteEditor = Globals<NoteEditor>.Instance;
        var pianoRollVisuals = Globals<PianoRollVisuals>.Instance;

        trackAction ??= BuildAction();

        actionBar.RemoveAction(trackAction);
        noteEditor.Hide();
        pianoRollVisuals.SetVisible(false);

        UnbindSong();
    }

    private void AddTrack()
    {
        song.tracks.Add(ReactiveTrack.Default);
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
