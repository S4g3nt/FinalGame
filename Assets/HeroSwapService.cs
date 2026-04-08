using UnityEngine;

/// <summary>
/// Replaces the scene Hero with another hero prefab at the same position, preserving velocity and facing.
/// Clears deployables (Yoru/Raze) and resets Astra gravity on the old hero before destroy.
/// </summary>
public static class HeroSwapService
{
    const string HeroTag = "Hero";

    public static bool TrySwapTo(GameObject heroPrefab)
    {
        if (heroPrefab == null) return false;

        GameObject oldHero = GameObject.FindGameObjectWithTag(HeroTag);
        if (oldHero == null) return false;

        if (AlreadyIsThatHero(oldHero, heroPrefab))
            return true;

        Vector3 pos = oldHero.transform.position;
        Quaternion rot = oldHero.transform.rotation;
        Vector3 scale = oldHero.transform.localScale;

        Rigidbody2D oldRb = oldHero.GetComponent<Rigidbody2D>();
        Vector2 vel = oldRb != null ? oldRb.linearVelocity : Vector2.zero;
        float angVel = oldRb != null ? oldRb.angularVelocity : 0f;

        CleanupOldHero(oldHero);
        Object.Destroy(oldHero);

        GameObject neu = Object.Instantiate(heroPrefab, pos, rot);
        neu.tag = HeroTag;

        neu.transform.localScale = scale;

        Rigidbody2D nrb = neu.GetComponent<Rigidbody2D>();
        if (nrb != null)
        {
            nrb.linearVelocity = vel;
            nrb.angularVelocity = angVel;
        }

        PlayerController npc = neu.GetComponent<PlayerController>();
        if (npc != null)
        {
            npc.EnableControls();
            npc.EndVoidDeathFreeze();
            npc.SetDeathVisual(false);
            npc.SetHurtColor(false);
        }

        RegionCamera rc = Object.FindFirstObjectByType<RegionCamera>();
        if (rc != null)
            rc.RebindHeroTarget();

        if (RegionManager.Instance != null)
            RegionManager.Instance.RefreshPlayerRigidbody();

        return true;
    }

    /// <summary>
    /// Astra prefab also has a disabled JettSkills on the root; check Astra before Jett so we don't treat Astra as "Jett archetype".
    /// </summary>
    enum HeroArchetype { Unknown, Astra, Raze, Yoru, Jett }

    static HeroArchetype GetArchetype(GameObject go)
    {
        if (go == null) return HeroArchetype.Unknown;
        if (go.GetComponent<AstraSkills>() != null) return HeroArchetype.Astra;
        if (go.GetComponent<RazeSkills>() != null) return HeroArchetype.Raze;
        if (go.GetComponent<YoruSkills>() != null) return HeroArchetype.Yoru;
        if (go.GetComponent<JettSkills>() != null) return HeroArchetype.Jett;
        return HeroArchetype.Unknown;
    }

    static bool AlreadyIsThatHero(GameObject instance, GameObject prefabAsset)
    {
        HeroArchetype p = GetArchetype(prefabAsset);
        HeroArchetype i = GetArchetype(instance);
        return p != HeroArchetype.Unknown && p == i;
    }

    static void CleanupOldHero(GameObject oldHero)
    {
        PlayerController pc = oldHero.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ForceExitGhostMode();
            pc.EndVoidDeathFreeze();
        }

        YoruSkills yoru = oldHero.GetComponent<YoruSkills>();
        if (yoru != null)
            yoru.ClearDeployedForRespawn();

        RazeSkills raze = oldHero.GetComponent<RazeSkills>();
        if (raze != null)
            raze.ClearDeployedGearForRespawn();

        AstraSkills astra = oldHero.GetComponent<AstraSkills>();
        if (astra != null)
            astra.ResetToNormalGravity();
    }
}
