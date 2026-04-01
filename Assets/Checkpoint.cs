using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("颜色设置")]
    public Color inactiveColor = Color.white; // 未激活时的颜色
    public Color activeColor = Color.green;   // 激活后的颜色

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.color = inactiveColor; // 初始设为未激活颜色
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 触碰 Hero 时，向 GameManager 申请激活自己
        if (collision.CompareTag("Hero"))
        {
            GameManager.Instance.SetActiveCheckpoint(this);
        }
    }

    // 被 GameManager 调用：变绿
    public void Activate()
    {
        sr.color = activeColor;
    }

    // 被 GameManager 调用：变回初始颜色
    public void Deactivate()
    {
        sr.color = inactiveColor;
    }
}