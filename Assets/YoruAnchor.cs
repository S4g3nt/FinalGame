using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class YoruAnchorLogic : MonoBehaviour
{
    [Header("锚点设置")]
    public float moveSpeed = 8f; // 锚点向前跑的速度
    public float duration = 15f; // 锚点存在的时间（比如10秒后自动消失，防止场上遗留太多）

    private Rigidbody2D rb;
    private float moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 和假人一样，加上负号反转方向
        moveDirection = -Mathf.Sign(transform.localScale.x);

        // 设定生命周期，超时自动销毁
        Destroy(gameObject, duration);
    }

    void FixedUpdate()
    {
        // 给锚点一个向前的速度，保留 Y 轴速度让它能受重力贴地
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("DeathZone"))
            Destroy(gameObject);
    }
}