using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;
using UnityEngine.UI;

public class MasterTrackUI : MonoBehaviour, IReactor<ReactiveMasterTrack>
{
    [SerializeField] private ReactiveButton editPipelineButton;
    [SerializeField] private UIImage bgImage;
    [SerializeField] private Slider volumeSlider;
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
    }

    public void Unbind()
    {
        masterTrack.volume.Remove(OnVolumeChanged);
        panKnob.Unbind();
        masterTrack = null;
    }

    private void OnVolumeSliderChanged(float volume)
    {
        masterTrack.volume.Value = volume;
    }

    private void OnEditPipelineButtonClick()
    {
        var trackEditor = Globals<TrackEditor>.Instance;
        trackEditor.Song.editingPipeline.Value = masterTrack;
    }

    private void OnVolumeChanged(float volume)
    {
        volumeSlider.value = volume;
    }
}