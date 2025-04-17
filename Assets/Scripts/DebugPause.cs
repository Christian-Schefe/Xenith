using UnityEditor;
using UnityEngine;

public class DebugPause : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            EditorApplication.isPaused = true;
        }
    }
#endif
}
