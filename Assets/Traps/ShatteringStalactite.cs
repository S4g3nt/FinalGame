using System.Collections;
using UnityEngine;

/// <summary>
/// 脆弱坠刺：待机时用向下射线检测 Hero / 假人 → 预警抖动 → 刚体下落 → 碰撞后程序化碎裂粒子 + 可选震屏。
/// 场景搭建步骤见本类顶部注释与项目说明。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ShatteringStalactite : MonoBehaviour
{
    [Header("检测（从物体位置向下）")]
    [Tooltip("射线起点相对本物体 pivot 的偏移（通常 pivot 在锥尖略上）")]
    public Vector2 rayOriginOffset = Vector2.zero;
    [Tooltip("向下检测长度（世界单位）")]
    public float detectionDistance = 12f;
    [Tooltip("射线只打勾选的层。Hero 与假人若在 Default 层，必须包含 Default；地面/平台/墙体在哪些层就一并勾选。\n默认 Everything（~0）= 全部层，已含 Default，一般无需改。")]
    public LayerMask detectionLayers = ~0;

    [Header("预警抖动")]
    public float warningDuration = 0.45f;
    [Tooltip("抖动角度（度）")]
    public float shakeAngleDeg = 6f;
    public float shakeFrequency = 18f;

    [Header("下落")]
    [Tooltip("开始下落后 Rigidbody2D.gravityScale")]
    public float fallGravityScale = 2.5f;
    public bool freezeRotationWhileFalling = true;
    [Tooltip("刚变为 Dynamic 后短时间内不触发碎裂，避免与天花板/嵌入的 static 体重叠时立刻 Shatter，表现为“不掉落”")]
    [Min(0f)] public float shatterCollisionGraceSeconds = 0.12f;

    [Header("复活后防连发")]
    [Tooltip("玩家复活重置坠刺后，这段时间内不做检测（避免与恢复 Hero 标签同一帧再次触发）")]
    [Min(0f)] public float detectionSuppressedAfterRespawnSeconds = 2f;
    [Tooltip("重置后须先出现「检测区内没有 Hero/假人」再允许触发，防止复活点仍在锥下时一恢复就落刺")]
    public bool requireClearLineAfterRespawn = true;

    [Header("碎裂粒子（纯代码生成）")]
    [Min(8)] public int particleBurstCount = 42;
    [Min(0.05f)] public float particleLifetime = 0.65f;
    public float particleSpeedMin = 2f;
    public float particleSpeedMax = 6.5f;
    public float particleSizeMin = 0.06f;
    public float particleSizeMax = 0.14f;
    public Color particleColorA = new Color(0.75f, 0.9f, 1f, 1f);
    public Color particleColorB = new Color(0.55f, 0.65f, 0.8f, 1f);
    [Tooltip("可选：碎屑贴图；不指定则为白点（URP 下仍可见）")]
    public Texture2D debrisTexture;

    [Header("可选震屏")]
    public bool cameraShakeOnShatter = true;
    public float shakeDuration = 0.22f;
    public float shakeAmplitude = 0.12f;

    [Header("运行时检测线（向下“镭射”示意）")]
    [Tooltip("原先只有选中物体时 Scene 里才看得到 Gizmo；勾选后游戏中用 LineRenderer 画向下检测段。")]
    public bool drawDetectionBeamInPlayMode = true;
    public Color beamColor = new Color(1f, 0.25f, 0.2f, 0.65f);
    [Min(0.001f)] public float beamWidth = 0.06f;

    Rigidbody2D _rb;
    Collider2D _col;
    Collider2D[] _ownColliders;
    LineRenderer _beamLine;
    float _baseRotationZ;
    bool _armed = true;
    bool _falling;
    bool _shattered;
    float _fallStartTime = -1000f;
    Vector3 _spawnWorldPos;
    Quaternion _spawnWorldRot;
    bool _spawnSaved;
    float _detectSuppressedUntil;
    bool _detectionPrimed = true;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _ownColliders = GetComponentsInChildren<Collider2D>(true);
        if (!_spawnSaved)
        {
            _spawnWorldPos = transform.position;
            _spawnWorldRot = transform.rotation;
            _spawnSaved = true;
        }
        _baseRotationZ = transform.eulerAngles.z;
        SetupIdlePhysics();
        if (drawDetectionBeamInPlayMode)
            EnsureDetectionBeam();
    }

    void SetupIdlePhysics()
    {
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.simulated = true;
        // 待机/预警阶段不要 FreezeRotation，否则部分版本下会抵消 transform 抖动；下落开始后再锁旋转
        _rb.constraints = RigidbodyConstraints2D.None;
    }

    bool IsOurCollider(Collider2D c)
    {
        if (c == null || _ownColliders == null) return false;
        foreach (Collider2D o in _ownColliders)
        {
            if (o == c) return true;
        }
        return false;
    }

    void EnsureDetectionBeam()
    {
        if (_beamLine != null) return;
        var beamGo = new GameObject("DetectionBeam");
        beamGo.transform.SetParent(transform, false);
        _beamLine = beamGo.AddComponent<LineRenderer>();
        _beamLine.useWorldSpace = true;
        _beamLine.positionCount = 2;
        _beamLine.startWidth = beamWidth;
        _beamLine.endWidth = beamWidth;
        _beamLine.numCapVertices = 4;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null)
        {
            var mat = new Material(sh);
            mat.color = beamColor;
            _beamLine.material = mat;
        }
        _beamLine.startColor = beamColor;
        _beamLine.endColor = beamColor;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _beamLine.sortingLayerID = sr.sortingLayerID;
            _beamLine.sortingOrder = sr.sortingOrder + 10;
        }
        _beamLine.enabled = true;
    }

    void LateUpdate()
    {
        if (_shattered || _beamLine == null) return;
        if (!drawDetectionBeamInPlayMode)
        {
            _beamLine.enabled = false;
            return;
        }
        _beamLine.enabled = !_falling;
        if (!_beamLine.enabled) return;
        Vector3 o = transform.position + (Vector3)rayOriginOffset;
        _beamLine.SetPosition(0, o);
        _beamLine.SetPosition(1, o + Vector3.down * detectionDistance);
        _beamLine.startWidth = beamWidth;
        _beamLine.endWidth = beamWidth;
    }

    void Update()
    {
        if (!_armed || _falling || _shattered) return;
        if (Time.time < _detectSuppressedUntil) return;

        bool seesVictim = TryDetectTargetBelow(out _);
        if (requireClearLineAfterRespawn && !_detectionPrimed)
        {
            if (!seesVictim) _detectionPrimed = true;
            return;
        }

        if (seesVictim)
            StartCoroutine(WarningThenFall());
    }

    bool TryDetectTargetBelow(out Collider2D targetCol)
    {
        targetCol = null;
        Vector2 origin = (Vector2)transform.position + rayOriginOffset;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, detectionDistance, detectionLayers);
        if (hits == null || hits.Length == 0) return false;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            // 起点在自身碰撞体内部时，RaycastAll 会先打到自己，误判为“被挡住”
            if (IsOurCollider(hit.collider)) continue;
            if (IsVictim(hit.collider))
            {
                targetCol = hit.collider;
                return true;
            }
            // 上方先有别的固体挡住视线，则不算“看到”下方目标
            break;
        }

        return false;
    }

    /// <summary>Hero 或任意阶段的 Yoru 假人（静止/已释放都算）。</summary>
    public static bool IsVictim(Collider2D c)
    {
        if (c == null) return false;
        if (c.CompareTag("Hero")) return true;
        return c.GetComponentInParent<YoruCloneLogic>() != null;
    }

    IEnumerator WarningThenFall()
    {
        _armed = false;
        float t = 0f;
        while (t < warningDuration)
        {
            t += Time.deltaTime;
            float wobble = Mathf.Sin(t * shakeFrequency * Mathf.PI * 2f) * shakeAngleDeg;
            transform.rotation = Quaternion.Euler(0f, 0f, _baseRotationZ + wobble);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, _baseRotationZ);
        StartFalling();
    }

    void StartFalling()
    {
        _falling = true;
        _fallStartTime = Time.time;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = fallGravityScale;
        _rb.WakeUp();
        _rb.simulated = true;
        if (freezeRotationWhileFalling)
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_shattered || collision.collider.isTrigger || !_falling) return;
        if (Time.time - _fallStartTime < shatterCollisionGraceSeconds) return;

        int n = collision.contactCount;
        Vector2 impact = collision.GetContact(0).point;
        for (int i = 0; i < n; i++)
        {
            var cp = collision.GetContact(i);
            TryKillVictim(cp.collider);
            impact = cp.point;
        }

        ShatterAt(impact);
    }

    void ShatterAt(Vector2 worldPoint)
    {
        _shattered = true;
        if (_beamLine != null) _beamLine.enabled = false;

        StalactiteDebrisBurst.Spawn(worldPoint, particleBurstCount, particleLifetime,
            particleSpeedMin, particleSpeedMax, particleSizeMin, particleSizeMax,
            particleColorA, particleColorB, debrisTexture);

        if (cameraShakeOnShatter)
            SimpleCameraShake.Shake(shakeDuration, shakeAmplitude);

        _col.enabled = false;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
            if (sr != null) sr.enabled = false;

        StartCoroutine(HideRootAfterShatterRoutine());
    }

    IEnumerator HideRootAfterShatterRoutine()
    {
        yield return new WaitForSeconds(particleLifetime + 0.35f);
        gameObject.SetActive(false);
    }

    /// <summary>玩家复活时由 <see cref="GameManager"/> 调用：恢复待机坠刺（含已碎裂隐藏的实例）。</summary>
    public void ResetForRespawn()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(_spawnWorldPos, _spawnWorldRot);
        _baseRotationZ = transform.eulerAngles.z;
        _armed = true;
        _falling = false;
        _shattered = false;
        _fallStartTime = -1000f;
        if (_col != null) _col.enabled = true;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            if (sr != null) sr.enabled = true;
        SetupIdlePhysics();
        if (drawDetectionBeamInPlayMode)
        {
            if (_beamLine == null) EnsureDetectionBeam();
            else _beamLine.enabled = true;
        }

        _detectSuppressedUntil = Time.time + detectionSuppressedAfterRespawnSeconds;
        _detectionPrimed = !requireClearLineAfterRespawn;
    }

    public static void ResetAllForRespawn()
    {
        foreach (ShatteringStalactite s in Object.FindObjectsByType<ShatteringStalactite>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s != null) s.ResetForRespawn();
        }
    }

    void TryKillVictim(Collider2D hitCollider)
    {
        if (hitCollider == null) return;

        if (hitCollider.CompareTag("Hero"))
        {
            var player = hitCollider.GetComponent<PlayerController>()
                         ?? hitCollider.GetComponentInParent<PlayerController>();
            if (player != null && GameManager.Instance != null)
            {
                player.DisableControls();
                player.SetDeathVisual(true);
                player.SetHurtColor(true);
                var prb = player.GetComponent<Rigidbody2D>();
                if (prb != null) prb.linearVelocity = Vector2.zero;
                GameManager.Instance.StartRespawn(player.gameObject);
            }
            return;
        }

        var cloneRoot = hitCollider.GetComponentInParent<YoruCloneLogic>();
        if (cloneRoot != null)
            Destroy(cloneRoot.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.85f);
        Vector3 o = transform.position + (Vector3)rayOriginOffset;
        Gizmos.DrawLine(o, o + Vector3.down * detectionDistance);
    }
}

/// <summary>
/// 在运行时创建 ParticleSystem，爆发一圈碎屑后自动销毁宿主物体。
/// </summary>
public static class StalactiteDebrisBurst
{
    public static void Spawn(Vector2 worldPos, int count, float lifetime,
        float speedMin, float speedMax, float sizeMin, float sizeMax,
        Color cA, Color cB, Texture2D texture)
    {
        var go = new GameObject("StalactiteDebrisBurst");
        go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.85f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(64, count * 2);

        var grad = new ParticleSystem.MinMaxGradient(cA, cB);
        main.startColor = grad;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;
        shape.arc = 360f;
        shape.rotation = new Vector3(0f, 0f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.y = new ParticleSystem.MinMaxCurve(0.5f, 2.2f);

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
        sz.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (texture != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.mainTexture = texture;
        }
        else
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh != null)
                renderer.material = new Material(sh);
        }

        ps.Play();
        Object.Destroy(go, lifetime + 0.5f);
    }
}

/// <summary>
/// 轻量震屏：协程挂在 Camera 上，不依赖第三方插件。
/// </summary>
public static class SimpleCameraShake
{
    static Coroutine _running;
    static MonoBehaviour _host;

    public static void Shake(float duration, float amplitude)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (_host == null)
            _host = cam.gameObject.GetComponent<CameraShakeHost>()
                    ?? cam.gameObject.AddComponent<CameraShakeHost>();
        if (_running != null)
            _host.StopCoroutine(_running);
        _running = _host.StartCoroutine(ShakeRoutine(cam.transform, duration, amplitude));
    }

    static IEnumerator ShakeRoutine(Transform camT, float duration, float amplitude)
    {
        Vector3 basePos = camT.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damp = 1f - (t / duration);
            float ox = (Mathf.PerlinNoise(t * 53f, 0f) * 2f - 1f) * amplitude * damp;
            float oy = (Mathf.PerlinNoise(0f, t * 53f) * 2f - 1f) * amplitude * damp;
            camT.localPosition = basePos + new Vector3(ox, oy, 0f);
            yield return null;
        }
        camT.localPosition = basePos;
        _running = null;
    }
}

/// <summary>仅用于承载协程，避免给场景相机随便挂未知脚本。</summary>
public sealed class CameraShakeHost : MonoBehaviour { }
