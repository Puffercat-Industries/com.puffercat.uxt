using UnityEngine.UIElements;

namespace Puffercat.Uxt.Editor.EditorUI
{
    public class ComponentBox : VisualElement
    {
        private VisualElement ContentBox { get; }
        private readonly Toggle m_toggle;
        
        public override VisualElement contentContainer { get; }

        public ComponentBox(string label)
        {
            var banner = new VisualElement
            {
                style =
                {
                    backgroundColor = new StyleColor(EditorConstants.Dark),
                    display = DisplayStyle.Flex,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart
                }
            };

            hierarchy.Add(banner);

            var foldOut = new Foldout();
            foldOut.contentContainer.style.display = DisplayStyle.None;
            foldOut.RegisterCallback<ChangeEvent<bool>>(_ =>
                foldOut.contentContainer.style.display = DisplayStyle.None);
            banner.Add(foldOut);

            m_toggle = new Toggle();
            banner.Add(m_toggle);
            banner.Add(new Label(label)
            {
                style = { marginLeft = 12 }
            });

            ContentBox = new VisualElement()
            {
                style =
                {
                    backgroundColor = EditorConstants.LightDark,
                    marginBottom = EditorConstants.MediumGap(),
                    paddingLeft = EditorConstants.ExtraLargeGap(),
                    paddingTop = EditorConstants.MediumGap(),
                    paddingRight = EditorConstants.MediumGap(),
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftColor = EditorConstants.Dark,
                    borderRightColor = EditorConstants.Dark,
                    borderBottomColor = EditorConstants.Dark
                }
            };

            foldOut.RegisterCallback<ChangeEvent<bool>>(
                evt =>
                {
                    var contentVisible = evt.newValue;
                    schedule.Execute(() =>
                    {
                        ContentBox.style.display = contentVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    });
                }
            );

            hierarchy.Add(ContentBox);

            contentContainer = ContentBox;
        }

        public IBindable Toggle => m_toggle;
    }
}