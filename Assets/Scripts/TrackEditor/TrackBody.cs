using UnityEngine;
using UnityEngine.EventSystems;

public class TrackBody : MonoBehaviour, IPointerClickHandler
{
    private System.Action onClick;

    public void Initialize(System.Action onClick)
    {
        this.onClick = onClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            onClick?.Invoke();
        }
    }
}
