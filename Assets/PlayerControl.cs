using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Color defaultColor; // 用来记录角色原本的颜色
    private Coroutine hurtCoroutine; // 用来引用正在运行的变红协程
    [Header("移动参数")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Jett 缓降被动 (Hover)")]
    public float hoverFallSpeed = 2f; // 按住空格时的恒定下落速度

    // ==== 你的专属领地：视觉效果特效 (VFX) ====
    [Header("Jett 气流特效 (VFX)")]
    public ParticleSystem airFlowParticles; // [核心：在这里拖入你刚建的粒子物体]
    public Color normalGhostColor = new Color(0.5f, 0.8f, 1f, 0.5f); // 顺风行用的风蓝色残影
    public Color hoverGhostColor = new Color(1f, 1f, 1f, 0.3f); // 缓降滑翔用的纯白淡雅残影 (可选)

    [Header("Jett 顺风行 (Tailwind - 8 Directions)")]
    public float dashSpeed = 25f; 
    public float dashDuration = 0.2f; 
    public GameObject ghostPrefab; 
    public float ghostSpawnInterval = 0.03f; 

    [Header("地面检测")]
    public Transform groundCheck; 
    public float checkRadius = 0.2f;
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
                    if (rb != null)
                    {
                        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    }
                    // 禁用控制时，强制关闭所有特效
                    StopAllVFX();
                }
            }
        }
    }

    // ==== 组件与状态 ====
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr; 
    
    [SerializeField] private bool isGrounded;
    public bool IsGrounded => isGrounded; 
    
    // 输入变量
    private float moveInput;
    private float verticalInput; 
    private Vector2 rawInput; 
    private bool isHovering; // 是否正在按住空格键

    private bool isDashing; 
    private bool canDash = true; // 是否持有冲刺次数

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        
        // 如果忘记在面板里拖拽粒子系统，尝试在自身子物体里查找
        if (airFlowParticles == null)
        {
            airFlowParticles = GetComponentInChildren<ParticleSystem>();
        }
        
        // 游戏开始时，粒子必须是关闭状态
        if (airFlowParticles != null)
        {
            airFlowParticles.Stop();
        }
        
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) box.size = new Vector2(0.9f, 2f);

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            defaultColor = sr.color; // 记录游戏开始时的颜色（通常是白色）
        }
    }

    void Update()
    {
        if (!ControlsEnabled)
        {
            UpdateCommonPhysicsAndAnimation(0f);
            return;
        }

        if (isDashing) return; 

        // 1. 获取输入 
        moveInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical"); 
        rawInput = new Vector2(moveInput, verticalInput);
        
        // 获取缓降输入 (按住空格键)
        isHovering = Input.GetKey(KeyCode.Space);

        // 2. 基础物理和动画更新
        UpdateCommonPhysicsAndAnimation(moveInput);

        // 落地瞬间刷新冲刺次数
        if (isGrounded && !isDashing)
        {
            canDash = true;
        }

        // 3. 跳跃输入 (K 键起跳)
        if (Input.GetKeyDown(KeyCode.K) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 4. 冲刺输入 (L 键)
        if (Input.GetKeyDown(KeyCode.L) && canDash)
        {
            StartCoroutine(DashAction());
        }
    }

    void UpdateCommonPhysicsAndAnimation(float hInput)
    {
        if (hInput < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (hInput > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        anim.SetFloat("Speed", Mathf.Abs(hInput));
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        anim.SetBool("IsGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        if (!ControlsEnabled || isDashing) return;
        
        // --- [新增部分：处理缓降物理与视觉特效的嵌合] ---
        float currentVelocityY = rb.linearVelocity.y;
        bool shouldHover = !isGrounded && currentVelocityY < 0 && isHovering;

        // 1. 物理层：锁定下落速度
        if (shouldHover)
        {
            currentVelocityY = -hoverFallSpeed; 
        }
        
        // 2. 视觉层：控制气流粒子的开关 [最最核心的代码]
        if (airFlowParticles != null)
        {
            if (shouldHover && !airFlowParticles.isPlaying)
            {
                // 触发缓降，播放粒子
                airFlowParticles.Play();
            }
            else if (!shouldHover && airFlowParticles.isPlaying)
            {
                // 接触缓降，停止粒子（会自动把已经喷出来的粒子飘完，手感更自然）
                airFlowParticles.Stop();
            }
        }

        // 应用最终的物理移动
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, currentVelocityY);
    }

    IEnumerator DashAction()
    {
        canDash = false; 
        isDashing = true; 

        // 冲刺前，强制关闭可能正在喷的气流
        if (airFlowParticles != null && airFlowParticles.isPlaying)
        {
            airFlowParticles.Stop();
        }

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0; 

        Vector2 dashDirVector = rawInput == Vector2.zero ? new Vector2(transform.localScale.x, 0f) : rawInput.normalized;

        if (dashDirVector.x != 0)
        {
            transform.localScale = new Vector3(dashDirVector.x < 0 ? 1 : -1, 1, 1);
        }

        rb.linearVelocity = dashDirVector * dashSpeed;

        float dashTimer = dashDuration;
        while (dashTimer > 0)
        {
            float expectedSpeedMag = dashDirVector.magnitude * dashSpeed; 
            if (rb.linearVelocity.magnitude < expectedSpeedMag * 0.5f && dashTimer < dashDuration - 0.05f)
            {
                break; 
            }

            if (ghostPrefab != null && sr != null)
            {
                GameObject ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
                SpriteRenderer ghostSr = ghost.GetComponent<SpriteRenderer>();
                if (ghostSr != null)
                {
                    ghostSr.sprite = sr.sprite;
                    ghost.transform.localScale = transform.localScale; 
                    // 顺风行用帅气的风蓝色
                    SetGhostColor(ghostSr, normalGhostColor); 
                }
            }

            dashTimer -= ghostSpawnInterval;
            yield return new WaitForSeconds(ghostSpawnInterval); 
        }

        yield return new WaitForSeconds(dashDuration - (dashDuration > 0 ? ghostSpawnInterval : 0f)); 
        rb.linearVelocity = Vector2.zero; 
        rb.gravityScale = originalGravity; 
        
        isDashing = false; 
    }
    
    // 视觉辅助方法：安全设置残影颜色
    void SetGhostColor(SpriteRenderer targetSr, Color targetColor)
    {
        // 这里只是为了演示，实际可能需要获取残影身上的脚本来设色
        // targetSr.color = targetColor; 
    }
    
    // 强制关闭所有特效的方法，用于队友调用
    private void StopAllVFX()
    {
        if (airFlowParticles != null && airFlowParticles.isPlaying)
        {
            airFlowParticles.Stop();
        }
    }

    public void EnableControls() => ControlsEnabled = true;
    public void DisableControls() => ControlsEnabled = false;
    public void SetControls(bool enabled) => ControlsEnabled = enabled;

public void PlayHurtEffect(float duration)
{
    // 如果之前已经有一个红光协程在跑，先停掉它，防止冲突
    if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
    hurtCoroutine = StartCoroutine(HurtRoutine(duration));
}

private IEnumerator HurtRoutine(float duration)
{
    if (sr != null)
    {
        sr.color = Color.red; 
        yield return new WaitForSeconds(duration);
        sr.color = defaultColor; // 恢复到记录好的初始颜色
    }
    hurtCoroutine = null;
}

public void ResetColor()
{
    if (hurtCoroutine != null)
    {
        StopCoroutine(hurtCoroutine); // 停止变红协程
        hurtCoroutine = null;
    }
    if (sr != null)
    {
        sr.color = defaultColor; // 强行变回初始颜色
    }
}

    public void SetDeathVisual(bool isDead)
{
    if (isDead)
    {
        // 旋转 90 度（如果方向不对，可以改成 -90f）
        transform.localEulerAngles = new Vector3(0, 0, 90f); 
        
        // 强制把动画速度设为 0，防止倒地了还在跑
        if (anim != null) 
        {
            anim.SetFloat("Speed", 0f);
            anim.SetBool("IsGrounded", true);
        }
    }
    else
    {
        // 恢复正常角度
        transform.localEulerAngles = Vector3.zero;
    }
}
public void SetHurtColor(bool isHurt)

{

if (sr != null)

{

// 如果 isHurt 为 true 就变红，否则恢复初始颜色

sr.color = isHurt ? Color.red : defaultColor;

}

}
}