using ReactiveData.UI;
using UnityEngine;

namespace ActionMenu
{
    public class ActionButton : MonoBehaviour
    {
        [SerializeField] private ReactiveButton button;
        [SerializeField] private TMPro.TextMeshProUGUI label;

        public void SetData(string text, System.Action onClick)
        {
            label.text = text;
            button.OnClick = onClick;
        }
    }
}