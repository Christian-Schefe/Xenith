using DSP;
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
            public SettingType type;
            public NodeOption prefab;
        }

        private Dictionary<SettingType, NodeOption> nodeOptionPrefabsDict;

        private List<NodeOption> options = new();
        private GraphNode parentNode;

        private void Awake()
        {
            BuildPrefabDict();
        }

        private void BuildPrefabDict()
        {
            if (nodeOptionPrefabsDict != null) return;
            nodeOptionPrefabsDict = new Dictionary<SettingType, NodeOption>();
            foreach (var prefab in nodeOptionPrefabs)
            {
                nodeOptionPrefabsDict[prefab.type] = prefab.prefab;
            }
        }

        public void Initialize(GraphNode parent, AudioNode node)
        {
            foreach (var option in options)
            {
                Destroy(option.gameObject);
            }
            options.Clear();

            BuildPrefabDict();
            if (node is SettingsNode settingsNode)
            {
                void OnChange()
                {
                    parent.OnSettingsChanged();
                }
                var settings = settingsNode.Settings;
                foreach (var setting in settings.settings)
                {
                    var optionPrefab = nodeOptionPrefabsDict[setting.Value.Type];
                    var option = Instantiate(optionPrefab, transform);
                    option.Initialize(setting.Value, OnChange);
                    options.Add(option);
                }
            }
        }

        public bool HasSettings()
        {
            return options.Count > 0;
        }

        public float ApplyHeight()
        {
            float height = 0;
            foreach (var option in options)
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

    public abstract class NodeOption : MonoBehaviour
    {
        public TextMeshProUGUI label;
        public Action onChange;
        protected NodeSetting setting;

        public abstract float GetHeight();

        public virtual void Initialize(NodeSetting setting, Action onChange)
        {
            this.setting = setting;
            label.text = setting.name;
            this.onChange = onChange;
        }
    }
}
