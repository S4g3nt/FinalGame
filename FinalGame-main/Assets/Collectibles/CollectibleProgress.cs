using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 跨场景持久化收集状态（PlayerPrefs）。同一关卡内 collectibleId 必须唯一。
/// UI 可用 GetCollectedCount / GetTotalInLevel，或订阅 LevelProgressChanged。
/// </summary>
public static class CollectibleProgress
{
    const char Sep = '|';
    const string SetPrefix = "CollIds_v1_";
    const string TotalPrefix = "CollTot_v1_";

    public static event Action<string> LevelProgressChanged;

    public static bool IsCollected(string levelId, string collectibleId)
    {
        if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(collectibleId)) return false;
        return LoadSet(levelId).Contains(collectibleId);
    }

    public static void MarkCollected(string levelId, string collectibleId)
    {
        if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(collectibleId)) return;
        var set = LoadSet(levelId);
        if (!set.Add(collectibleId)) return;
        SaveSet(levelId, set);
        PlayerPrefs.Save();
        LevelProgressChanged?.Invoke(levelId);
    }

    public static int GetCollectedCount(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return 0;
        return LoadSet(levelId).Count;
    }

    /// <summary>进入关卡时由 Collectible2D 自动写入场景中放置的总数（含未收集）。</summary>
    public static int GetTotalInLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return 0;
        return PlayerPrefs.GetInt(TotalPrefix + levelId, 0);
    }

    internal static void SetLevelTotal(string levelId, int totalPlacedInScene)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        string key = TotalPrefix + levelId;
        int n = Mathf.Max(0, totalPlacedInScene);
        int old = PlayerPrefs.GetInt(key, -1);
        PlayerPrefs.SetInt(key, n);
        if (old != n)
            LevelProgressChanged?.Invoke(levelId);
    }

    /// <summary>调试：清空某一关的收集记录与总数缓存。</summary>
    public static void ClearLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        PlayerPrefs.DeleteKey(SetPrefix + levelId);
        PlayerPrefs.DeleteKey(TotalPrefix + levelId);
        PlayerPrefs.Save();
        LevelProgressChanged?.Invoke(levelId);
    }

    static HashSet<string> LoadSet(string levelId)
    {
        var raw = PlayerPrefs.GetString(SetPrefix + levelId, "");
        if (string.IsNullOrEmpty(raw)) return new HashSet<string>();
        var parts = raw.Split(Sep, StringSplitOptions.RemoveEmptyEntries);
        return new HashSet<string>(parts);
    }

    static void SaveSet(string levelId, HashSet<string> set)
    {
        var list = new List<string>(set);
        list.Sort(StringComparer.Ordinal);
        PlayerPrefs.SetString(SetPrefix + levelId, string.Join(Sep, list));
    }
}
