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
    [Tooltip("按下 L 后多帧采样方向键并做“或”合并，避免 W 与 L 同按时 Vertical 轴晚一帧仍为 0")]
    [SerializeField] int dashDirectionSampleFrames = 4;

    [Header("音效")]
    public AudioClip dashSfx;
    [Range(0f, 2f)] public float dashSfxVolume = 1f;

    private bool isHovering; 
    private bool isDashing; 
    private bool canDash = true; 

    AudioSource _sfx;

    void Start()
    {
        player = GetComponent<PlayerController>();

        if (airFlowParticles == null) airFlowParticles = GetComponentInChildren<ParticleSystem>();
        if (airFlowParticles != null) airFlowParticles.Stop();

        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
    }

    void Update()
    {
        if (GameplayInputLock.IsLocked)
            return;

        if (player.IsAwaitingRespawn)
        {
            StopAllVFX();
            return;
        }

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
        if (player.IsAwaitingRespawn || !player.ControlsEnabled || isDashing) return;
        
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

        if (dashSfx != null && _sfx != null)
            _sfx.PlayOneShot(dashSfx, dashSfxVolume);
        
        // 告诉基础控制器锁定常规行动
        player.IsSkillLocked = true; 

        if (airFlowParticles != null && airFlowParticles.isPlaying) airFlowParticles.Stop();

        float originalGravity = player.Rb.gravityScale;
        player.Rb.gravityScale = 0; 

        bool anyLeft = false, anyRight = false, anyDown = false, anyUp = false;
        int samples = Mathf.Max(1, dashDirectionSampleFrames);
        for (int i = 0; i < samples; i++)
        {
            ReadMovementKeysHeld(out bool kl, out bool kr, out bool kd, out bool ku);
            anyLeft |= kl;
            anyRight |= kr;
            anyDown |= kd;
            anyUp |= ku;
            if (i < samples - 1)
                yield return null;
        }

        Vector2 directInput = BuildCardinalInputFromKeys(anyLeft, anyRight, anyDown, anyUp);
        if (directInput == Vector2.zero)
            directInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 使用采样得到的 directInput 来决定冲刺方向
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
    
    static void ReadMovementKeysHeld(out bool left, out bool right, out bool down, out bool up)
    {
        left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    static Vector2 BuildCardinalInputFromKeys(bool left, bool right, bool down, bool up)
    {
        float hx = 0f;
        if (right && !left) hx = 1f;
        else if (left && !right) hx = -1f;
        else if (left || right)
            hx = Mathf.Sign(Input.GetAxisRaw("Horizontal"));

        float hy = 0f;
        if (up && !down) hy = 1f;
        else if (down && !up) hy = -1f;
        else if (up || down)
            hy = Mathf.Sign(Input.GetAxisRaw("Vertical"));

        return new Vector2(hx, hy);
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