using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹簧踏板：Hero、Yoru 锚点、假人（YoruCloneLogic / YoruClone 标签）踩到后获得向上速度。
/// 预制体使用实体 BoxCollider2D（非 Trigger），放在地面上即可。
/// </summary>
public class SpringPad : MonoBehaviour
{
    [Header("弹跳")]
    [Tooltip("施加的竖直速度（与玩家 jumpForce 同量级时可对比微调）")]
    public float bounceVelocityY = 16f;

    [Tooltip("仅当目标竖直速度不大于该值时才弹（避免上升过程中重复触发）")]
    public float maxUpwardSpeedToAccept = 0.5f;

    [Tooltip("同一刚体再次触发的最短间隔（秒）")]
    public float cooldownPerTarget = 0.25f;

    private readonly Dictionary<int, float> _nextAllowedTime = new Dictionary<int, float>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!TryGetTargetRb(collision.collider, out Rigidbody2D rb)) return;

        int id = rb.GetInstanceID();
        if (_nextAllowedTime.TryGetValue(id, out float t) && Time.time < t) return;

        if (rb.linearVelocity.y > maxUpwardSpeedToAccept) return;

        _nextAllowedTime[id] = Time.time + cooldownPerTarget;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceVelocityY);
    }

    private static bool TryGetTargetRb(Collider2D col, out Rigidbody2D rb)
    {
        rb = col.attachedRigidbody;
        if (rb == null) rb = col.GetComponent<Rigidbody2D>();
        if (rb == null) return false;

        if (col.CompareTag("Hero")) return true;
        if (col.CompareTag("YoruClone")) return true;
        if (col.GetComponent<YoruCloneLogic>() != null) return true;
        if (col.GetComponent<YoruAnchorLogic>() != null) return true;
        return false;
    }
}
