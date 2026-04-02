using UnityEngine;

/// <summary>
/// Raze：J = 脚下炸药包（再按 J 引爆）；落地（由非地面→地面）时刷新 2 次使用。L = 长按瞄准抛物线，松手投掷，再按 L 引爆手雷。
/// 与 Raze、炸药包、手雷之间的碰撞请在生成时用 Physics2D.IgnoreCollision 排除（本脚本已处理）。
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class RazeSkills : MonoBehaviour
{
    [Header("Raze — 速度上限")]
    [Tooltip("仅 Raze：刚体合速度上限；0 表示不限制")]
    [Min(0f)]
    public float razeMaxLinearSpeed = 20f;

    [Header("按键")]
    public KeyCode satchelKey = KeyCode.J;
    public KeyCode grenadeKey = KeyCode.L;

    [Header("炸药包预制体")]
    [Tooltip("需挂 RazeSatchel + Collider2D + Rigidbody2D（Dynamic），会自己下落；与角色碰撞已在生成时 Ignore")]
    public GameObject satchelPrefab;

    [Header("炸药包 — 生成")]
    public Vector2 satchelSpawnOffset = new Vector2(0f, -0.6f);
    [Tooltip("生成时炸药包继承 Raze 的水平速度 × 该系数")]
    [Range(0f, 2f)]
    public float satchelInheritVelocityScale = 0.8f;
    [Tooltip("生成时继承 Raze 的竖直速度 × 该系数。设为 1 且重力倍率与角色一致时，下落/上升与角色同步，不会在快速下落时相对跑到角色上方；0 则竖直初速为 0（旧行为）")]
    [Range(0f, 2f)]
    public float satchelInheritVerticalVelocityScale = 1f;
    [Tooltip("炸药包 Rigidbody2D.gravityScale = Raze 的 gravityScale × 该倍数（1 = 与角色同倍重力）")]
    [Min(0f)]
    public float satchelGravityScaleMultiplier = 1f;

    [Header("炸药包 — 爆炸")]
    [Tooltip("只对圆内的 Raze 生效（圆形范围衰减）")]
    public float satchelBlastRadius = 2.5f;
    [Tooltip("在爆炸圆心处、沿「爆炸点→Raze」方向叠加到线速度的最大标量；边缘按曲线衰减，与原有速度矢量相加")]
    public float satchelBlastForce = 18f;
    public AnimationCurve satchelForceFalloff = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(1f, 0f));
    [Tooltip("距离衰减曲线算出的系数不会低于该值（相对满力的比例）。贴爆炸半径边缘时仍有明显击退；0 则完全按曲线")]
    [Range(0f, 1f)]
    public float satchelKnockbackEdgeFloor = 0.38f;

    [Header("炸药包 — 击退微调")]
    [Tooltip("圆心附近额外变弱：在距离=0 时，在曲线结果上再乘该系数；到「混合满强」半径比例前平滑过渡到不削弱")]
    [Range(0f, 1f)]
    public float satchelNearCenterKnockbackScale = 0.42f;
    [Tooltip("距离/爆炸半径 ≤ 该值时，近心削弱从满过渡到关（1）。设为 0.5 时半径一半处不再因近心项变弱，便于对齐你说的「一半距离正好」")]
    [Range(0.05f, 1f)]
    public float satchelNearCenterBlendToFullByNorm = 0.5f;
    [Tooltip("高速时加强击退：最终再乘 (1 + 该项×当前线速度/参考速度)，解决跑得快时击退不明显")]
    [Min(0f)]
    public float satchelKnockbackSpeedGain = 0.55f;
    [Tooltip("与 PlayerController.moveSpeed 接近即可，用作速度归一化")]
    [Min(0.01f)]
    public float satchelKnockbackSpeedReference = 6f;
    [Tooltip("速度带来的倍数上限，防止极端速度飞太远")]
    [Min(1f)]
    public float satchelKnockbackSpeedMultiplierCap = 2.75f;

    [Header("炸药包 — 爆炸范围示意")]
    public bool satchelShowBlastRadiusRing = true;
    [Min(0.05f)] public float satchelBlastRingDuration = 0.55f;
    [Range(8, 128)] public int satchelBlastRingSegments = 48;
    public Color satchelBlastRingColor = new Color(1f, 0.45f, 0.1f, 0.9f);
    [Min(0.001f)] public float satchelBlastRingLineWidth = 0.07f;

    [Header("手雷预制体")]
    [Tooltip("需挂 RazeGrenade + Rigidbody2D(Dynamic) + Collider2D")]
    public GameObject grenadePrefab;

    [Header("手雷 — 物理与引线")]
    [Tooltip("与静态物碰撞并达到足够相对速度时记一次反弹；达到此次数后不再弹跳并锁死刚体（不引爆），仍受引线/手动引爆影响")]
    [Min(0)]
    public int grenadeMaxBounces = 2;
    [Tooltip("出手后经过该秒数自动爆炸；0 表示仅手动引爆且不按时间自爆")]
    [Min(0f)]
    public float grenadeFuseSeconds = 2f;
    [Tooltip("投掷时为手雷所有 Collider2D 设置 PhysicsMaterial2D 弹性")]
    [Range(0f, 1f)]
    public float grenadePhysicsBounciness = 0.48f;
    [Tooltip("投掷时为手雷 Collider2D 设置摩擦")]
    [Range(0f, 1f)]
    public float grenadePhysicsFriction = 0.32f;
    [Tooltip("相对速度低于此值的碰撞不计入反弹（减轻贴地抖动误计数）")]
    [Min(0f)]
    public float grenadeMinBounceImpactSpeed = 1f;
    public bool grenadeUseContinuousCollision = true;

    [Header("手雷 — 固定抛物线（相对面朝方向）")]
    [Tooltip("水平分量与面朝方向一致；45° 约为 (1,1) 归一化后乘速度")]
    public float grenadeThrowSpeed = 14f;
    [Range(15f, 75f)] public float grenadeThrowAngleDeg = 42f;

    [Header("手雷 — 轨迹预览")]
    public LineRenderer trajectoryLine;
    public int trajectorySteps = 32;
    public float trajectoryTimeStep = 0.05f;

    [Header("手雷 — 引爆")]
    [Tooltip("销毁带 DestructibleTerrain 标签的碰撞体所在物体")]
    public float grenadeDestroyRadius = 2f;

    [Header("手雷 — 生效范围示意")]
    public bool grenadeShowDestroyRadiusRing = true;
    [Min(0.05f)] public float grenadeBlastRingDuration = 0.55f;
    [Range(8, 128)] public int grenadeBlastRingSegments = 48;
    public Color grenadeBlastRingColor = new Color(0.2f, 0.85f, 1f, 0.9f);
    [Min(0.001f)] public float grenadeBlastRingLineWidth = 0.07f;

    private PlayerController player;
    private Rigidbody2D rb;

    private GameObject activeSatchel;
    private int satchelCharges = 2;
    bool wasGroundedLastFrame;

    private RazeGrenade activeGrenade;
    private bool grenadeAimHeld;
    /// <summary>用 L 引爆后必须松开 L 一次，才允许再次瞄准/投掷（避免松键瞬间误扔第二颗）。</summary>
    private bool grenadeSuppressUntilKeyRelease;
    /// <summary>本帧刚手动引爆的帧号，防止同帧或紧接着误触发投掷。</summary>
    private int grenadeLastDetonateFrame = -1;

    public Rigidbody2D PlayerRb => rb;

    /// <summary>炸药包等外部击退：水平进入 PlayerController 缓冲与行走合并，竖直立即叠加。</summary>
    public void ApplyPlayerKnockback(Vector2 delta)
    {
        if (player != null)
            player.ApplyExternalKnockback(delta);
        ClampRazeMaxLinearSpeed();
    }

    void FixedUpdate()
    {
        ClampRazeMaxLinearSpeed();
    }

    void LateUpdate()
    {
        ClampRazeMaxLinearSpeed();
    }

    void ClampRazeMaxLinearSpeed()
    {
        if (rb == null || razeMaxLinearSpeed <= 0f) return;
        Vector2 v = rb.linearVelocity;
        float m = v.magnitude;
        if (m > razeMaxLinearSpeed)
            rb.linearVelocity = v * (razeMaxLinearSpeed / m);
    }

    /// <param name="distOverRadius">距离 / 爆炸半径，0 在圆心</param>
    public float GetSatchelNearCenterKnockbackScale(float distOverRadius)
    {
        float norm = Mathf.Clamp01(distOverRadius);
        float end = Mathf.Clamp(satchelNearCenterBlendToFullByNorm, 0.05f, 1f);
        float k = Mathf.Clamp01(norm / end);
        k = k * k * (3f - 2f * k);
        return Mathf.Lerp(satchelNearCenterKnockbackScale, 1f, k);
    }

    public float GetSatchelSpeedKnockbackMultiplier(Vector2 velocity)
    {
        if (satchelKnockbackSpeedGain <= 0f) return 1f;
        float s = velocity.magnitude;
        float m = 1f + satchelKnockbackSpeedGain * (s / Mathf.Max(0.01f, satchelKnockbackSpeedReference));
        return Mathf.Min(m, satchelKnockbackSpeedMultiplierCap);
    }

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        bool grounded = player.IsGrounded;

        if (!player.ControlsEnabled || player.IsSkillLocked)
        {
            wasGroundedLastFrame = grounded;
            grenadeAimHeld = false;
            grenadeSuppressUntilKeyRelease = false;
            SetTrajectoryVisible(false);
            return;
        }

        // 仅在「落地」时补满 2 次；持续贴地不会每帧刷新，避免站在包上连按 J 刷出第三次
        if (grounded && !wasGroundedLastFrame)
            satchelCharges = 2;
        wasGroundedLastFrame = grounded;

        HandleSatchelInput();
        HandleGrenadeInput();
    }

    void HandleSatchelInput()
    {
        if (!Input.GetKeyDown(satchelKey)) return;

        if (activeSatchel != null)
        {
            var satchel = activeSatchel.GetComponent<RazeSatchel>();
            if (satchel != null)
                satchel.Detonate(satchelBlastRadius, satchelBlastForce, satchelForceFalloff);
            activeSatchel = null;
            return;
        }

        if (satchelCharges <= 0) return;
        if (satchelPrefab == null) return;

        Vector2 spawnPos = (Vector2)transform.position + SatchelOffsetWorld();
        activeSatchel = Instantiate(satchelPrefab, spawnPos, Quaternion.identity);
        var s = activeSatchel.GetComponent<RazeSatchel>();
        if (s != null) s.owner = this;

        var satchelRb = activeSatchel.GetComponent<Rigidbody2D>();
        if (satchelRb != null)
        {
            satchelRb.bodyType = RigidbodyType2D.Dynamic;
            satchelRb.gravityScale = rb.gravityScale * satchelGravityScaleMultiplier;
            satchelRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            float vx = rb.linearVelocity.x * satchelInheritVelocityScale;
            float vy = rb.linearVelocity.y * satchelInheritVerticalVelocityScale;
            satchelRb.linearVelocity = new Vector2(vx, vy);
        }

        IgnoreCollisionsWithPlayer(activeSatchel);
        if (activeGrenade != null)
            IgnoreCollisionsBetweenObjects(activeSatchel, activeGrenade.gameObject);

        satchelCharges--;
    }

    void HandleGrenadeInput()
    {
        if (activeGrenade != null)
        {
            SetTrajectoryVisible(false);
            grenadeAimHeld = false;
            if (Input.GetKeyDown(grenadeKey))
            {
                // 无论短按还是长按，都必须等 L 松开后才能再瞄/扔，否则会与「松手投掷」逻辑冲突多扔一颗
                grenadeSuppressUntilKeyRelease = true;
                DetonateActiveGrenade();
            }
            return;
        }

        if (grenadeSuppressUntilKeyRelease)
        {
            SetTrajectoryVisible(false);
            grenadeAimHeld = false;
            if (!Input.GetKey(grenadeKey))
                grenadeSuppressUntilKeyRelease = false;
            return;
        }

        if (Input.GetKey(grenadeKey))
        {
            grenadeAimHeld = true;
            DrawTrajectoryPreview();
        }
        else
        {
            if (grenadeAimHeld && Time.frameCount != grenadeLastDetonateFrame)
            {
                grenadeAimHeld = false;
                ThrowGrenade();
            }
            else if (!Input.GetKey(grenadeKey))
                grenadeAimHeld = false;

            SetTrajectoryVisible(false);
        }
    }

    Vector2 SatchelOffsetWorld()
    {
        float face = Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(face, 0f)) face = 1f;
        return new Vector2(satchelSpawnOffset.x * -face, satchelSpawnOffset.y);
    }

    /// <summary>与 PlayerController / Yoru 一致：scale.x 为负时朝世界右方。</summary>
    float FacingSignWorld()
    {
        float face = -Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(face, 0f)) face = 1f;
        return face;
    }

    Vector2 ComputeThrowVelocity()
    {
        float rad = grenadeThrowAngleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad) * FacingSignWorld(), Mathf.Sin(rad));
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
        return dir.normalized * grenadeThrowSpeed;
    }

    Vector2 GravityAccel()
    {
        return Physics2D.gravity * rb.gravityScale;
    }

    void DrawTrajectoryPreview()
    {
        if (grenadePrefab == null) return;

        Vector2 origin = (Vector2)transform.position + GrenadeSpawnOffsetWorld();
        Vector2 v = ComputeThrowVelocity();
        Vector2 g = GravityAccel();

        if (trajectoryLine == null)
        {
            Debug.DrawRay(origin, v.normalized * 2f, Color.yellow);
            return;
        }

        trajectoryLine.positionCount = trajectorySteps;
        trajectoryLine.enabled = true;
        Vector2 p = origin;
        Vector2 vel = v;
        for (int i = 0; i < trajectorySteps; i++)
        {
            trajectoryLine.SetPosition(i, p);
            vel += g * trajectoryTimeStep;
            p += vel * trajectoryTimeStep;
        }
    }

    Vector2 GrenadeSpawnOffsetWorld()
    {
        float f = FacingSignWorld();
        return new Vector2(0.35f * f, 0.2f);
    }

    void SetTrajectoryVisible(bool on)
    {
        if (trajectoryLine != null)
            trajectoryLine.enabled = on;
    }

    void ThrowGrenade()
    {
        if (grenadePrefab == null) return;

        Vector2 origin = (Vector2)transform.position + GrenadeSpawnOffsetWorld();
        GameObject go = Instantiate(grenadePrefab, origin, Quaternion.identity);
        var rg = go.GetComponent<Rigidbody2D>();
        if (rg != null)
        {
            rg.gravityScale = rb.gravityScale;
            rg.linearVelocity = ComputeThrowVelocity();
        }

        activeGrenade = go.GetComponent<RazeGrenade>();
        if (activeGrenade != null)
            activeGrenade.owner = this;

        IgnoreCollisionsWithPlayer(go);
        if (activeSatchel != null)
            IgnoreCollisionsBetweenObjects(go, activeSatchel);

        if (activeGrenade != null)
            activeGrenade.BeginLifecycle();
    }

    void DetonateActiveGrenade()
    {
        if (activeGrenade == null) return;
        activeGrenade.Detonate(grenadeDestroyRadius);
        activeGrenade = null;
        grenadeLastDetonateFrame = Time.frameCount;
    }

    /// <summary>手雷被销毁（引爆或掉出场景）时由 RazeGrenade 回调，避免引用悬挂。</summary>
    public void NotifyGrenadeDestroyed(RazeGrenade g)
    {
        if (g != null && activeGrenade == g)
            activeGrenade = null;
    }

    /// <summary>
    /// 玩家死亡复活：场上炸药包/手雷直接消失（不引爆），技能计数与瞄准状态复位。
    /// </summary>
    public void ClearDeployedGearForRespawn()
    {
        if (activeSatchel != null)
        {
            Destroy(activeSatchel);
            activeSatchel = null;
        }

        if (activeGrenade != null)
        {
            Destroy(activeGrenade.gameObject);
            activeGrenade = null;
        }

        satchelCharges = 2;
        wasGroundedLastFrame = false;
        grenadeAimHeld = false;
        grenadeSuppressUntilKeyRelease = false;
        grenadeLastDetonateFrame = -1;
        SetTrajectoryVisible(false);
    }

    void IgnoreCollisionsWithPlayer(GameObject other)
    {
        Collider2D[] pc = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] oc = other.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D a in pc)
        {
            if (a == null || !a.enabled) continue;
            foreach (Collider2D b in oc)
            {
                if (b == null || !b.enabled) continue;
                Physics2D.IgnoreCollision(a, b, true);
            }
        }
    }

    static void IgnoreCollisionsBetweenObjects(GameObject a, GameObject b)
    {
        Collider2D[] ac = a.GetComponentsInChildren<Collider2D>(true);
        Collider2D[] bc = b.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D x in ac)
        {
            if (x == null || !x.enabled) continue;
            foreach (Collider2D y in bc)
            {
                if (y == null || !y.enabled) continue;
                Physics2D.IgnoreCollision(x, y, true);
            }
        }
    }

    /// <summary>在爆炸点绘制与 <see cref="satchelBlastRadius"/> 一致的圆环（仅供调试/手感确认）。</summary>
    public void SpawnSatchelBlastRadiusRing(Vector2 worldPosition, float radiusWorld)
    {
        if (!satchelShowBlastRadiusRing || radiusWorld <= 0f) return;
        RazeSatchelBlastRing.Spawn(
            worldPosition,
            radiusWorld,
            satchelBlastRingDuration,
            Mathf.Max(8, satchelBlastRingSegments),
            satchelBlastRingColor,
            satchelBlastRingLineWidth,
            "SatchelBlastRing");
    }

    /// <summary>在手雷爆炸点绘制与 <see cref="grenadeDestroyRadius"/> 一致的圆环。</summary>
    public void SpawnGrenadeDestroyRadiusRing(Vector2 worldPosition, float radiusWorld)
    {
        if (!grenadeShowDestroyRadiusRing || radiusWorld <= 0f) return;
        RazeSatchelBlastRing.Spawn(
            worldPosition,
            radiusWorld,
            grenadeBlastRingDuration,
            Mathf.Max(8, grenadeBlastRingSegments),
            grenadeBlastRingColor,
            grenadeBlastRingLineWidth,
            "GrenadeBlastRing");
    }
}
