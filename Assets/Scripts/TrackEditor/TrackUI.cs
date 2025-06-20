using NodeGraph;
using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrackUI : MonoBehaviour, IReactor<ReactiveTrack>
{
    [SerializeField] private TMPro.TMP_InputField trackNameText;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider panSlider;
    [SerializeField] private ReactiveToggleButton muteButton;
    [SerializeField] private ReactiveToggleButton soloButton;
    [SerializeField] private ReactiveToggleButton bgVisibleButton;
    [SerializeField] private Button setInstrumentButton;
    [SerializeField] private Button editPipelineButton;
    [SerializeField] private ReactiveButton openButton;
    [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;

    private ReactiveTrack track;
    private IReactive<ReactiveTrack> activeTrack;

    public void Bind(ReactiveTrack track)
    {
        this.track = track;
        track.name.AddAndCall(OnNameChanged);
        track.volume.AddAndCall(OnVolumeChanged);
        track.pan.AddAndCall(OnPanChanged);
        track.instrument.AddAndCall(OnInstrumentChanged);

        muteButton.Bind(track.isMuted);
        soloButton.Bind(track.isSoloed);
        bgVisibleButton.Bind(track.isBGVisible);

        track.isSoloed.AddAndCall(OnSoloChanged);

        var trackEditor = Globals<TrackEditor>.Instance;
        activeTrack = trackEditor.Song.activeTrack;
        activeTrack.OnChanged += OnActiveTrackChanged;
        OnActiveTrackChanged(activeTrack.Value);
    }

    public void Unbind()
    {
        track.name.Remove(OnNameChanged);
        track.volume.Remove(OnVolumeChanged);
        track.pan.Remove(OnPanChanged);
        track.instrument.Remove(OnInstrumentChanged);

        muteButton.Unbind();
        soloButton.Unbind();
        bgVisibleButton.Unbind();

        track.isSoloed.Remove(OnSoloChanged);

        var trackEditor = Globals<TrackEditor>.Instance;
        activeTrack.OnChanged -= OnActiveTrackChanged;
        activeTrack = null;
        track = null;
    }

    private void OnEnable()
    {
        setInstrumentButton.onClick.AddListener(OnSetInstrumentButtonClick);
        editPipelineButton.onClick.AddListener(OnEditPipelineButtonClick);
        openButton.AddListener(OnOpenClick);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        panSlider.onValueChanged.AddListener(OnPanSliderChanged);
        trackNameText.onEndEdit.AddListener(OnTrackNameInputEndEdit);
    }

    private void OnDisable()
    {
        setInstrumentButton.onClick.RemoveListener(OnSetInstrumentButtonClick);
        editPipelineButton.onClick.RemoveListener(OnEditPipelineButtonClick);
        openButton.RemoveListener(OnOpenClick);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        panSlider.onValueChanged.RemoveListener(OnPanSliderChanged);
        trackNameText.onEndEdit.RemoveListener(OnTrackNameInputEndEdit);
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

    private void OnPanSliderChanged(float pan)
    {
        track.pan.Value = pan * 2 - 1;
    }

    private void OnOpenClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.Value = track;
    }

    private void OnSetInstrumentButtonClick()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenInstrument(inst =>
        {
            track.instrument.Value = inst;
        }, () => { });
    }

    private void OnEditPipelineButtonClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.editingPipeline.Value = track;
    }

    private void OnTrackNameInputEndEdit(string val)
    {
        if (track == null) return;
        if (string.IsNullOrEmpty(val))
        {
            trackNameText.text = track.name.Value;
            return;
        }
        track.name.Value = val;
    }

    private void OnNameChanged(string name)
    {
        trackNameText.text = name;
    }

    private void OnVolumeChanged(float volume)
    {
        volumeSlider.value = volume;
    }

    private void OnPanChanged(float pan)
    {
        panSlider.value = pan * 0.5f + 0.5f;
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
