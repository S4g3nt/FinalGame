using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 新增：为了获取当前关卡的名字
using TMPro;

/// <summary>
/// 将当前关收集进度显示到 UI Text（格式可改）。
/// 支持选关界面手动填入 levelId，也支持关卡内自动识别当前场景。
/// </summary>
public class CollectibleCounterDisplay : MonoBehaviour
{
    [Tooltip("选关界面：填入具体的关卡名(如 Level_01)。关卡内HUD：直接留空，会自动读取当前场景名！")]
    [SerializeField] string levelId = ""; 
    
    [SerializeField] TMP_Text label;
    [SerializeField] string format = "{0}/{1}";

    void Awake()
    {
        // 核心修改：如果面板里没填 levelId，就自动把当前场景的名字塞进去
        if (string.IsNullOrEmpty(levelId))
        {
            levelId = SceneManager.GetActiveScene().name;
        }
    }

    void OnEnable()
    {
        CollectibleProgress.LevelProgressChanged += OnProgressChanged;
        Refresh();
    }

    void Start() => Refresh();

    void OnDisable()
    {
        CollectibleProgress.LevelProgressChanged -= OnProgressChanged;
    }

    void OnProgressChanged(string changedLevel)
    {
        if (changedLevel == levelId) Refresh();
    }

    void Refresh()
    {
        if (label == null) return;
        int got = CollectibleProgress.GetCollectedCount(levelId);
        int total = CollectibleProgress.GetTotalInLevel(levelId);
        label.text = string.Format(format, got, total);
    }
}