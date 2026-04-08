using System.Collections.Generic;
using UnityEngine;

// --- 数据结构 ---
[System.Serializable]
public class GhostFrame
{
    public Vector3 position;
    public bool keyLeft;
    public bool keyRight;
    public bool keyJump;
    public bool keySkill;
}

[System.Serializable]
public class GhostData
{
    public List<GhostFrame> frames = new List<GhostFrame>();
}

// --- 核心管理器 ---
public class GhostGuideManager : MonoBehaviour
{
    public static GhostGuideManager Instance;

    [Header("全局配置")]
    [Tooltip("打勾：开发者录制模式 (按R录制/结束)\n取消打勾：玩家游玩模式 (按Q播放)")]
    public bool isRecordingMode = false;
    
    [Tooltip("用于展示残影的预制体（仅需 SpriteRenderer，设为半透明）")]
    public GameObject ghostPrefab;
    
    [Tooltip("主角的 Transform，用于获取录制坐标")]
    public Transform playerTransform;

    [Header("当前状态 (运行时自动获取)")]
    public GhostRegion currentRegion; // 当前玩家所在的区域
    
    private GhostData currentData = new GhostData();
    private GameObject currentGhost;
    private bool isRecording = false;
    private bool isPlaying = false;
    private int currentPlaybackFrame = 0;

    void Awake()
    {
        Instance = this;
    }

    // 由 GhostRegion 的触发器调用
    public void EnterRegion(GhostRegion region)
    {
        currentRegion = region;
        Debug.Log("进入可引导区域: " + region.regionName);
    }

    public void ExitRegion(GhostRegion region)
    {
        if (currentRegion == region)
        {
            currentRegion = null;
            if(isPlaying) StopPlayback(); // 离开区域则强制停止播放
        }
    }

    void Update()
    {
        if (currentRegion == null) return;

        // 开发者：录制控制 (R)
        if (isRecordingMode && Input.GetKeyDown(KeyCode.R))
        {
            if (!isRecording) StartRecording();
            else StopRecordingAndSave();
        }

        // 玩家：播放控制 (Q)
        if (!isRecordingMode && Input.GetKeyDown(KeyCode.Q))
        {
            if (!isPlaying && currentRegion.regionDataFile != null)
                StartPlayback();
        }
    }

    void FixedUpdate()
    {
        // 使用 FixedUpdate 保证无论帧率高低，录像速度一致
        if (isRecording) RecordFrame();
        if (isPlaying) PlayFrame();
    }

    // ================= 录制逻辑 =================
    void StartRecording()
    {
        currentData.frames.Clear();
        isRecording = true;
        Debug.Log($"正在录制区域 {currentRegion.regionName}...");
    }

    void RecordFrame()
    {
        if (playerTransform == null) return;

        GhostFrame frame = new GhostFrame();
        frame.position = playerTransform.position;
        
        // 【注意】这里需要替换为你游戏实际的输入检测方式
        frame.keyLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        frame.keyRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        frame.keyJump = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W);
        frame.keySkill = Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.Mouse0); 

        currentData.frames.Add(frame);
    }

    void StopRecordingAndSave()
    {
        isRecording = false;
        string json = JsonUtility.ToJson(currentData);
        Debug.Log($"区域 {currentRegion.regionName} 录制完成！请复制以下 JSON 保存为 txt 文件：\n" + json);
    }

    // ================= 播放逻辑 =================
    void StartPlayback()
    {
        // 解析当前区域绑定的 JSON 文件
        currentData = JsonUtility.FromJson<GhostData>(currentRegion.regionDataFile.text);
        if (currentData == null || currentData.frames.Count == 0) return;

        if (currentGhost == null) currentGhost = Instantiate(ghostPrefab);
        currentGhost.SetActive(true);
        currentPlaybackFrame = 0;
        isPlaying = true;
    }

    void PlayFrame()
    {
        if (currentPlaybackFrame >= currentData.frames.Count)
        {
            StopPlayback(); // 播放完毕
            return;
        }

        GhostFrame frame = currentData.frames[currentPlaybackFrame];
        
        // 移动残影位置
        currentGhost.transform.position = frame.position;

        // 发送按键数据给 UI 更新显示
        if (GhostUIManager.Instance != null)
            GhostUIManager.Instance.UpdateHint(frame.keyLeft, frame.keyRight, frame.keyJump, frame.keySkill);

        currentPlaybackFrame++;
    }

    void StopPlayback()
    {
        isPlaying = false;
        if(currentGhost) currentGhost.SetActive(false);
        if(GhostUIManager.Instance) GhostUIManager.Instance.HideAllHints();
    }
}