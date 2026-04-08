using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 2D 收集物：碰 Hero 触发器拾取，已拾取的存档后不再出现。
/// 未被拾取时会进行 Sin 模式的上下浮动视觉效果。
/// 拾取时会生成临时物体播放 2D 音效，防止因自身销毁导致声音中断。
/// 请为每个实例设置 levelId（本关统一）与 collectibleId（本关唯一）。
/// </summary>
[DefaultExecutionOrder(-200)]
public class Collectible2D : MonoBehaviour
{
    [Header("Save Settings (存档设置)")]
    [Tooltip("本关标识，例如场景名或 Level_01，需与 UI 查询一致")]
    [SerializeField] string levelId = "Level_01";

    [Tooltip("同一 levelId 下必须唯一，例如 coin_01、gem_a")]
    [SerializeField] string collectibleId = "item_01";

    [SerializeField] bool destroyOnPickup = true;

    [SerializeField] UnityEvent onCollected;

    // ================= NEW AUDIO SETTINGS =================
    [Header("Audio Settings (音效设置)")]
    [Tooltip("拾取时播放的音效")]
    [SerializeField] private AudioClip pickupSound;
    // =======================================================

    // ================= NEW VISUAL SETTINGS =================
    [Header("Visual Effects (Sin 浮动视觉特效)")]
    [Tooltip("物体上下移动的距离（振幅）")]
    [SerializeField] private float floatAmplitude = 0.2f;

    [Tooltip("物体浮动的速度（频率）")]
    [SerializeField] private float floatSpeed = 2.0f;
    // =======================================================

    static int _totalsSceneHandle = int.MinValue;

    public string LevelId => levelId;
    public string CollectibleId => collectibleId;

    // 用于存储物体的初始世界坐标
    private Vector3 startPosition;

    void Awake()
    {
        RefreshTotalsForSceneIfNeeded();

        if (CollectibleProgress.IsCollected(levelId, collectibleId))
        {
            // 如果已收集，则禁用该物体，Update 也不会运行。
            gameObject.SetActive(false);
        }
        else
        {
            // 如果需要显示（未被收集），则记录初始位置，以便开始 Sin 移动。
            startPosition = transform.position;
        }
    }

    void Update()
    {
        // 处理上下移动 (Sin 模式)
        // 这一步只在物体处于 Active 状态下运行（即已被 Awake 确认为未收集状态）。

        // Mathf.Sin(...) 返回一个在 -1 到 1 之间周期性变化的值
        float newY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        // 应用新的位置，保持 X 和 Z轴不变，只在初始 Y轴偏移量
        transform.position = new Vector3(startPosition.x, startPosition.y + newY, startPosition.z);
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

        // --- 播放收集音效逻辑 ---
        if (pickupSound != null)
        {
            // 动态创建一个名为 PickupSound 的空物体
            GameObject audioObj = new GameObject("PickupSound_" + collectibleId);
            audioObj.transform.position = transform.position;
            
            // 给空物体挂载 AudioSource 组件
            AudioSource src = audioObj.AddComponent<AudioSource>();
            src.clip = pickupSound;
            src.spatialBlend = 0f; // 强制设置为 0 (纯 2D 声音)，无论相机多远都能清晰听到
            src.Play();
            
            // 设定在音效时长结束后，自动销毁这个临时物体
            Destroy(audioObj, pickupSound.length);
        }
        // ------------------------

        CollectibleProgress.MarkCollected(levelId, collectibleId);
        onCollected?.Invoke();

        if (destroyOnPickup)
        {
            // 销毁后，Update 自动停止。
            Destroy(gameObject);
        }
        else
        {
            // 隐藏后，Update 也不会运行。
            gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(collectibleId))
            collectibleId = "item_" + GetInstanceID().ToString("x");
    }
#endif
}