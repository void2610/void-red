using System;
using UnityEngine;

/// <summary>
/// カードの効果を表すクラス
/// 許し、拒絶、空白の3つのパラメータを持つ
/// </summary>
[Serializable]
public class CardStatus
{
    [SerializeField] private float forgiveness; // 許し
    [SerializeField] private float rejection;   // 拒絶  
    [SerializeField] private float blank;       // 空白
    
    // プロパティ
    public float Forgiveness => forgiveness;
    public float Rejection => rejection;
    public float Blank => blank;
    
    public CardStatus(float forgiveness, float rejection, float blank)
    {
        this.forgiveness = forgiveness;
        this.rejection = rejection;
        this.blank = blank;
    }
    
    /// <summary>
    /// 効果の合計値を取得
    /// </summary>
    public float GetTotalEffect()
    {
        return forgiveness + rejection + blank;
    }
    
    /// <summary>
    /// 他のCardStatusとの距離を計算（ユークリッド距離）
    /// </summary>
    public float GetDistanceTo(CardStatus other)
    {
        if (other == null) return float.MaxValue;
        
        var forgivenessDistance = forgiveness - other.forgiveness;
        var rejectionDistance = rejection - other.rejection;
        var blankDistance = blank - other.blank;
        
        return Mathf.Sqrt(
            forgivenessDistance * forgivenessDistance +
            rejectionDistance * rejectionDistance +
            blankDistance * blankDistance
        );
    }
}