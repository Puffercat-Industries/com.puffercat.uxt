using Puffercat.Uxt.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Puffercat.Uxt.Editor.EditorUI
{
    [CustomPropertyDrawer(typeof(Optional<>))]
    public class OptionalPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var propHasValue = property.FindPropertyRelative("m_hasValue");
            var propValue = property.FindPropertyRelative("m_value");

            var root = new VisualElement
            {
                style =
                {
                    display = DisplayStyle.Flex,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    marginTop = EditorConstants.SmallGap(),
                    marginBottom = EditorConstants.SmallGap()
                }
            };

            var toggle = new Toggle()
            {
                style =
                {
                    marginRight = EditorConstants.LargeGap(1.4f),
                }
            };
            toggle.BindProperty(propHasValue);
            root.Add(toggle);

            var propertyField = new PropertyField(propValue, property.displayName)
            {
                style =
                {
                    flexGrow = 1 ,
                    backgroundColor = EditorConstants.Dark
                }
            };
            propertyField.SetEnabled(propHasValue.boolValue);
            propertyField.TrackPropertyValue(propHasValue, prop =>
            {
                propertyField.SetEnabled(prop.boolValue);
            });
            root.Add(propertyField);

            return root;
        }
    }
}