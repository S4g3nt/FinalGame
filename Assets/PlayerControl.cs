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
    private Animator anim; // [新增] 声明一个 Animator 变量来控制动画大脑
    private bool isGrounded;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // [新增] 游戏开始时，自动获取身上的 Animator 组件
    }

    void Update()
    {
        // 1. 左右移动输入 (A, D 或 左右方向键，返回 -1, 0 或 1)
        moveInput = Input.GetAxisRaw("Horizontal");

        // --- [新增部分 1：角色转向逻辑] ---
        // 当输入大于 0 (往右走) 时，保持原有朝向
        if (moveInput < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        // 当输入小于 0 (往左走) 时，把 X 轴缩放变成 -1，实现完美镜像翻转
        else if (moveInput > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // --- [新增部分 2：向动画大脑发送信号] ---
        // 使用 Mathf.Abs 取绝对值。因为往左走 moveInput 是 -1，但我们的 Speed 必须是正数才能触发动画
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // 2. 检测是否在地面上
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        anim.SetBool("IsGrounded", isGrounded);

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