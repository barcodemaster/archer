using UnityEngine;
using UnityEngine.U2D;

public static class IconHelper
{
    private static SpriteAtlas _atlas;
    private static SpriteAtlas _commonAtlas;

    public static Sprite GetSprite(string name)
    {
        if (_atlas == null)
            _atlas = Resources.Load<SpriteAtlas>("UI/Icon");

        Sprite sprite = _atlas != null ? _atlas.GetSprite(name) : null;

        if (sprite == null)
        {
            if (_commonAtlas == null)
                _commonAtlas = Resources.Load<SpriteAtlas>("UI/Common");
            sprite = _commonAtlas != null ? _commonAtlas.GetSprite(name) : null;
        }

        return sprite;
    }
}
