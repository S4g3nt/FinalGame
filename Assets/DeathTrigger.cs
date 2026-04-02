using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰到的是不是 Hero
        if (collision.CompareTag("Hero"))
        {
            Debug.Log("Hero 掉入陷阱！启动复活程序...");
            
            // 确保 GameManager 存在
            if (GameManager.Instance != null)
            {
                // 停止 Hero 当前的动作，避免在黑屏期间继续移动
                Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;

                // 启动 GameManager 里的协程
                GameManager.Instance.StartRespawn(collision.gameObject);
            }
            else
            {
                Debug.LogError("错误：场景中没有找到 GameManager！请创建一个空物体并挂载 GameManager 脚本。");
            }
        }
    }
}