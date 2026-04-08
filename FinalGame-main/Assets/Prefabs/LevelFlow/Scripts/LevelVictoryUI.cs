using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 通关界面：全屏变暗 + 中央面板，显示 Congratulations、本关收集、用时、三个按钮。
/// 请在 Canvas 下搭好 UI 并把引用拖到 Inspector；或使用同物体上的子物体自动查找（见 Awake）。
/// </summary>
public class LevelVictoryUI : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    [Tooltip("需高于 GameManager 全屏 Fade（常见为 100），否则全透明 Fade 仍可能抢点击")]
    [SerializeField] int canvasSortOrder = 400;

    Canvas _canvas;

    [Header("面板与遮罩")]
    [SerializeField] GameObject rootPanel;
    [Tooltip("可选：全屏半透明黑底；若为空会尝试用 rootPanel 下第一个 Image")]
    [SerializeField] Image dimOverlay;

    [Header("文案")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text collectiblesText;
    [SerializeField] TMP_Text timeText;

    [Header("按钮")]
    [SerializeField] Button playAgainButton;
    [SerializeField] Button nextLevelButton;
    [SerializeField] Button levelSelectButton;

    [Header("场景名（与 Build Settings 中一致）")]
    [Tooltip("留空则隐藏「下一关」按钮")]
    [SerializeField] string nextSceneName = "";
    [SerializeField] string levelSelectSceneName = "0_Menu";

    [Header("Text format")]
    [SerializeField] string collectiblesFormat = "Collected: {0} / {1}";
    [SerializeField] string timeFormat = "Time: {0:mm\\:ss\\.f}";

    string _levelId;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (rootPanel != null)
            rootPanel.SetActive(false);
        // DimOverlay 常与 rootPanel 同级（全屏灰底）；仅关 rootPanel 时遮罩仍会显示
        if (dimOverlay != null)
            dimOverlay.gameObject.SetActive(false);

        AutoWireIfNeeded();
    }

    void AutoWireIfNeeded()
    {
        if (dimOverlay == null && rootPanel != null)
            dimOverlay = rootPanel.GetComponentInChildren<Image>(true);
    }

    void OnEnable()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgain);
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevel);
        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(OnLevelSelect);
    }

    void OnDisable()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveListener(OnPlayAgain);
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevel);
        if (levelSelectButton != null)
            levelSelectButton.onClick.RemoveListener(OnLevelSelect);
    }

    /// <param name="delayBeforeTimeScaleZero">
    /// 大于 0 时，在暂停游戏时间前按真实时间等待（秒）。用于先播放终点音效，避免 Time.timeScale=0 把声音卡掉。
    /// </param>
    public void ShowFromPortal(float delayBeforeTimeScaleZero = 0f)
    {
        if (LevelRunTracker.Instance == null)
            Debug.LogWarning("LevelVictoryUI：场景中未放置 LevelRunTracker，用时将为 0；建议在关卡中加一个 LevelRunTracker。");

        _levelId = LevelRunTracker.Instance != null
            ? LevelRunTracker.Instance.LevelId
            : SceneManager.GetActiveScene().name;

        float elapsed = LevelRunTracker.Instance != null
            ? LevelRunTracker.Instance.GetElapsedSeconds()
            : 0f;

        int got = CollectibleProgress.GetCollectedCount(_levelId);
        int total = CollectibleProgress.GetTotalInLevel(_levelId);

        if (titleText != null)
            titleText.text = "Congratulations";

        if (collectiblesText != null)
            collectiblesText.text = string.Format(collectiblesFormat, got, total);

        if (timeText != null)
        {
            var ts = System.TimeSpan.FromSeconds(elapsed);
            timeText.text = string.Format(timeFormat, ts);
        }

        if (nextLevelButton != null)
        {
            bool hasNext = !string.IsNullOrEmpty(nextSceneName);
            nextLevelButton.gameObject.SetActive(hasNext);
        }

        GameObject hero = GameObject.FindGameObjectWithTag("Hero");
        if (hero != null)
        {
            PlayerController pc = hero.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.DisableControls();
                if (pc.Rb != null)
                    pc.Rb.linearVelocity = Vector2.zero;
            }
        }

        if (dimOverlay != null)
            dimOverlay.gameObject.SetActive(true);
        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = canvasSortOrder;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SyncFadeRaycastTarget();

        if (dimOverlay != null)
        {
            Color c = dimOverlay.color;
            c.a = Mathf.Clamp01(c.a > 0.01f ? c.a : 0.75f);
            dimOverlay.color = c;
        }

        IsShowing = true;
        ApplyPauseAfterOptionalDelay(delayBeforeTimeScaleZero);
    }

    void ApplyPauseAfterOptionalDelay(float delayBeforeTimeScaleZero)
    {
        if (delayBeforeTimeScaleZero > 0.001f)
            StartCoroutine(PauseTimeScaleAfterRealtime(delayBeforeTimeScaleZero));
        else
            Time.timeScale = 0f;
    }

    IEnumerator PauseTimeScaleAfterRealtime(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (IsShowing)
            Time.timeScale = 0f;
    }

    void CloseAndRestoreTime()
    {
        IsShowing = false;
        Time.timeScale = 1f;
        if (dimOverlay != null)
            dimOverlay.gameObject.SetActive(false);
        if (rootPanel != null)
            rootPanel.SetActive(false);

        EndPortal2D.ResetGateForNewRun();
    }

    void OnPlayAgain()
    {
        CollectibleProgress.ClearLevel(_levelId);
        CloseAndRestoreTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnNextLevel()
    {
        if (string.IsNullOrEmpty(nextSceneName))
            return;

        CloseAndRestoreTime();
        SceneManager.LoadScene(nextSceneName);
    }

    void OnLevelSelect()
    {
        if (string.IsNullOrEmpty(levelSelectSceneName))
            return;

        CloseAndRestoreTime();
        SceneManager.LoadScene(levelSelectSceneName);
    }
}
