using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AlienLogic))]
public class InEditorNPCPropertiest : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        AlienLogic script = (AlienLogic)target;
     
        if (GUILayout.Button("Set as Target"))
        {
            script.NPCManager.SetTarget(Selection.activeGameObject.transform.position);
        }

        //GUILayout.Label("Offset from others: " + script.behaviourState.offsetFromOther);
    }
}
