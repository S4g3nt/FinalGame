using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// In-level pause menu: main options, key rebinding. Created as a child of GameManager at runtime.
/// </summary>
[DisallowMultipleComponent]
public class LevelPauseMenu : MonoBehaviour
{
    static Font _cachedUiFont;

    /// <summary>
    /// Unity 6+: UI.Text needs an explicit font or nothing renders. Prefer common Latin UI fonts.
    /// </summary>
    static Font GetOrCreateUiFont()
    {
        if (_cachedUiFont != null)
            return _cachedUiFont;

        try
        {
            _cachedUiFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Segoe UI", "Arial", "Helvetica Neue", "Roboto",
                    "Microsoft YaHei UI", "PingFang SC", "Noto Sans CJK SC"
                },
                24);
        }
        catch
        {
            _cachedUiFont = null;
        }

        if (_cachedUiFont == null)
            _cachedUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedUiFont == null)
            _cachedUiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return _cachedUiFont;
    }

    static void SetupText(Text te)
    {
        if (te == null) return;
        var f = GetOrCreateUiFont();
        if (f != null)
            te.font = f;
        te.raycastTarget = false;
    }

    GameManager _gm;
    Canvas _canvas;
    GameObject _root;
    GameObject _panelMain;
    GameObject _panelKeys;

    Text _titleMain;
    Button _btnToMenu;
    Button _btnKeySettings;
    Button _btnJump;
    Button _btnGhost;
    Button _btnCheckpoint;
    Button _btnKeysBack;

    bool _paused;
    GameInputAction? _listening;
    int _ignoreInputFrames;

    public bool IsPaused => _paused;

    public void BindGameManager(GameManager gm) => _gm = gm;

    void Awake()
    {
        GameInputSettings.EnsureLoaded();
        BuildUi();
        HideAllVisuals();
    }

    void OnDestroy()
    {
        if (_paused)
            Time.timeScale = 1f;
    }

    public void OnEscapePressed()
    {
        if (_listening.HasValue)
        {
            _listening = null;
            _ignoreInputFrames = 0;
            RefreshKeyButtonLabels();
            return;
        }

        if (_panelKeys.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        if (_paused)
            ResumeGame();
        else
            OpenPause();
    }

    public void OpenPause()
    {
        if (_paused) return;
        _paused = true;
        Time.timeScale = 0f;
        _root.SetActive(true);
        ShowMainPanel();
    }

    void ResumeGame()
    {
        _paused = false;
        Time.timeScale = 1f;
        _listening = null;
        _ignoreInputFrames = 0;
        HideAllVisuals();
    }

    void ShowMainPanel()
    {
        _listening = null;
        _ignoreInputFrames = 0;
        _panelMain.SetActive(true);
        _panelKeys.SetActive(false);
    }

    void ShowKeysPanel()
    {
        _listening = null;
        _ignoreInputFrames = 0;
        _panelMain.SetActive(false);
        _panelKeys.SetActive(true);
        RefreshKeyButtonLabels();
    }

    void HideAllVisuals()
    {
        _root.SetActive(false);
        _panelMain.SetActive(false);
        _panelKeys.SetActive(false);
        _listening = null;
    }

    void Update()
    {
        if (!_listening.HasValue || !_paused)
            return;

        if (_ignoreInputFrames > 0)
        {
            _ignoreInputFrames--;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _listening = null;
            RefreshKeyButtonLabels();
            return;
        }

        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(k))
                continue;
            if (!GameInputSettings.IsAllowedBindingKey(k))
                continue;

            if (!GameInputSettings.TrySetActionKey(_listening.Value, k, out string err))
            {
                SetListenHint(err);
                continue;
            }

            _listening = null;
            RefreshKeyButtonLabels();
            break;
        }
    }

    void SetListenHint(string msg)
    {
        switch (_listening)
        {
            case GameInputAction.Jump:
                SetButtonCaption(_btnJump, $"Press a key… {msg}");
                break;
            case GameInputAction.Ghost:
                SetButtonCaption(_btnGhost, $"Press a key… {msg}");
                break;
            case GameInputAction.CheckpointRespawn:
                SetButtonCaption(_btnCheckpoint, $"Press a key… {msg}");
                break;
        }
    }

    void RefreshKeyButtonLabels()
    {
        GameInputSettings.EnsureLoaded();
        SetButtonCaption(_btnJump, $"Jump: {GameInputSettings.KeyLabel(GameInputSettings.JumpKey)}");
        SetButtonCaption(_btnGhost, $"Ghost mode: {GameInputSettings.KeyLabel(GameInputSettings.GhostKey)}");
        SetButtonCaption(_btnCheckpoint, $"Last checkpoint: {GameInputSettings.KeyLabel(GameInputSettings.CheckpointKey)}");
    }

    static void SetButtonCaption(Button btn, string text)
    {
        if (btn == null) return;
        var t = btn.GetComponentInChildren<Text>();
        if (t != null) t.text = text;
    }

    void BuildUi()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 800;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem_PauseBootstrap");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        _root = new GameObject("PauseMenuRoot");
        _root.transform.SetParent(transform, false);
        var rootRt = _root.AddComponent<RectTransform>();
        StretchFull(rootRt);

        var dim = CreateChildImage(_root.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
        StretchFull(dim.rectTransform);
        dim.raycastTarget = true;

        var box = new GameObject("MenuBox");
        box.transform.SetParent(_root.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(520, 460);
        var boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);
        boxBg.raycastTarget = true;

        _panelMain = CreatePanel(box.transform, "MainPanel");
        _titleMain = CreateTitle(_panelMain.transform, "Paused");
        AddSpacer(_panelMain.transform, 12);
        _btnToMenu = CreateMenuButton(_panelMain.transform, "Main menu", OnClickReturnToMenu);
        AddSpacer(_panelMain.transform, 8);
        _btnKeySettings = CreateMenuButton(_panelMain.transform, "Key bindings", () =>
        {
            ShowKeysPanel();
        });
        AddSpacer(_panelMain.transform, 8);
        CreateMenuButton(_panelMain.transform, "Resume", ResumeGame);

        _panelKeys = CreatePanel(box.transform, "KeysPanel");
        CreateTitle(_panelKeys.transform, "Key bindings");
        AddSpacer(_panelKeys.transform, 8);
        _btnJump = CreateMenuButton(_panelKeys.transform, "Jump", () => BeginListen(GameInputAction.Jump));
        AddSpacer(_panelKeys.transform, 6);
        _btnGhost = CreateMenuButton(_panelKeys.transform, "Ghost mode", () => BeginListen(GameInputAction.Ghost));
        AddSpacer(_panelKeys.transform, 6);
        _btnCheckpoint = CreateMenuButton(_panelKeys.transform, "Last checkpoint", () => BeginListen(GameInputAction.CheckpointRespawn));
        AddSpacer(_panelKeys.transform, 10);
        _btnKeysBack = CreateMenuButton(_panelKeys.transform, "Back", ShowMainPanel);

        var hint = CreateChildText(_panelKeys.transform, "EscHint", "Esc: go back to the pause menu", 18, new Color(0.75f, 0.75f, 0.75f));
        var hintLe = hint.gameObject.AddComponent<LayoutElement>();
        hintLe.preferredHeight = 40;
        hintLe.minHeight = 40;
        hint.rectTransform.sizeDelta = new Vector2(480, 40);

        HideAllVisuals();
    }

    void BeginListen(GameInputAction action)
    {
        _listening = action;
        _ignoreInputFrames = 2;
        switch (action)
        {
            case GameInputAction.Jump:
                SetButtonCaption(_btnJump, "Press a new key… (Esc to cancel)");
                break;
            case GameInputAction.Ghost:
                SetButtonCaption(_btnGhost, "Press a new key… (Esc to cancel)");
                break;
            case GameInputAction.CheckpointRespawn:
                SetButtonCaption(_btnCheckpoint, "Press a new key… (Esc to cancel)");
                break;
        }
    }

    void OnClickReturnToMenu()
    {
        ResumeGame();
        var g = _gm != null ? _gm : GameManager.Instance;
        if (g != null)
            g.ReturnToLevelSelect();
    }

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.flexibleHeight = 1;

        var v = go.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(28, 28, 24, 24);
        v.spacing = 0f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        return go;
    }

    static Text CreateTitle(Transform parent, string title)
    {
        var t = CreateChildText(parent, "Title", title, 32, Color.white);
        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(460, 48);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 48;
        le.minHeight = 48;
        return t;
    }

    static void AddSpacer(Transform parent, float h)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    static Button CreateMenuButton(Transform parent, string caption, Action onClick)
    {
        var go = new GameObject("Button_" + caption);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 52;
        le.minHeight = 52;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.32f, 0.42f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        StretchFull(trt);
        var te = textGo.AddComponent<Text>();
        te.text = caption;
        te.alignment = TextAnchor.MiddleCenter;
        te.color = Color.white;
        te.fontSize = 22;
        te.resizeTextForBestFit = false;
        SetupText(te);
        return btn;
    }

    static Image CreateChildImage(Transform parent, string name, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    static Text CreateChildText(Transform parent, string name, string msg, int size, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var te = go.AddComponent<Text>();
        te.text = msg;
        te.fontSize = size;
        te.color = c;
        te.alignment = TextAnchor.MiddleCenter;
        te.resizeTextForBestFit = false;
        SetupText(te);
        return te;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
