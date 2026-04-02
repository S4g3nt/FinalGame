using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 2D 收集物：碰 Hero 触发器拾取，已拾取的存档后不再出现。
/// 请为每个实例设置 levelId（本关统一）与 collectibleId（本关唯一）。
/// </summary>
[DefaultExecutionOrder(-200)]
public class Collectible2D : MonoBehaviour
{
    [Tooltip("本关标识，例如场景名或 Level_01，需与 UI 查询一致")]
    [SerializeField] string levelId = "Level_01";

    [Tooltip("同一 levelId 下必须唯一，例如 coin_01、gem_a")]
    [SerializeField] string collectibleId = "item_01";

    [SerializeField] bool destroyOnPickup = true;

    [SerializeField] UnityEvent onCollected;

    static int _totalsSceneHandle = int.MinValue;

    public string LevelId => levelId;
    public string CollectibleId => collectibleId;

    void Awake()
    {
        RefreshTotalsForSceneIfNeeded();

        if (CollectibleProgress.IsCollected(levelId, collectibleId))
            gameObject.SetActive(false);
    }

    void RefreshTotalsForSceneIfNeeded()
    {
        var scene = gameObject.scene;
        if (scene.handle == _totalsSceneHandle) return;
        _totalsSceneHandle = scene.handle;

        var all = FindObjectsByType<Collectible2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var counts = new Dictionary<string, int>();
        foreach (var c in all)
        {
            if (c == null) continue;
            string lid = c.levelId;
            if (string.IsNullOrEmpty(lid)) continue;
            counts.TryGetValue(lid, out int n);
            counts[lid] = n + 1;
        }

        foreach (var kv in counts)
            CollectibleProgress.SetLevelTotal(kv.Key, kv.Value);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Hero")) return;
        if (CollectibleProgress.IsCollected(levelId, collectibleId)) return;

        CollectibleProgress.MarkCollected(levelId, collectibleId);
        onCollected?.Invoke();

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(collectibleId))
            collectibleId = "item_" + GetInstanceID().ToString("x");
    }
#endif
}
