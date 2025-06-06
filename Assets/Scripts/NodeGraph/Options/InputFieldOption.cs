using DSP;
using ReactiveData.App;
using System;
using UnityEngine;

namespace NodeGraph
{
    public class InputFieldOption : NodeOption
    {
        [SerializeField] private TMPro.TMP_InputField inputField;
        [SerializeField] private float height;

        public override float GetHeight() => height;

        private void OnEndEditing(string value)
        {
            if (setting == null) return;
            if (setting is ReactiveFloatSetting floatSetting)
            {
                var floatVal = float.TryParse(value, out var val) ? val : floatSetting.value.Value;
                floatSetting.value.Value = floatVal;
            }
            else if (setting is ReactiveIntSetting intSetting)
            {
                var intVal = int.TryParse(value, out var result) ? result : intSetting.value.Value;
                intSetting.value.Value = intVal;
            }
            else if (setting is ReactiveStringSetting stringSetting)
            {
                stringSetting.value.Value = value;
            }
        }

        private void OnEnable()
        {
            inputField.onEndEdit.AddListener(OnEndEditing);
        }

        private void OnDisable()
        {
            inputField.onEndEdit.RemoveListener(OnEndEditing);
        }

        protected override void OnValueChanged()
        {
            var type = setting.Type;
            inputField.contentType = type switch
            {
                ReactiveSettingType.Float => TMPro.TMP_InputField.ContentType.Standard,
                ReactiveSettingType.Int => TMPro.TMP_InputField.ContentType.IntegerNumber,
                ReactiveSettingType.String => TMPro.TMP_InputField.ContentType.Standard,
                _ => throw new ArgumentOutOfRangeException("Setting Variant", type, "Unsupported SettingType")
            };
            inputField.text = setting switch
            {
                ReactiveFloatSetting floatSetting => floatSetting.value.Value.ToString("R"),
                ReactiveIntSetting intSetting => intSetting.value.Value.ToString(),
                ReactiveStringSetting stringSetting => stringSetting.value.Value,
                _ => throw new ArgumentOutOfRangeException("Setting Variant", type, "Unsupported SettingType")
            };
        }
    }
}
