using NodeGraph;
using ReactiveData.App;
using ReactiveData.Core;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrackUI : MonoBehaviour, IReactor<ReactiveTrack>
{
    [SerializeField] private TMPro.TextMeshProUGUI trackNameText;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button setInstrumentButton;
    [SerializeField] private Button openButton;
    [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;

    private ReactiveTrack track;

    public void Bind(ReactiveTrack track)
    {
        this.track = track;
        track.name.AddAndCall(OnNameChanged);
        track.volume.AddAndCall(OnVolumeChanged);
        track.instrument.AddAndCall(OnInstrumentChanged);

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.AddAndCall(OnActiveTrackChanged);
    }

    public void Unbind()
    {
        track.name.Remove(OnNameChanged);
        track.volume.Remove(OnVolumeChanged);
        track.instrument.Remove(OnInstrumentChanged);
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.Remove(OnActiveTrackChanged);
        track = null;
    }

    private void OnEnable()
    {
        muteButton.onClick.AddListener(OnMuteButtonClick);
        soloButton.onClick.AddListener(OnSoloButtonClick);
        setInstrumentButton.onClick.AddListener(OnSetInstrumentButtonClick);
        openButton.onClick.AddListener(OnOpenClick);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private void OnDisable()
    {
        muteButton.onClick.RemoveListener(OnMuteButtonClick);
        soloButton.onClick.RemoveListener(OnSoloButtonClick);
        setInstrumentButton.onClick.RemoveListener(OnSetInstrumentButtonClick);
        openButton.onClick.RemoveListener(OnOpenClick);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
    }

    private void OnMuteButtonClick()
    {
        track.isMuted.Value = !track.isMuted.Value;
    }

    private void OnSoloButtonClick()
    {
        track.isSoloed.Value = !track.isSoloed.Value;
        if (track.isSoloed.Value)
        {
            track.isMuted.Value = false;
        }
    }

    private void OnVolumeSliderChanged(float volume)
    {
        track.volume.Value = volume;
    }

    private void OnOpenClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.Value = track;
    }

    private void OnSetInstrumentButtonClick()
    {
        var database = Globals<GraphDatabase>.Instance;
        var graphs = database.GetGraphs().ToList();
        var index = graphs.FindIndex(g => g.Key.ToResource() == track.instrument.Value);
        index = (index + 1) % graphs.Count;
        track.instrument.Value = graphs[index].Key.ToResource();
    }

    private void OnNameChanged(string name)
    {
        trackNameText.text = name;
    }

    private void OnVolumeChanged(float volume)
    {
        volumeSlider.value = volume;
    }

    private void OnInstrumentChanged(DTO.NodeResource instrument)
    {
        instrumentNameText.text = instrument.id;
    }

    private void OnActiveTrackChanged(ReactiveTrack activeTrack)
    {
        bgImage.outline = activeTrack == track;
    }
}
