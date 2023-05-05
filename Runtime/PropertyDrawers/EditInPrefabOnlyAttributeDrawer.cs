#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Puffercat.Uxt.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(EditInPrefabOnlyAttribute))]
    public class EditInPrefabOnlyAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.isInstantiatedPrefab)
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

        }
    }
}
#endif