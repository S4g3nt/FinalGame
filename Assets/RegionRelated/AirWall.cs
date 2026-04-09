using UnityEngine;

public class AirWall : MonoBehaviour
{
    [Header("关联的区域")]
    [Tooltip("这个空气墙用于阻挡进入哪个区域？")]
    public Region targetRegion;

    [Header("调试")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1, 0, 0, 0.5f);

    private Collider2D wallCollider;

    private void Awake()
    {
        wallCollider = GetComponent<Collider2D>();
        if (wallCollider == null)
        {
            Debug.LogError($"空气墙 {gameObject.name} 缺少 Collider2D 组件！");
            return;
        }

        // 默认禁用
        wallCollider.enabled = false;
    }

    /// <summary>
    /// 启用空气墙（由 RegionManager 调用）
    /// </summary>
    public void Enable()
    {
        if (wallCollider != null)
            wallCollider.enabled = true;
    }

    /// <summary>
    /// 禁用空气墙（例如复活后重置）
    /// </summary>
    public void Disable()
    {
        if (wallCollider != null)
            wallCollider.enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // 在 Scene 视图中可视化空气墙位置（方便摆放）
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider2D box)
            {
                Gizmos.DrawCube(box.offset, box.size);
            }
            else if (col is EdgeCollider2D edge)
            {
                // 简单绘制边缘线
                Gizmos.DrawLine(edge.points[0], edge.points[1]);
            }
            else
            {
                Gizmos.DrawWireCube(col.bounds.center - transform.position, col.bounds.size);
            }
        }
    }
}