using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReactiveData.UI
{
    public class ReactiveButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] protected UIImage image;
        [SerializeField] protected TMP_Text text;
        public Color normal, hovered, pressed;
        public Color outlineNormal, outlineHovered, outlinePressed;
        public Color textNormal, textHovered, textPressed;

        public System.Action OnClick;

        private bool isPointerInside;
        private bool isPressed;

        protected enum State
        {
            Normal, Hovered, Pressed
        }

        private void Awake()
        {
            UpdateState();
        }

        private void OnDisable()
        {
            isPointerInside = false;
            isPressed = false;
            UpdateState();
        }

        public void AddListener(System.Action onClick)
        {
            OnClick += onClick;
        }

        public void RemoveListener(System.Action onClick)
        {
            OnClick -= onClick;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isPointerInside)
            {
                isPressed = true;
            }
            UpdateState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            UpdateState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            UpdateState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            UpdateState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }

        public void UpdateState()
        {
            var state = isPressed ? State.Pressed : (isPointerInside ? State.Hovered : State.Normal);
            UpdateUI(state);
        }

        protected virtual void UpdateUI(State state)
        {
            if (image != null)
            {
                image.color = state switch
                {
                    State.Normal => normal,
                    State.Hovered => hovered,
                    State.Pressed => pressed,
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
        }
    }
}
