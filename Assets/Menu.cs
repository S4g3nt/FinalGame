using UnityEngine;
using UnityEngine.SceneManagement; // 必须有这句才能切场景

public class MenuManager : MonoBehaviour
{
    [Header("把两个 UI 面板拖进来")]
    public GameObject titlePanel;       // 你的“按任意键”主界面
    public GameObject levelSelectPanel; // 你的选关界面

    private bool isAtTitle = true; // 状态锁：判断当前是不是在主界面

    void Start()
    {
        // 游戏一运行，强制显示主界面，隐藏选关界面
        titlePanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        isAtTitle = true;
    }

    void Update()
    {
        // 如果当前在主界面，并且玩家按下了键盘任意键或鼠标左右键
        if (isAtTitle && Input.anyKeyDown)
        {
            GoToLevelSelect();
        }
    }

    // 切换到选关界面的逻辑
    public void GoToLevelSelect()
    {
        titlePanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        isAtTitle = false; // 关掉检测，防止在选关界面乱按报错
    }

    // 给选关界面的“返回”按钮用的
    public void BackToTitle()
    {
        levelSelectPanel.SetActive(false);
        titlePanel.SetActive(true);
        isAtTitle = true; // 重新开启任意键检测
    }

    // 🌟 给你的“第一关”、“第二关”按钮用的
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}