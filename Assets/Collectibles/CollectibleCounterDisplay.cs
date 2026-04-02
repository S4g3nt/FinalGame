using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将当前关收集进度显示到 UI Text（格式可改）。levelId 需与场景中 Collectible2D 一致。
/// </summary>
public class CollectibleCounterDisplay : MonoBehaviour
{
    [SerializeField] string levelId = "Level_01";
    [SerializeField] Text label;
    [SerializeField] string format = "{0}/{1}";

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
