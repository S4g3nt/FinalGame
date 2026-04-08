using UnityEngine;

/// <summary>
/// 挂在手雷预制体上。投掷后由 <see cref="BeginLifecycle"/> 启动引线；反弹达上限后不再弹并锁死刚体（不引爆）；时间到自动 <see cref="Detonate"/>；也可由 RazeSkills 手动引爆。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RazeGrenade : MonoBehaviour
{
    [HideInInspector] public RazeSkills owner;

    [Tooltip("用于识别可破坏地形（需与关卡物体 Tag 一致）")]
    public string destructibleTag = "DestructibleTerrain";

    int maxBounces;
    float minImpactForBounce;
    int bounceCount;
    bool detonated;
    bool frozenInPlace;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>在 <see cref="RazeSkills.ThrowGrenade"/> 里设置好 owner 并忽略与角色碰撞后调用。</summary>
    public void BeginLifecycle()
    {
        if (owner == null) return;

        maxBounces = Mathf.Max(0, owner.grenadeMaxBounces);
        minImpactForBounce = owner.grenadeMinBounceImpactSpeed;

        if (rb != null)
        {
            rb.collisionDetectionMode = owner.grenadeUseContinuousCollision
                ? CollisionDetectionMode2D.Continuous
                : CollisionDetectionMode2D.Discrete;
        }

        ApplyPhysicsMaterial(owner.grenadePhysicsBounciness, owner.grenadePhysicsFriction);

        if (owner.grenadeFuseSeconds > 0f)
            Invoke(nameof(TimedDetonate), owner.grenadeFuseSeconds);
    }

    void ApplyPhysicsMaterial(float bounciness, float friction)
    {
        var mat = new PhysicsMaterial2D("RazeGrenadeRuntime")
        {
            bounciness = Mathf.Clamp01(bounciness),
            friction = Mathf.Clamp01(friction)
        };
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col != null)
                col.sharedMaterial = mat;
        }
    }

    void TimedDetonate()
    {
        if (detonated) return;
        float r = owner != null ? owner.grenadeDestroyRadius : 2f;
        Detonate(r);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (detonated || frozenInPlace || owner == null) return;
        if (maxBounces <= 0) return;
        if (collision.relativeVelocity.magnitude < minImpactForBounce) return;

        bounceCount++;
        if (bounceCount >= maxBounces)
            FreezeInPlace();
    }

    /// <summary>达到反弹次数：清零速度、关重力、改为 Kinematic，不再弹跳与移动；仍可被引线/手动引爆。</summary>
    void FreezeInPlace()
    {
        if (frozenInPlace || rb == null) return;
        frozenInPlace = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        var mat = new PhysicsMaterial2D("RazeGrenadeStuck")
        {
            bounciness = 0f,
            friction = 1f
        };
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col != null)
                col.sharedMaterial = mat;
        }
    }

    void OnDestroy()
    {
        CancelInvoke(nameof(TimedDetonate));
        if (owner != null)
            owner.NotifyGrenadeDestroyed(this);
    }

    public void Detonate(float destroyRadius)
    {
        if (detonated) return;
        detonated = true;
        CancelInvoke(nameof(TimedDetonate));

        if (owner != null)
        {
            owner.PlayGrenadeExplosionSound();
            owner.SpawnGrenadeDestroyRadiusRing(transform.position, destroyRadius);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, destroyRadius);
        foreach (Collider2D c in hits)
        {
            if (c == null) continue;
            if (!c.CompareTag(destructibleTag)) continue;
            Destroy(c.gameObject);
        }

        Destroy(gameObject);
    }
}
