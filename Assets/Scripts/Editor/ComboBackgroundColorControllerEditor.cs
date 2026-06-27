using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ComboBackgroundColorController))]
public sealed class ComboBackgroundColorControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ComboBackgroundColorController controller = (ComboBackgroundColorController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (GUILayout.Button("Preview Next Lerp Color"))
        {
            Undo.RecordObject(controller, "Preview Next Background Color");
            controller.PreviewNextColor();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Preview Random Lerp Color"))
        {
            Undo.RecordObject(controller, "Preview Random Background Color");
            controller.PreviewRandomColor();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Apply Preview Color Immediately"))
        {
            Undo.RecordObject(controller, "Apply Background Preview Color");
            controller.ApplyPreviewImmediately();
            EditorUtility.SetDirty(controller);
        }
    }
}
