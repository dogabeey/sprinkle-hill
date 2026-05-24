using UnityEditor;
using UnityEngine; using Game.EventManagement;

#if UNITY_EDITOR

[CustomEditor(typeof(FlexibleGridLayoutGroup))]
[CanEditMultipleObjects]
public class FlexibleLayoutGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "m_CellSize", "m_Spacing", "m_Padding");

        serializedObject.ApplyModifiedProperties();
    }
}
#endif