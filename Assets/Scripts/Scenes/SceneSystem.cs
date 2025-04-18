using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSystem : MonoBehaviour
{
    public enum EventType
    {
        BeforeSceneUnload,
        LateBeforeSceneUnload,
        ApplicationQuit
    }

    private readonly Dictionary<EventType, Action> globalEventActions = new();
    private readonly Dictionary<EventType, Action> sceneEventActions = new();

    public static void AddListener(EventType type, Action action, bool isGlobal)
    {
        var dict = isGlobal ? Globals<SceneSystem>.Instance.globalEventActions : Globals<SceneSystem>.Instance.sceneEventActions;

        if (!dict.ContainsKey(type)) dict[type] = action;
        else dict[type] += action;
    }


    public static void RemoveListener(EventType type, Action action, bool isGlobal)
    {
        var dict = isGlobal ? Globals<SceneSystem>.Instance.globalEventActions : Globals<SceneSystem>.Instance.sceneEventActions;

        if (!dict.ContainsKey(type)) return;
        dict[type] -= action;
    }

    private void Awake()
    {
        if (Globals<SceneSystem>.RegisterOrDestroy(this))
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void InvokeEvent(EventType type)
    {
        if (globalEventActions.TryGetValue(type, out var globalAction)) globalAction?.Invoke();
        if (sceneEventActions.TryGetValue(type, out var sceneAction)) sceneAction?.Invoke();
    }

    private void OnSwitchScene()
    {
        InvokeEvent(EventType.BeforeSceneUnload);
        InvokeEvent(EventType.LateBeforeSceneUnload);
        sceneEventActions.Clear();
    }

    private void OnApplicationQuit()
    {
        InvokeEvent(EventType.BeforeSceneUnload);
        InvokeEvent(EventType.LateBeforeSceneUnload);
        InvokeEvent(EventType.ApplicationQuit);
        sceneEventActions.Clear();
    }

    public static void LoadScene(SceneReference scene)
    {
        if (scene == null)
        {
            Debug.LogWarning("Scene reference is null.");
            return;
        }
        Globals<SceneSystem>.Instance.OnSwitchScene();
        SceneManager.LoadScene(scene.sceneName);
    }

    public static void ReloadScene()
    {
        Globals<SceneSystem>.Instance.OnSwitchScene();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
