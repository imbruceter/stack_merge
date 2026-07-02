using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/Ratio Horizontal Layout Group")]
    public sealed class RatioHorizontalLayoutGroup : HorizontalLayoutGroup
    {
        [Range(0f, 1f)]
        [SerializeField]
        private float leftRatio = 0.75f;

        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();

            if (rectChildren.Count != 2)
                return;

            float width = rectTransform.rect.width;
            width -= padding.left + padding.right;
            width -= spacing;

            float leftWidth = width * leftRatio;
            float rightWidth = width - leftWidth;

            SetChildAlongAxis(rectChildren[0], 0, padding.left, leftWidth);
            SetChildAlongAxis(rectChildren[1], 0, padding.left + leftWidth + spacing, rightWidth);
        }
    }
}