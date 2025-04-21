using System.Collections.Generic;
using UnityEngine;

namespace ActionMenu
{
    public class ActionDropdown : MonoBehaviour
    {
        [System.Serializable]
        public struct DropdownEntry
        {
            public ActionType.Variant action;
            public ActionDropdownElement element;
        }

        [SerializeField] private RectTransform elementParent;
        [SerializeField] private List<DropdownEntry> entries;

        private readonly Dictionary<ActionType.Variant, ActionDropdownElement> elementPrefabs = new();
        private readonly List<ActionDropdownElement> elements = new();

        private bool isOpen = false;

        private void BuildPrefabMap()
        {
            if (elementPrefabs.Count > 0) return;
            elementPrefabs.Clear();
            foreach (var entry in entries)
            {
                if (elementPrefabs.ContainsKey(entry.action))
                {
                    Debug.LogError($"Duplicate action type {entry.action} found in ActionDropdown.");
                    continue;
                }

                elementPrefabs[entry.action] = entry.element;
            }
        }

        public void Open(RectTransform origin, List<ActionType> actions)
        {
            if (isOpen) return;
            BuildPrefabMap();
            foreach (var action in actions)
            {
                var prefab = elementPrefabs[action.Type];
                var instance = Instantiate(prefab, elementParent);
                instance.SetData(action);
                elements.Add(instance);
            }
            var worldCorners = new Vector3[4];
            origin.GetWorldCorners(worldCorners);
            transform.position = worldCorners[0];
            gameObject.SetActive(true);
            isOpen = true;
        }

        public void Close()
        {
            foreach (var element in elements)
            {
                Destroy(element.gameObject);
            }
            elements.Clear();
            gameObject.SetActive(false);
            isOpen = false;
        }
    }
}