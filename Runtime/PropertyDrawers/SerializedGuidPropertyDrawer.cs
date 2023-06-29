#if UNITY_EDITOR
using Puffercat.Uxt.Utils;
using UnityEditor;
using UnityEngine;

namespace Puffercat.Uxt.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(SerializedGuid))]
    public class SerializableGuidPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var ySep = EditorGUIUtility.singleLineHeight;
            var part1 = property.FindPropertyRelative("GuidPart1");
            var part2 = property.FindPropertyRelative("GuidPart2");
            var part3 = property.FindPropertyRelative("GuidPart3");
            var part4 = property.FindPropertyRelative("GuidPart4");
            var oldGuid = SerializedGuid.FromInts(part1.intValue, part2.intValue, part3.intValue, part4.intValue);

            EditorGUI.BeginProperty(position, label, property);
            {
                position = EditorGUI.PrefixLabel(
                    new Rect(position.x, position.y + ySep / 2, position.width, position.height),
                    GUIUtility.GetControlID(FocusType.Passive), label);
                
                position.y -= ySep / 2; // Offsets position so we can draw the label for the field centered
                
                var buttonSize =
                    position.width /
                    3; // Update size of buttons to always fit perfeftly above the string representation field

                if (GUI.Button(new Rect(position.xMin, position.yMin, buttonSize, ySep - 2), "New"))
                {
                    var guid = SerializedGuid.NewGuid();
                    part1.intValue = guid.GuidPart1;
                    part2.intValue = guid.GuidPart2;
                    part3.intValue = guid.GuidPart3;
                    part4.intValue = guid.GuidPart4;
                }

                if (GUI.Button(new Rect(position.xMin + buttonSize, position.yMin, buttonSize, ySep - 2), "Copy"))
                {
                    EditorGUIUtility.systemCopyBuffer = oldGuid.ToString();
                }

                if (GUI.Button(new Rect(position.xMin + 2 * buttonSize, position.yMin, buttonSize, ySep - 2), "Empty"))
                {
                    part1.intValue = 0;
                    part2.intValue = 0;
                    part3.intValue = 0;
                    part4.intValue = 0;
                }

                var pos = new Rect(position.xMin, position.yMin + ySep, position.width, ySep - 2);
                GUI.Label(pos, oldGuid.ToString());
        
                EditorGUI.EndProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }
    }
}
#endif