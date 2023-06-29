using UnityEngine;
using UnityEngine.UIElements;

namespace Puffercat.Uxt.Editor.EditorUI
{
    public static class EditorConstants
    {
        public static float SmallGap(float multiplier = 1.0f)
        {
            return 2.0f * multiplier;
        }

        public static float MediumGap(float multiplier = 1.0f)
        {
            return 2.0f * SmallGap(multiplier);
        }

        public static float LargeGap(float multiplier = 1.0f)
        {
            return 2.0f * MediumGap(multiplier);
        }

        public static float ExtraLargeGap(float multiplier = 1.0f)
        {
            return 1.5f * LargeGap(multiplier);
        }

        public static readonly Color Dark = new(0.1f, 0.1f, 0.1f);

        public static readonly Color MediumDark = new(56f / 255f, 56f / 255f, 56f / 255f);

        public static readonly Color LightDark = new(60f / 255f, 60f / 255f, 60f / 255f);
        
        public static readonly Color LightGrey = new(81f / 255f, 81f / 255f, 81f / 255f);
    }
}