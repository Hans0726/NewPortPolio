using UnityEngine;

public static class CombatSpriteUtility
{
    private static Sprite _fallbackSprite;

    public static Sprite GetCardSprite(CardData card)
    {
        if (card != null && card.cardImage != null)
        {
            return card.cardImage;
        }

        if (card != null)
        {
            Sprite resourceSprite = Resources.Load<Sprite>("CardImage/" + card.cardName);
            if (resourceSprite != null)
            {
                return resourceSprite;
            }
        }

        return GetFallbackSprite();
    }

    private static Sprite GetFallbackSprite()
    {
        if (_fallbackSprite != null)
        {
            return _fallbackSprite;
        }

        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        _fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return _fallbackSprite;
    }
}
