using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class YoruCloneLogic : MonoBehaviour
{
    [Header("假人设置")]
    public float moveSpeed = 5f;
    public float duration = 5f; 
    
    [HideInInspector] // 在编辑器里隐藏，由脚本控制
    public bool isMoving = false; 

    private Rigidbody2D rb;
    private float moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 初始方向逻辑不变
        moveDirection = -Mathf.Sign(transform.localScale.x);
        
        // 注意：这里删掉了 Start 里的 Destroy 代码，因为我们要在激活后才开始倒计时
    }

    // 这是一个公共方法，供 YoruSkills 脚本调用来“激活”假人
    public void ActivateClone()
    {
        if (!isMoving)
        {
            isMoving = true;
            // 激活时才开始计算寿命倒计时
            Destroy(gameObject, duration);
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            // 只有在激活状态下才赋予速度
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // 未激活时保持静止（但保留重力，让它能站在地上）
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("DeathZone"))
        {
            Destroy(gameObject);
        }
    }
}