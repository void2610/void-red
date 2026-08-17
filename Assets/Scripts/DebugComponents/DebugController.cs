using UnityEngine;
using VContainer;
using Void2610.UnityTemplate.Steam;

/// <summary>
/// デバッグ機能をまとめて管理するコンポーネント
/// </summary>
public class DebugController : MonoBehaviour
{
    [Header("セーブデータ")]
    [SerializeField] private bool showSaveInfo = false;

    [Header("Steam連携")]
    [SerializeField] private bool resetSteamStats = false; // Steamの実績・統計情報をリセットする

    private GameProgressService _gameProgressService;
    private SaveDataManager _saveDataManager;
    private SteamService _steamService;

    [Inject]
    public void Construct(GameProgressService gameProgressService, SaveDataManager saveDataManager, SteamService steamService)
    {
        _gameProgressService = gameProgressService;
        _saveDataManager = saveDataManager;
        _steamService = steamService;
        Init();
    }

    private void Init()
    {
        if (!Application.isEditor)
        {
            Destroy(this);
            return;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        if (resetSteamStats)
        {
            var success = _steamService.ResetAllStats();
            if (success) Debug.Log("[Debug] Steamの実績・統計情報をリセットしました");
            else Debug.LogWarning("[Debug] Steamの実績・統計情報のリセットに失敗しました");
        }
#endif
    }

    private void OnGUI()
    {
        if (!Application.isEditor || !showSaveInfo || _gameProgressService == null || _saveDataManager == null) return;

        GUILayout.BeginArea(new Rect(10, 100, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== セーブデータ情報 ===");

        // セーブファイル存在確認
        var saveExists = _saveDataManager.SaveFileExists();
        GUILayout.Label($"セーブファイル: {(saveExists ? "存在" : "なし")}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
