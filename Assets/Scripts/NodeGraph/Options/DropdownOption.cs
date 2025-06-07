using ReactiveData.App;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeGraph
{
    public class DropdownOption : NodeOption
    {
        [SerializeField] private TMPro.TMP_Dropdown dropdown;
        [SerializeField] private float height;

        private List<int> options;

        public override float GetHeight() => height;

        private void OnDropdownValueChanged(int value)
        {
            if (setting == null) return;
            var enumSetting = (ReactiveEnumSetting)setting;
            enumSetting.value.Value = options[value];
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

            options = enumSetting.options.OrderBy(e => e.Key).Select(e => e.Key).ToList();

            dropdown.ClearOptions();
            var dropdownOptions = options.Select(value => new TMPro.TMP_Dropdown.OptionData(enumSetting.options[value])).ToList();
            dropdown.AddOptions(dropdownOptions);
            int val = options.IndexOf(enumSetting.value.Value);
            dropdown.value = val >= 0 ? val : 0;
            if (val < 0)
            {
                enumSetting.value.Value = options[0];
            }
        }
    }
}
