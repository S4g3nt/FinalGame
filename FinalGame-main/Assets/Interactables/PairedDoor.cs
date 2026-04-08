using UnityEngine;

/// <summary>
/// 与按钮配对的门：通过显示/隐藏「关」「开」两套子物体表现状态；可选同步阻挡用 Collider2D。
/// </summary>
public class PairedDoor : MonoBehaviour
{
    [Header("视觉（二选一显示）")]
    [SerializeField] GameObject visualClosed;
    [SerializeField] GameObject visualOpen;

    [Header("可选物理")]
    [Tooltip("关门时启用、开门时禁用，用于挡住通道")]
    [SerializeField] Collider2D blockingCollider;

    [SerializeField] bool startOpen;

    void Start()
    {
        SetOpen(startOpen);
    }

    public void SetOpen(bool open)
    {
        if (visualClosed != null) visualClosed.SetActive(!open);
        if (visualOpen != null) visualOpen.SetActive(open);
        if (blockingCollider != null) blockingCollider.enabled = !open;
    }
}
