#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ClearSaveTool : MonoBehaviour
{
    // 这会在 Unity 顶部的菜单栏里加一个按钮
    [MenuItem("Tools/🕹️ 清空所有游戏存档 (PlayerPrefs)")]
    public static void ClearAllSaves()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=green><b>[完美] 所有存档已清空！再按 Play 收集品就全回来了！</b></color>");
    }
}
#endif