using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class TrackUI : MonoBehaviour, IReactor<ReactiveTrack>
{
    [SerializeField] private TMPro.TMP_InputField trackNameText;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMPro.TextMeshProUGUI volumeText;
    [SerializeField] private TMPro.TextMeshProUGUI panText;
    [SerializeField] private ReactiveKnob panKnob;
    [SerializeField] private ReactiveToggleButton muteButton;
    [SerializeField] private ReactiveToggleButton soloButton;
    [SerializeField] private ReactiveToggleButton bgVisibleButton;
    [SerializeField] private ReactiveButton setInstrumentButton;
    [SerializeField] private ReactiveButton editPipelineButton;
    [SerializeField] private ReactiveButton openButton;
    [SerializeField] private ReactiveButton deleteButton;
    [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;

    private ReactiveTrack track;
    private IReactive<ReactiveTrack> activeTrack;

    public void Bind(ReactiveTrack track)
    {
        this.track = track;
        track.name.AddAndCall(OnNameChanged);
        track.volume.AddAndCall(OnVolumeChanged);
        panKnob.Bind(track.pan, -1, 1);
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
        panKnob.Unbind();
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
        setInstrumentButton.OnClick += OnSetInstrumentButtonClick;
        editPipelineButton.OnClick += OnEditPipelineButtonClick;
        deleteButton.OnClick += OnDeleteClick;
        openButton.AddListener(OnOpenClick);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        trackNameText.onEndEdit.AddListener(OnTrackNameInputEndEdit);
    }

    private void OnDisable()
    {
        setInstrumentButton.OnClick -= OnSetInstrumentButtonClick;
        editPipelineButton.OnClick -= OnEditPipelineButtonClick;
        deleteButton.OnClick -= OnDeleteClick;
        openButton.RemoveListener(OnOpenClick);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        trackNameText.onEndEdit.RemoveListener(OnTrackNameInputEndEdit);
    }

    private void OnSoloChanged(bool _)
    {
        if (track.isSoloed.Value)
        {
            track.isMuted.Value = false;
        }
    }

    private void OnOpenClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.activeTrack.Value = track;
    }

    private void OnDeleteClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.DeleteTrack(track);
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

    private void OnVolumeSliderChanged(float alpha)
    {
        track.volume.Value = alpha * alpha * 2;
    }

    private void OnVolumeChanged(float volume)
    {
        var newAlpha = Mathf.Sqrt(volume * 0.5f);
        volumeSlider.value = newAlpha;
        float dB = 20f * Mathf.Log10(volume);
        var dBString = dB.ToString("F1");
        volumeText.text = $"{(dBString.StartsWith("-") ? "" : "+")}{dBString} dB";
    }

    private void OnInstrumentChanged(DTO.NodeResource instrument)
    {
        instrumentNameText.text = instrument.id;
    }

    private void OnActiveTrackChanged(ReactiveTrack activeTrack)
    {
        bgImage.outline = activeTrack == track;
    }

    private void OnPanChanged(float pan)
    {
        var panInt = Mathf.RoundToInt(pan * 100);
        panText.text = $"{(panInt > 0 ? "+" : "")}{panInt}";
    }
}
