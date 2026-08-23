using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoidRed.UI.Views
{
    /// <summary>
    /// 各感情タイプのバーを表示するView
    /// </summary>
    public class EmotionGaugeView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private float animationDuration = 0.5f;

        private MotionHandle _currentAnimation;
        private int _currentValue;

        /// <summary>
        /// バーの色を設定
        /// </summary>
        public void SetColor(Color color) => fillImage.color = color;

        /// <summary>
        /// バーの値を即座に設定（0.0〜1.0）
        /// </summary>
        public void SetValue(float normalizedValue)
        {
            CancelAnimation();
            fillImage.fillAmount = Mathf.Clamp01(normalizedValue);
        }

        /// <summary>
        /// バーの値を即座に設定（実数値）
        /// </summary>
        public void SetValue(int current, int max)
        {
            _currentValue = current;
            UpdateValueText();

            var normalizedValue = max > 0 ? (float)current / max : 0f;
            SetValue(normalizedValue);
        }

        /// <summary>
        /// バーの値をアニメーションで変更（0.0〜1.0）
        /// </summary>
        public void AnimateToValue(float targetValue)
        {
            CancelAnimation();
            var startValue = fillImage.fillAmount;
            var endValue = Mathf.Clamp01(targetValue);

            _currentAnimation = LMotion.Create(startValue, endValue, animationDuration)
                .WithEase(Ease.OutCubic)
                .BindToFillAmount(fillImage);
        }

        /// <summary>
        /// バーの値をアニメーションで変更（実数値）
        /// </summary>
        public void AnimateToValue(int targetValue, int max)
        {
            var normalizedTarget = max > 0 ? (float)targetValue / max : 0f;

            // 数値もアニメーション
            CancelAnimation();
            var startFill = fillImage.fillAmount;
            var endFill = Mathf.Clamp01(normalizedTarget);
            var startNum = _currentValue;
            var endNum = targetValue;

            _currentAnimation = LMotion.Create(0f, 1f, animationDuration)
                .WithEase(Ease.OutCubic)
                .WithOnComplete(() =>
                {
                    _currentValue = endNum;
                    UpdateValueText();
                })
                .Bind(t =>
                {
                    fillImage.fillAmount = Mathf.Lerp(startFill, endFill, t);
                    _currentValue = Mathf.RoundToInt(Mathf.Lerp(startNum, endNum, t));
                    UpdateValueText();
                });
        }

        private void UpdateValueText()
        {
            valueText.text = _currentValue.ToString();
        }

        private void CancelAnimation()
        {
            _currentAnimation.TryCancel();
        }

        private void OnDestroy()
        {
            CancelAnimation();
        }
    }
}
