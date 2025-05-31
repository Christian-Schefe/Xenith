using UnityEngine;
using UnityEngine.EventSystems;

namespace ReactiveData.UI
{
    public class ReactiveButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] protected UIImage image;
        [SerializeField] protected Color normal, hovered, pressed;
        [SerializeField] protected Color outlineNormal, outlineHovered, outlinePressed;

        private System.Action onClick;

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

        public void AddListener(System.Action onClick)
        {
            this.onClick += onClick;
        }

        public void RemoveListener(System.Action onClick)
        {
            this.onClick -= onClick;
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
            onClick?.Invoke();
        }

        protected void UpdateState()
        {
            var state = isPressed ? State.Pressed : (isPointerInside ? State.Hovered : State.Normal);
            UpdateUI(state);
        }

        protected virtual void UpdateUI(State state)
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
    }
}
