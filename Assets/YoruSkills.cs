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
    [Tooltip("假人向前生成的最大距离；锚点默认在脚下生成，不使用此距离")]
    public float spawnOffset = 1.0f;
    
    [Tooltip("假人生成的高度偏移（正数，防卡地）")]
    public float cloneHeightOffset = 0.5f; 
    
    [Tooltip("锚点生成的高度偏移（负数，用于贴地）")]
    public float anchorHeightOffset = -0.8f; 

    [Header("音效")]
    public AudioClip cloneReleaseSfx;
    [Range(0f, 2f)] public float cloneReleaseSfxVolume = 1f;
    public AudioClip anchorTeleportSfx;
    [Range(0f, 2f)] public float anchorTeleportSfxVolume = 1f;

    [Header("防卡墙设置")]
    [Tooltip("选择代表墙体/地面的 Layer，射线检测到这些层就会阻挡生成")]
    public LayerMask obstacleLayer;
    [Tooltip("生成物距离墙面的安全缓冲距离（防止模型边缘依然嵌在墙里）")]
    public float wallBufferDistance = 0.3f;

    // 用来记录当前场上的锚点和假人
    private GameObject currentAnchor;
    private GameObject currentClone;

    AudioSource _sfx;
    PlayerController _player;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
    }

    void Update()
    {
        if (GameplayInputLock.IsLocked)
            return;
        if (_player != null && _player.IsAwaitingRespawn)
            return;

        HandleAnchorSkill();
        HandleCloneSkill();
    }

        private void HandleAnchorSkill()
    {
        if (Input.GetKeyDown(anchorKey))
        {
            // 检查当前锚点是否正在消散
            bool isVanishing = false;
            if (currentAnchor != null)
            {
                var anchorLogic = currentAnchor.GetComponent<YoruAnchorLogic>();
                if (anchorLogic != null && anchorLogic.IsVanishing)
                    isVanishing = true;
            }

            // 如果场上没有锚点（或者锚点已经因为超时等原因被销毁了）
            if (currentAnchor == null)
            {
                // 锚点在脚下生成，避免门前前方射线把位置挤进门缝/地下
                Vector3 spawnPosition = GetSafeSpawnPosition(anchorHeightOffset, atFeet: true); 
                currentAnchor = Instantiate(anchorPrefab, spawnPosition, Quaternion.identity);
                SyncDirection(currentAnchor);
                TryIgnoreAnchorCloneCollisions();
            }
            else if (!isVanishing)
            {
                // 如果场上已经有锚点且未消散，执行传送
                if (anchorTeleportSfx != null && _sfx != null)
                    _sfx.PlayOneShot(anchorTeleportSfx, anchorTeleportSfxVolume);
                transform.position = currentAnchor.transform.position;
                // 传送后销毁锚点，这样下次按 L 就可以重新放置了
                Destroy(currentAnchor);
                currentAnchor = null;
            }
            // 如果锚点正在消散，则什么都不做
        }
    }

        private void HandleCloneSkill()
    {
        if (Input.GetKeyDown(cloneKey))
        {
            // 检查当前假人是否正在消散
            bool isVanishing = false;
            if (currentClone != null)
            {
                var cloneLogic = currentClone.GetComponent<YoruCloneLogic>();
                if (cloneLogic != null && cloneLogic.IsVanishing)
                    isVanishing = true;
            }

            // 情况1：场上没有假人 -> 生成一个静止的假人（预制）
            if (currentClone == null)
            {
                // (使用防卡墙的安全位置)
                Vector3 spawnPosition = GetSafeSpawnPosition(cloneHeightOffset);
                currentClone = Instantiate(clonePrefab, spawnPosition, Quaternion.identity);
                SyncDirection(currentClone);
                
                // 确保它刚出来时是不动的
                YoruCloneLogic logic = currentClone.GetComponent<YoruCloneLogic>();
                if (logic != null) logic.isMoving = false;
                TryIgnoreAnchorCloneCollisions();
            }
            // 情况2：场上已经有一个假人 -> 检查它是否处于静止状态，如果是，则释放它
            else if (!isVanishing)
            {
                YoruCloneLogic logic = currentClone.GetComponent<YoruCloneLogic>();
                if (logic != null && !logic.isMoving)
                {
                    // 再次按下 J，释放假人
                    logic.ActivateClone();
                    if (cloneReleaseSfx != null && _sfx != null)
                        _sfx.PlayOneShot(cloneReleaseSfx, cloneReleaseSfxVolume);
                }
            }
            // 如果假人正在消散，则什么都不做
        }
    }

    // 获取防卡墙的安全生成位置；atFeet 时锚点与角色同 X，不做前方射线（避免门前卡缝/卡地）
    private Vector3 GetSafeSpawnPosition(float heightOffset, bool atFeet = false)
    {
        Vector3 startPos = transform.position + new Vector3(0, heightOffset, 0);
        Vector3 horizontalPos;

        if (atFeet)
        {
            horizontalPos = startPos;
        }
        else
        {
            float facingDirection = -Mathf.Sign(transform.localScale.x);
            Vector2 castDir = new Vector2(facingDirection, 0);
            RaycastHit2D hit = Physics2D.Raycast(startPos, castDir, spawnOffset, obstacleLayer);

            if (hit.collider != null)
            {
                float safeDistance = hit.distance - wallBufferDistance;
                safeDistance = Mathf.Max(0, safeDistance);
                horizontalPos = startPos + new Vector3(facingDirection * safeDistance, 0, 0);
            }
            else
            {
                horizontalPos = startPos + new Vector3(facingDirection * spawnOffset, 0, 0);
            }
        }
        
        // 垂直检测：防止生成在地面以下
        float verticalCheckDistance = 1.0f;
        // 从水平位置稍微上方开始向下检测，避免从内部开始
        RaycastHit2D groundHit = Physics2D.Raycast(horizontalPos + Vector3.up * 0.5f, Vector2.down, verticalCheckDistance, obstacleLayer);
        if (groundHit.collider != null)
        {
            // 如果检测到地面，将位置调整到地面之上
            horizontalPos.y = groundHit.point.y + 0.1f;
        }
        
        return horizontalPos;
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