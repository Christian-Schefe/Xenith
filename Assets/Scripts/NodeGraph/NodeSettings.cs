using DSP;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NodeGraph
{
    public class NodeSettingsContainer : MonoBehaviour
    {
        [SerializeField] private List<NodeOption> nodeOptionPrefabs;

        private Dictionary<SettingType, NodeOption> nodeOptionPrefabsDict;

        private List<NodeOption> options;

        private void Awake()
        {
            nodeOptionPrefabsDict = new Dictionary<SettingType, NodeOption>();
            foreach (var prefab in nodeOptionPrefabs)
            {
                nodeOptionPrefabsDict[prefab.type] = prefab;
            }
        }

        public void Initialize(NodeSettings settings)
        {
            foreach (var setting in settings.settings)
            {
                var optionPrefab = nodeOptionPrefabsDict[setting.Value.Type];
                var option = Instantiate(optionPrefab, transform);
                option.Initialize(setting.Value);
                options.Add(option);
            }
        }
    }

    public class NodeOption : MonoBehaviour
    {
        public SettingType type;
        public TextMeshProUGUI label;

        public void Initialize(NodeSetting setting)
        {
            label.text = setting.name;
        }
    }
}
