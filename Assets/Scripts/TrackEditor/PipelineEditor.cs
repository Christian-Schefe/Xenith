using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;

public class PipelineEditor : MonoBehaviour
{
    [SerializeField] private ReactiveButton backButton;
    [SerializeField] private TMPro.TextMeshProUGUI trackNameText;

    private void OnEnable()
    {
        backButton.OnClick += OnBackButtonClick;
    }

    private void OnDisable()
    {
        backButton.OnClick -= OnBackButtonClick;
    }

    private void Start()
    {
        var main = Globals<Main>.Instance;
        var editingTrack = new NestedReactive<ReactiveSong, ReactiveTrack>(main.app.openSong, song => song?.editingPipeline);
        editingTrack.AddAndCall(OnOpenTrackChanged);
    }

    private void OnOpenTrackChanged(ReactiveTrack track)
    {
        if (track != null)
        {
            gameObject.SetActive(true);
            trackNameText.text = track.name.Value;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnBackButtonClick()
    {
        var main = Globals<Main>.Instance;
        main.app.openSong.Value.editingPipeline.Value = null;
    }
}