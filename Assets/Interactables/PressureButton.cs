using System.Collections.Generic;
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
    /// <summary>当前在触发区内且满足 IsActivator 的碰撞体（用于 Hold，及假人从“预制体→释放”后仍在按钮上的情况）。</summary>
    readonly HashSet<Collider2D> _activeOccupants = new HashSet<Collider2D>();

    void Start()
    {
        ApplyButtonVisual(false);
        if (targetDoor != null && mode == PressureButtonMode.Hold)
            targetDoor.SetOpen(false);
    }

    void OnTriggerEnter2D(Collider2D other) => TryRegisterActivator(other);

    /// <summary>
    /// 假人先在按钮上再激活时不会再次触发 Enter，需在 Stay 里把“刚变为有效”的碰撞体补登记。
    /// </summary>
    void OnTriggerStay2D(Collider2D other) => TryRegisterActivator(other);

    void OnTriggerExit2D(Collider2D other)
    {
        if (!_activeOccupants.Remove(other)) return;
        if (mode == PressureButtonMode.Latch) return;

        if (_activeOccupants.Count == 0)
        {
            ApplyButtonVisual(false);
            if (targetDoor != null) targetDoor.SetOpen(false);
        }
    }

    void TryRegisterActivator(Collider2D other)
    {
        if (!IsActivator(other)) return;

        if (!_activeOccupants.Add(other)) return;

        if (mode == PressureButtonMode.Latch)
        {
            if (_latched) return;
            _latched = true;
            ApplyButtonVisual(true);
            if (targetDoor != null) targetDoor.SetOpen(true);
            return;
        }

        if (_activeOccupants.Count == 1)
        {
            ApplyButtonVisual(true);
            if (targetDoor != null) targetDoor.SetOpen(true);
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
        if (col.CompareTag("YoruClone"))
        {
            // 两阶段假人：预制体阶段不触发按钮；释放后（isMoving=true）才触发
            var logic = col.GetComponent<YoruCloneLogic>();
            return logic == null || logic.isMoving;
        }

        var cloneLogic = col.GetComponent<YoruCloneLogic>();
        if (cloneLogic != null) return cloneLogic.isMoving;
        return false;
    }
}
