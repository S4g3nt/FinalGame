using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Color defaultColor; 
    private Coroutine hurtCoroutine; 

    [Header("移动参数")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

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

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
        Sr = GetComponent<SpriteRenderer>();
        
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) box.size = new Vector2(0.9f, 2f);

        if (Sr != null)
        {
            defaultColor = Sr.color; 
        }
    }

    void Update()
    {
        // 【核心修复】：将地面检测移到最前面！
        // 无论玩家是否被技能锁死，地面检测必须每帧实时执行，防止状态滞后。
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
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
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, jumpForce);
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

    void FixedUpdate()
    {
        if (!ControlsEnabled || IsSkillLocked) return;
        
        Rb.linearVelocity = new Vector2(moveInput * moveSpeed, Rb.linearVelocity.y);
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
}