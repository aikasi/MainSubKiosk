using UnityEngine;

/// <summary>
/// Lazy Load된 Texture2D를 안전하게 파괴합니다.
/// null 체크 후 Destroy를 수행하여 메모리 누수를 방지합니다.
/// </summary>
public static class ResourceCleaner
{
    /// <summary>
    /// Texture2D를 안전하게 파괴하고 참조를 null로 설정합니다.
    /// </summary>
    /// <param name="texture">파괴할 Texture2D (ref로 전달하여 null 할당)</param>
    public static void DestroyTexture(ref Texture2D texture)
    {
        if (texture != null)
        {
            Object.Destroy(texture);
            texture = null;
        }
    }

    /// <summary>
    /// Sprite와 연결된 Texture2D를 함께 안전하게 파괴합니다.
    /// </summary>
    /// <param name="sprite">파괴할 Sprite (ref로 전달하여 null 할당)</param>
    public static void DestroySprite(ref Sprite sprite)
    {
        if (sprite != null)
        {
            // Sprite가 참조하는 Texture2D도 함께 파괴
            if (sprite.texture != null)
            {
                Object.Destroy(sprite.texture);
            }
            Object.Destroy(sprite);
            sprite = null;
        }
    }
}
