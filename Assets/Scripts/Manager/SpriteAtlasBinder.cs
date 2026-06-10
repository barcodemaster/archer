using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// SpriteAtlas V2 Late Binding 콜백을 등록하여 런타임에 아틀라스를 바인딩한다.
/// </summary>
public static class SpriteAtlasBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SpriteAtlasManager.atlasRequested += OnAtlasRequested;
    }

    /// <summary>
    /// 아틀라스 요청 시 Resources 폴더에서 로드하여 바인딩한다.
    /// </summary>
    static void OnAtlasRequested(string tag, System.Action<SpriteAtlas> callback)
    {
        SpriteAtlas atlas = Resources.Load<SpriteAtlas>($"UI/{tag}");
        if (atlas != null)
            callback(atlas);
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[SpriteAtlasBinder] SpriteAtlas not found: UI/{tag}");
#endif
        }
    }
}
