using UnityEngine;
using System.Collections;
public class SpikeTrap : MonoBehaviour
{
    [Header("设置")]
    public float freezeTime = 0.5f; // 变红并卡住的时间

private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Hero"))
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.DisableControls();
            player.SetDeathVisual(true); // 倒下
            
            // --- 修改：直接变红，它会一直红下去，直到有人叫它停 ---
            player.SetHurtColor(true); 

            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            GameManager.Instance.StartRespawn(collision.gameObject);
        }
    }
}

    // 一个简单的辅助，确保玩家复活后能动（或者你也可以在 GameManager 的 RespawnSequence 末尾加）
    private IEnumerator ReEnableControlsAfterDelay(PlayerController player, float delay)
    {
        yield return new WaitForSeconds(delay);
        player.EnableControls();
    }
}