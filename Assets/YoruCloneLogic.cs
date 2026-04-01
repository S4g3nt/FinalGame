using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class YoruCloneLogic : MonoBehaviour
{
    /// <summary>
    /// 仅当假人已从「静止预制体」被释放（<see cref="isMoving"/>）时视为有效；
    /// 碰撞体可在子物体上，会向父级查找脚本。
    /// </summary>
    public static bool IsReleasedClone(Collider2D col)
    {
        if (col == null) return false;
        var logic = col.GetComponent<YoruCloneLogic>();
        if (logic == null) logic = col.GetComponentInParent<YoruCloneLogic>();
        return logic != null && logic.isMoving;
    }

    [Header("假人设置")]
    public float moveSpeed = 5f;
    public float duration = 5f; 
    private Animator anim;
    
    [HideInInspector] // 在编辑器里隐藏，由脚本控制
    public bool isMoving = false; 

    private Rigidbody2D rb;
    private float moveDirection;
    bool _pendingSpringContactCheck;
    readonly ContactPoint2D[] _contactScratch = new ContactPoint2D[12];

    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        // 初始方向逻辑不变
        anim = GetComponent<Animator>(); // 获取动画机组件
        anim.SetBool("isMoving", false);
        
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
            if (anim != null) 
            {

                // 方案 B：如果你用的是 Bool (布尔值)
                anim.SetBool("isMoving", true);
            }
            Destroy(gameObject, duration);
            // 已在弹簧上时不会收到 OnCollisionEnter2D，下一物理帧补一次弹跳判定
            _pendingSpringContactCheck = true;
        }
    }

    void FixedUpdate()
    {
        if (_pendingSpringContactCheck)
        {
            _pendingSpringContactCheck = false;
            TryBounceFromTouchingSprings();
        }

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
        if (collision != null && collision.gameObject.CompareTag("DeathZone"))
            Destroy(gameObject);
    }

    void TryBounceFromTouchingSprings()
    {
        if (rb == null) return;
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        int n = rb.GetContacts(_contactScratch);
        for (int i = 0; i < n; i++)
        {
            Collider2D other = _contactScratch[i].collider;
            if (other == null) continue;
            var spring = other.GetComponent<SpringPad>();
            if (spring == null) spring = other.GetComponentInParent<SpringPad>();
            if (spring != null)
                spring.TryBounce(col, rb);
        }
    }
}