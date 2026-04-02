using System.Collections;
using UnityEngine;

// 强制要求挂载该脚本的物体上也必须有 PlayerController 组件
[RequireComponent(typeof(PlayerController))]
public class JettSkills : MonoBehaviour
{
    private PlayerController player; // 引用基础控制器

    [Header("Jett 缓降被动 (Hover)")]
    public float hoverFallSpeed = 2f; 

    [Header("Jett 气流特效 (VFX)")]
    public ParticleSystem airFlowParticles; 
    public Color normalGhostColor = new Color(0.5f, 0.8f, 1f, 0.5f); 
    public Color hoverGhostColor = new Color(1f, 1f, 1f, 0.3f); 

    [Header("Jett 顺风行 (Tailwind)")]
    public float dashSpeed = 25f; 
    public float dashDuration = 0.2f; 
    public GameObject ghostPrefab; 
    public float ghostSpawnInterval = 0.03f; 

    private bool isHovering; 
    private bool isDashing; 
    private bool canDash = true; 

    void Start()
    {
        player = GetComponent<PlayerController>();

        if (airFlowParticles == null) airFlowParticles = GetComponentInChildren<ParticleSystem>();
        if (airFlowParticles != null) airFlowParticles.Stop();
    }

    void Update()
    {
        if (!player.ControlsEnabled)
        {
            StopAllVFX();
            return;
        }

        if (isDashing) return; 

        // 1. 获取缓降输入
        isHovering = Input.GetKey(KeyCode.J);

        // 2. 落地刷新冲刺
        if (player.IsGrounded && !isDashing)
        {
            canDash = true;
        }

        // 3. 冲刺输入 (L 键)
        if (Input.GetKeyDown(KeyCode.L) && canDash)
        {
            StartCoroutine(DashAction());
        }
    }

    void FixedUpdate()
    {
        if (!player.ControlsEnabled || isDashing) return;
        
        // 缓降物理处理
        float currentVelocityY = player.Rb.linearVelocity.y;
        bool shouldHover = !player.IsGrounded && currentVelocityY < 0 && isHovering;

        if (shouldHover)
        {
            currentVelocityY = -hoverFallSpeed; 
            player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, currentVelocityY);
        }
        
        // 气流粒子表现
        if (airFlowParticles != null)
        {
            if (shouldHover && !airFlowParticles.isPlaying) airFlowParticles.Play();
            else if (!shouldHover && airFlowParticles.isPlaying) airFlowParticles.Stop();
        }
    }

    IEnumerator DashAction()
    {
        canDash = false; 
        isDashing = true; 
        
        // 告诉基础控制器锁定常规行动
        player.IsSkillLocked = true; 

        if (airFlowParticles != null && airFlowParticles.isPlaying) airFlowParticles.Stop();

        float originalGravity = player.Rb.gravityScale;
        player.Rb.gravityScale = 0; 

        // 【核心修复点】：在这里直接、实时地获取玩家瞬间的输入！
        // 避开了依赖 PlayerController 读取带来的延迟或锁死问题
        float dashH = Input.GetAxisRaw("Horizontal");
        float dashV = Input.GetAxisRaw("Vertical");
        Vector2 directInput = new Vector2(dashH, dashV);

        // 使用实时获取的 directInput 来决定冲刺方向
        Vector2 dashDirVector = directInput == Vector2.zero ? new Vector2(-transform.localScale.x, 0f) : directInput.normalized;

        if (dashDirVector.x != 0)
        {
            transform.localScale = new Vector3(dashDirVector.x < 0 ? 1 : -1, 1, 1);
        }

        player.Rb.linearVelocity = dashDirVector * dashSpeed;

        float dashTimer = dashDuration;
        while (dashTimer > 0)
        {
            float expectedSpeedMag = dashDirVector.magnitude * dashSpeed; 
            if (player.Rb.linearVelocity.magnitude < expectedSpeedMag * 0.5f && dashTimer < dashDuration - 0.05f)
            {
                break; 
            }

            if (ghostPrefab != null && player.Sr != null)
            {
                GameObject ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
                SpriteRenderer ghostSr = ghost.GetComponent<SpriteRenderer>();
                if (ghostSr != null)
                {
                    ghostSr.sprite = player.Sr.sprite;
                    ghost.transform.localScale = transform.localScale; 
                    SetGhostColor(ghostSr, normalGhostColor); 
                }
            }

            dashTimer -= ghostSpawnInterval;
            yield return new WaitForSeconds(ghostSpawnInterval); 
        }

        yield return new WaitForSeconds(dashDuration - (dashDuration > 0 ? ghostSpawnInterval : 0f)); 
        player.Rb.linearVelocity = Vector2.zero; 
        player.Rb.gravityScale = originalGravity; 
        
        isDashing = false; 
        
        // 冲刺结束，交还控制权
        player.IsSkillLocked = false; 
    }
    
    void SetGhostColor(SpriteRenderer targetSr, Color targetColor)
    {
        // targetSr.color = targetColor; 
    }
    
    private void StopAllVFX()
    {
        if (airFlowParticles != null && airFlowParticles.isPlaying) airFlowParticles.Stop();
    }
}