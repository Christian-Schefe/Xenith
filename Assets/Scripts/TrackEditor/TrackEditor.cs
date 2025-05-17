using ActionMenu;
using DSP;
using DTO;
using PianoRoll;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TrackEditor : MonoBehaviour
{
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private RectTransform trackContainer;

    [SerializeField] private Track trackPrefab;

    private readonly List<Track> tracks = new();

    private TopLevelAction trackAction = null;
    private bool isVisible = false;

    private TopLevelAction BuildAction()
    {
        return new("Track", new() { new ActionType.Button("Add Track", () => AddTrack()) });
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
        noteEditor.SetSong(song);
        noteEditor.ShowTrack(song.tracks[0]);
        pianoRollVisuals.SetVisible(true);

        for (int i = 0; i < song.tracks.Count; i++)
        {
            var trackInstance = Instantiate(trackPrefab, trackContainer);
            trackInstance.Initialize(song.tracks[i]);
            tracks.Add(trackInstance);
        }
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
        noteEditor.HideTrack();
        pianoRollVisuals.SetVisible(false);

        foreach (var track in tracks)
        {
            Destroy(track.gameObject);
        }
        tracks.Clear();
    }

    private void AddTrack()
    {
        var main = Globals<Main>.Instance;
        var song = main.CurrentSong;
        var track = DTO.Track.Default();
        song.tracks.Add(track);
        var trackInstance = Instantiate(trackPrefab, trackContainer);
        trackInstance.Initialize(track);
        tracks.Add(trackInstance);
    }
}
