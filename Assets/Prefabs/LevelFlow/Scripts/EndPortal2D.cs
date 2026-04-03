using UnityEngine;

/// <summary>
/// 终点传送门：带 Trigger 的 Collider2D，Hero 进入后弹出胜利界面。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EndPortal2D : MonoBehaviour
{
    [SerializeField] LevelVictoryUI victoryUI;

    [Tooltip("若未指定 victoryUI，会在场景中查找 LevelVictoryUI")]
    [SerializeField] bool findVictoryUIIfNull = true;

    [Header("音效")]
    [SerializeField] AudioClip portalEnterClip;
    [SerializeField] [Range(0f, 1f)] float portalEnterVolume = 1f;
    [Tooltip("在 clip 时长之外多等一会再 Time.timeScale=0，避免尾音被截断")]
    [SerializeField] float portalSoundPostBuffer = 0.05f;

    static bool _gateUsedThisSession;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null)
            c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Hero")) return;
        if (LevelVictoryUI.IsShowing) return;
        if (_gateUsedThisSession) return;

        LevelVictoryUI ui = victoryUI;
        if (ui == null && findVictoryUIIfNull)
            ui = Object.FindFirstObjectByType<LevelVictoryUI>(FindObjectsInactive.Include);

        if (ui == null)
        {
            Debug.LogWarning("EndPortal2D：未找到 LevelVictoryUI，请在场景中放置胜利界面或指定引用。");
            return;
        }

        float pauseDelay = 0f;
        if (portalEnterClip != null)
        {
            pauseDelay = Mathf.Max(0f, portalEnterClip.length + portalSoundPostBuffer);
            AudioSource.PlayClipAtPoint(portalEnterClip, transform.position, portalEnterVolume);
        }

        _gateUsedThisSession = true;
        ui.ShowFromPortal(pauseDelay);
    }

    /// <summary>重载关卡或切场景前由 LevelVictoryUI 调用，允许新一局再次触发传送门。</summary>
    public static void ResetGateForNewRun() => _gateUsedThisSession = false;
}
