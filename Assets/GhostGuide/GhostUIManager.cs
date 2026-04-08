using UnityEngine;
using UnityEngine.UI;

public class GhostUIManager : MonoBehaviour
{
    public static GhostUIManager Instance;

    [Header("UI 容器与按键图片")]
    [Tooltip("包含按键UI的父物体，用于整体隐藏/显示")]
    public GameObject uiPanel; 
    public Image leftKeyImage;
    public Image rightKeyImage;
    public Image jumpKeyImage;
    public Image skillKeyImage;

    [Header("按键状态颜色")]
    public Color normalColor = new Color(1, 1, 1, 0.3f); // 未按下时（如半透明白）
    public Color pressedColor = new Color(0, 1, 0, 1f);  // 按下时（如绿色高亮）

    void Awake()
    {
        Instance = this;
        HideAllHints();
    }

    // 被 GhostGuideManager 逐帧调用
    public void UpdateHint(bool left, bool right, bool jump, bool skill)
    {
        if (uiPanel != null && !uiPanel.activeSelf) 
            uiPanel.SetActive(true);
        
        if (leftKeyImage) leftKeyImage.color = left ? pressedColor : normalColor;
        if (rightKeyImage) rightKeyImage.color = right ? pressedColor : normalColor;
        if (jumpKeyImage) jumpKeyImage.color = jump ? pressedColor : normalColor;
        if (skillKeyImage) skillKeyImage.color = skill ? pressedColor : normalColor;
    }

    public void HideAllHints()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
    }
}