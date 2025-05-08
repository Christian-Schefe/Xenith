using DSP;
using NodeGraph;
using PianoRoll;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Track : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI trackNameText;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button setInstrumentButton;
    [SerializeField] private Button openButton;
    [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;

    private int trackIndex;
    private string trackName;
    private NodeResource instrument;
    private bool isMuted;
    private bool isSoloed;
    private float volume;
    private float pan;

    private SerializedPianoRoll serializedPianoRoll;
    public SerializedPianoRoll PianoRoll { get => serializedPianoRoll; set => serializedPianoRoll = value; }

    public void Initialize(int trackIndex)
    {
        this.trackIndex = trackIndex;
        trackName = "New Track";
        instrument = new("Piano", "piano", false);
        isMuted = false;
        isSoloed = false;
        volume = 0.5f;
        pan = 0.0f;
        serializedPianoRoll = new SerializedPianoRoll()
        {
            notes = new(),
        };
        UpdateUI();
    }

    private void OnEnable()
    {
        muteButton.onClick.AddListener(OnMuteButtonClick);
        soloButton.onClick.AddListener(OnSoloButtonClick);
        setInstrumentButton.onClick.AddListener(OnSetInstrumentButtonClick);
        openButton.onClick.AddListener(OnOpenClick);
    }

    private void OnDisable()
    {
        muteButton.onClick.RemoveListener(OnMuteButtonClick);
        soloButton.onClick.RemoveListener(OnSoloButtonClick);
        setInstrumentButton.onClick.RemoveListener(OnSetInstrumentButtonClick);
        openButton.onClick.RemoveListener(OnOpenClick);
    }

    private void OnMuteButtonClick()
    {
        isMuted = !isMuted;
    }

    private void OnSoloButtonClick()
    {
        isSoloed = !isSoloed;
        if (isSoloed)
        {
            isMuted = false;
        }
    }

    private void OnOpenClick()
    {
        var noteEditor = Globals<NoteEditor>.Instance;
        noteEditor.SetActiveTrack(trackIndex);
    }

    private void OnSetInstrumentButtonClick()
    {
        var database = Globals<GraphDatabase>.Instance;
        var graphs = database.GetGraphs().ToList();
        var index = graphs.FindIndex(g => g.id == instrument);
        index = (index + 1) % graphs.Count;
        instrument = graphs[index].id;
        UpdateUI();
    }

    private void UpdateUI()
    {
        trackNameText.text = trackName;
        volumeSlider.value = volume;
        instrumentNameText.text = instrument.displayName;
    }

    public SerializedTrack Serialize()
    {
        return new SerializedTrack
        {
            name = trackName,
            instrument = instrument,
            isMuted = isMuted,
            isSoloed = isSoloed,
            volume = volume,
            pan = pan,
            serializedPianoRoll = serializedPianoRoll,
        };
    }

    public void Deserialize(int index, SerializedTrack track)
    {
        Initialize(index);
        trackName = track.name;
        instrument = track.instrument;
        isMuted = track.isMuted;
        isSoloed = track.isSoloed;
        volume = track.volume;
        pan = track.pan;
        serializedPianoRoll = track.serializedPianoRoll;
        UpdateUI();
    }
}

public struct SerializedTrack
{
    public string name;
    public NodeResource instrument;
    public bool isMuted;
    public bool isSoloed;
    public float volume;
    public float pan;
    public SerializedPianoRoll serializedPianoRoll;

    public static SerializedTrack Default()
    {
        return new SerializedTrack
        {
            name = "New Track",
            instrument = new NodeResource("Piano", "piano", false),
            isMuted = false,
            isSoloed = false,
            volume = 0.5f,
            pan = 0.0f,
            serializedPianoRoll = new SerializedPianoRoll()
            {
                notes = new(),
            },
        };
    }

    public readonly AudioNode BuildAudioNode(float startTime, List<TempoEvent> tempoEvents)
    {
        var graphDatabase = Globals<GraphDatabase>.Instance;
        if (!graphDatabase.GetNodeFromTypeId(instrument, null, out var audioNode))
        {
            throw new System.Exception($"Failed to create audio node of type {instrument}");
        }
        var sequencer = new Sequencer(startTime, serializedPianoRoll.GetNotes(tempoEvents), () => audioNode.Clone());
        return sequencer;
    }
}
