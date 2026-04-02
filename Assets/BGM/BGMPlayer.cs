using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    public static BGMPlayer Instance;

    [Tooltip("本关的背景音乐")]
    public AudioClip sceneBGM;

    private AudioSource audioSource;

    void Awake()
    {
        // --- 核心单例与跨场景保持逻辑 ---
        if (Instance == null)
        {
            // 如果是全场第一个 BGM 播放器，确立霸权，并让自己切场景不被销毁
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 强制 2D

            PlayCurrentBGM();
        }
        else
        {
            // 如果场景里已经有一个 BGM 播放器了（比如死亡重载场景，或者切到了新关卡）
            if (Instance.sceneBGM != this.sceneBGM)
            {
                // 如果发现新场景的音乐跟正在播的不一样，就换碟
                Instance.sceneBGM = this.sceneBGM;
                Instance.PlayCurrentBGM();
            }
            // 销毁这个多余的克隆体，让老祖宗继续播
            Destroy(gameObject);
        }
    }

    private void PlayCurrentBGM()
    {
        if (sceneBGM != null && audioSource != null)
        {
            audioSource.clip = sceneBGM;
            audioSource.Play();
        }
    }
}