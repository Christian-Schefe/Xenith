using DSP;
using ReactiveData.App;
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

        private void OnDropdownValueChanged(int value)
        {
            if (setting == null) return;
            var enumSetting = (ReactiveEnumSetting)setting;
            enumSetting.value.Value = values[value];
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        protected override void OnValueChanged()
        {
            if (setting is not ReactiveEnumSetting enumSetting)
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
            dropdown.value = values.IndexOf(enumSetting.value.Value);
        }
    }
}
