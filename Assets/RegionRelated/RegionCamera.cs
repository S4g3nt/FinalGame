using UnityEngine;
using System.Collections;

public class RegionCamera : MonoBehaviour
{
    [Header("摄像头设置")]
    [SerializeField] private Transform target;  // 跟随目标（玩家）
    
    [Header("摄像机Z轴位置")]
    [SerializeField] private float fixedZPosition = -10f;  // 固定Z坐标
    
    [Header("竖直位置设置")]
    [SerializeField] [Range(0f, 1f)] private float bottomMinDistance = 0.3f;  // 距离底部最小距离
    [SerializeField] [Range(0f, 1f)] private float bottomMaxDistance = 0.45f;  // 距离底部最大距离
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 私有变量
    private Camera cam;
    private Bounds currentRegionBounds = new Bounds();
    private bool isTransitioning = false;
    /// <summary>为 true 时 LateUpdate 不再跟随目标（用于虚空死亡等演出）。</summary>
    private bool followFrozen;
    
    // 竖直方向跟踪状态
    private float currentCameraY = 0f;  // 当前摄像头Y坐标
    
    /// <summary>After swapping the hero prefab, re-point follow target at the new Hero.</summary>
    public void RebindHeroTarget()
    {
        if (cam == null) cam = GetComponent<Camera>();
        GameObject h = GameObject.FindGameObjectWithTag("Hero");
        target = h != null ? h.transform : null;
        if (target != null)
            SetInitialPosition();
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
        {
            GameObject h = GameObject.FindGameObjectWithTag("Hero");
            if (h != null)
                target = h.transform;
        }
        
        if (target != null)
        {
            // 设置初始摄像头位置
            SetInitialPosition();
        }
        
        // 验证参数设置
        if (bottomMinDistance < 0f || bottomMinDistance > 1f)
        {
            bottomMinDistance = Mathf.Clamp(bottomMinDistance, 0f, 1f);
        }
        
        if (bottomMaxDistance < 0f || bottomMaxDistance > 1f)
        {
            bottomMaxDistance = Mathf.Clamp(bottomMaxDistance, 0f, 1f);
        }
        
        if (bottomMinDistance > bottomMaxDistance)
        {
            // 交换两者，确保min <= max
            float temp = bottomMinDistance;
            bottomMinDistance = bottomMaxDistance;
            bottomMaxDistance = temp;
        }
    }
    
    // 设置初始位置
    private void SetInitialPosition()
    {
        if (target == null) return;
        
        // 初始位置：水平居中，竖直方向将角色放在距离底部0.4的位置（中间值）
        float initialBottomDistance = (bottomMinDistance + bottomMaxDistance) * 0.5f;
        Vector3 desiredPosition = CalculateCameraPosition(initialBottomDistance);
        Vector3 limitedPosition = LimitPositionToRegion(desiredPosition);
        
        // 设置摄像头位置
        transform.position = new Vector3(limitedPosition.x, limitedPosition.y, fixedZPosition);
        currentCameraY = transform.position.y;
        
    }
    
    /// <summary>暂停/恢复摄像机跟随玩家（位置保持为冻结瞬间的值）。</summary>
    public void SetFollowFrozen(bool frozen) => followFrozen = frozen;

    private void LateUpdate()
    {
        if (followFrozen) return;
        if (target == null)
        {
            GameObject h = GameObject.FindGameObjectWithTag("Hero");
            if (h != null)
                target = h.transform;
        }

        // 幽灵模式：玩家始终在画面中心，不做竖直滞后也不做 Region 夹紧
        PlayerController pc = target.GetComponent<PlayerController>();
        if (pc != null && pc.GhostModeActive)
        {
            Vector3 centered = new Vector3(target.position.x, target.position.y, fixedZPosition);
            transform.position = centered;
            currentCameraY = centered.y;
            return;
        }

        if (isTransitioning) return;

        // 计算目标摄像头位置
        Vector3 desiredPosition = CalculateCameraPosition();
        
        // 将位置限制在当前区域边界内
        Vector3 limitedPosition = LimitPositionToRegion(desiredPosition);
        
        // 实时设置摄像头位置（无平滑）
        transform.position = new Vector3(limitedPosition.x, limitedPosition.y, fixedZPosition);
        currentCameraY = transform.position.y;
        
        // 调试信息
        if (showDebugInfo && Time.frameCount % 60 == 0)  // 每60帧输出一次
        {
            
            // 计算玩家在视口中的位置
            Vector3 viewportPos = cam.WorldToViewportPoint(target.position);
            float distanceToBottom = viewportPos.y;  // 视口y坐标就是距离底部的距离
        }
    }
    
    // 计算摄像头位置（使用当前玩家位置计算合适的摄像头位置）
    private Vector3 CalculateCameraPosition()
    {
        if (target == null) return transform.position;
        
        // 计算水平位置（始终居中）
        float targetX = target.position.x;
        
        // 计算竖直位置
        float targetY = CalculateVerticalPosition();
        
        return new Vector3(targetX, targetY, fixedZPosition);
    }
    
    // 计算摄像头竖直位置
    private float CalculateVerticalPosition()
    {
        if (target == null) return currentCameraY;
        
        // 获取摄像头视口尺寸
        float cameraHeight = 2f * cam.orthographicSize;
        
        // 计算玩家在当前摄像头视口中的垂直位置（0=底部, 1=顶部）
        Vector3 viewportPos = cam.WorldToViewportPoint(target.position);
        float distanceToBottom = viewportPos.y;  // 玩家距离底部的距离
        
        // 计算当前摄像头应该处于的Y坐标
        float desiredCameraY = currentCameraY;
        
        if (distanceToBottom < bottomMinDistance)
        {
            // 玩家低于最小边界（距离底部太近），需要将摄像头调低
            // 计算需要的摄像头Y坐标，使玩家刚好在bottomMinDistance位置
            desiredCameraY = CalculateCameraYForBottomDistance(bottomMinDistance);
        }
        else if (distanceToBottom > bottomMaxDistance)
        {
            // 玩家高于最大边界（距离底部太远），需要将摄像头调高
            // 计算需要的摄像头Y坐标，使玩家刚好在bottomMaxDistance位置
            desiredCameraY = CalculateCameraYForBottomDistance(bottomMaxDistance);
        }
        else
        {
            // 玩家在目标范围内，保持当前摄像头Y坐标
            desiredCameraY = currentCameraY;
        }
        
        return desiredCameraY;
    }
    
    // 计算指定底部距离对应的摄像头Y坐标
    private float CalculateCameraYForBottomDistance(float bottomDistance)
    {
        // 视口坐标转换：玩家在视口中的y坐标 = (玩家世界Y - 摄像头世界Y + 摄像头高度/2) / 摄像头高度
        // 变换公式：摄像头世界Y = 玩家世界Y + 摄像头高度/2 - 视口y * 摄像头高度
        // 其中视口y = bottomDistance
        
        float cameraHeight = 2f * cam.orthographicSize;
        float cameraY = target.position.y + cameraHeight * 0.5f - bottomDistance * cameraHeight;
        
        return cameraY;
    }
    
    // 计算指定底部距离对应的摄像头位置（用于初始位置计算）
    private Vector3 CalculateCameraPosition(float bottomDistance)
    {
        if (target == null) return transform.position;
        
        // 水平位置：玩家X坐标
        float targetX = target.position.x;
        
        // 竖直位置：计算指定底部距离对应的摄像头Y坐标
        float targetY = CalculateCameraYForBottomDistance(bottomDistance);
        
        return new Vector3(targetX, targetY, fixedZPosition);
    }
    
    // 将位置限制在区域边界内
    private Vector3 LimitPositionToRegion(Vector3 desiredPosition)
    {
        if (currentRegionBounds.size == Vector3.zero)
        {
            // 如果没有设置区域边界，直接返回期望位置
            return new Vector3(desiredPosition.x, desiredPosition.y, fixedZPosition);
        }
        
        float cameraHeight = 2f * cam.orthographicSize;
        float cameraWidth = cameraHeight * cam.aspect;
        
        float minX = currentRegionBounds.min.x + cameraWidth * 0.5f;
        float maxX = currentRegionBounds.max.x - cameraWidth * 0.5f;
        float minY = currentRegionBounds.min.y + cameraHeight * 0.5f;
        float maxY = currentRegionBounds.max.y - cameraHeight * 0.5f;
        
        // 如果区域太小，就居中
        if (minX > maxX)
        {
            minX = maxX = (currentRegionBounds.min.x + currentRegionBounds.max.x) * 0.5f;
        }
        if (minY > maxY)
        {
            minY = maxY = (currentRegionBounds.min.y + currentRegionBounds.max.y) * 0.5f;
        }
        
        // 应用边界限制
        float finalX = Mathf.Clamp(desiredPosition.x, minX, maxX);
        float finalY = Mathf.Clamp(desiredPosition.y, minY, maxY);
        
        return new Vector3(finalX, finalY, fixedZPosition);
    }
    
    // 切换到新区域
    public void TransitionToRegion(Bounds newBounds, float transitionTime = 0.1f)
    {
        StartCoroutine(TransitionRoutine(newBounds, transitionTime));
    }
    
    private IEnumerator TransitionRoutine(Bounds newBounds, float transitionTime)
    {
        isTransitioning = true;
        
        Vector3 startPosition = transform.position;
        currentRegionBounds = newBounds;
        
        // 计算目标位置
        Vector3 desiredPosition = CalculateCameraPosition();
        Vector3 endPosition = LimitPositionToRegion(desiredPosition);
        endPosition.z = fixedZPosition;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionTime);
            float smoothT = 1f - Mathf.Pow(1f - t, 3);
            
            Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, smoothT);
            currentPos.z = fixedZPosition;
            transform.position = currentPos;
            currentCameraY = currentPos.y;
            
            yield return null;
        }
        
        transform.position = endPosition;
        currentCameraY = endPosition.y;
        isTransitioning = false;
        
    }
    
    // 设置当前区域
    public void SetCurrentRegionBounds(Bounds bounds)
    {
        currentRegionBounds = bounds;
    }
    
    // 调试：在Scene视图中显示摄像头范围和玩家位置
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !showDebugInfo) return;
        
        if (cam != null && target != null)
        {
            // 绘制摄像头当前视口范围
            Gizmos.color = Color.red;
            float cameraHeight = 2f * cam.orthographicSize;
            float cameraWidth = cameraHeight * cam.aspect;
            
            Vector3 camPos = transform.position;
            Vector3 halfSize = new Vector3(cameraWidth * 0.5f, cameraHeight * 0.5f, 0);
            
            Gizmos.DrawWireCube(camPos, halfSize * 2);
            
            // 绘制竖直方向边界（距离底部的0.3和0.5位置）
            Gizmos.color = Color.yellow;
            
            // 底部Y坐标
            float bottomY = camPos.y - cameraHeight * 0.5f;
            
            // 计算距离底部0.3和0.5的世界Y坐标
            float minY = bottomY + cameraHeight * bottomMinDistance;
            float maxY = bottomY + cameraHeight * bottomMaxDistance;
            
            // 绘制水平线表示边界
            Vector3 minLineStart = new Vector3(camPos.x - cameraWidth * 0.5f, minY, 0);
            Vector3 minLineEnd = new Vector3(camPos.x + cameraWidth * 0.5f, minY, 0);
            Gizmos.DrawLine(minLineStart, minLineEnd);
            
            Vector3 maxLineStart = new Vector3(camPos.x - cameraWidth * 0.5f, maxY, 0);
            Vector3 maxLineEnd = new Vector3(camPos.x + cameraWidth * 0.5f, maxY, 0);
            Gizmos.DrawLine(maxLineStart, maxLineEnd);
            
            // 绘制玩家位置
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(target.position, 0.3f);
            
            // 绘制玩家在屏幕中的位置指示
            Vector3 viewportPos = cam.WorldToViewportPoint(target.position);
            if (viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                Gizmos.color = Color.cyan;
                float screenY = camPos.y - cameraHeight * 0.5f + viewportPos.y * cameraHeight;
                Vector3 screenPos = new Vector3(camPos.x, screenY, 0);
                Gizmos.DrawWireSphere(screenPos, 0.2f);
                
                // 绘制从玩家到其在屏幕上位置的连线
                Gizmos.DrawLine(target.position, screenPos);
            }
        }
    }
}