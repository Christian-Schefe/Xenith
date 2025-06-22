using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;

public class PipelineEditor : MonoBehaviour
{
    [SerializeField] private ReactiveButton backButton;
    [SerializeField] private TMPro.TextMeshProUGUI trackNameText;
    [SerializeField] private EffectUI effectPrefab;
    [SerializeField] private RectTransform effectContainer;
    [SerializeField] private ReactiveButton addEffectButton;

    private ReactiveUIBinder<ReactiveEffect, EffectUI> effectBinder;
    public ReactiveTrackBase track;

    private void OnEnable()
    {
        backButton.OnClick += OnBackButtonClick;
        addEffectButton.OnClick += OnAddEffect;
    }

    private void OnDisable()
    {
        backButton.OnClick -= OnBackButtonClick;
        addEffectButton.OnClick -= OnAddEffect;
    }

    private void Awake()
    {
        effectBinder = new(null, _ => Instantiate(effectPrefab, effectContainer), effect => Destroy(effect.gameObject));
    }

    private void Start()
    {
        var main = Globals<Main>.Instance;
        var editingTrack = new NestedReactive<ReactiveSong, ReactiveTrackBase>(main.app.openSong, song => song?.editingPipeline);
        editingTrack.AddAndCall(OnOpenTrackChanged);
    }

    private void OnAddEffect()
    {
        var fileBrowser = Globals<FileBrowser>.Instance;
        fileBrowser.OpenEffect(effect =>
        {
            if (track == null) return;
            track.effects.Add(new ReactiveEffect(effect));
        }, () => { });
    }

    private void OnOpenTrackChanged(ReactiveTrackBase track)
    {
        if (track != null)
        {
            gameObject.SetActive(true);
            trackNameText.text = track.name.Value;
            effectBinder.ChangeSource(track.effects);
        }
        else
        {
            gameObject.SetActive(false);
            effectBinder.ChangeSource(null);
        }
        this.track = track;
    }

    private void OnBackButtonClick()
    {
        var main = Globals<Main>.Instance;
        main.app.openSong.Value.editingPipeline.Value = null;
    }
}