using ReactiveData.UI;
using UnityEngine;

namespace ActionMenu
{
    public class ButtonVariant : ActionDropdownElement
    {
        [SerializeField] private ReactiveButton button;
        [SerializeField] private TMPro.TextMeshProUGUI text;

        public override void SetData(ActionType action)
        {
            if (action is not ActionType.Button data)
            {
                Debug.LogError($"Action is not a Button: {action}");
                return;
            }
            text.text = data.name;
            button.OnClick = data.onClick;
        }
    }
}
