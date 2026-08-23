using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敵の顔アイコンを表示するView
/// </summary>
public class EnemyFaceView : MonoBehaviour
{
    [SerializeField] private Image faceIcon;

    public void Initialize(ParticipantData participant) => faceIcon.sprite = participant.IconSprite ? participant.IconSprite : participant.Portrait;
}
