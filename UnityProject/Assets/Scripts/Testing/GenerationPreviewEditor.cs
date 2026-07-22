using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerationPreview))]
public class GenerationPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GenerationPreview generator = (GenerationPreview)target;

        if (GUILayout.Button("Generate"))
        {
            DataManager.Initialize();
            
            generator.Generate();
        }
    }
}
