using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // 引入渲染命名空间，用于 SortingGroup

[RequireComponent(typeof(Rigidbody2D))]
public class YoruCloneLogic : MonoBehaviour
{
    public static bool IsReleasedClone(Collider2D col)
    {
        if (col == null) return false;
        var logic = col.GetComponent<YoruCloneLogic>();
        if (logic == null) logic = col.GetComponentInParent<YoruCloneLogic>();
        return logic != null && logic.isMoving;
    }

    [Header("假人设置")]
    public float moveSpeed = 5f;
    public float duration = 5f; 
    private Animator anim;
    
    [HideInInspector]
    public bool isMoving = false; 

    private Rigidbody2D rb;
    private float moveDirection;
    private SpriteRenderer spriteRenderer;
    private bool isVanishing = false;
    public bool IsVanishing => isVanishing;
    private Coroutine vanishCoroutine;
    bool _pendingSpringContactCheck;
    readonly ContactPoint2D[] _contactScratch = new ContactPoint2D[12];

    [Header("消散动画设置")]
    public float vanishDuration = 0.5f;
    [Tooltip("斜切的角度（决定消散切口的倾斜度）")]
    public float vanishAngle = -30f; 

    // 静态缓存遮罩图片，防止每次生成都占用新内存
    private static Sprite vanishMaskSprite; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); 
        if(anim != null) anim.SetBool("isMoving", false);
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        moveDirection = -Mathf.Sign(transform.localScale.x);
    }

    public void ActivateClone()
    {
        if (!isMoving)
        {
            isMoving = true;
            if (anim != null) 
            {
                anim.SetBool("isMoving", true);
            }
            Invoke("StartVanish", duration);
            _pendingSpringContactCheck = true;
        }
    }

    void FixedUpdate()
    {
        if (_pendingSpringContactCheck)
        {
            _pendingSpringContactCheck = false;
            TryBounceFromTouchingSprings();
        }

        if (isMoving && !isVanishing) // 增加 !isVanishing 判断，消散时不给速度
        {
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        }
        else if (!isVanishing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("DeathZone"))
            StartVanish();
    }

    void TryBounceFromTouchingSprings()
    {
        if (rb == null) return;
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        int n = rb.GetContacts(_contactScratch);
        for (int i = 0; i < n; i++)
        {
            Collider2D other = _contactScratch[i].collider;
            if (other == null) continue;
            var spring = other.GetComponent<SpringPad>();
            if (spring == null) spring = other.GetComponentInParent<SpringPad>();
            if (spring != null)
                spring.TryBounce(col, rb);
        }
    }

    public void StartVanish()
    {
        if (isVanishing) return;
        isVanishing = true;
        
        // 1. 禁用碰撞体
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        // 2. 彻底冻结物理与动作！让它钉在原地
        if (rb != null) 
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // 关闭物理模拟，免疫一切重力或推力
        }
        if (anim != null) anim.enabled = false; // 定格动画播放
        
        if (vanishCoroutine != null) StopCoroutine(vanishCoroutine);
        vanishCoroutine = StartCoroutine(VanishRoutine());
    }
    
    private IEnumerator VanishRoutine()
    {
        // === 核心技术：程序化斜切遮罩 ===
        
        // A. 确保遮罩只擦除假人自己，不会把游戏背景或其他角色也擦掉
        if (gameObject.GetComponent<SortingGroup>() == null)
        {
            gameObject.AddComponent<SortingGroup>();
        }

        // B. 动态创建一个遮罩物体
        GameObject maskObj = new GameObject("VanishMask");
        maskObj.transform.SetParent(transform);
        maskObj.transform.localPosition = Vector3.zero;
        
        // 设置斜切角度
        maskObj.transform.localRotation = Quaternion.Euler(0, 0, vanishAngle);
        // 把遮罩放大，确保能完全盖住角色
        maskObj.transform.localScale = new Vector3(15f, 15f, 1f);

        SpriteMask mask = maskObj.AddComponent<SpriteMask>();
        
        // C. 在内存中画一张白色的纯色图作为遮罩
        if (vanishMaskSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            vanishMaskSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        mask.sprite = vanishMaskSprite;

        // D. 命令假人：只准在遮罩覆盖的区域内显示
        if (spriteRenderer != null)
        {
            spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        // E. 开始执行斜向滑动遮罩的动画
        float timer = 0f;
        Vector3 startPos = maskObj.transform.position;
        // 让遮罩顺着它的“下方”移动（因为已经旋转过了，所以是斜向下的）
        Vector3 endPos = startPos - maskObj.transform.up * 8f; 
        
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0.2f); // 稍微变透明一点

        while (timer < vanishDuration)
        {
            timer += Time.deltaTime;
            float t = timer / vanishDuration;
            
            // 假人完全不动，只移动遮罩，形成切除效果
            maskObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        // 动画结束，销毁一切
        Destroy(maskObj);
        Destroy(gameObject);
    }
}