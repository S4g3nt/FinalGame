using UnityEngine;
using UnityEngine.SceneManagement; // [极其重要] 必须加这句才能切场景！

public class MenuUIManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject titlePanel;
    public GameObject levelSelectPanel;

    private bool isWaitingForKey = true; 

    void Start()
    {
        titlePanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    void Update()
    {
        if (isWaitingForKey && Input.anyKeyDown)
        {
            GoToLevelSelect();
        }
    }

    void GoToLevelSelect()
    {
        isWaitingForKey = false; 
        titlePanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    // ==== [新增] 给按钮用的加载关卡魔法 ====
    // 括号里的 string levelName 意味着你可以在 Unity 面板里直接填入关卡名字！
    public void LoadLevel(string levelName)
    {
        Debug.Log("正在准备进入关卡: " + levelName);
        SceneManager.LoadScene(levelName);
    }
}