using UnityEngine;

public enum PressureButtonMode
{
    /// <summary>碰一次后按钮保持按下，门保持打开。</summary>
    Latch,
    /// <summary>有 Hero/假人在触发区内则按下并开门，全部离开后弹起并关门。</summary>
    Hold
}

/// <summary>
/// 与 <see cref="PairedDoor"/> 配对：Hero 或 Yoru 假人进入触发区时驱动按钮与门。
/// 按钮物体上需带 <b>Is Trigger</b> 的 Collider2D（建议略大于踏板便于踩）。
/// </summary>
public class PressureButton : MonoBehaviour
{
    [SerializeField] PairedDoor targetDoor;
    [SerializeField] PressureButtonMode mode = PressureButtonMode.Latch;

    [Header("按钮视觉")]
    [SerializeField] GameObject visualUnpressed;
    [SerializeField] GameObject visualPressed;

    bool _latched;
    int _occupants;

    void Start()
    {
        ApplyButtonVisual(false);
        if (targetDoor != null && mode == PressureButtonMode.Hold)
            targetDoor.SetOpen(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActivator(other)) return;

        if (mode == PressureButtonMode.Latch)
        {
            if (_latched) return;
            _latched = true;
            ApplyButtonVisual(true);
            if (targetDoor != null) targetDoor.SetOpen(true);
            return;
        }

        _occupants++;
        if (_occupants == 1)
        {
            ApplyButtonVisual(true);
            if (targetDoor != null) targetDoor.SetOpen(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsActivator(other)) return;
        if (mode == PressureButtonMode.Latch) return;

        _occupants = Mathf.Max(0, _occupants - 1);
        if (_occupants == 0)
        {
            ApplyButtonVisual(false);
            if (targetDoor != null) targetDoor.SetOpen(false);
        }
    }

    void ApplyButtonVisual(bool pressed)
    {
        if (visualUnpressed != null) visualUnpressed.SetActive(!pressed);
        if (visualPressed != null) visualPressed.SetActive(pressed);
    }

    /// <summary>与 SpringPad 一致：本体 Hero、YoruClone 标签，或带 YoruCloneLogic 的假人。</summary>
    public static bool IsActivator(Collider2D col)
    {
        if (col == null) return false;
        if (col.CompareTag("Hero")) return true;
        if (col.CompareTag("YoruClone")) return true;
        if (col.GetComponent<YoruCloneLogic>() != null) return true;
        return false;
    }
}
