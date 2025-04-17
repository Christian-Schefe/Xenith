using DSP;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class DropdownOption : NodeOption
    {
        [SerializeField] private TMPro.TMP_Dropdown dropdown;
        [SerializeField] private float height;

        public override float GetHeight() => height;

        private List<int> values;

        public override void Initialize(NodeSetting setting, Action onChange)
        {
            base.Initialize(setting, onChange);
            if (setting is not EnumSetting enumSetting)
            {
                throw new ArgumentException("Setting must be of type EnumSetting", nameof(setting));
            }

            dropdown.ClearOptions();
            values = new();
            var enumValues = Enum.GetValues(enumSetting.type);
            var options = new List<TMPro.TMP_Dropdown.OptionData>();
            for (int i = 0; i < enumValues.Length; i++)
            {
                var val = enumValues.GetValue(i);
                var name = Enum.GetName(enumSetting.type, val);
                values.Add((int)val);
                options.Add(new TMPro.TMP_Dropdown.OptionData(name));
            }
            dropdown.AddOptions(options);
            dropdown.value = values.IndexOf(enumSetting.value);
        }

        private void OnValueChanged(int value)
        {
            if (setting == null) return;
            var enumSetting = (EnumSetting)setting;
            enumSetting.value = values[value];
            onChange?.Invoke();
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
