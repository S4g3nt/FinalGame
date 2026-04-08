using UnityEngine;

/// <summary>
/// Global key bindings persisted with PlayerPrefs: jump, ghost mode, checkpoint respawn.
/// </summary>
public enum GameInputAction
{
    Jump,
    Ghost,
    CheckpointRespawn
}

public static class GameInputSettings
{
    const string PJump = "GameInput_Jump";
    const string PGhost = "GameInput_Ghost";
    const string PCheckpoint = "GameInput_Checkpoint";

    static bool _loaded;

    public static KeyCode JumpKey { get; private set; } = KeyCode.K;
    public static KeyCode GhostKey { get; private set; } = KeyCode.P;
    public static KeyCode CheckpointKey { get; private set; } = KeyCode.Minus;

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        JumpKey = (KeyCode)PlayerPrefs.GetInt(PJump, (int)KeyCode.K);
        GhostKey = (KeyCode)PlayerPrefs.GetInt(PGhost, (int)KeyCode.P);
        CheckpointKey = (KeyCode)PlayerPrefs.GetInt(PCheckpoint, (int)KeyCode.Minus);
        SanitizeLoaded();
    }

    static void SanitizeLoaded()
    {
        if (!IsAllowedBindingKey(JumpKey)) JumpKey = KeyCode.K;
        if (!IsAllowedBindingKey(GhostKey)) GhostKey = KeyCode.P;
        if (!IsAllowedBindingKey(CheckpointKey)) CheckpointKey = KeyCode.Minus;
    }

    public static bool IsAllowedBindingKey(KeyCode k)
    {
        if (k == KeyCode.None || k == KeyCode.Escape)
            return false;
        int v = (int)k;
        if (v >= (int)KeyCode.Mouse0 && v <= (int)KeyCode.Mouse6)
            return false;
        if (v >= (int)KeyCode.JoystickButton0 && v <= (int)KeyCode.Joystick8Button19)
            return false;
        return true;
    }

    public static bool WouldConflict(GameInputAction except, KeyCode k)
    {
        EnsureLoaded();
        if (except != GameInputAction.Jump && JumpKey == k) return true;
        if (except != GameInputAction.Ghost && GhostKey == k) return true;
        if (except != GameInputAction.CheckpointRespawn && CheckpointKey == k) return true;
        return false;
    }

    public static bool TrySetActionKey(GameInputAction action, KeyCode k, out string failReason)
    {
        EnsureLoaded();
        failReason = null;
        if (!IsAllowedBindingKey(k))
        {
            failReason = "That key is not allowed.";
            return false;
        }

        if (WouldConflict(action, k))
        {
            failReason = "Already used by another action.";
            return false;
        }

        switch (action)
        {
            case GameInputAction.Jump:
                JumpKey = k;
                PlayerPrefs.SetInt(PJump, (int)k);
                break;
            case GameInputAction.Ghost:
                GhostKey = k;
                PlayerPrefs.SetInt(PGhost, (int)k);
                break;
            case GameInputAction.CheckpointRespawn:
                CheckpointKey = k;
                PlayerPrefs.SetInt(PCheckpoint, (int)k);
                break;
        }

        PlayerPrefs.Save();
        return true;
    }

    public static string KeyLabel(KeyCode k) => k.ToString();

    public static bool GetJumpDown()
    {
        EnsureLoaded();
        return Input.GetKeyDown(JumpKey);
    }

    public static bool GetGhostDown()
    {
        EnsureLoaded();
        return Input.GetKeyDown(GhostKey);
    }

    public static bool GetCheckpointRespawnDown()
    {
        EnsureLoaded();
        return Input.GetKeyDown(CheckpointKey);
    }
}
