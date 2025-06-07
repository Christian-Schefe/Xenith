using ReactiveData.App;
using ReactiveData.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NodeGraph
{
    public class NodeSettingsContainer : MonoBehaviour
    {
        [SerializeField] private List<PrefabEntry> nodeOptionPrefabs;

        [Serializable]
        private struct PrefabEntry
        {
            public ReactiveSettingType type;
            public NodeOption prefab;
        }

        private Dictionary<ReactiveSettingType, NodeOption> nodeOptionPrefabsDict;

        private ReactiveUIBinder<ReactiveNodeSetting, NodeOption> optionBinder;

        private ReactiveNode node;

        private void Awake()
        {
            BuildPrefabDict();
            optionBinder = new(null, setting => Instantiate(nodeOptionPrefabsDict[setting.Type], transform), setting => Destroy(setting.gameObject));
        }

        private void BuildPrefabDict()
        {
            if (nodeOptionPrefabsDict != null) return;
            nodeOptionPrefabsDict = new();
            foreach (var prefab in nodeOptionPrefabs)
            {
                nodeOptionPrefabsDict[prefab.type] = prefab.prefab;
            }
        }

        public void Bind(ReactiveNode node)
        {
            this.node = node;
            optionBinder.ChangeSource(node.settings.Values);
        }

        public void Unbind()
        {
            optionBinder.ChangeSource(null);
            node = null;
        }

        public bool HasSettings()
        {
            return node.settings.Count > 0;
        }

        public float ApplyHeight()
        {
            float height = 0;
            foreach (var option in optionBinder.UIElements)
            {
                height += option.GetHeight();
            }
            var rectTransform = GetComponent<RectTransform>();
            var sizeDelta = rectTransform.sizeDelta;
            sizeDelta.y = height;
            rectTransform.sizeDelta = sizeDelta;
            return height;
        }
    }

    public abstract class NodeOption : MonoBehaviour, IReactor<ReactiveNodeSetting>
    {
        public TextMeshProUGUI label;
        public ReactiveNodeSetting setting;

        public abstract float GetHeight();

        protected abstract void OnValueChanged();

        private void OnNameChanged(string name)
        {
            label.text = name;
        }

        public void Bind(ReactiveNodeSetting data)
        {
            setting = data;
            data.name.AddAndCall(OnNameChanged);
            data.Value.OnChanged += OnValueChanged;
            OnValueChanged();
        }

        public void Unbind()
        {
            setting.name.Remove(OnNameChanged);
            setting.Value.OnChanged -= OnValueChanged;
            setting = null;
        }
    }
}
