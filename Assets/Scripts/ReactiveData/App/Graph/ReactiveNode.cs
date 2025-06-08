using DSP;
using DTO;
using ReactiveData.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReactiveData.App
{
    public class ReactiveNode : IKeyed
    {
        public Reactive<Vector2> position;
        public Reactive<NodeResource> id;
        public ReactiveDict<string, ReactiveNodeSetting> settings;

        public ReactiveNode(Vector2 position, NodeResource id, Dictionary<string, ReactiveNodeSetting> settings)
        {
            this.position = new(position);
            this.id = new(id);
            this.settings = new(settings);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;

        public void ValidateSettingsFromNode(SettingsNode node)
        {
            var defaultSettings = node.DefaultSettings;
            foreach (var setting in defaultSettings)
            {
                var type = setting.Type;
                if (settings.TryGetValue(setting.name, out var settingValue))
                {
                    var presentType = settingValue.Type;
                    bool valid = (type, presentType) switch
                    {
                        (SettingType.Int, ReactiveSettingType.Int) => true,
                        (SettingType.Float, ReactiveSettingType.Float) => true,
                        (SettingType.String, ReactiveSettingType.String) => true,
                        (SettingType.Enum, ReactiveSettingType.Enum) => true,
                        _ => false
                    };
                    if (valid) continue;
                }
                ReactiveNodeSetting instance = type switch
                {
                    SettingType.Int => new ReactiveIntSetting(setting.name, ((IntSetting)setting).value),
                    SettingType.Float => new ReactiveFloatSetting(setting.name, ((FloatSetting)setting).value),
                    SettingType.String => new ReactiveStringSetting(setting.name, ((StringSetting)setting).value),
                    SettingType.Enum => new ReactiveEnumSetting(setting.name, ((EnumSetting)setting).names, ((EnumSetting)setting).value),
                    _ => throw new ArgumentException($"Unknown setting type: {type}")
                };
                settings[setting.name] = instance;
            }
            var settingKeys = defaultSettings.Select(e => e.name).ToHashSet();
            foreach (var (name, _) in settings)
            {
                if (!settingKeys.Contains(name))
                {
                    settings.Remove(name);
                }
            }
        }

        public void ApplySettings(SettingsNode settingsNode)
        {
            ValidateSettingsFromNode(settingsNode);

            foreach (var (name, setting) in settingsNode.Settings)
            {
                var type = setting.Type;
                var reactiveSetting = settings[name];
                if (type == SettingType.String)
                {
                    ((StringSetting)setting).value = ((ReactiveStringSetting)reactiveSetting).value.Value;
                }
                else if (type == SettingType.Int)
                {
                    ((IntSetting)setting).value = ((ReactiveIntSetting)reactiveSetting).value.Value;
                }
                else if (type == SettingType.Float)
                {
                    ((FloatSetting)setting).value = ((ReactiveFloatSetting)reactiveSetting).value.Value;
                }
                else if (type == SettingType.Enum)
                {
                    ((EnumSetting)setting).value = ((ReactiveEnumSetting)reactiveSetting).value.Value;
                }
                else
                {
                    throw new ArgumentException($"Unknown setting type: {type} for setting {name} in node {id.Value}");
                }
            }
            settingsNode.OnSettingsChanged();
        }
    }
}
