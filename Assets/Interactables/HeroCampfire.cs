using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Campfire trigger: while the Hero is inside, press E to open a hero switch panel (Jett / Yoru / Raze / Astra).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class HeroCampfire : MonoBehaviour
{
    static Font _cachedFont;

    [Header("Hero prefabs (root GameObject of each hero prefab)")]
    [SerializeField] GameObject jettPrefab;
    [SerializeField] GameObject yoruPrefab;
    [SerializeField] GameObject razePrefab;
    [SerializeField] GameObject astraPrefab;

    [Header("Input")]
    [SerializeField] KeyCode openKey = KeyCode.E;

    int _heroOverlap;
    Canvas _uiCanvas;
    GameObject _uiRoot;
    bool _panelOpen;

    /// <summary>Runtime-resolved prefabs (avoids broken serialized refs / bad closures).</summary>
    readonly GameObject[] _heroes = new GameObject[4];

    public static bool IsHeroSelectOpen { get; private set; }

    static Font GetUiFont()
    {
        if (_cachedFont != null) return _cachedFont;
        try
        {
            _cachedFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Helvetica Neue", "Roboto" }, 24);
        }
        catch { /* ignore */ }
        if (_cachedFont == null)
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null)
            _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _cachedFont;
    }

    static void SetupTextFont(Text te)
    {
        if (te == null) return;
        var f = GetUiFont();
        if (f != null) te.font = f;
        te.raycastTarget = false;
    }

    void Awake() => RefreshResolvedPrefabs();

    void RefreshResolvedPrefabs()
    {
        _heroes[0] = SafeReadSerializedPrefab(() => jettPrefab);
        _heroes[1] = SafeReadSerializedPrefab(() => yoruPrefab);
        _heroes[2] = SafeReadSerializedPrefab(() => razePrefab);
        _heroes[3] = SafeReadSerializedPrefab(() => astraPrefab);
    }

    /// <summary>Reading a broken Unity object reference can throw; evaluate getter inside try.</summary>
    static GameObject SafeReadSerializedPrefab(Func<GameObject> getter)
    {
        try
        {
            return SafePrefab(getter());
        }
        catch (MissingReferenceException)
        {
            return null;
        }
    }

    /// <summary>Unity "fake null" / missing script ref safe.</summary>
    static GameObject SafePrefab(GameObject g)
    {
        if (!g) return null;
        try
        {
            _ = g.name;
            return g;
        }
        catch (MissingReferenceException)
        {
            return null;
        }
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsHeroCollider(other))
            _heroOverlap++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsHeroCollider(other))
            _heroOverlap = Mathf.Max(0, _heroOverlap - 1);
    }

    static bool IsHeroCollider(Collider2D c)
    {
        Transform t = c.transform;
        while (t != null)
        {
            if (t.CompareTag("Hero")) return true;
            t = t.parent;
        }
        return false;
    }

    void Update()
    {
        if (_panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ClosePanel();
            return;
        }

        if (_heroOverlap <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.IsLevelPaused) return;
        if (!Input.GetKeyDown(openKey)) return;

        TryOpenPanel();
    }

    bool AllHeroPrefabsOk()
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            if (!_heroes[i])
                return false;
        }
        return true;
    }

    void TryOpenPanel()
    {
        RefreshResolvedPrefabs();

        if (!AllHeroPrefabsOk())
        {
            Debug.LogError(
                "HeroCampfire: One or more hero prefab references are missing. Select the HeroCampfire object and assign Jett, Yoru, Raze, Astra prefabs (drag the prefab asset from Project).",
                this);
            return;
        }

        EnsureUiBuilt();
        _panelOpen = true;
        IsHeroSelectOpen = true;
        Time.timeScale = 0f;
        _uiRoot.SetActive(true);
    }

    void ClosePanel()
    {
        _panelOpen = false;
        IsHeroSelectOpen = false;
        Time.timeScale = 1f;
        if (_uiRoot != null)
            _uiRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (_panelOpen)
        {
            Time.timeScale = 1f;
            IsHeroSelectOpen = false;
        }
    }

    void EnsureUiBuilt()
    {
        if (_uiCanvas != null) return;

        var go = new GameObject("HeroSwitchCanvas");
        go.transform.SetParent(transform, false);

        _uiCanvas = go.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 850;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem_HeroSwitchBootstrap");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        _uiRoot = new GameObject("Root");
        _uiRoot.transform.SetParent(go.transform, false);
        var rootRt = _uiRoot.AddComponent<RectTransform>();
        Stretch(rootRt);

        var dim = new GameObject("Dim");
        dim.transform.SetParent(_uiRoot.transform, false);
        var dimRt = dim.AddComponent<RectTransform>();
        Stretch(dimRt);
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.5f);
        dimImg.raycastTarget = true;

        var box = new GameObject("Box");
        box.transform.SetParent(_uiRoot.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(480, 420);
        boxRt.anchoredPosition = Vector2.zero;
        var boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
        boxBg.raycastTarget = true;

        var v = box.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 20, 20);
        v.spacing = 8;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        AddTitle(box.transform, "Choose hero");
        AddButton(box.transform, "Jett", () => OnPickByIndex(0));
        AddButton(box.transform, "Yoru", () => OnPickByIndex(1));
        AddButton(box.transform, "Raze", () => OnPickByIndex(2));
        AddButton(box.transform, "Astra", () => OnPickByIndex(3));
        AddButton(box.transform, "Cancel", ClosePanel);

        _uiRoot.SetActive(false);
    }

    void OnPickByIndex(int index)
    {
        if (index < 0 || index >= _heroes.Length)
        {
            ClosePanel();
            return;
        }

        var prefab = _heroes[index];
        if (!prefab)
        {
            Debug.LogError($"HeroCampfire: hero prefab at index {index} is missing.", this);
            ClosePanel();
            return;
        }

        HeroSwapService.TrySwapTo(prefab);
        ClosePanel();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AddTitle(Transform parent, string text)
    {
        var go = new GameObject("Title");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 44;
        var te = go.AddComponent<Text>();
        te.text = text;
        te.fontSize = 28;
        te.color = Color.white;
        te.alignment = TextAnchor.MiddleCenter;
        SetupTextFont(te);
    }

    static void AddButton(Transform parent, string label, Action onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 48;
        le.minHeight = 48;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.34f, 0.42f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var tg = new GameObject("Text");
        tg.transform.SetParent(go.transform, false);
        var trt = tg.AddComponent<RectTransform>();
        Stretch(trt);
        var te = tg.AddComponent<Text>();
        te.text = label;
        te.fontSize = 22;
        te.color = Color.white;
        te.alignment = TextAnchor.MiddleCenter;
        SetupTextFont(te);
    }
}
