using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 8f;
    public float jumpForce = 20f;

    [Header("地面检测")]
    public Transform groundCheck; // 在脚下放一个空物体
    public float checkRadius = 0.2f;
    public LayerMask groundLayer; // 刚才设置的 Ground 层

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. 左右移动输入 (A, D 或 左右方向键)
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 检测是否在地面上
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 3. 跳跃输入 (K 键)
        if (Input.GetKeyDown(KeyCode.K) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        // 4. 应用物理移动
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }
}