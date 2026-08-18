using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 現在の EventSystem 選択オブジェクトをインスペクタで確認するデバッグ用コンポーネント
/// </summary>
public class CurrentSelectedGameObjectChecker : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Awake()
    {
        // エディタ専用の観測コンポーネントのため、ビルドでは毎フレームの Update を走らせない
        if (!Application.isEditor) Destroy(this);
    }

    private void Update()
    {
        targetObject = EventSystem.current.currentSelectedGameObject;
    }
}
