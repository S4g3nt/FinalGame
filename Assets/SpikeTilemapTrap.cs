using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapCollider2D))]
public class SpikeTilemapTrap : MonoBehaviour
{
    [Header("判定")]
    [Tooltip("为空=这张 Tilemap 上任何非空 Tile 都算刺；不为空=只有列表里的 Tile 才算刺")]
    public List<TileBase> spikeTiles = new List<TileBase>();

    [Tooltip("同一个角色再次触发的冷却（防止多次重复调用复活）")]
    public float rehitCooldown = 0.5f;

    private Tilemap _tilemap;
    private readonly Dictionary<int, float> _lastHitTimeByHero = new Dictionary<int, float>();
    private HashSet<TileBase> _spikeTileSet;

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        var col = GetComponent<TilemapCollider2D>();
        col.isTrigger = true;

        _spikeTileSet = new HashSet<TileBase>(spikeTiles ?? new List<TileBase>());
    }

    private void OnValidate()
    {
        _spikeTileSet = new HashSet<TileBase>(spikeTiles ?? new List<TileBase>());
    }

    private void OnTriggerEnter2D(Collider2D collision) => TryHit(collision);
    private void OnTriggerStay2D(Collider2D collision) => TryHit(collision);

    private void TryHit(Collider2D collision)
    {
        if (!collision || !collision.CompareTag("Hero")) return;

        int heroId = collision.gameObject.GetInstanceID();
        if (_lastHitTimeByHero.TryGetValue(heroId, out float lastHitTime) &&
            Time.time - lastHitTime < rehitCooldown)
        {
            return;
        }

        if (!IsHeroOverSpikeTile(collision)) return;
        _lastHitTimeByHero[heroId] = Time.time;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        player.DisableControls();
        player.SetDeathVisual(true);
        player.SetHurtColor(true);

        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null)
            GameManager.Instance.StartRespawn(collision.gameObject);
    }

    private bool IsHeroOverSpikeTile(Collider2D heroCollider)
    {
        // 你把刺单独放在一张 Tilemap 上的话，直接命中即可
        if (_spikeTileSet == null || _spikeTileSet.Count == 0)
            return true;

        Bounds b = heroCollider.bounds;
        Vector3Int minCell = _tilemap.WorldToCell(b.min);
        Vector3Int maxCell = _tilemap.WorldToCell(b.max);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var tile = _tilemap.GetTile(new Vector3Int(x, y, 0));
                if (tile != null && _spikeTileSet.Contains(tile))
                    return true;
            }
        }

        return false;
    }
}

