using UnityEngine;

/// <summary>
/// 放在关卡场景中任意物体上（建议空物体）。记录本关开始时间，供终点 UI 显示用时。
/// 每关放一个即可。
/// </summary>
[DefaultExecutionOrder(-100)]
public class LevelRunTracker : MonoBehaviour
{
    public static LevelRunTracker Instance { get; private set; }

    [Tooltip("留空则使用当前场景名作为 levelId（需与 Collectible2D 的 levelId 一致）")]
    [SerializeField] string levelIdOverride = "";

    public string LevelId =>
        string.IsNullOrEmpty(levelIdOverride)
            ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            : levelIdOverride;

    float _startUnscaled;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        _startUnscaled = Time.unscaledTime;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>从进入本关（本组件 Start）到当前的用时，不受 Time.timeScale 影响。</summary>
    public float GetElapsedSeconds() => Mathf.Max(0f, Time.unscaledTime - _startUnscaled);
}
