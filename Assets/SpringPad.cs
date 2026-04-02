using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹簧踏板：Hero、Yoru 锚点、假人踩到后获得沿弹簧表面朝向的速度。
/// 预制体使用实体 BoxCollider2D（非 Trigger），放在地面或天花板上即可。
/// 【修正版】：支持反重力，根据弹簧自身的 transform.up 确定弹跳方向。
/// </summary>
public class SpringPad : MonoBehaviour
{
    [Header("弹跳设置")]
    [Tooltip("施加的弹跳速度（沿弹簧 transform.up 方向）")]
    public float bounceVelocity = 16f;

    [Tooltip("仅当目标沿弹簧朝向的速度不大于该值时才弹（避免重复触发）")]
    public float maxRelativeSpeedToAccept = 0.5f;

    [Tooltip("同一刚体再次触发的最短间隔（秒）")]
    public float cooldownPerTarget = 0.25f;

    private readonly Dictionary<int, float> _nextAllowedTime = new Dictionary<int, float>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!TryGetTargetRb(collision.collider, out Rigidbody2D rb)) return;

        int id = rb.GetInstanceID();
        if (_nextAllowedTime.TryGetValue(id, out float t) && Time.time < t) return;

        // 【核心修复 1】：获取弹簧的朝向（通常是向上）
        Vector2 bounceDir = transform.up;

        // 【核心修复 2】：计算玩家当前在弹簧朝向上的速度分量
        // 如果玩家已经正在远离弹簧，则不触发
        float currentRelativeSpeed = Vector2.Dot(rb.linearVelocity, bounceDir);
        if (currentRelativeSpeed > maxRelativeSpeedToAccept) return;

        // 记录冷却
        _nextAllowedTime[id] = Time.time + cooldownPerTarget;

        // 【核心修复 3】：应用速度
        // 保留玩家在垂直于弹簧方向的速度（例如左右移动惯性），并设置弹簧朝向的速度
        Vector2 tangentDir = new Vector2(-bounceDir.y, bounceDir.x); // 弹簧的切线方向
        float tangentSpeed = Vector2.Dot(rb.linearVelocity, tangentDir);

        rb.linearVelocity = bounceDir * bounceVelocity + tangentDir * tangentSpeed;
        
        // 尝试播放动画（如果有的话）
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Spring");
    }

    private static bool TryGetTargetRb(Collider2D col, out Rigidbody2D rb)
    {
        rb = col.attachedRigidbody;
        if (rb == null) rb = col.GetComponent<Rigidbody2D>();
        if (rb == null) return false;

        // 检查 Tag 或 脚本
        if (col.CompareTag("Hero")) return true;
        if (col.CompareTag("YoruClone")) return true;
        
        // 使用 TryGetComponent 效率更高且更安全
        if (col.TryGetComponent<YoruCloneLogic>(out _)) return true;
        if (col.TryGetComponent<YoruAnchorLogic>(out _)) return true;
        
        return false;
    }
}