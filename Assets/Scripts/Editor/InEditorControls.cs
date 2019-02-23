using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NPCManager))]
 public class InEditorScript : Editor
{
    void OnSceneGUI()
    {
        NPCManager script = (NPCManager)target;
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                {
                    if (e.button == 1 && e.shift)
                    {
                        RaycastHit hit;
                        Ray worldRay = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
                        if (Physics.Raycast(worldRay, out hit, Mathf.Infinity))
                        {
                            script.SetTarget(hit.point);
                        }
                    }
                    break;
                }
        }
        if (GUILayout.Button("Set as Target"))
        {
            base.OnInspectorGUI();
            script.SetTarget(Selection.activeGameObject.transform.position);
        }
    }
}
