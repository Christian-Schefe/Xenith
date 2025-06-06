using NodeGraph;
using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrackUI : MonoBehaviour, IReactor<ReactiveTrack>
{
    [SerializeField] private TMPro.TextMeshProUGUI trackNameText;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private ReactiveToggleButton muteButton;
    [SerializeField] private ReactiveToggleButton soloButton;
    [SerializeField] private ReactiveToggleButton bgVisibleButton;
    [SerializeField] private Button setInstrumentButton;
    [SerializeField] private ReactiveButton openButton;
    [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;

    private ReactiveTrack track;

    public void Bind(ReactiveTrack track)
    {
        this.track = track;
        track.name.AddAndCall(OnNameChanged);
        track.volume.AddAndCall(OnVolumeChanged);
        track.instrument.AddAndCall(OnInstrumentChanged);

        muteButton.Bind(track.isMuted);
        soloButton.Bind(track.isSoloed);
        bgVisibleButton.Bind(track.isBGVisible);

        track.isSoloed.AddAndCall(OnSoloChanged);

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.AddAndCall(OnActiveTrackChanged);
    }

    public void Unbind()
    {
        track.name.Remove(OnNameChanged);
        track.volume.Remove(OnVolumeChanged);
        track.instrument.Remove(OnInstrumentChanged);

        muteButton.Unbind();
        soloButton.Unbind();
        bgVisibleButton.Unbind();

        track.isSoloed.Remove(OnSoloChanged);

        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.Remove(OnActiveTrackChanged);
        track = null;
    }

    private void OnEnable()
    {
        setInstrumentButton.onClick.AddListener(OnSetInstrumentButtonClick);
        openButton.AddListener(OnOpenClick);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private void OnDisable()
    {
        setInstrumentButton.onClick.RemoveListener(OnSetInstrumentButtonClick);
        openButton.RemoveListener(OnOpenClick);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
    }

    private void OnSoloChanged(bool _)
    {
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
        var index = graphs.FindIndex(g => new DTO.NodeResource(g.Key, false) == track.instrument.Value);
        index = (index + 1) % graphs.Count;
        track.instrument.Value = new DTO.NodeResource(graphs[index].Key, false);
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
