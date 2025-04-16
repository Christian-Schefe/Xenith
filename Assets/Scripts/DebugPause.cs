using UnityEditor;
using UnityEngine;

public class DebugPause : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            EditorApplication.isPaused = true;
        }
    }
#endif
}
