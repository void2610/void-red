using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// novel-kit のIAudioChannel実装
/// SEはAudioSourceをプールしている共通のSeManagerへ委譲する (BGMはノベル側で扱わないため未対応)
/// </summary>
public class NovelKitAudioChannel : MonoBehaviour, IAudioChannel
{
    /// <summary>
    /// 直近に鳴らしたSEの残り時間 (秒)。オート進行がSEを鳴らし切ってから進むために使う
    /// </summary>
    public float SeRemainingSeconds => Mathf.Max(0f, _seEndTime - Time.time);
    // SeManager が pitch 未指定時に使うランダム幅と同じ
    private const float SE_PITCH_MIN = 0.8f;
    private const float SE_PITCH_MAX = 1.2f;

    private bool _bgmWarned;
    private float _seEndTime;

    public void StopBgm() => WarnBgmUnsupported();
    public void PlayBgm(string bgmKey) => WarnBgmUnsupported();

    // 再生完了まで待つとセリフ送りが止まるため、鳴らし始めたら即座に次へ進める
    public UniTask PlaySeAsync(string seKey, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return UniTask.FromCanceled(ct);

        PlayAndRecordLength(seKey);
        return UniTask.CompletedTask;
    }

    public async UniTask PlaySeLoopAsync(string seKey, float interval, int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            PlayAndRecordLength(seKey);
            if (i < count - 1) await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);
        }
    }

    private void PlayAndRecordLength(string seKey)
    {
        var seManager = SeManager.Instance;

        // SeManager 任せだとピッチが分からず実際の長さを誤るため、こちらで決めて渡す (既定のランダム幅に合わせる)
        var pitch = UnityEngine.Random.Range(SE_PITCH_MIN, SE_PITCH_MAX);
        seManager.PlaySe(seKey, pitch: pitch);
        _seEndTime = Time.time + seManager.GetSeLength(seKey) / pitch;
    }

    // シナリオ側の bgm 呼び出しミスに気付けるよう、無音で捨てず一度だけ警告する
    private void WarnBgmUnsupported()
    {
        if (_bgmWarned) return;

        _bgmWarned = true;
        Debug.LogWarning("[NovelKitAudioChannel] BGM は未対応のため無視した");
    }
}
