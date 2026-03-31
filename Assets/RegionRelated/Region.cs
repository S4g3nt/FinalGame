using UnityEngine;

public class Region : MonoBehaviour
{
    [Header("区域设置")]
    public Bounds regionBounds;  // 区域边界
    
    [Header("调试")]
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    private void Awake()
    {
        // 如果没有设置边界，使用Collider的边界
        if (regionBounds.size == Vector3.zero && GetComponent<Collider2D>() != null)
        {
            regionBounds = GetComponent<Collider2D>().bounds;
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Hero"))
        {
            // RegionManager.Instance.PlayerInRegion(this);
            // 记得调回来！！！！！！！！！！！！！！！！！！
        }
    }
    
    // 在Scene视图中绘制区域边界
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position + regionBounds.center, regionBounds.size);
        Gizmos.DrawCube(transform.position + regionBounds.center, regionBounds.size);
    }
    
    // 检查点是否在区域内
    public bool ContainsPoint(Vector3 point)
    {
        Bounds worldBounds = new Bounds(
            transform.position + regionBounds.center,
            regionBounds.size
        );
        return worldBounds.Contains(point);
    }
}