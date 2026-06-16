using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraShake))]
public class CameraShakeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Test Camera Shake"))
            {
                ((CameraShake)target).Shake();
            }
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode to test the camera shake button.", MessageType.Info);
    }
}
