/// <summary>
/// True while pause menu or hero-select UI is open (Time.timeScale may be 0 but Update still runs).
/// </summary>
public static class GameplayInputLock
{
    public static bool IsLocked =>
        HeroCampfire.IsHeroSelectOpen ||
        (GameManager.Instance != null && GameManager.Instance.IsLevelPaused);
}
