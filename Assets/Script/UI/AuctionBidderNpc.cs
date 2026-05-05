using UnityEngine;

// =============================================================================
// AuctionBidderNpc：拍賣會場上的單一參與者 NPC。
// -----------------------------------------------------------------------------
// 本類別只負責「該 NPC 自身的視覺」，現在收斂成一件事：
//   1. 角色 Sprite（立繪）
//
// 對話框 (BubbleRoot / BubbleText) 與「目前最高出價者標示 (Highest Bidder Marker)」
// 已不在本類別管理範圍——AuctionView 會依 bidderSpawnPoints 中各筆 UI 點位
// 統一生成／搬移／顯示與隱藏。
//
// 一般使用方式：
//   - 建立一個 prefab，掛上本元件，並在 prefab 裡準備好：
//       * SpriteRenderer（角色圖）
//   - AuctionView 會在拍賣會開場時依 spawn point 生成數隻，逐隻 ApplySprite。
//   - ID／顯示名稱由 AuctionView 端（bidderSpawnPoints）統一保管，本類別不持有。
// =============================================================================
public class AuctionBidderNpc : MonoBehaviour
{
    [Header("Sprite")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;        // 角色 Sprite 顯示元件

    private void Awake()
    {
        // 若 Inspector 沒指定，嘗試從子物件抓 SpriteRenderer
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>套用角色 Sprite（一般在 AuctionView Spawn 時提供）。</summary>
    public void ApplySprite(Sprite sprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;
    }
}
