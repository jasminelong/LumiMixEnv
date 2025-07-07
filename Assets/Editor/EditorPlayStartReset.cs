#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorPlayStartReset
{
    static EditorPlayStartReset()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            TrialState.Reset();
            Debug.Log("🧹 清空旧状态，准备生成新随机序列");
        }
    }
}
#endif
