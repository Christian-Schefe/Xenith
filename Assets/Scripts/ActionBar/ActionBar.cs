using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ActionMenu
{
    public class ActionBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform actionParent;
        [SerializeField] private TMPro.TextMeshProUGUI titleText;
        [SerializeField] private ActionButton actionButtonPrefab;
        [SerializeField] private ActionDropdown actionDropdownPrefab;

        private List<TopLevelAction> actions;
        private readonly List<ActionButton> buttons = new();

        private ActionDropdown dropdown;
        private int openButtonIndex = -1;
        private bool isMouseInside = false;

        public void SetTitle(string title)
        {
            titleText.text = title;
        }

        public void SetActions(List<TopLevelAction> actions)
        {
            this.actions = actions;

            for (int i = 0; i < actions.Count; i++)
            {
                var index = i;
                var action = actions[i];
                var instance = Instantiate(actionButtonPrefab, actionParent);
                instance.SetData(action.name, () => OnActionButtonClick(index));
                buttons.Add(instance);
            }
        }

        private void OnActionButtonClick(int buttonIndex)
        {
            bool pressedOpenMenu = openButtonIndex == buttonIndex;
            CloseDropdown();
            if (pressedOpenMenu) return;

            var instance = Instantiate(actionDropdownPrefab, transform);
            instance.Open(buttons[buttonIndex].GetComponent<RectTransform>(), actions[buttonIndex].actions);
            dropdown = instance;
            openButtonIndex = buttonIndex;
        }

        public void CloseDropdown()
        {
            if (dropdown != null)
            {
                dropdown.Close();
                Destroy(dropdown.gameObject);
                dropdown = null;
            }
            openButtonIndex = -1;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isMouseInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isMouseInside = false;
        }

        private void Update()
        {
            if (!isMouseInside && Input.GetMouseButtonDown(0))
            {
                CloseDropdown();
            }
        }
    }

    public class TopLevelAction
    {
        public string name;
        public List<ActionType> actions;

        public TopLevelAction(string name, List<ActionType> actions)
        {
            this.name = name;
            this.actions = actions;
        }
    }
}
