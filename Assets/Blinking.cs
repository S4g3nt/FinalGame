using UnityEngine;

public class BlinkingText : MonoBehaviour
{
    [Header("闪烁速度 (数值越大闪得越快，推荐 2-4)")]
    public float blinkSpeed = 3f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        // 自动帮你找这个组件，没有就自动装一个，绝对不让你手动拖！
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) 
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        // 利用数学里的正弦波 (Sin)，让不透明度在 0 到 1 之间丝滑地来回呼吸
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        }
    }
}