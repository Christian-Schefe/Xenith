using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ActionMenu
{
    public class TabButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image img;
        [SerializeField] private Color normalColor, selectedColor;
        [SerializeField] private Button closeButton;

        private int index;
        private System.Action<int> onClick;
        private System.Action<int> onClose;

        public void Initialize(int index, string text, System.Action<int> onClick, System.Action<int> onClose)
        {
            this.index = index;
            this.onClick = onClick;
            this.onClose = onClose;
            this.text.text = text;
            SetSelected(false);
        }

        public void SetIndex(int index)
        {
            this.index = index;
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(OnClose);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(OnClose);
        }

        public void SetSelected(bool selected)
        {
            img.color = selected ? selectedColor : normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onClick?.Invoke(index);
            }
        }

        public void OnClose()
        {
            onClose?.Invoke(index);
        }
    }
}
