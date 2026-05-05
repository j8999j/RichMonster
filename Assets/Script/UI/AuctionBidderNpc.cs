using UnityEngine;

// =============================================================================
// AuctionBidderNpc：拍賣會場上的單一參與者 NPC（極簡視覺元件）。
// -----------------------------------------------------------------------------
// 本類別只保留一件事：套用該 NPC 的 Sprite。
// 對話框 (BubbleSpawnPoint) 與最高出價標示停靠點 (HighestMarkerPoint) 的位置，
// 一律由 AuctionView 的 AuctionBidderSpawnInfo（Inspector 拖設定）統一提供，
// 不在本元件上儲存任何錨點。
// =============================================================================
public class AuctionBidderNpc : MonoBehaviour
{
    [Header("Sprite")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;        // 角色 Sprite 顯示元件

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    /// <summary>套用角色 Sprite（一般在 AuctionView Spawn 時提供）。</summary>
    public void ApplySprite(Sprite sprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;
    }
}
