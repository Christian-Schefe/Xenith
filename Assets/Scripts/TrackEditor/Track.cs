using NodeGraph;
using PianoRoll;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Track : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI trackNameText;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button setInstrumentButton;
    [SerializeField] private Button openButton;
    [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;

    private DTO.Track track;

    public void Initialize(DTO.Track track)
    {
        this.track = track;
        UpdateUI();
    }

    private void OnEnable()
    {
        muteButton.onClick.AddListener(OnMuteButtonClick);
        soloButton.onClick.AddListener(OnSoloButtonClick);
        setInstrumentButton.onClick.AddListener(OnSetInstrumentButtonClick);
        openButton.onClick.AddListener(OnOpenClick);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnDisable()
    {
        muteButton.onClick.RemoveListener(OnMuteButtonClick);
        soloButton.onClick.RemoveListener(OnSoloButtonClick);
        setInstrumentButton.onClick.RemoveListener(OnSetInstrumentButtonClick);
        openButton.onClick.RemoveListener(OnOpenClick);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void OnMuteButtonClick()
    {
        track.isMuted = !track.isMuted;
    }

    private void OnSoloButtonClick()
    {
        track.isSoloed = !track.isSoloed;
        if (track.isSoloed)
        {
            track.isMuted = false;
        }
    }

    private void OnVolumeChanged(float volume)
    {
        track.SetVolume(volume);
    }

    private void OnOpenClick()
    {
        var noteEditor = Globals<NoteEditor>.Instance;
        noteEditor.ShowTrack(track);
        UpdateUI();
    }

    private void OnSetInstrumentButtonClick()
    {
        var database = Globals<GraphDatabase>.Instance;
        var graphs = database.GetGraphs().ToList();
        var index = graphs.FindIndex(g => g.Key.ToResource() == track.instrument);
        index = (index + 1) % graphs.Count;
        track.instrument = graphs[index].Key.ToResource();
        UpdateUI();
    }

    private void Update()
    {
        var noteEditor = Globals<NoteEditor>.Instance;
        var isActive = noteEditor.ActiveTrack == track;

        bgImage.outline = isActive;
    }

    private void UpdateUI()
    {
        Update();
        trackNameText.text = track.name;
        volumeSlider.value = track.volume;
        instrumentNameText.text = track.instrument.id;
    }
}
