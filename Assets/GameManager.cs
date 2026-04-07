using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private const string HeroTag = "Hero";

    [Header("场景跳转")]
    [Tooltip("选关界面场景的名称")]
    public string levelSelectSceneName = "0_Menu";

    [Header("暂停菜单")]
    [Tooltip("闯关场景内按 Esc 打开暂停菜单；关闭此项则 Esc 仍直接回主菜单")]
    public bool usePauseMenuInLevels = true;

    LevelPauseMenu pauseMenu;

    [Header("复活设置")]
    public Vector3 lastCheckpointPos;
    [Tooltip("进入本关时玩家出生点；未激活任何存档点时 - 键复活落点")]
    public Vector3 levelDefaultSpawnPos;
    public Image fadeImage;
    public float fadeSpeed = 2f;

    [Header("虚空死亡（DeathZone / 马里奥式）")]
    [Tooltip("触坑后先小跳起阶段时长，之后切换为快速下坠")]
    public float voidDeathPopDuration = 0.2f;
    [Tooltip("从触坑到开始黑屏渐隐前的演出总时长")]
    public float voidDeathVisibleTime = 1.15f;
    [Tooltip("弹起竖直速度（世界坐标，向上为正）")]
    public float voidDeathPopUpSpeed = 6.5f;
    [Tooltip("弹起水平速度随机范围（约 ± 该值）")]
    public float voidDeathPopHorizontalSpread = 2.4f;
    [Tooltip("进入下坠阶段时的竖直速度（应为负值）")]
    public float voidDeathFallDownSpeed = -12f;
    [Tooltip("下坠阶段重力缩放相对角色原 gravityScale 的倍数")]
    public float voidDeathFallGravityMultiplier = 3.5f;

    // --- 新增：记录当前激活的存档点脚本 ---
    private Checkpoint currentActiveCheckpoint;

    public bool IsLevelPaused => usePauseMenuInLevels && pauseMenu != null && pauseMenu.IsPaused;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameInputSettings.EnsureLoaded();
            if (usePauseMenuInLevels)
                EnsureLevelPauseMenu();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void EnsureLevelPauseMenu()
    {
        pauseMenu = GetComponentInChildren<LevelPauseMenu>(true);
        if (pauseMenu == null)
        {
            var go = new GameObject("LevelPauseMenu");
            go.transform.SetParent(transform, false);
            pauseMenu = go.AddComponent<LevelPauseMenu>();
        }

        pauseMenu.BindGameManager(this);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentActiveCheckpoint = null;
        // DontDestroyOnLoad 后，上一关的 FadeImage 已销毁，Inspector 引用会变成“空”，需按场景重新绑定
        TryBindFadeImageForCurrentScene();

        GameObject player = GameObject.FindGameObjectWithTag(HeroTag);
        if (player != null)
        {
            Vector3 p = player.transform.position;
            lastCheckpointPos = p;
            levelDefaultSpawnPos = p;
        }
    }

    void Update()
    {
        if (Instance != this)
            return;

        if (IsInGameplayScene())
        {
            if (usePauseMenuInLevels && pauseMenu != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    pauseMenu.OnEscapePressed();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToLevelSelect();
            }
        }

        if (GameplayInputLock.IsLocked)
            return;

        if (GameInputSettings.GetCheckpointRespawnDown())
            TryCheatRespawnNearestOrDefault();
    }

    bool IsInGameplayScene()
    {
        if (string.IsNullOrEmpty(levelSelectSceneName))
            return true;
        return SceneManager.GetActiveScene().name != levelSelectSceneName;
    }

    /// <summary>
    /// 退出当前关卡，返回选关场景
    /// </summary>
    public void ReturnToLevelSelect()
    {
        // 1. 强制把黑屏遮罩关掉，防止带着黑屏进选关界面
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            SyncFadeRaycastTarget();
        }

        // 2. 停止所有正在跑的协程（比如正在进行的复活倒计时）
        StopAllCoroutines();

        // 3. 恢复时间缩放（防止游戏处于暂停状态）
        Time.timeScale = 1f;

        // --- 新增：停止并销毁关卡内的背景音乐 ---
        if (BGMPlayer.Instance != null)
        {
            Destroy(BGMPlayer.Instance.gameObject);
            BGMPlayer.Instance = null; // 清空引用，方便后续进入新关卡时重新生成
        }

        // 4. 加载选关场景
        if (!string.IsNullOrEmpty(levelSelectSceneName))
        {
            SceneManager.LoadScene(levelSelectSceneName);
        }
        else
        {
            Debug.LogError("未设置选关场景名称！请在 GameManager 面板的 Level Select Scene Name 中填入场景名。");
        }
    }

    /// <summary>
    /// - 键：已激活存档点时传送到当前激活存档点（与 lastCheckpointPos 一致）；否则传送到本关初始出生点。无渐隐，并重置部分关卡状态。
    /// </summary>
    public void TryCheatRespawnNearestOrDefault()
    {
        GameObject player = GameObject.FindGameObjectWithTag(HeroTag);
        if (player == null) return;

        bool hasActivatedCheckpoint = currentActiveCheckpoint != null;
        Vector3 target = hasActivatedCheckpoint ? lastCheckpointPos : levelDefaultSpawnPos;

        player.transform.position = target;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ForceExitGhostMode();
            pc.EndVoidDeathFreeze();
            pc.SetDeathVisual(false);
            pc.SetHurtColor(false);
            pc.EnableControls();
        }

        AstraSkills astra = player.GetComponent<AstraSkills>();
        if (astra != null)
            astra.ResetToNormalGravity();

        RegionCamera regionCam = Object.FindFirstObjectByType<RegionCamera>();
        if (regionCam != null)
            regionCam.SetFollowFrozen(false);

        lastCheckpointPos = target;
        ResetLevelStateAfterPlayerDeath(player);
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(HeroTag);
        if (player != null)
        {
            Vector3 p = player.transform.position;
            lastCheckpointPos = p;
            levelDefaultSpawnPos = p;
        }

        TryBindFadeImageForCurrentScene();
    }

    /// <summary>
    /// 切换关卡后重新绑定名为 FadeImage 的全屏 Image（与场景内物体一致）。
    /// </summary>
    void TryBindFadeImageForCurrentScene()
    {
        if (fadeImage == null)
        {
            GameObject go = GameObject.Find("FadeImage");
            if (go != null)
                fadeImage = go.GetComponent<Image>();
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            SyncFadeRaycastTarget();
        }
    }

    /// <summary>
    /// 与全屏 Fade 的 alpha 同步：全透明时不接收射线，避免上层通关 UI 被挡点击。
    /// </summary>
    public void SyncFadeRaycastTarget()
    {
        if (fadeImage == null) return;
        fadeImage.raycastTarget = fadeImage.color.a > 0.01f;
    }

    // --- 新增：核心逻辑，确保只有一个绿 ---
    public void SetActiveCheckpoint(Checkpoint newCheckpoint)
    {
        // 如果踩到的就是当前已经激活的，直接无视
        if (currentActiveCheckpoint == newCheckpoint) return;

        // 1. 如果之前有激活的存档点，让它变回白色
        if (currentActiveCheckpoint != null)
        {
            currentActiveCheckpoint.Deactivate();
        }

        // 2. 更新当前存档点引用，并让它变绿
        currentActiveCheckpoint = newCheckpoint;
        currentActiveCheckpoint.Activate();

        // 3. 更新复活坐标
        lastCheckpointPos = newCheckpoint.transform.position;
        Debug.Log("存档点更新！坐标：" + lastCheckpointPos);
    }

    public void StartRespawn(GameObject player)
    {
        if (player == null) return;
        // 已在复活流程中（标签已剥掉）：忽略重复死亡，避免叠加重叠渐隐
        if (!player.CompareTag(HeroTag)) return;

        player.tag = "Untagged";
        StartCoroutine(RespawnSequenceStandard(player));
    }

    /// <summary>掉入虚空 DeathZone：定格动作、摄像机不动、抛起后快速坠落，再进入与普通死亡相同的渐隐复活。</summary>
    public void StartVoidDeathFall(GameObject player)
    {
        if (player == null) return;
        if (!player.CompareTag(HeroTag)) return;

        player.tag = "Untagged";
        StartCoroutine(VoidDeathFallRoutine(player));
    }

    IEnumerator RespawnSequenceStandard(GameObject player)
    {
        if (player == null) yield break;

        // 1. 等待死亡瞬间的停顿（此时玩家是红色的）
        yield return new WaitForSeconds(0.5f);
        if (player == null) yield break;

        yield return RespawnSequenceCore(player);
    }

    IEnumerator VoidDeathFallRoutine(GameObject player)
    {
        if (player == null) yield break;

        RegionCamera regionCam = Object.FindFirstObjectByType<RegionCamera>();
        if (regionCam != null)
            regionCam.SetFollowFrozen(true);

        PlayerController pc = player.GetComponent<PlayerController>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (pc != null)
            pc.BeginVoidDeathFreeze();

        AstraSkills astra = player.GetComponent<AstraSkills>();
        if (astra != null)
            astra.ResetToNormalGravity();

        float origGravityScale = rb != null ? rb.gravityScale : 1f;
        if (rb != null)
        {
            float hx = (Random.value * 2f - 1f) * voidDeathPopHorizontalSpread;
            hx += Random.Range(-0.6f, 0.6f);
            rb.linearVelocity = new Vector2(hx, voidDeathPopUpSpeed);
        }

        float elapsed = 0f;
        bool switchedToFall = false;
        while (elapsed < voidDeathVisibleTime && player != null)
        {
            elapsed += Time.deltaTime;
            if (!switchedToFall && elapsed >= voidDeathPopDuration && rb != null)
            {
                switchedToFall = true;
                float g = Mathf.Abs(origGravityScale) < 0.01f ? 1f : origGravityScale;
                rb.gravityScale = g * voidDeathFallGravityMultiplier;
                float vx = rb.linearVelocity.x * 0.25f;
                rb.linearVelocity = new Vector2(vx, voidDeathFallDownSpeed);
            }

            yield return null;
        }

        if (rb != null)
            rb.gravityScale = origGravityScale;

        yield return RespawnSequenceCore(player);
    }

    IEnumerator RespawnSequenceCore(GameObject player)
    {
        if (player == null) yield break;

        if (fadeImage == null)
            TryBindFadeImageForCurrentScene();

        // 屏幕变黑
        if (fadeImage != null)
        {
            while (fadeImage.color.a < 1f)
            {
                Color c = fadeImage.color;
                c.a += Time.deltaTime * fadeSpeed;
                fadeImage.color = c;
                yield return null;
            }
        }

        if (player == null) yield break;

        // 执行传送
        player.transform.position = lastCheckpointPos;

        ResetLevelStateAfterPlayerDeath(player);

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetDeathVisual(false);
            pc.SetHurtColor(false);
            pc.EnableControls();
            pc.EndVoidDeathFreeze();
        }

        AstraSkills astra = player.GetComponent<AstraSkills>();
        if (astra != null)
            astra.ResetToNormalGravity();

        player.tag = HeroTag;

        RegionCamera regionCam = Object.FindFirstObjectByType<RegionCamera>();
        if (regionCam != null)
            regionCam.SetFollowFrozen(false);

        // 屏幕变亮
        yield return new WaitForSeconds(0.3f);
        if (fadeImage != null)
        {
            while (fadeImage.color.a > 0f)
            {
                Color c = fadeImage.color;
                c.a -= Time.deltaTime * fadeSpeed;
                fadeImage.color = c;
                yield return null;
            }

            SyncFadeRaycastTarget();
        }
    }

    /// <summary>
    /// 复活时重置关卡交互与角色遗留物：压力板、坠刺、Raze 部署物、Yoru 锚点/假人。
    /// </summary>
    static void ResetLevelStateAfterPlayerDeath(GameObject player)
    {
        foreach (PressureButton btn in Object.FindObjectsByType<PressureButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (btn != null) btn.ResetToUnpressedState();
        }

        if (player == null) return;

        RazeSkills raze = player.GetComponent<RazeSkills>();
        if (raze != null) raze.ClearDeployedGearForRespawn();

        YoruSkills yoru = player.GetComponent<YoruSkills>();
        if (yoru != null) yoru.ClearDeployedForRespawn();

        ShatteringStalactite.ResetAllForRespawn();
    }
}