using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.UI;

namespace Game.Editor
{
    [CustomEditor(typeof(ClickSoundButton))]
    [CanEditMultipleObjects]
    public class ClickSoundButtonEditor : ButtonEditor
    {
        private SerializedProperty buttonClickSoundProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            buttonClickSoundProperty = serializedObject.FindProperty(nameof(ClickSoundButton.buttonClickSound));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            DrawClickSoundDropdown();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawClickSoundDropdown()
        {
            List<ValueDropdownItem<string>> soundOptions = ConstantManager.GetSoundNames().ToList();
            string[] optionLabels = new string[soundOptions.Count + 1];
            optionLabels[0] = "None";
            for (int i = 0; i < soundOptions.Count; i++)
                optionLabels[i + 1] = soundOptions[i].Text;

            int selectedIndex = 0;
            if (!buttonClickSoundProperty.hasMultipleDifferentValues)
            {
                int soundIndex = soundOptions.FindIndex(option => option.Value == buttonClickSoundProperty.stringValue);
                selectedIndex = soundIndex >= 0 ? soundIndex + 1 : 0;
            }

            bool previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = buttonClickSoundProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup("Button Click Sound", selectedIndex, optionLabels);
            if (EditorGUI.EndChangeCheck())
                buttonClickSoundProperty.stringValue = newSelectedIndex > 0 ? soundOptions[newSelectedIndex - 1].Value : string.Empty;

            EditorGUI.showMixedValue = previousShowMixedValue;
        }
    }
}
