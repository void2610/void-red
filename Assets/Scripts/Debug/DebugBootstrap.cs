using UnityEngine;
using Void2610.LiminalPalette.Integration.VContainer;

/// <summary>
/// DebugLifetimeScope を初回シーン読込み時に自動生成する登録 (以後は DontDestroyOnLoad で常駐し再生成されない)
/// sceneLoaded / playModeStateChanged 購読や EditorOnly タグ付け等は LiminalPalette 側の SceneLifetimeScopeBootstrap に集約されている
/// </summary>
internal static class DebugBootstrap
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorRegister()
    {
        SceneLifetimeScopeBootstrap.Register<DebugLifetimeScope>();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PlayerRegister()
    {
        SceneLifetimeScopeBootstrap.Register<DebugLifetimeScope>();
    }
}
