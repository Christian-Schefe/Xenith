using UnityEngine;
using UnityEngine.UI;

public class FileTreeEntry : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;
    [SerializeField] private Button button;

    public void SetData(string name, System.Action onClick)
    {
        text.text = name;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}