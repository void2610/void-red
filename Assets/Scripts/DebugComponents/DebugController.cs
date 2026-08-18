using UnityEngine;
using VContainer;
using Void2610.UnityTemplate.Steam;

/// <summary>
/// デバッグ機能をまとめて管理するコンポーネント
/// セーブデータ等の状態観測は LiminalPalette のコマンド (Save/FileExists 等) を使う
/// </summary>
public class DebugController : MonoBehaviour
{
    [Header("Steam連携")]
    [SerializeField] private bool resetSteamStats = false; // Steamの実績・統計情報をリセットする

    private SteamService _steamService;

    [Inject]
    public void Construct(SteamService steamService)
    {
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
}
