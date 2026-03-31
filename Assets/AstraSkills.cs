using UnityEngine;

/// <summary>
/// Astra：切换自身重力方向（约 180°），不旋转摄像机。
/// 需与 PlayerController 同物体；跳跃方向由 PlayerController 根据重力自动修正。
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

    private PlayerController player;
    private Transform groundCheck;
    private Vector3 groundCheckDefaultLocal;
    private SpriteRenderer sr;
    private bool gravityInverted;
    private float baselineGravityScale;

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
    }

    void Update()
    {
        if (!player.ControlsEnabled || player.IsSkillLocked)
            return;

        if (Input.GetKeyDown(gravityToggleKey))
            ToggleGravity();
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

        // 去掉反重力阶段残留的竖直速度，否则会一直带着向上/向下的惯性飘
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (groundCheck != null)
        {
            groundCheck.localPosition = gravityInverted
                ? new Vector3(groundCheckDefaultLocal.x, -groundCheckDefaultLocal.y, groundCheckDefaultLocal.z)
                : groundCheckDefaultLocal;
        }

        if (flipSpriteWhenInverted && sr != null)
            sr.flipY = gravityInverted;
    }

    /// <summary>复活等逻辑若重置角色时，可调用以恢复默认重力与检测点。</summary>
    public void ResetToNormalGravity()
    {
        if (!gravityInverted)
            return;

        gravityInverted = false;
        Rigidbody2D rb = player.Rb;
        rb.gravityScale = baselineGravityScale;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        if (groundCheck != null)
            groundCheck.localPosition = groundCheckDefaultLocal;
        if (sr != null)
            sr.flipY = false;
    }
}
