using UnityEngine;

// 这个脚本挂载在你那张雪山背景 Quad 或大 Sprite 上
public class SingleBgSubtleParallax : MonoBehaviour
{
    [Header("主摄像机 (留空则自动找 Main Camera)")]
    public Transform cameraTransform;

    [Header("微小移动系数 (数值越小移动越少，如 0.05 或 0.1)")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.05f;

    private Vector3 initialBackgroundPosition; // 背景的初始世界坐标
    private Vector3 initialCameraPosition;     // 摄像机的初始世界坐标

    void Start()
    {
        // 1. 如果没有拖拽摄像机，尝试自动寻找主摄像机
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("单图微视差脚本：没有找到摄像机！请手动拖拽摄像机到脚本卡槽里。");
            return;
        }

        // 2. 存储初始位置，作为微调的基准点
        initialBackgroundPosition = transform.position;
        initialCameraPosition = cameraTransform.position;
    }

    // 关键点：不用 Update，用 LateUpdate。
    // 确保摄像机已经完成了所有跟随角色的移动后，我们再来 LateUpdate 里“修正”背景位置。
    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 3. 计算从场景开始到现在，摄像机具体移动了多少 X 轴距离
        float cameraDisplacementX = cameraTransform.position.x - initialCameraPosition.x;

        // 4. 根据系数计算背景应该移动多少
        // dist 越小，背景就越静止
        float backgroundDist = cameraDisplacementX * parallaxFactor;

        // 5. 应用位移
        // 摄像机向右移动时（Positive），cameraDisplacementX 为正，backgroundDist 为正，背景向右（正向）微移。
        transform.position = new Vector3(
            initialBackgroundPosition.x + backgroundDist,
            initialBackgroundPosition.y, // 保持 Y 轴不动
            initialBackgroundPosition.z  // 保持 Z 轴不动
        );
    }
}