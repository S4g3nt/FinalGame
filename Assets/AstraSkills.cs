// using UnityEngine;

// /// <summary>
// /// Astra：切换自身重力方向（约 180°），不旋转摄像机。
// /// 需与 PlayerController 同物体；跳跃方向由 PlayerController 根据重力自动修正。
// /// </summary>
// [RequireComponent(typeof(PlayerController))]
// public class AstraSkills : MonoBehaviour
// {
//     [Header("按键")]
//     public KeyCode gravityToggleKey = KeyCode.Q;

//     [Header("表现")]
//     [Tooltip("为 true 时用 SpriteRenderer.flipY 翻转贴图，便于看出“在天花板上”。")]
//     public bool flipSpriteWhenInverted = true;

//     [Header("反重力下落")]
//     [Tooltip("翻转后重力相对默认值的倍数。1 = 与最初「只乘 -1」完全一致；略小于 1 时反重力侧下落稍慢一点。")]
//     [Range(0.88f, 1f)]
//     public float invertedGravityStrength = 0.93f;

//     private PlayerController player;
//     private Transform groundCheck;
//     private Vector3 groundCheckDefaultLocal;
//     private SpriteRenderer sr;
//     private bool gravityInverted;
//     private float baselineGravityScale;

//     void Awake()
//     {
//         // 不依赖 PlayerController.Start，避免执行顺序导致 Rb 未赋值或读到错误基准
//         Rigidbody2D rbBody = GetComponent<Rigidbody2D>();
//         baselineGravityScale = rbBody.gravityScale;
//         if (Mathf.Abs(baselineGravityScale) < 1e-4f)
//             baselineGravityScale = 1f;
//     }

//     void Start()
//     {
//         player = GetComponent<PlayerController>();
//         groundCheck = player.groundCheck;
//         if (groundCheck != null)
//             groundCheckDefaultLocal = groundCheck.localPosition;
//         sr = GetComponent<SpriteRenderer>();
//         if (sr == null && player != null)
//             sr = player.Sr;
//     }

//     void Update()
//     {
//         if (!player.ControlsEnabled || player.IsSkillLocked)
//             return;

//         if (Input.GetKeyDown(gravityToggleKey) && player.IsGrounded)
//             ToggleGravity();
//     }

//     public void ToggleGravity()
//     {
//         gravityInverted = !gravityInverted;

//         Rigidbody2D rb = player.Rb;
//         if (gravityInverted)
//         {
//             float m = Mathf.Abs(baselineGravityScale);
//             float s = Mathf.Sign(baselineGravityScale);
//             if (m < 1e-4f)
//                 m = 1f;
//             rb.gravityScale = -s * m * invertedGravityStrength;
//         }
//         else
//         {
//             rb.gravityScale = baselineGravityScale;
//         }

//         // 去掉反重力阶段残留的竖直速度，否则会一直带着向上/向下的惯性飘
//         rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

//         if (groundCheck != null)
//         {
//             // 增加一个微调值，让它不要钻得那么深
//             float offset = 0.15f; // 你可以根据需要调整这个值
//             groundCheck.localPosition = gravityInverted
//                 ? new Vector3(groundCheckDefaultLocal.x, -groundCheckDefaultLocal.y - offset, groundCheckDefaultLocal.z)
//                 : groundCheckDefaultLocal;
//         }

//         if (flipSpriteWhenInverted && sr != null)
//             sr.flipY = gravityInverted;
//     }

//     /// <summary>复活等逻辑若重置角色时，可调用以恢复默认重力与检测点。</summary>
//     public void ResetToNormalGravity()
//     {
//         if (!gravityInverted)
//             return;

//         gravityInverted = false;
//         Rigidbody2D rb = player.Rb;
//         rb.gravityScale = baselineGravityScale;
//         rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
//         if (groundCheck != null)
//             groundCheck.localPosition = groundCheckDefaultLocal;
//         if (sr != null)
//             sr.flipY = false;
//     }
// }
using UnityEngine;

/// <summary>
/// Astra：切换自身重力方向（约 180°），不旋转摄像机。
/// 需与 PlayerController 同物体；跳跃方向由 PlayerController 根据重力自动修正。
/// 规则：一次着地仅给一次翻转机会。若在地面使用，空中则无法再次使用。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class AstraSkills : MonoBehaviour
{
    [Header("按键")]
    public KeyCode gravityToggleKey = KeyCode.Q;

    [Header("表现")]
    [Tooltip("为 true 时用 SpriteRenderer.flipY 翻转贴图，便于看出“在天花板上”。")]
    public bool flipSpriteWhenInverted = true;

    [Header("反重力下落")]
    [Tooltip("翻转后重力相对默认值的倍数。1 = 与最初「只乘 -1」完全一致；略小于 1 时反重力侧下落稍慢一点。")]
    [Range(0.88f, 1f)]
    public float invertedGravityStrength = 0.93f;

    [Header("音效")]
    public AudioClip gravityToggleSfx;
    [Range(0f, 2f)] public float gravityToggleSfxVolume = 1f;

    [Header("次数限制")]
    [SerializeField] private bool hasFlipCharge = true; // 是否拥有翻转电量
    private float lastFlipTime; // 记录上次翻转的时间
    private const float flipCooldown = 0.2f; // 防止在地面翻转瞬间立刻回电的缓冲时间

    private PlayerController player;
    private Transform groundCheck;
    private Vector3 groundCheckDefaultLocal;
    private SpriteRenderer sr;
    private bool gravityInverted;
    private float baselineGravityScale;
    AudioSource _sfx;

    void Awake()
    {
        // 不依赖 PlayerController.Start，避免执行顺序导致 Rb 未赋值或读到错误基准
        Rigidbody2D rbBody = GetComponent<Rigidbody2D>();
        baselineGravityScale = rbBody.gravityScale;
        if (Mathf.Abs(baselineGravityScale) < 1e-4f)
            baselineGravityScale = 1f;
    }

    void Start()
    {
        player = GetComponent<PlayerController>();
        groundCheck = player.groundCheck;
        if (groundCheck != null)
            groundCheckDefaultLocal = groundCheck.localPosition;
        sr = GetComponent<SpriteRenderer>();
        if (sr == null && player != null)
            sr = player.Sr;

        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
    }

    void Update()
    {
        if (GameplayInputLock.IsLocked)
            return;

        if (!player.ControlsEnabled || player.IsSkillLocked || player.IsAwaitingRespawn)
            return;

        // 【充电逻辑】：如果玩家着地，并且已经过了翻转缓冲期，则充满电
        if (player.IsGrounded && Time.time > lastFlipTime + flipCooldown)
        {
            hasFlipCharge = true;
        }

        // 【翻转逻辑】：只有在有电的情况下才能触发
        if (Input.GetKeyDown(gravityToggleKey) && hasFlipCharge)
        {
            ToggleGravity();
            hasFlipCharge = false; // 按下后立即消耗电量，直到下次着地
            lastFlipTime = Time.time; // 记录翻转时刻
        }
    }

    public void ToggleGravity()
    {
        gravityInverted = !gravityInverted;

        Rigidbody2D rb = player.Rb;
        if (gravityInverted)
        {
            float m = Mathf.Abs(baselineGravityScale);
            float s = Mathf.Sign(baselineGravityScale);
            if (m < 1e-4f)
                m = 1f;
            rb.gravityScale = -s * m * invertedGravityStrength;
        }
        else
        {
            rb.gravityScale = baselineGravityScale;
        }

        // 去掉反重力阶段残留的竖直速度，避免惯性飘移
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (groundCheck != null)
        {
            // 翻转检测点位置
            // offset 用于微调，防止检测点钻入砖块过深或够不着表面
            float offset = 0.15f; 
            groundCheck.localPosition = gravityInverted
                ? new Vector3(groundCheckDefaultLocal.x, -groundCheckDefaultLocal.y - offset, groundCheckDefaultLocal.z)
                : groundCheckDefaultLocal;
        }

        if (flipSpriteWhenInverted && sr != null)
            sr.flipY = gravityInverted;

        if (gravityToggleSfx != null && _sfx != null)
            _sfx.PlayOneShot(gravityToggleSfx, gravityToggleSfxVolume);
    }

    /// <summary>复活重置逻辑</summary>
    public void ResetToNormalGravity()
    {
        gravityInverted = false;
        hasFlipCharge = true; // 重置电量

        Rigidbody2D rb = player.Rb;
        if (rb != null)
        {
            rb.gravityScale = baselineGravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        if (groundCheck != null)
            groundCheck.localPosition = groundCheckDefaultLocal;

        if (sr != null)
            sr.flipY = false;
    }
}