using LitMotion;
using UnityEngine;

/// <summary>
/// 天秤の傾きをLitMotionで制御する
/// tilt範囲: -1（敵側が重い）～ 0（水平）～ +1（プレイヤー側が重い）
/// 実際の天秤と同様に、入札が多い方が下がる
/// </summary>
public class BalanceTiltController : MonoBehaviour
{
    [SerializeField] private RectTransform balanceBar;
    [SerializeField] private RectTransform balanceLeft;
    [SerializeField] private RectTransform balanceRight;

    // tilt=+1.0時の各トランスフォーム値（プレイヤー側が重い=左が下がる）
    private static readonly Vector2 _maxBarPosition = new(4f, -1f);
    private const float MAX_BAR_ROTATION = -10f;
    private const float MAX_LEFT_Y = 50f;
    private const float MAX_RIGHT_Y = -50f;

    private const float OVERSHOOT_AMOUNT = 0.2f;
    private const float OVERSHOOT_DURATION = 0.15f;
    private const float SETTLE_DURATION = 0.25f;

    private float _currentTilt;
    private MotionHandle _handle;

    /// <summary>
    /// 即座に傾きを設定
    /// </summary>
    public void SetTilt(float tilt)
    {
        _handle.TryCancel();
        _currentTilt = tilt;
        ApplyTilt(tilt);
    }

    /// <summary>
    /// オーバーシュート付きアニメーションで傾きを変更
    /// </summary>
    public void AnimateTilt(float target)
    {
        _handle.TryCancel();

        var start = _currentTilt;
        var direction = Mathf.Sign(target - start);
        var overshoot = Mathf.Clamp(target + direction * OVERSHOOT_AMOUNT, -1f, 1f);
        const float totalDuration = OVERSHOOT_DURATION + SETTLE_DURATION;
        const float ratio = OVERSHOOT_DURATION / totalDuration;

        _handle = LMotion.Create(0f, 1f, totalDuration)
            .WithOnComplete(() =>
            {
                _currentTilt = target;
                ApplyTilt(target);
            })
            .Bind(t =>
            {
                float value;
                if (t < ratio)
                {
                    // フェーズ1: 目標を超えた位置まで素早く移動
                    var p = t / ratio;
                    value = Mathf.Lerp(start, overshoot, Smoothstep(p));
                }
                else
                {
                    // フェーズ2: 目標値に戻る
                    var p = (t - ratio) / (1f - ratio);
                    value = Mathf.Lerp(overshoot, target, Smoothstep(p));
                }

                _currentTilt = value;
                ApplyTilt(value);
            });
    }

    private static float Smoothstep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void ApplyTilt(float tilt)
    {
        balanceBar.localEulerAngles = new Vector3(0f, 0f, MAX_BAR_ROTATION * tilt);
        balanceBar.anchoredPosition = _maxBarPosition * tilt;
        balanceLeft.anchoredPosition = new Vector2(0f, MAX_LEFT_Y * tilt);
        balanceRight.anchoredPosition = new Vector2(0f, MAX_RIGHT_Y * tilt);
    }

    private void OnDestroy()
    {
        _handle.TryCancel();
    }
}
