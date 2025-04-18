using UnityEngine;

public class BaseDialog : MonoBehaviour
{
    public bool isOpen = false;

    public virtual void Open()
    {
        if (isOpen) return;
        isOpen = true;
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        gameObject.SetActive(false);
    }
}
