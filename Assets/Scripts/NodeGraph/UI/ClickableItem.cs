using UnityEngine;
using UnityEngine.UI;

namespace NodeGraph
{
    public class ClickableItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMPro.TextMeshProUGUI text;

        public void SetText(string newText)
        {
            text.text = newText;
        }

        public void SetOnClickListener(UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
