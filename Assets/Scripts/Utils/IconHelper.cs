using UnityEngine;
using UnityEngine.U2D;

public static class IconHelper
{
    private static SpriteAtlas _atlas;

    public static Sprite GetSprite(string name)
    {
        if (_atlas == null)
            _atlas = Resources.Load<SpriteAtlas>("UI/Icon");
        int a = 0;
        return _atlas != null ? _atlas.GetSprite(name) : null;
    }
}
