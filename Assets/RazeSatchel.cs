using UnityEngine;

/// <summary>
/// 挂在炸药包预制体上。由 RazeSkills 生成并调用 <see cref="Detonate"/>。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RazeSatchel : MonoBehaviour
{
    [HideInInspector] public RazeSkills owner;

    /// <summary>
    /// 在爆炸半径内给 Raze 叠加线速度：方向为「爆炸点 → Raze 质心」的单位向量，大小按距离衰减，与原有 <see cref="Rigidbody2D.linearVelocity"/> 相加。
    /// </summary>
    public void Detonate(float blastRadius, float blastForce, AnimationCurve forceFalloff)
    {
        if (owner == null) return;

        owner.SpawnSatchelBlastRadiusRing(transform.position, blastRadius);

        Rigidbody2D playerRb = owner.PlayerRb;
        if (playerRb != null)
        {
            Vector2 blastPos = transform.position;
            Vector2 playerPos = playerRb.worldCenterOfMass;
            // 连线方向：从爆炸点指向 Raze（与冲量同向，效果为把 Raze 沿该射线推离爆炸中心）
            Vector2 fromBlastToRaze = playerPos - blastPos;
            float dist = fromBlastToRaze.magnitude;
            if (dist < blastRadius)
            {
                float norm = blastRadius > 1e-4f ? dist / blastRadius : 0f;
                float t = forceFalloff != null && forceFalloff.length > 0
                    ? forceFalloff.Evaluate(norm)
                    : 1f - norm;
                t = Mathf.Clamp01(t);
                t = Mathf.Max(t, owner.satchelKnockbackEdgeFloor);
                float nearScale = owner.GetSatchelNearCenterKnockbackScale(norm);
                float speedScale = owner.GetSatchelSpeedKnockbackMultiplier(playerRb.linearVelocity);
                float knockMag = blastForce * t * nearScale * speedScale;

                Vector2 pushDir;
                if (dist > 1e-4f)
                {
                    pushDir = fromBlastToRaze / dist;
                }
                else
                {
                    // 与爆炸点几乎重合：沿全局「背离重力」方向推，避免随机
                    Vector2 g = Physics2D.gravity;
                    pushDir = g.sqrMagnitude > 1e-6f ? (-g).normalized : Vector2.up;
                }

                owner.ApplyPlayerKnockback(pushDir * knockMag);
            }
        }

        Destroy(gameObject);
    }
}

/// <summary>引爆时在世界上画一圈圆线，与爆炸判定半径一致，一段时间后自动销毁。</summary>
public static class RazeSatchelBlastRing
{
    public static void Spawn(Vector2 worldPos, float radius, float duration, int segments, Color color, float lineWidth, string objectName = "SatchelBlastRing")
    {
        if (radius <= 0f || segments < 3) return;

        var go = new GameObject(string.IsNullOrEmpty(objectName) ? "BlastRing" : objectName);
        go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);

        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        var sh = Shader.Find("Sprites/Default");
        if (sh != null)
            lr.material = new Material(sh);

        for (int i = 0; i < segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f));
        }

        var fade = go.AddComponent<RazeSatchelBlastRingBehaviour>();
        fade.duration = Mathf.Max(0.05f, duration);
        fade.startColor = color;
    }
}

public class RazeSatchelBlastRingBehaviour : MonoBehaviour
{
    public float duration = 0.5f;
    public Color startColor = Color.white;
    float elapsed;

    void Update()
    {
        elapsed += Time.deltaTime;
        float a = 1f - Mathf.Clamp01(elapsed / duration);
        var c = startColor;
        c.a *= a;
        var lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.startColor = c;
            lr.endColor = c;
        }
        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
