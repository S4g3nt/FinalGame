using UnityEngine;

public class YoruSkills : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode anchorKey = KeyCode.L;
    public KeyCode cloneKey = KeyCode.J;

    [Header("预制体引用")]
    public GameObject anchorPrefab;
    public GameObject clonePrefab;

    [Header("技能参数")]
    public float spawnOffset = 1.0f; // 技能释放在角色前方的距离
    
    [Tooltip("假人生成的高度偏移（正数，防卡地）")]
    public float cloneHeightOffset = 0.5f; 
    
    [Tooltip("锚点生成的高度偏移（负数，用于贴地）")]
    public float anchorHeightOffset = -0.8f; 

    // 用来记录当前场上的锚点和假人
    private GameObject currentAnchor;
    private GameObject currentClone;

    void Update()
    {
        HandleAnchorSkill();
        HandleCloneSkill();
    }

    private void HandleAnchorSkill()
    {
        if (Input.GetKeyDown(anchorKey))
        {
            // 如果场上没有锚点（或者锚点已经因为超时等原因被销毁了）
            if (currentAnchor == null)
            {
                // 放置锚点
                Vector3 spawnPosition = GetSpawnPositionFront(anchorHeightOffset); 
                currentAnchor = Instantiate(anchorPrefab, spawnPosition, Quaternion.identity);
                SyncDirection(currentAnchor);
                TryIgnoreAnchorCloneCollisions();
            }
            else
            {
                // 如果场上已经有锚点，执行传送
                transform.position = currentAnchor.transform.position;
                // 传送后销毁锚点，这样下次按 L 就可以重新放置了
                Destroy(currentAnchor);
            }
        }
    }

    private void HandleCloneSkill()
    {
        if (Input.GetKeyDown(cloneKey))
        {
            // 情况1：场上没有假人 -> 生成一个静止的假人（预制）
            if (currentClone == null)
            {
                Vector3 spawnPosition = GetSpawnPositionFront(cloneHeightOffset);
                currentClone = Instantiate(clonePrefab, spawnPosition, Quaternion.identity);
                SyncDirection(currentClone);
                
                // 确保它刚出来时是不动的
                YoruCloneLogic logic = currentClone.GetComponent<YoruCloneLogic>();
                if (logic != null) logic.isMoving = false;
                TryIgnoreAnchorCloneCollisions();
            }
            // 情况2：场上已经有一个假人 -> 检查它是否处于静止状态，如果是，则释放它
            else
            {
                YoruCloneLogic logic = currentClone.GetComponent<YoruCloneLogic>();
                if (logic != null && !logic.isMoving)
                {
                    // 再次按下 J，释放假人
                    logic.ActivateClone();
                }
            }
        }
    }

    // 获取前方位置
    private Vector3 GetSpawnPositionFront(float heightOffset)
    {
        float facingDirection = -Mathf.Sign(transform.localScale.x);
        Vector3 offset = new Vector3(facingDirection * spawnOffset, heightOffset, 0);
        return transform.position + offset;
    }

    // 同步朝向给生成的物体
    private void SyncDirection(GameObject spawnedObject)
    {
        Vector3 newScale = spawnedObject.transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * Mathf.Sign(transform.localScale.x);
        spawnedObject.transform.localScale = newScale;
    }

    /// <summary>
    /// 让 Yoru 本体、锚点、假人互相不阻挡（仍与地形等实体正常碰撞）。
    /// </summary>
    private void TryIgnoreAnchorCloneCollisions()
    {
        Collider2D[] heroCols = GetComponentsInChildren<Collider2D>(true);

        if (currentAnchor != null)
        {
            Collider2D[] anchorCols = currentAnchor.GetComponentsInChildren<Collider2D>(true);
            IgnoreCollisionPairs(heroCols, anchorCols);
        }

        if (currentClone != null)
        {
            Collider2D[] cloneCols = currentClone.GetComponentsInChildren<Collider2D>(true);
            IgnoreCollisionPairs(heroCols, cloneCols);
        }

        if (currentAnchor != null && currentClone != null)
        {
            Collider2D[] anchorCols = currentAnchor.GetComponentsInChildren<Collider2D>(true);
            Collider2D[] cloneCols = currentClone.GetComponentsInChildren<Collider2D>(true);
            IgnoreCollisionPairs(anchorCols, cloneCols);
        }
    }

    private static void IgnoreCollisionPairs(Collider2D[] a, Collider2D[] b)
    {
        if (a == null || b == null) return;
        foreach (Collider2D ca in a)
        {
            if (ca == null || !ca.enabled || ca.isTrigger) continue;
            foreach (Collider2D cb in b)
            {
                if (cb == null || !cb.enabled || cb.isTrigger) continue;
                Physics2D.IgnoreCollision(ca, cb, true);
            }
        }
    }

    /// <summary>
    /// 玩家死亡复活：销毁场上锚点与假人。
    /// </summary>
    public void ClearDeployedForRespawn()
    {
        if (currentAnchor != null)
        {
            Destroy(currentAnchor);
            currentAnchor = null;
        }

        if (currentClone != null)
        {
            Destroy(currentClone);
            currentClone = null;
        }
    }
}