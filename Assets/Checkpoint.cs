using UnityEngine;

// 确保物体上有这两个组件，防止报错
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class Checkpoint : MonoBehaviour
{
    [Header("视觉设置 (Sprites)")]
    [Tooltip("未激活时显示的图片")]
    public Sprite inactiveSprite; // 图片1
    [Tooltip("激活后显示的图片")]
    public Sprite activeSprite;   // 图片2

    [Header("音效设置")]
    [Tooltip("激活时播放的一声音效")]
    public AudioClip activateSound;

    private SpriteRenderer sr;
    private AudioSource audioSource;
    private bool hasBeenActivated = false; // 记录状态，防止由于 GameManager 逻辑导致音效重复播放

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // 配置 AudioSource，确保它不会在游戏开始时自动播放，且不是 3D 音效（如果是 2D 游戏的话）
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        // 如果是 2D 平台游戏，建议将 spatialBlend 设为 0 (完全 2D)
        audioSource.spatialBlend = 0f; 

        // 初始化视觉状态
        if (inactiveSprite != null)
        {
            sr.sprite = inactiveSprite; // 初始设为图片1
        }
        
        // 既然使用了图片切换，通常要把颜色设为纯白(不叠加颜色)，除非你想给图片打个色偏
        sr.color = Color.white; 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 触碰 Hero 时，向 GameManager 申请激活自己
        if (collision.CompareTag("Hero"))
        {
            GameManager.Instance.SetActiveCheckpoint(this);
        }
    }

    // 被 GameManager 调用：切换图片 + 播放音效
    public void Activate()
    {
        // 切换图片
        if (activeSprite != null)
        {
            sr.sprite = activeSprite;
        }

        // 播放音效 (仅在第一次从非激活变激活时播放)
        if (!hasBeenActivated && activateSound != null)
        {
            // 使用 PlayOneShot 可以防止短时间内重复触发导致声音卡顿，
            // 且不会打断 AudioSource 上正在播放的其他声音（如果有的话）
            audioSource.PlayOneShot(activateSound);
            hasBeenActivated = true; // 标记为已激活
        }
    }

    // 被 GameManager 调用：变回初始图片
    public void Deactivate()
    {
        if (inactiveSprite != null)
        {
            sr.sprite = inactiveSprite;
        }
        hasBeenActivated = false; // 重置音效标记，以便下次可以再次触发
    }
}