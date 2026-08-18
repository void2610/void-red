using UnityEditor;
using UnityEngine;

/// <summary>
/// GameViewCapture のインスペクタに撮影操作ボタンを追加するカスタムエディタ
/// </summary>
[CustomEditor(typeof(GameViewCapture))]
public class GameViewCaptureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var capture = (GameViewCapture)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

        // 実行中のみボタンを有効化
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("スクリーンショット撮影", GUILayout.Height(30)))
        {
            capture.CaptureScreenshot();
        }

        GUI.enabled = true;

        if (GUILayout.Button("スクリーンショットフォルダを開く"))
        {
            capture.OpenScreenshotFolder();
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("スクリーンショット撮影はゲーム実行中のみ可能です", MessageType.Info);
        }
    }
}
