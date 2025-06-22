using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;
using UnityEngine.UI;

public class MasterTrackUI : MonoBehaviour, IReactor<ReactiveMasterTrack>
{
    [SerializeField] private TMPro.TextMeshProUGUI volumeText;
    [SerializeField] private ReactiveButton editPipelineButton;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMPro.TextMeshProUGUI panText;
    [SerializeField] private ReactiveKnob panKnob;

    private ReactiveMasterTrack masterTrack;

    private void OnEnable()
    {
        editPipelineButton.OnClick += OnEditPipelineButtonClick;
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private void OnDisable()
    {
        editPipelineButton.OnClick -= OnEditPipelineButtonClick;
        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
    }

    public void Bind(ReactiveMasterTrack track)
    {
        masterTrack = track;
        masterTrack.volume.AddAndCall(OnVolumeChanged);
        panKnob.Bind(masterTrack.pan, -1, 1);
        masterTrack.pan.AddAndCall(OnPanChanged);
    }

    public void Unbind()
    {
        masterTrack.volume.Remove(OnVolumeChanged);
        panKnob.Unbind();
        masterTrack.pan.Remove(OnPanChanged);
        masterTrack = null;
    }

    private void OnVolumeSliderChanged(float alpha)
    {
        masterTrack.volume.Value = alpha * alpha * 2;
    }

    private void OnVolumeChanged(float volume)
    {
        var newAlpha = Mathf.Sqrt(volume * 0.5f);
        volumeSlider.value = newAlpha;
        float dB = 20f * Mathf.Log10(volume);
        var dBString = dB.ToString("F1");
        volumeText.text = $"{(dBString.StartsWith("-") ? "" : "+")}{dBString} dB";
    }

    private void OnEditPipelineButtonClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.editingPipeline.Value = masterTrack;
    }

    private void OnPanChanged(float pan)
    {
        var panInt = Mathf.RoundToInt(pan * 100);
        panText.text = $"{(panInt > 0 ? "+" : "")}{panInt}";
    }
}