using UnityEngine;

namespace ActionMenu
{
    public abstract class ActionDropdownElement : MonoBehaviour
    {
        public abstract void SetData(ActionType action);
    }
}