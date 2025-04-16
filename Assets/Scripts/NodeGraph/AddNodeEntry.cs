using UnityEngine;
using UnityEngine.UI;

namespace NodeGraph
{
    public class AddNodeEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMPro.TextMeshProUGUI nameText;

        public void SetText(string text)
        {
            nameText.text = text;
        }

        public void SetOnClick(System.Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
