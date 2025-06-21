using ReactiveData.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ReactiveData.UI
{
    public class ReactiveToggleButton : ReactiveButton
    {
        public Color offNormal, offHovered, offPressed;
        public Image iconImage;
        public Sprite offIcon, onIcon;

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
            if (image != null)
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
            if (text != null)
            {
                text.color = state switch
                {
                    State.Normal => textNormal,
                    State.Hovered => textHovered,
                    State.Pressed => textPressed,
                    _ => text.color
                };
            }
            if (iconImage != null)
            {
                iconImage.sprite = toggle.Value ? onIcon : offIcon;
                iconImage.color = state switch
                {
                    State.Normal => textNormal,
                    State.Hovered => textHovered,
                    State.Pressed => textPressed,
                    _ => text.color
                };
            }
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
