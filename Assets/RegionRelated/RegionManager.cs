using UnityEngine;
using System.Collections;  // 确保有这一行
using System.Collections.Generic;

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance;
    
    [Header("区域设置")]
    [SerializeField] private List<Region> allRegions = new List<Region>();
    
    [Header("摄像头过渡")]
    [SerializeField] private float transitionTime = 0.1f;
    
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
        // 检查玩家是否真的在区域内（中心点）
        GameObject player = GameObject.FindGameObjectWithTag("Hero");
        if (player == null) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null && pc.GhostModeActive) return;
        
        Vector3 playerCenter = player.transform.position;
        
        if (region.ContainsPoint(playerCenter))
        {
            StartCoroutine(TransitionToRegion(region));  // 这行应该用IEnumerator，而不是IEnumerator<T>
        }
    }
    
    // 修正：将返回类型改为非泛型的IEnumerator
    private IEnumerator TransitionToRegion(Region newRegion)
    {
        Debug.Log("TransitionToRegion 协程启动！");
        isTransitioning = true;
        if (playerRigidbody == null)
            Debug.LogWarning("玩家刚体引用丢失，请重新获取！");
        // 1. 保存玩家物理状态
        if (playerRigidbody != null)
        {
            savedVelocity = playerRigidbody.linearVelocity;
            savedAngularVelocity = playerRigidbody.angularVelocity;
            
            // 暂停物理
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