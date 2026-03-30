using UnityEngine;

public class JetSkills : MonoBehaviour
{
    [Header("技能设置")]
    [Tooltip("缓降的恒定下落速度")]
    public float floatSpeed = 0.5f;
    [Tooltip("缓降生效的最低离地高度")]
    public float floatActivationHeight = 1.0f;
    
    [Space(10)]
    [Tooltip("冲刺的速度")]
    public float dashSpeed = 30f;
    [Tooltip("冲刺的持续时间（秒）")]
    public float dashDuration = 0.1f;
    
    [Header("组件与状态链接")]
    [Tooltip("自动获取，用于控制物理移动")]
    private Rigidbody2D rb;
    [Tooltip("自动获取，用于设置技能动画参数")]
    private Animator anim;
    [Tooltip("自动获取，用于查询玩家是否在地面")]
    private PlayerController playerController;
    
    [Header("调试")]
    public bool showDebugInfo = true;
    
    // ==== 核心技能状态 ====
    // 缓降
    private bool isFloated = false;
    private float originalGravityScale; // 用于存储原始重力值
    
    // 冲刺
    private bool dashState = true;      // 当前是否允许冲刺
    private bool isDashing = false;     // 当前是否正处于冲刺过程中
    private float dashTimeLeft = 0f;    // 冲刺剩余时间
    private Vector2 dashDirection;      // 本次冲刺的方向
    
    // 用于防止多次结束冲刺
    private bool dashEnding = false;
    
    void Start()
    {
        // 获取必要的组件引用
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("PlayerSkills: 在同个GameObject上未找到PlayerController脚本！");
        }
        
        // 存储原始重力值
        originalGravityScale = rb.gravityScale;
        
        if (showDebugInfo) Debug.Log("PlayerSkills: 技能系统初始化完成。");
    }
    
    void Update()
    {
        // 如果正在冲刺，跳过缓降输入检测
        if (!isDashing)
        {
            HandleFloatInput();
        }
        
        HandleDashInput();
        UpdateTimers();
        UpdateAnimatorParams();
    }
    
    void FixedUpdate()
    {
        // 如果正在冲刺，跳过缓降效果
        if (!isDashing)
        {
            ApplyFloatEffect();
        }
        
        ApplyDashEffect();
    }
    
    // ==================== 缓降逻辑 ====================
    private void HandleFloatInput()
    {
        // 如果正在冲刺，不处理缓降输入
        if (isDashing)
        {
            if (isFloated) EndFloat();
            return;
        }

        // 获取玩家是否在地面的状态
        bool isGrounded = (playerController != null) ? playerController.IsGrounded : false;

        // 核心条件1: 玩家必须长按跳跃键(K)
        bool tryingToFloat = Input.GetKey(KeyCode.K);

        // 核心条件2: 玩家不在地面，且正在下落
        bool canFloat = !isGrounded && rb.linearVelocity.y < 0;

        if( tryingToFloat && canFloat ){
            StartFloat();
        }else if(isFloated){
            EndFloat();
        }
    }

    private void ApplyFloatEffect()
    {
        // 如果正处于缓降状态，应用恒定的下落速度
        if (isFloated)
        {
            // 将Y轴速度锁定为缓降速度，同时保持原有的X轴速度
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -floatSpeed);
        }
        // 注意：重力缩放已在 StartFloat() 和 EndFloat() 中设置，此处无需重复设置
    }
    
    private void StartFloat()
    {
        isFloated = true;
        // 缓降时降低重力，使下落更平滑
        rb.gravityScale = 0.1f;
        
        if (showDebugInfo) Debug.Log("PlayerSkills: 缓降开始");
    }
    
    private void EndFloat()
    {
        isFloated = false;
        // 恢复原始重力
        rb.gravityScale = originalGravityScale;
        
        if (showDebugInfo) Debug.Log("PlayerSkills: 缓降结束");
    }
    
    // ==================== 冲刺逻辑 ====================
    private void HandleDashInput()
    {
        // 如果按下E键，且可以冲刺，且不在冲刺过程中
        if (Input.GetKeyDown(KeyCode.E) && dashState && !isDashing)
        {
            // 计算冲刺方向
            Vector2 desiredDirection = CalculateDashDirection();
            
            if (desiredDirection != Vector2.zero)
            {
                StartDash(desiredDirection);
            }
        }
    }
    
    private Vector2 CalculateDashDirection()
    {
        Vector2 direction = Vector2.zero;
        
        // 获取当前帧的精确输入
        float horizontal = Input.GetAxisRaw("Horizontal"); // 返回 -1, 0, 1
        float vertical = Input.GetAxisRaw("Vertical");
        
        // 应用方向优先级：右 > 左，上 > 下
        if (horizontal > 0) direction.x = 1;        // 右
        else if (horizontal < 0) direction.x = -1;  // 左
        
        if (vertical > 0) direction.y = 1;          // 上
        else if (vertical < 0) direction.y = -1;    // 下
        
        // 如果没有任何方向输入，默认向角色当前面向的方向冲刺
        if (direction == Vector2.zero)
        {
            // 假设PlayerController中，transform.localScale.x为负时面朝左
            direction = (transform.localScale.x > 0) ? Vector2.left : Vector2.right;
        }
        
        return direction.normalized; // 返回单位向量，确保斜方向速度正确
    }
    
    private void StartDash(Vector2 direction)
    {
        dashDirection = direction;
        isDashing = true;
        dashState = false;
        dashTimeLeft = dashDuration;
        dashEnding = false;
        
        // 冲刺开始时禁用PlayerController的控制
        if (playerController != null)
        {
            playerController.DisableControls();
        }
        
        // 冲刺时完全禁用重力
        rb.gravityScale = 0f;
        // 清除所有速度，避免惯性影响
        rb.linearVelocity = Vector2.zero;
        // 禁用阻力
        rb.linearDamping = 0f;
        
        if (showDebugInfo) Debug.Log($"PlayerSkills: 冲刺开始！方向: {dashDirection}, 持续时间: {dashDuration}秒");
    }
    
    private void ApplyDashEffect()
    {
        if (isDashing && dashTimeLeft > 0)
        {
            // 持续应用冲刺速度
            rb.linearVelocity = dashDirection * dashSpeed;
        }
    }
    
    private void EndDash()
    {
        if (dashEnding) return; // 防止重复调用
        
        dashEnding = true;
        isDashing = false;
        
        // 冲刺结束时立即停止所有运动
        rb.linearVelocity = Vector2.zero;
        
        // 恢复重力
        rb.gravityScale = originalGravityScale;
        // 恢复阻力
        rb.linearDamping = 0.5f; // Unity默认值
        
        // 恢复PlayerController的控制
        if (playerController != null)
        {
            playerController.EnableControls();
        }
        
    }
    
    // ==================== 状态与计时器更新 ====================
    private void UpdateTimers()
    {
        // 更新冲刺持续时间计时器
        if (isDashing && dashTimeLeft > 0)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                EndDash();
            }
        }
        
    }
    
    private void TryResetDashState()
    {
        // 重置冲刺状态的条件：1. 冷却结束 2. 玩家在地面
        if ( playerController != null && playerController.IsGrounded)
        {
            dashState = true;
        }
    }
    
    // ==================== 动画与工具方法 ====================
    private void UpdateAnimatorParams()
    {
        if (anim != null)
        {
            anim.SetBool("IsFloated", isFloated);
            anim.SetBool("IsDashing", isDashing);
        }
        
        TryResetDashState();
    }
    
    // 公共方法：供其他系统（如道具、技能点）调用，强制重置冲刺能力
    public void ResetDashAbility()
    {
        dashState = true;
        if (showDebugInfo) Debug.Log("PlayerSkills: 外部调用重置了冲刺能力。");
    }
    
    // 从外部禁用/启用技能系统
    public void DisableSkills()
    {
        // 如果正在冲刺，结束冲刺
        if (isDashing)
        {
            EndDash();
        }
        // 如果正在缓降，结束缓降
        if (isFloated)
        {
            EndFloat();
        }
        // 禁用冲刺输入
        dashState = false;
    }
    
    public void EnableSkills()
    {
        dashState = true;
    }
    
}