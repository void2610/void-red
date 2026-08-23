using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

// 8種類の感情リソースを車輪UIで表示するView
// クリックで回転し、選択中の感情タイプを変更
public class EmotionResourceDisplayView : MonoBehaviour
{
    [Header("車輪UI")]
    [SerializeField] private Transform wheelContainer;
    [SerializeField] private Button wheelButton;
    [SerializeField] private float rotationOffset; // 選択位置の角度オフセット

    [Header("感情リソースアイテム（事前配置）")]
    [SerializeField] private List<EmotionResourceItemView> items;

    public Observable<EmotionType> OnEmotionSelected => _onEmotionSelected;

    /// <summary>ホイールが今指している属性</summary>
    public EmotionType SelectedEmotion => items.Count > 0 ? items[_currentIndex].Emotion : EmotionType.Joy;

    private const int EMOTION_COUNT = 8;
    private const float ANGLE_PER_ITEM = 360f / EMOTION_COUNT; // 45度
    private const float ROTATION_DURATION = 0.3f;

    private readonly Subject<EmotionType> _onEmotionSelected = new();
    private Dictionary<EmotionType, EmotionResourceItemView> _itemDict = new();
    private int _currentIndex;
    private float _currentAngle;
    private bool _isRotating;

    public void SetInteractable(bool interactable) => wheelButton.interactable = interactable;

    public void SetSelectedEmotion(EmotionType emotion)
    {
        // emotionに対応するindexを見つけて即座に設定
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Emotion == emotion)
            {
                _currentIndex = i;
                _currentAngle = i * ANGLE_PER_ITEM;
                ApplyRotation(_currentAngle);
                return;
            }
        }
    }

    public void UpdateResources(IReadOnlyDictionary<EmotionType, int> resources)
    {
        foreach (var (emotion, amount) in resources)
        {
            if (_itemDict.TryGetValue(emotion, out var item))
            {
                item.UpdateAmount(amount);
            }
        }
    }

    private void RotateWheel()
    {
        if (_isRotating) return;

        _isRotating = true;
        var previousAngle = _currentAngle;
        _currentIndex = (_currentIndex + 1) % EMOTION_COUNT;
        _currentAngle += ANGLE_PER_ITEM;

        // 車輪回転SE再生
        SeManager.Instance.PlaySe("Wheel");

        LMotion.Create(previousAngle, _currentAngle, ROTATION_DURATION)
            .WithEase(Ease.OutBack)
            .Bind(angle =>
            {
                var angleWithOffset = angle + rotationOffset;
                wheelContainer.localEulerAngles = new Vector3(0, 0, angleWithOffset);
                // 各アイテムを逆回転させてテキストを正立に保つ
                foreach (var item in items)
                {
                    item.transform.localEulerAngles = new Vector3(0, 0, -angleWithOffset);
                }
            })
            .AddTo(gameObject)
            .ToUniTask(cancellationToken: destroyCancellationToken)
            .ContinueWith(() =>
            {
                _isRotating = false;
                _onEmotionSelected.OnNext(items[_currentIndex].Emotion);
            }).Forget();
    }

    private void ApplyRotation(float angle)
    {
        var angleWithOffset = angle + rotationOffset;
        wheelContainer.localEulerAngles = new Vector3(0, 0, angleWithOffset);
        foreach (var item in items)
        {
            item.transform.localEulerAngles = new Vector3(0, 0, -angleWithOffset);
        }
    }

    private void Awake()
    {
        // アイテムの初期化
        _itemDict.Clear();
        for (var i = 0; i < items.Count; i++)
        {
            var emotion = (EmotionType)i;
            items[i].Initialize(emotion);
            _itemDict[emotion] = items[i];
        }

        wheelButton.OnClickAsObservable().Subscribe(_ => RotateWheel()).AddTo(this);
    }

    private void OnDestroy()
    {
        _onEmotionSelected.Dispose();
    }
}
