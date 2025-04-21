using UnityEngine;
using UnityEngine.UI;

namespace ActionMenu
{
    public class ToggleVariant : ActionDropdownElement
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMPro.TextMeshProUGUI text;

        private Box<bool> state;

        public override void SetData(ActionType action)
        {
            if (action is not ActionType.Toggle data)
            {
                Debug.LogError($"Action is not a Button: {action}");
                return;
            }
            state = data.state;
            state.AddAndCallListener(OnStateChanged);

            text.text = data.name;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((value) =>
            {
                data.state.Value = value;
            });
        }

        private void OnStateChanged(bool value)
        {
            toggle.SetIsOnWithoutNotify(value);
        }

        private void OnDisable()
        {
            state.RemoveListener(OnStateChanged);
            toggle.onValueChanged.RemoveAllListeners();
        }
    }
}
