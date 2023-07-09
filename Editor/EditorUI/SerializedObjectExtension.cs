using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Puffercat.Uxt.Editor.EditorUI
{
    public static class SerializedObjectExtension
    {
        public static VisualElement CreatePropertyDrawer(this SerializedObject so, string propertyName)
        {
            var property = so.FindProperty(propertyName);
            return new PropertyField(so.FindProperty(propertyName));
        }

        public static VisualElement CreatePropertyDrawer(this SerializedProperty property)
        {
            return new PropertyField(property);
        }
        
        public static IEnumerable<VisualElement> CreatePropertyDrawersForAllChildren(this SerializedProperty property)
        {
            foreach (SerializedProperty child in property)
            {
                if (child is null) continue;
                yield return new PropertyField(child);
            }
        }
    }
}