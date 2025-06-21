using ReactiveData.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReactiveData.UI
{
    public class ReactiveKnob : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] protected RectTransform pointer;

        [SerializeField] private float deltaAngle;
        [SerializeField] private float sensitivity = 1f;

        private IWritableReactive<float> value;
        private float min, max;

        private bool isDragging;
        private Vector2 startDragPosition;
        private float startDragValue;

        public void Bind(IWritableReactive<float> value, float min, float max)
        {
            this.value = value;
            this.min = min;
            this.max = max;
            value.OnChanged += OnValueChanged;
            OnValueChanged(value.Value);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (value == null) return;
            isDragging = true;
            startDragPosition = eventData.position;
            startDragValue = value.Value;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            Vector2 delta = eventData.position - startDragPosition;
            float deltaValue = delta.y * 0.01f * sensitivity;
            value.SetValue(Mathf.Clamp(startDragValue + deltaValue, min, max));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2 && value != null)
            {
                float midpoint = (min + max) / 2f;
                value.SetValue(midpoint);
            }
        }

        public void Unbind()
        {
            if (value != null)
            {
                value.OnChanged -= OnValueChanged;
            }
            value = null;
        }

        private void OnValueChanged(float newValue)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var alpha = Mathf.InverseLerp(min, max, value.Value);
            var angle = 90 + (0.5f - alpha) * deltaAngle;
            pointer.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
