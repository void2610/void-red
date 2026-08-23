using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// プレイヤーの顔アイコンと状態ゲージを表示するView
/// </summary>
public class PlayerFaceView : MonoBehaviour
{
    [SerializeField] private GaugeView painGauge;
    [SerializeField] private GaugeView dilutionGauge;

    private float _lastPainValue;
    private float _lastDilutionValue;

    public void UpdatePainGauge(float value)
    {
        if (value > _lastPainValue)
            SeManager.Instance.PlaySe("SE_PAIN_UP", pitch: 1f);
        _lastPainValue = value;
        painGauge.SetValue(value);
    }

    public void UpdateDilutionGauge(float value)
    {
        if (value > _lastDilutionValue)
            SeManager.Instance.PlaySe("SE_FADE_UP", pitch: 1f);
        _lastDilutionValue = value;
        dilutionGauge.SetValue(value);
    }
}
