using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class YoruAnchorLogic : MonoBehaviour
{
    [Header("锚点设置")]
    public float moveSpeed = 8f; // 锚点向前跑的速度
    public float duration = 15f; // 锚点存在的时间（比如10秒后自动消失，防止场上遗留太多）

        private Rigidbody2D rb;
    private float moveDirection;
    private SpriteRenderer spriteRenderer;
    private bool isVanishing = false;
    public bool IsVanishing => isVanishing;
    private Coroutine vanishCoroutine;

    [Header("消散动画")]
    public float vanishDuration = 0.5f;
    public float vanishDownwardSpeed = 2f;
    public float vanishScaleShrink = 0.5f;

        void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // 和假人一样，加上负号反转方向
        moveDirection = -Mathf.Sign(transform.localScale.x);

        // 设定生命周期，超时自动消散
        Invoke("StartVanish", duration);
    }

    void FixedUpdate()
    {
        // 给锚点一个向前的速度，保留 Y 轴速度让它能受重力贴地
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

        private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.gameObject.CompareTag("DeathZone"))
            return;

        // 尖刺等与下落坑共用 DeathZone 标签，但挂 SpikeTrap：允许锚点穿过，便于落在本体过不去的位置
        if (collision.GetComponentInParent<SpikeTrap>() != null)
            return;

                StartVanish();
    }

    public void StartVanish()
    {
        if (isVanishing) return;
        isVanishing = true;
        
        // 禁用碰撞体
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        // 停止移动
        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        // 开始消散协程
        if (vanishCoroutine != null) StopCoroutine(vanishCoroutine);
        vanishCoroutine = StartCoroutine(VanishRoutine());
    }
    
    private IEnumerator VanishRoutine()
    {
        float timer = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * vanishScaleShrink;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (timer < vanishDuration)
        {
            timer += Time.deltaTime;
            float t = timer / vanishDuration;
            
            // 缩放
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            // 透明度
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            // 向下移动
            transform.Translate(Vector3.down * vanishDownwardSpeed * Time.deltaTime, Space.World);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}