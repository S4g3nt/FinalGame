using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TutorialArea : MonoBehaviour
{
    [Header("UI 画面物体 (子物体)")]
    public GameObject uiObject; 

    [Header("淡入淡出速度")]
    public float fadeSpeed = 5f; 

    [Header("一开始是否显示？")]
    public bool showOnStart = true;

    private CanvasGroup canvasGroup;
    private bool isPlayerInside = false;
    private int collidersInside = 0;

    void Awake()
    {
        if (uiObject == null) return;

        // 【自动化核心】：代码自动帮你加组件，不用你动手！
        canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = uiObject.AddComponent<CanvasGroup>();
        
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void Start()
    {
        if (uiObject != null)
        {
            isPlayerInside = showOnStart;
            canvasGroup.alpha = isPlayerInside ? 1f : 0f;
            uiObject.SetActive(isPlayerInside);
        }
    }

    void Update()
    {
        if (uiObject == null) return;

        float targetAlpha = isPlayerInside ? 1f : 0f;

        // 1. 如果要显示，先激活物体
        if (isPlayerInside && !uiObject.activeSelf) uiObject.SetActive(true);

        // 2. 匀速改变不透明度
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // 3. 彻底消失后关闭物体
        if (!isPlayerInside && canvasGroup.alpha <= 0f && uiObject.activeSelf)
        {
            uiObject.SetActive(false);
        }
    }

    // 同时兼容 Hero 和 Player 标签，防爆雷
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hero") || other.CompareTag("Player"))
        {
            collidersInside++;
            isPlayerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Hero") || other.CompareTag("Player"))
        {
            collidersInside--;
            if (collidersInside <= 0)
            {
                collidersInside = 0;
                isPlayerInside = false;
            }
        }
    }
}