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
                // 虚空死亡：马里奥式弹起 + 快速坠落 + 摄像机冻结，再进入渐隐复活
                GameManager.Instance.StartVoidDeathFall(collision.gameObject);
            }
            else
            {
                Debug.LogError("错误：场景中没有找到 GameManager！请创建一个空物体并挂载 GameManager 脚本。");
            }
        }
    }
}