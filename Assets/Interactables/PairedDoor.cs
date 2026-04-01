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

    [Header("逻辑设置")]
    [Tooltip("关卡开始时的初始状态。")]
    [SerializeField] bool startOpen;

    [Tooltip("【重要】如果勾选，接收到按钮信号时会执行相反动作（按下按钮关门，松开开门）。")]
    [SerializeField] bool invertLogic = false;

    // 记录当前的逻辑状态，用于重置
    private bool isCurrentlyOpen;

    void Awake()
    {
        // 游戏开始，执行初始重置
        ResetDoor();
    }

    /// <summary>
    /// 被按钮调用：根据输入信号决定开关
    /// </summary>
    /// <param name="signal">按钮发送的信号（通常按下为 true，松开为 false）</param>
    public void SetOpen(bool signal)
    {
        // 如果逻辑反转，则将信号取反
        bool finalState = invertLogic ? !signal : signal;
        ApplyState(finalState);
    }

    /// <summary>
    /// 内部方法：执行具体的显示/隐藏和物理开关
    /// </summary>
    private void ApplyState(bool open)
    {
        isCurrentlyOpen = open;
        if (visualClosed != null) visualClosed.SetActive(!open);
        if (visualOpen != null) visualOpen.SetActive(open);
        if (blockingCollider != null) blockingCollider.enabled = !open;
    }

    /// <summary>
    /// 复活时被 GameManager 调用，恢复初始状态
    /// </summary>
    public void ResetDoor()
    {
        // 恢复到初始设定的 startOpen 状态
        ApplyState(startOpen);
    }
}