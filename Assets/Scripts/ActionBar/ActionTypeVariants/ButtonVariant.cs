using UnityEngine;
using UnityEngine.UI;

namespace ActionMenu
{
    public class ButtonVariant : ActionDropdownElement
    {
        [SerializeField] private Button button;
        [SerializeField] private TMPro.TextMeshProUGUI text;

        public override void SetData(ActionType action)
        {
            if (action is not ActionType.Button data)
            {
                Debug.LogError($"Action is not a Button: {action}");
                return;
            }
            text.text = data.name;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => data.onClick());
        }
    }
}
