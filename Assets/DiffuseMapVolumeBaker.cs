using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(DiffusesNodeMap))]
public class EditorDiffuseMapBaker : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DiffusesNodeMap script = (DiffusesNodeMap)target;

        if (GUILayout.Button("Bake"))
        {
            script.bake = true;
        }
    }
}