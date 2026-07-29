using System;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// タイトル画面でのアイドル時PV再生を制御するPresenter
/// </summary>
public class TitleIdlePVPresenter : IStartable, ITickable, IDisposable
{
    private readonly ExhibitSettings _settings;
    private readonly IdleDetector _idleDetector;
    private readonly TitlePVView _titlePVView;

    private bool _isPVPlaying;

    public TitleIdlePVPresenter(
        ExhibitSettings settings,
        IdleDetector idleDetector,
        TitlePVView titlePVView)
    {
        _settings = settings;
        _idleDetector = idleDetector;
        _titlePVView = titlePVView;
    }

    public void Tick()
    {
        if (!_settings.EnableTitleIdlePv) return;

        if (!_isPVPlaying)
        {
            // アイドル時間が閾値を超えたらPV再生
            if (_idleDetector.IdleSeconds >= _settings.TitleIdleToPvSeconds)
            {
                _titlePVView.Play();
                _isPVPlaying = true;
            }
        }
        else
        {
            // 入力があったらPV停止
            if (_idleDetector.IdleSeconds < 1f)
            {
                _titlePVView.Stop();
                _isPVPlaying = false;
            }
        }
    }

    public void Start() { }

    public void Dispose() { }
}
