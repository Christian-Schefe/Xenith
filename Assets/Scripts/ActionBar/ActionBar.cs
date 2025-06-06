using ReactiveData.App;
using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ActionMenu
{
    public class ActionBar : MonoBehaviour
    {
        [SerializeField] private RectTransform actionParent;
        [SerializeField] private RectTransform tabParent;
        [SerializeField] private TMPro.TextMeshProUGUI titleText;
        [SerializeField] private ActionButton actionButtonPrefab;
        [SerializeField] private TabButton tabButtonPrefab;
        [SerializeField] private ActionDropdown actionDropdownPrefab;

        public System.Action<IReactiveTab> onTabClick;
        public System.Action<IReactiveTab> onTabClose;

        private readonly List<TopLevelAction> actions = new();
        private readonly List<ActionButton> buttons = new();

        private IReactiveEnumerable<IReactiveTab> tabs;
        private IReactive<IReactiveTab> openTab;
        private ReactiveUIBinder<IReactiveTab, TabButton> tabButtons;

        private ActionDropdown dropdown;
        private int openButtonIndex = -1;

        private void Awake()
        {
            tabButtons = new(tabs, _ =>
            {
                var tabButton = Instantiate(tabButtonPrefab, tabParent);
                tabButton.Initialize(openTab, OnTabClick, OnTabClose);
                return tabButton;
            }, tab => Destroy(tab.gameObject));
        }

        public void SetTitle(string title)
        {
            titleText.text = title;
        }

        public void AddActions(List<TopLevelAction> actionList)
        {
            int offset = actions.Count;
            actions.AddRange(actionList);

            for (int i = 0; i < actionList.Count; i++)
            {
                var index = i + offset;
                var action = actions[index];
                var instance = Instantiate(actionButtonPrefab, actionParent);
                instance.SetData(action.name, () => OnActionButtonClick(index));
                buttons.Add(instance);
            }
        }

        public void RemoveAction(TopLevelAction action)
        {
            int index = actions.IndexOf(action);
            if (openButtonIndex >= index)
            {
                CloseDropdown();
            }
            if (index != -1)
            {
                Destroy(buttons[index].gameObject);
                buttons.RemoveAt(index);
                actions.RemoveAt(index);
                for (int i = index; i < buttons.Count; i++)
                {
                    buttons[i].SetData(action.name, () => OnActionButtonClick(index));
                }
            }
        }

        public void BindTabs(IReactiveEnumerable<IReactiveTab> tabs, IReactive<IReactiveTab> openTab)
        {
            this.tabs = tabs;
            this.openTab = openTab;
        }

        public void CloseTab(IReactiveTab tab)
        {
            OnTabClose(tab);
        }

        private void OnTabClick(IReactiveTab tab)
        {
            onTabClick?.Invoke(tab);
        }

        private void OnTabClose(IReactiveTab tab)
        {
            onTabClose?.Invoke(tab);
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

        private void Update()
        {
            if (dropdown != null)
            {
                var rect = dropdown.elementParent;
                bool isMouseInside = RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);
                if (!isMouseInside && Input.GetMouseButtonDown(0))
                {
                    CloseDropdown();
                }
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
