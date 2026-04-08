using UnityEngine;

public class GhostRegion : MonoBehaviour
{
    [Header("区域配置")]
    public string regionName = "Region_1"; 
    
    [Header("该区域的引导录像")]
    [Tooltip("把录制好并保存的 .txt JSON 文件拖到这里")]
    public TextAsset regionDataFile;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Hero"))
        {
            if (GhostGuideManager.Instance != null)
                GhostGuideManager.Instance.EnterRegion(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Hero"))
        {
            if (GhostGuideManager.Instance != null)
                GhostGuideManager.Instance.ExitRegion(this);
        }
    }
}