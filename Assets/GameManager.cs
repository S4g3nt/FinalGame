using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("复活设置")]
    public Vector3 lastCheckpointPos;
    public Image fadeImage;
    public float fadeSpeed = 2f;

    // --- 新增：记录当前激活的存档点脚本 ---
    private Checkpoint currentActiveCheckpoint;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Hero");
        if (player != null)
        {
            lastCheckpointPos = player.transform.position;
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
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
        StartCoroutine(RespawnSequence(player));
    }

IEnumerator RespawnSequence(GameObject player)
{
    // 1. 等待死亡瞬间的停顿（此时玩家是红色的）
    yield return new WaitForSeconds(0.5f);

    // 2. 屏幕变黑
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

    // 3. 执行传送
    player.transform.position = lastCheckpointPos;
    
    // 4. --- 核心：在这里恢复一切视觉状态 ---
    PlayerController pc = player.GetComponent<PlayerController>();
    if (pc != null)
    {
        pc.SetDeathVisual(false); // 站起来
        pc.SetHurtColor(false);   // <--- 关键：在此处变回正常颜色
        pc.EnableControls();      // 恢复操作
    }

    AstraSkills astra = player.GetComponent<AstraSkills>();
    if (astra != null)
        astra.ResetToNormalGravity();

    // 5. 屏幕变亮
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
    }
}
}