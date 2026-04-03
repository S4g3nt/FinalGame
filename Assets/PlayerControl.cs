using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Color defaultColor; 
    private Coroutine hurtCoroutine; 

    [Header("移动参数")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    [Tooltip("爆炸等效果叠在水平速度上的额外量，每 FixedUpdate 乘该系数衰减（避免与行走覆盖冲突）")]
    [Range(0f, 1f)]
    public float externalKnockbackHorizontalDecay = 0.88f;

    [Header("地面检测")]
    [Tooltip("检测中心，建议放在两脚之间的正下方")]
    public Transform groundCheck;
    [Tooltip("检测盒完整尺寸：X 略大于两脚跨度（解决平台边缘点检测不到），Y 很扁")]
    public Vector2 groundCheckBoxSize = new Vector2(0.72f, 0.08f);
    public LayerMask groundLayer;

    [Header("控制状态")]
    [SerializeField] private bool controlsEnabled = true; 
    public bool ControlsEnabled 
    { 
        get => controlsEnabled; 
        set
        {
            if (controlsEnabled != value)
            {
                controlsEnabled = value;
                if (!controlsEnabled)
                {
                    moveInput = 0;
                    if (Rb != null)
                    {
                        Rb.linearVelocity = new Vector2(0, Rb.linearVelocity.y);
                    }
                }
            }
        }
    }

    public bool IsSkillLocked { get; set; } = false; 
    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Sr { get; private set; }
    public Animator Anim { get; private set; }
    public bool IsGrounded { get; private set; }
    public Vector2 RawInput { get; private set; }
    
    private float moveInput;
    private float verticalInput;

    /// <summary>虚空死亡：冻结当前动画帧并暂时关闭碰撞，由 GameManager 协程结束后恢复。</summary>
    private bool voidDeathActive;
    public bool IsVoidDeathActive => voidDeathActive;
    private Collider2D[] voidDeathColliders;
    private float voidDeathSavedAnimSpeed = 1f;
    /// <summary>由炸药包等写入；FixedUpdate 中与 moveInput*moveSpeed 相加后再写入刚体，避免每帧被行走逻辑覆盖。</summary>
    float horizontalKnockback;

    [Header("调试：幽灵模式（P 开关）")]
    [Tooltip("Kinematic + 关固体碰撞，六向飘移；再按 P 关闭")]
    [SerializeField] float ghostMoveSpeed = 32f;

    bool ghostModeActive;
    RigidbodyType2D ghostSavedBodyType;
    float ghostSavedGravityScale;
    readonly List<(Collider2D col, bool wasEnabled)> ghostColliderStates = new List<(Collider2D, bool)>();

    public bool GhostModeActive => ghostModeActive;

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
        Sr = GetComponent<SpriteRenderer>();
        
        BoxCollider2D box = GetComponent<BoxCollider2D>();

        if (Sr != null)
        {
            defaultColor = Sr.color; 
        }
    }

    void Update()
    {
        if (voidDeathActive)
            return;

        if (Input.GetKeyDown(KeyCode.P))
            ToggleGhostMode();

        if (ghostModeActive)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            RawInput = new Vector2(moveInput, verticalInput);
            UpdateCommonPhysicsAndAnimation(moveInput);
            if (Anim != null)
            {
                Anim.SetBool("IsGrounded", false);
                Anim.SetFloat("Speed", RawInput.sqrMagnitude > 0.01f ? 1f : 0f);
            }
            return;
        }

        // 【核心修复】：将地面检测移到最前面！
        // 无论玩家是否被技能锁死，地面检测必须每帧实时执行，防止状态滞后。
        if (groundCheck == null)
            IsGrounded = false;
        else
        {
            Vector2 center = groundCheck.position;
            float angle = groundCheck.eulerAngles.z;
            IsGrounded = Physics2D.OverlapBox(center, groundCheckBoxSize, angle, groundLayer) != null;
        }
        if (Anim != null) Anim.SetBool("IsGrounded", IsGrounded);

        if (!ControlsEnabled)
        {
            UpdateCommonPhysicsAndAnimation(0f);
            return;
        }

        // 技能接管时跳过按键输入读取
        if (IsSkillLocked) return; 

        moveInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical"); 
        RawInput = new Vector2(moveInput, verticalInput);
        
        UpdateCommonPhysicsAndAnimation(moveInput);

        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log($"尝试跳跃! 地面状态: {IsGrounded}, 控制锁定: {IsSkillLocked}");
            if (IsGrounded && !IsSkillLocked)
            {
                // 与 Physics2D.gravity * gravityScale 相反的方向起跳（支持 Astra 反重力）
                float gy = Physics2D.gravity.y * Rb.gravityScale;
                float jumpY = Mathf.Approximately(gy, 0f) ? jumpForce : -Mathf.Sign(gy) * Mathf.Abs(jumpForce);
                Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, jumpY);
            }
        }
    }

    void UpdateCommonPhysicsAndAnimation(float hInput)
    {
        if (hInput < 0) transform.localScale = new Vector3(1, 1, 1);
        else if (hInput > 0) transform.localScale = new Vector3(-1, 1, 1);

        if (Anim != null) Anim.SetFloat("Speed", Mathf.Abs(hInput));
        // 原本在这里的 IsGrounded 检测已经被移到了 Update 最上方
    }

    /// <summary>水平额外速度写入缓冲；竖直分量立即加到刚体（行走不覆盖 y）。</summary>
    public void ApplyExternalKnockback(Vector2 delta)
    {
        if (Rb == null) return;
        horizontalKnockback += delta.x;
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Rb.linearVelocity.y + delta.y);
    }

    void FixedUpdate()
    {
        if (voidDeathActive)
            return;

        if (ghostModeActive)
        {
            if (Rb == null) return;
            Vector2 dir = new Vector2(moveInput, verticalInput);
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();
            Rb.MovePosition(Rb.position + dir * ghostMoveSpeed * Time.fixedDeltaTime);
            return;
        }

        if (!ControlsEnabled || IsSkillLocked) return;

        float walkX = moveInput * moveSpeed;
        Rb.linearVelocity = new Vector2(walkX + horizontalKnockback, Rb.linearVelocity.y);
        horizontalKnockback *= externalKnockbackHorizontalDecay;
        if (Mathf.Abs(horizontalKnockback) < 0.02f)
            horizontalKnockback = 0f;
    }

    public void EnableControls() => ControlsEnabled = true;
    public void DisableControls() => ControlsEnabled = false;
    public void SetControls(bool enabled) => ControlsEnabled = enabled;

    public void PlayHurtEffect(float duration)
    {
        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        hurtCoroutine = StartCoroutine(HurtRoutine(duration));
    }

    private IEnumerator HurtRoutine(float duration)
    {
        if (Sr != null)
        {
            Sr.color = Color.red; 
            yield return new WaitForSeconds(duration);
            Sr.color = defaultColor; 
        }
        hurtCoroutine = null;
    }

    public void ResetColor()
    {
        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine); 
            hurtCoroutine = null;
        }
        if (Sr != null) Sr.color = defaultColor; 
    }

    public void SetDeathVisual(bool isDead)
    {
        if (isDead)
        {
            transform.localEulerAngles = new Vector3(0, 0, 90f); 
            if (Anim != null) 
            {
                Anim.SetFloat("Speed", 0f);
                Anim.SetBool("IsGrounded", true);
            }
        }
        else
        {
            transform.localEulerAngles = Vector3.zero;
        }
    }

    public void SetHurtColor(bool isHurt)
    {
        if (Sr != null) Sr.color = isHurt ? Color.red : defaultColor;
    }

    /// <summary>触碰到虚空 DeathZone：定格当前动作、关掉碰撞，便于无视地形被抛起再坠落。</summary>
    public void BeginVoidDeathFreeze()
    {
        voidDeathActive = true;
        ControlsEnabled = false;
        moveInput = 0f;
        horizontalKnockback = 0f;
        if (Rb != null)
            Rb.linearVelocity = Vector2.zero;

        if (Anim != null)
        {
            voidDeathSavedAnimSpeed = Anim.speed;
            Anim.speed = 0f;
        }

        voidDeathColliders = GetComponentsInChildren<Collider2D>(true);
        if (voidDeathColliders != null)
        {
            foreach (Collider2D c in voidDeathColliders)
            {
                if (c != null)
                    c.enabled = false;
            }
        }
    }

    /// <summary>复活流程末尾调用；若未进行过虚空死亡则立即返回。</summary>
    public void EndVoidDeathFreeze()
    {
        if (!voidDeathActive)
            return;

        voidDeathActive = false;
        if (Anim != null)
            Anim.speed = voidDeathSavedAnimSpeed > 0f ? voidDeathSavedAnimSpeed : 1f;

        if (voidDeathColliders != null)
        {
            foreach (Collider2D c in voidDeathColliders)
            {
                if (c != null)
                    c.enabled = true;
            }
            voidDeathColliders = null;
        }
    }

    void ToggleGhostMode()
    {
        if (Rb == null)
            Rb = GetComponent<Rigidbody2D>();
        if (Rb == null) return;

        ghostModeActive = !ghostModeActive;

        if (ghostModeActive)
        {
            ghostSavedBodyType = Rb.bodyType;
            ghostSavedGravityScale = Rb.gravityScale;
            Rb.bodyType = RigidbodyType2D.Kinematic;
            Rb.gravityScale = 0f;
            Rb.linearVelocity = Vector2.zero;
            horizontalKnockback = 0f;

            ghostColliderStates.Clear();
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c == null || c.isTrigger) continue;
                ghostColliderStates.Add((c, c.enabled));
                c.enabled = false;
            }
        }
        else
        {
            Rb.bodyType = ghostSavedBodyType;
            Rb.gravityScale = ghostSavedGravityScale;
            Rb.linearVelocity = Vector2.zero;

            foreach (var pair in ghostColliderStates)
            {
                if (pair.col != null)
                    pair.col.enabled = pair.wasEnabled;
            }
            ghostColliderStates.Clear();
        }
    }

    /// <summary>外部强制关闭幽灵模式（例如快捷复活后恢复碰撞）。</summary>
    public void ForceExitGhostMode()
    {
        if (!ghostModeActive) return;
        ghostModeActive = false;
        if (Rb == null) return;
        Rb.bodyType = ghostSavedBodyType;
        Rb.gravityScale = ghostSavedGravityScale;
        Rb.linearVelocity = Vector2.zero;
        foreach (var pair in ghostColliderStates)
        {
            if (pair.col != null)
                pair.col.enabled = pair.wasEnabled;
        }
        ghostColliderStates.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Application.isPlaying && IsGrounded ? Color.cyan : Color.yellow;
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(groundCheck.position, Quaternion.Euler(0f, 0f, groundCheck.eulerAngles.z), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(groundCheckBoxSize.x, groundCheckBoxSize.y, 0.01f));
        Gizmos.matrix = prev;
    }
}