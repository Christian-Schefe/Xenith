using UnityEngine;
using UnityEngine.UI;

namespace ActionMenu
{
    public class ActionButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMPro.TextMeshProUGUI label;

        public void SetData(string text, System.Action onClick)
        {
            label.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}