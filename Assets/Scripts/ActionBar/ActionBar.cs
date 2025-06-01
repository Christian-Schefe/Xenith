using ReactiveData.App;
using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

        private readonly List<TopLevelAction> actions = new();
        private readonly List<ActionButton> buttons = new();

        private readonly ReactiveList<ActionTab> tabs = new();
        private ReactiveListBinder<ActionTab, TabButton> tabButtons;
        private readonly Reactive<ActionTab> openTab = new(null);

        private ActionDropdown dropdown;
        private int openButtonIndex = -1;

        private void Awake()
        {
            tabButtons = new(tabs, _ =>
            {
                var tab = Instantiate(tabButtonPrefab, tabParent);
                tab.Initialize(openTab, OnTabClick, OnTabClose);
                return tab;
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

        public void CloseTab(ActionTab tab)
        {
            OnTabClose(tab);
        }

        public void AddTab(ActionTab tab, bool setActive)
        {
            tabs.Add(tab);

            if (openTab.Value == null)
            {
                openTab.Value = tab;
                SelectTab(tab);
            }

            if (setActive)
            {
                OnTabClick(tab);
            }
        }

        private void SelectTab(ActionTab tab)
        {
            tab.onSelect?.Invoke();
        }

        private void DeselectTab(ActionTab tab)
        {
            tab.onDeselect?.Invoke();
        }

        private void OnTabClick(ActionTab tab)
        {
            if (tab == openTab.Value) return;

            if (openTab.Value != null)
            {
                DeselectTab(openTab.Value);
            }
            openTab.Value = tab;
            SelectTab(tab);
        }

        private void OnTabClose(ActionTab tab)
        {
            void CloseInternal()
            {
                if (tab == openTab.Value)
                {
                    DeselectTab(tab);
                    int index = tabs.IndexOf(tab);
                    int newIndex = tabs.Count >= 2 ? (index == 0 ? 1 : index - 1) : -1;
                    openTab.Value = newIndex != -1 ? tabs[newIndex] : null;
                    if (openTab.Value != null)
                    {
                        SelectTab(openTab.Value);
                    }
                }
                tabs.Remove(tab);
            }

            if (tab.onTryClose != null)
            {
                tab.onTryClose(CloseInternal);
            }
            else
            {
                CloseInternal();
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

    public class ActionTab : IKeyed
    {
        public ReactiveTab tab;
        public System.Action onSelect;
        public System.Action onDeselect;
        public System.Action<System.Action> onTryClose;

        public ActionTab(ReactiveTab tab, System.Action onSelect, System.Action onDeselect, System.Action<System.Action> onTryClose)
        {
            this.tab = tab;
            this.onSelect = onSelect;
            this.onDeselect = onDeselect;
            this.onTryClose = onTryClose;
        }

        public string Key => tab.Key;
    }
}
