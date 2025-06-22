using ReactiveData.App;
using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;

public class EffectUI : MonoBehaviour, IReactor<ReactiveEffect>
{
    [SerializeField] private TMPro.TextMeshProUGUI effectNameText;
    [SerializeField] private ReactiveButton deleteButton;

    private ReactiveEffect effect;

    private void OnEnable()
    {
        deleteButton.OnClick += Delete;
    }

    private void OnDisable()
    {
        deleteButton.OnClick -= Delete;
    }

    public void Bind(ReactiveEffect effect)
    {
        this.effect = effect;
        effect.effect.AddAndCall(OnEffectChange);
    }

    public void Unbind()
    {
        effect.effect.Remove(OnEffectChange);
        effect = null;
    }

    private void OnEffectChange(DTO.NodeResource effectResource)
    {
        effectNameText.text = effectResource.id;
    }

    private void Delete()
    {
        var pipelineEditor = Globals<PipelineEditor>.Instance;
        pipelineEditor.track.effects.Remove(effect);
    }
}