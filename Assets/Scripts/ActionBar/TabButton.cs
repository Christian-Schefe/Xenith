using Colors;
using ReactiveData.Core;
using ReactiveData.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ActionMenu
{
    public class TabButton : MonoBehaviour, IReactor<ActionTab>
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private ReactiveButton button;
        [SerializeField] private ColorApplierReactiveButton colorApplier;
        [SerializeField] private ColorPaletteColor normalNormalColor, normalHoveredColor, normalPressedColor;
        [SerializeField] private ColorPaletteColor selectedNormalColor, selectedHoveredColor, selectedPressedColor;
        [SerializeField] private Button closeButton;

        private System.Action<ActionTab> onClick;
        private System.Action<ActionTab> onClose;

        private Reactive<ColorPaletteColor> normalColor, hoveredColor, pressedColor;
        private IReactive<ActionTab> selectedTab;

        private ActionTab tab;

        private void Awake()
        {
            normalColor = new(normalNormalColor);
            hoveredColor = new(normalHoveredColor);
            pressedColor = new(normalPressedColor);
            colorApplier.Bind(normalColor, hoveredColor, pressedColor, null, null, null, null, null, null);
        }

        public void Initialize(IReactive<ActionTab> selectedTab, System.Action<ActionTab> onClick, System.Action<ActionTab> onClose)
        {
            this.selectedTab = selectedTab;
            this.onClick = onClick;
            this.onClose = onClose;

            selectedTab.OnChanged += OnSelectedTabChanged;
        }

        private void OnDestroy()
        {
            if (selectedTab != null)
            {
                selectedTab.OnChanged -= OnSelectedTabChanged;
            }
        }

        public void OnNameChanged(string name)
        {
            text.text = name;
        }

        private void OnEnable()
        {
            button.AddListener(OnClick);
            closeButton.onClick.AddListener(OnClose);
        }

        private void OnDisable()
        {
            button.RemoveListener(OnClick);
            closeButton.onClick.RemoveListener(OnClose);
        }

        public void OnSelectedTabChanged(ActionTab selected)
        {
            if (selected == tab)
            {
                normalColor.Value = selectedNormalColor;
                hoveredColor.Value = selectedHoveredColor;
                pressedColor.Value = selectedPressedColor;
            }
            else
            {
                normalColor.Value = normalNormalColor;
                hoveredColor.Value = normalHoveredColor;
                pressedColor.Value = normalPressedColor;
            }
        }

        private void OnClick()
        {
            onClick?.Invoke(tab);
        }

        public void OnClose()
        {
            onClose?.Invoke(tab);
        }

        public void Bind(ActionTab data)
        {
            tab = data;
            data.tab.name.AddAndCall(OnNameChanged);
        }

        public void Unbind()
        {
            tab.tab.name.Remove(OnNameChanged);
        }
    }
}
