using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance;

    [Header("区域设置")]
    [SerializeField] private List<Region> allRegions = new List<Region>();

    [Header("摄像头过渡")]
    [SerializeField] private float transitionTime = 0.1f;

    [Header("区域锁定功能（空气墙）")]
    [Tooltip("启用后，离开过的区域入口会被空气墙阻挡")]
    public bool enableRegionLock = true;

    [Tooltip("场景中所有的空气墙（可留空，脚本会自动查找）")]
    [SerializeField] private List<AirWall> allAirWalls = new List<AirWall>();

    // 私有变量
    private Region currentRegion;
    private RegionCamera regionCamera;
    private Rigidbody2D playerRigidbody;
    private Vector2 savedVelocity;
    private float savedAngularVelocity;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        regionCamera = Camera.main.GetComponent<RegionCamera>();
        if (regionCamera == null)
        {
            Debug.LogError("主摄像头没有RegionCamera组件！");
        }

        // 自动查找所有Region
        if (allRegions.Count == 0)
        {
            Region[] foundRegions = FindObjectsOfType<Region>();
            allRegions.AddRange(foundRegions);
        }

        // 自动查找所有空气墙（如果没有手动拖入）
        if (allAirWalls.Count == 0)
        {
            AirWall[] foundWalls = FindObjectsOfType<AirWall>();
            allAirWalls.AddRange(foundWalls);
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Hero");
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        // 初始化摄像头到玩家所在的第一个区域
        if (player != null && allRegions.Count > 0)
        {
            foreach (Region region in allRegions)
            {
                if (region.ContainsPoint(player.transform.position))
                {
                    SetCurrentRegion(region);
                    break;
                }
            }
        }
    }

    // 玩家进入区域时调用
    public void PlayerInRegion(Region region)
    {
        if (isTransitioning || region == currentRegion) return;
        if (playerRigidbody == null)
            Debug.LogWarning("玩家刚体引用丢失，请重新获取！");

        GameObject player = GameObject.FindGameObjectWithTag("Hero");
        if (player == null) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null && pc.GhostModeActive) return;   // 幽灵模式无视区域锁定

        Vector3 playerCenter = player.transform.position;

        if (region.ContainsPoint(playerCenter))
        {
            StartCoroutine(TransitionToRegion(region));
        }
    }

    private IEnumerator TransitionToRegion(Region newRegion)
    {
        Debug.Log("TransitionToRegion 协程启动！");
        isTransitioning = true;

        if (playerRigidbody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Hero");
            if (player != null)
                playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        // 1. 保存玩家物理状态
        if (playerRigidbody != null)
        {
            savedVelocity = playerRigidbody.linearVelocity;
            savedAngularVelocity = playerRigidbody.angularVelocity;

            playerRigidbody.isKinematic = true;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // 2. 暂停时间
        Time.timeScale = 0f;

        // 3. 过渡摄像头
        regionCamera.TransitionToRegion(
            new Bounds(newRegion.transform.position + newRegion.regionBounds.center,
                      newRegion.regionBounds.size),
            transitionTime
        );

        // 4. 等待摄像头过渡完成（使用真实时间）
        yield return new WaitForSecondsRealtime(transitionTime);

        // 5. 恢复物理和时间
        Time.timeScale = 1f;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = savedVelocity;
            playerRigidbody.angularVelocity = savedAngularVelocity;
        }

        // ========== 新增：启用指向旧区域的空气墙 ==========
        if (enableRegionLock && currentRegion != null)
        {
            EnableAirWallsForRegion(currentRegion);
        }
        // ================================================

        // 6. 更新当前区域
        currentRegion = newRegion;
        regionCamera.SetCurrentRegionBounds(
            new Bounds(newRegion.transform.position + newRegion.regionBounds.center,
                      newRegion.regionBounds.size)
        );

        isTransitioning = false;
    }

    // 手动设置当前区域
    public void SetCurrentRegion(Region region)
    {
        if (region == null || region == currentRegion) return;

        currentRegion = region;
        regionCamera.SetCurrentRegionBounds(
            new Bounds(region.transform.position + region.regionBounds.center,
                      region.regionBounds.size)
        );
    }

    /// <summary>Call after the Hero GameObject was replaced (e.g. campfire swap).</summary>
    public void RefreshPlayerRigidbody()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Hero");
        playerRigidbody = player != null ? player.GetComponent<Rigidbody2D>() : null;
    }

    // ========== 空气墙管理方法 ==========

    /// <summary>
    /// 启用所有指向指定区域的空气墙
    /// </summary>
    private void EnableAirWallsForRegion(Region region)
    {
        foreach (AirWall wall in allAirWalls)
        {
            if (wall != null && wall.targetRegion == region)
            {
                wall.Enable();
            }
        }
    }

    /// <summary>
    /// 禁用所有空气墙（用于复活、重置关卡等）
    /// </summary>
    public void DisableAllAirWalls()
    {
        foreach (AirWall wall in allAirWalls)
        {
            if (wall != null)
                wall.Disable();
        }
    }

    // 注册区域
    public void RegisterRegion(Region region)
    {
        if (!allRegions.Contains(region))
        {
            allRegions.Add(region);
        }
    }

    // 取消注册区域
    public void UnregisterRegion(Region region)
    {
        if (allRegions.Contains(region))
        {
            allRegions.Remove(region);
        }
    }
}