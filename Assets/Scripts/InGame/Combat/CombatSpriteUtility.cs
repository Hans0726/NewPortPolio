using UnityEngine;

public static class CombatSpriteUtility
{
    private static Sprite _fallbackSprite;

    public static Sprite GetCardSprite(CardData card)
    {
        if (card == null)
        {
            return GetFallbackSprite();
        }

        if (card.fieldSprite != null)
        {
            return card.fieldSprite;
        }

        Sprite fieldSprite = Resources.Load<Sprite>("FieldSprites/" + card.cardName);
        if (fieldSprite != null)
        {
            return fieldSprite;
        }

        // Existing cards continue to work until each one receives a dedicated field sprite.
        if (card.cardIllustration != null)
        {
            return card.cardIllustration;
        }

        Sprite legacyCardSprite = Resources.Load<Sprite>("CardImage/" + card.cardName);
        if (legacyCardSprite != null)
        {
            return legacyCardSprite;
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
