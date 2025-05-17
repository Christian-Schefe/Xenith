using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ActionMenu
{
    public class ActionBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform actionParent;
        [SerializeField] private RectTransform tabParent;
        [SerializeField] private TMPro.TextMeshProUGUI titleText;
        [SerializeField] private ActionButton actionButtonPrefab;
        [SerializeField] private TabButton tabButtonPrefab;
        [SerializeField] private ActionDropdown actionDropdownPrefab;

        private readonly List<TopLevelAction> actions = new();
        private readonly List<ActionTab> tabs = new();
        private readonly List<ActionButton> buttons = new();
        private readonly List<TabButton> tabButtons = new();

        private ActionDropdown dropdown;
        private int openButtonIndex = -1;
        private bool isMouseInside = false;
        private int openTabIndex = -1;

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
            CloseDropdown();
            int index = actions.IndexOf(action);
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
            int index = tabs.IndexOf(tab);
            if (index != -1)
            {
                OnTabClose(index);
            }
        }

        public void AddTab(ActionTab tab, bool setActive)
        {
            tabs.Add(tab);
            var instance = Instantiate(tabButtonPrefab, tabParent);
            int index = tabs.Count - 1;
            instance.Initialize(index, tab.name, (i) => OnTabClick(i), (i) => OnTabClose(i));
            tabButtons.Add(instance);

            if (openTabIndex == -1)
            {
                openTabIndex = index;
                SelectTab(index);
            }

            if (setActive)
            {
                OnTabClick(index);
            }
        }

        public void UpdateTab(ActionTab tab)
        {
            var index = tabs.IndexOf(tab);
            if (index != -1)
            {
                tabButtons[index].Initialize(index, tab.name, (i) => OnTabClick(i), (i) => OnTabClose(i));
                if (openTabIndex == index)
                {
                    tabButtons[index].SetSelected(true);
                }
            }
        }

        private void SelectTab(int index)
        {
            tabs[index].onSelect?.Invoke();
            tabButtons[index].SetSelected(true);
        }

        private void DeselectTab(int index)
        {
            tabs[index].onDeselect?.Invoke();
            tabButtons[index].SetSelected(false);
        }

        private void OnTabClick(int index)
        {
            if (index == openTabIndex) return;

            if (openTabIndex != -1)
            {
                DeselectTab(openTabIndex);
            }
            openTabIndex = index;
            SelectTab(index);
        }

        private void OnTabClose(int index)
        {
            void CloseInternal()
            {
                bool openTabChanged = openTabIndex == index;
                if (index == openTabIndex)
                {
                    DeselectTab(index);
                    openTabIndex = tabs.Count >= 2 ? (index == 0 ? 1 : index - 1) : -1;
                }
                Destroy(tabButtons[index].gameObject);
                tabButtons.RemoveAt(index);
                tabs.RemoveAt(index);
                for (int i = index; i < tabButtons.Count; i++)
                {
                    tabButtons[i].SetIndex(i);
                }
                if (openTabIndex >= index && openTabIndex > 0) openTabIndex--;

                if (openTabChanged && openTabIndex != -1)
                {
                    SelectTab(openTabIndex);
                }
            }
            if (tabs[index].onTryClose != null)
            {
                tabs[index].onTryClose.Invoke(CloseInternal);
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

    public class ActionTab
    {
        public string name;
        public System.Action onSelect;
        public System.Action onDeselect;
        public System.Action<System.Action> onTryClose;

        public ActionTab(string name, System.Action onSelect, System.Action onDeselect, System.Action<System.Action> onTryClose)
        {
            this.name = name;
            this.onSelect = onSelect;
            this.onDeselect = onDeselect;
            this.onTryClose = onTryClose;
        }
    }
}
