using DSP;
using System;
using UnityEngine;

namespace NodeGraph
{
    public class InputFieldOption : NodeOption
    {
        [SerializeField] private TMPro.TMP_InputField inputField;
        [SerializeField] private float height;

        public override float GetHeight() => height;

        public override void Initialize(NodeSetting setting, Action onChange)
        {
            base.Initialize(setting, onChange);
            this.setting = setting;
            var type = setting.Type;
            inputField.contentType = type switch
            {
                SettingType.Float => TMPro.TMP_InputField.ContentType.Standard,
                SettingType.Int => TMPro.TMP_InputField.ContentType.IntegerNumber,
                SettingType.String => TMPro.TMP_InputField.ContentType.Standard,
                _ => throw new ArgumentOutOfRangeException("Setting Type", type, "Unsupported SettingType")
            };
            inputField.text = setting switch
            {
                FloatSetting floatSetting => floatSetting.value.ToString("R"),
                IntSetting intSetting => intSetting.value.ToString(),
                StringSetting stringSetting => stringSetting.value,
                _ => throw new ArgumentOutOfRangeException("Setting Type", type, "Unsupported SettingType")
            };
        }

        private void OnEndEditing(string value)
        {
            if (setting == null) return;
            var finalString = "";
            if (setting is FloatSetting floatSetting)
            {
                var floatVal = float.TryParse(value, out var val) ? val : floatSetting.value;
                finalString = floatVal.ToString("R");
                floatSetting.value = floatVal;
            }
            else if (setting is IntSetting intSetting)
            {
                var intVal = int.TryParse(value, out var result) ? result : intSetting.value;
                finalString = intVal.ToString();
                intSetting.value = intVal;
            }
            else if (setting is StringSetting stringSetting)
            {
                finalString = value;
                stringSetting.value = value;
            }
            inputField.text = finalString;
            onChange?.Invoke();
        }

        private void OnEnable()
        {
            inputField.onEndEdit.AddListener(OnEndEditing);
        }

        private void OnDisable()
        {
            inputField.onEndEdit.RemoveListener(OnEndEditing);
        }
    }
}
