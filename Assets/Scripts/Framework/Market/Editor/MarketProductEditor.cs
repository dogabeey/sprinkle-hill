#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(MarketProduct))]
    public sealed class MarketProductEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "rewards");

            SerializedProperty rewards = serializedObject.FindProperty("rewards");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Rewards stack. The selected Buy Count is applied to every reward. Add currency, an action-bar inventory item (boosters or power-ups), an event callback, or implement a custom MarketReward.", MessageType.Info);

            for (int i = 0; i < rewards.arraySize; i++)
            {
                SerializedProperty reward = rewards.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(reward, new GUIContent($"Reward {i + 1}"), true);
                if (GUILayout.Button("Remove Reward"))
                {
                    rewards.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Currency Reward")) Add(rewards, new CurrencyMarketReward());
            if (GUILayout.Button("Add Action Inventory Reward")) Add(rewards, new ActionInventoryMarketReward());
            if (GUILayout.Button("Add Event Reward")) Add(rewards, new EventMarketReward());
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        private static void Add(SerializedProperty rewards, MarketReward reward)
        {
            int index = rewards.arraySize;
            rewards.InsertArrayElementAtIndex(index);
            rewards.GetArrayElementAtIndex(index).managedReferenceValue = reward;
        }
    }
}
#endif
