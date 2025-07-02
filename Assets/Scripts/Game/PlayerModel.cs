using System;
using R3;
using UnityEngine;

/// <summary>
/// プレイヤーの属性データモデル（Model Layer）
/// 精神力などカードに関係ないプレイヤー固有の属性を管理
/// </summary>
public class PlayerModel : IDisposable
{
    // 公開プロパティ（読み取り専用）
    public ReadOnlyReactiveProperty<int> MentalPower => _mentalPower;
    
    // 定数
    public static int MaxMentalPower => MAX_MENTAL_POWER;
    private const int MAX_MENTAL_POWER = 20;
    
    // プライベートフィールド
    private readonly ReactiveProperty<int> _mentalPower = new();
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public PlayerModel()
    {
        _mentalPower.Value = MAX_MENTAL_POWER;
    }
    
    // === 精神力関連メソッド ===
    
    /// <summary>
    /// 精神力を消費
    /// </summary>
    /// <param name="amount">消費量</param>
    /// <returns>消費に成功したかどうか</returns>
    public bool TryConsumeMentalPower(int amount)
    {
        if (_mentalPower.Value < amount) return false;
        _mentalPower.Value -= amount;
        return true;
    }
    
    /// <summary>
    /// 精神力を回復
    /// </summary>
    /// <param name="amount">回復量</param>
    public void RestoreMentalPower(int amount)
    {
        _mentalPower.Value = Mathf.Min(_mentalPower.Value + amount, MAX_MENTAL_POWER);
    }
    
    /// <summary>
    /// 精神力を強制設定
    /// </summary>
    /// <param name="value">設定する値</param>
    public void SetMentalPower(int value)
    {
        _mentalPower.Value = Mathf.Clamp(value, 0, MAX_MENTAL_POWER);
    }
    
    /// <summary>
    /// リソースの解放
    /// </summary>
    public void Dispose()
    {
        _mentalPower?.Dispose();
    }
}