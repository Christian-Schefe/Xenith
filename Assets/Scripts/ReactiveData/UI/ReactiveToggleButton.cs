using ReactiveData.Core;
using UnityEngine;

namespace ReactiveData.UI
{
    public class ReactiveToggleButton : ReactiveButton
    {
        [SerializeField] private Color offNormal, offHovered, offPressed;

        private Reactive<bool> toggle = new(false);

        public void Bind(Reactive<bool> toggle)
        {
            if (this.toggle != null)
            {
                Unbind();
            }

            AddListener(() => toggle.Value = !toggle.Value);
            this.toggle = toggle;
            toggle.AddAndCall(OnToggleChanged);
        }

        protected override void UpdateUI(State state)
        {
            image.color = state switch
            {
                State.Normal => toggle.Value ? normal : offNormal,
                State.Hovered => toggle.Value ? hovered : offHovered,
                State.Pressed => toggle.Value ? pressed : offPressed,
                _ => image.color
            };
            image.outlineColor = state switch
            {
                State.Normal => outlineNormal,
                State.Hovered => outlineHovered,
                State.Pressed => outlinePressed,
                _ => image.outlineColor
            };
            image.outline = image.outlineColor.a > 0.01f;
        }

        public void Unbind()
        {
            toggle.Remove(OnToggleChanged);
        }

        private void OnToggleChanged(bool value)
        {
            UpdateState();
        }
    }
}
