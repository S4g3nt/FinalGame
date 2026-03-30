using UnityEngine;

// 脚本作用：让这个物体（残影）慢慢变透明直到消失
public class JettGhostEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;

    [Header("残影设置")]
    public float fadeSpeed = 3f; // 消失速度
    public Color ghostColor = new Color(0.5f, 0.8f, 1f, 0.5f); // 捷风风属性的淡蓝色，带 0.5 透明度

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // 设置初始颜色和透明度
        sr.color = ghostColor; 
    }

    void Update()
    {
        // 1. 每帧减少透明度 (Alpha)
        float newAlpha = sr.color.a - (fadeSpeed * Time.deltaTime);
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, newAlpha);

        // 2. 如果完全透明了，自动摧毁这个克隆体，节省内存
        if (sr.color.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}