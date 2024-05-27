#if UNITY_EDITOR
namespace UnityEngine.UIElements
{
    [UnityEngine.Scripting.Preserve]
    public class LabelAutoFit : Label
    {
        public int MinSize { get; set; }
        public int MaxSize { get; set; }
        public Axis LabelAxis { get; set; }
        public float Ratio { get; set; }

        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<LabelAutoFit, UxmlTraits>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public new class UxmlTraits : Label.UxmlTraits
        {
            UxmlFloatAttributeDescription _ratio = new UxmlFloatAttributeDescription
            {
                name = "ratio",
                defaultValue = 0.1f,
                restriction = new UxmlValueBounds { min = "0.0", max = "0.9", excludeMin = false, excludeMax = true }
            };

            UxmlEnumAttributeDescription<Axis> _axis = new UxmlEnumAttributeDescription<Axis>
            {
                name = "ratio-axis",
                defaultValue = Axis.Horizontal
            };

            UxmlIntAttributeDescription _minSize = new UxmlIntAttributeDescription
            {
                name = "min-size",
                defaultValue = 10,
                restriction = new UxmlValueBounds { min = "1", max = "99", excludeMin = true, excludeMax = true }
            };

            UxmlIntAttributeDescription _maxSize = new UxmlIntAttributeDescription
            {
                name = "max-size",
                defaultValue = 50,
                restriction = new UxmlValueBounds { min = "1", max = "99", excludeMin = true, excludeMax = true }
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                LabelAutoFit instance = ve as LabelAutoFit;
                if (instance == null) return;
                instance.RegisterCallback<GeometryChangedEvent>(instance.OnGeometryChanged);
                instance.Ratio = _ratio.GetValueFromBag(bag, cc);
                instance.LabelAxis = _axis.GetValueFromBag(bag, cc);
                instance.MinSize = _minSize.GetValueFromBag(bag, cc);
                instance.MaxSize = _maxSize.GetValueFromBag(bag, cc);
                instance.style.fontSize = 1; // trigger GeometryChangedEvent
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            float newRectLenght = this.LabelAxis == Axis.Vertical ? evt.newRect.height : evt.newRect.width;

            float oldFontSize = this.style.fontSize.value.value;
            float newFontSize = newRectLenght * this.Ratio;

            float fontSizeDelta = Mathf.Abs(oldFontSize - newFontSize);
            float fontSizeDeltaNormalized = fontSizeDelta / Mathf.Max(oldFontSize, 1);
            newFontSize = Mathf.Clamp(newFontSize, MinSize, MaxSize);
            if (fontSizeDeltaNormalized > 0.01f)
                this.style.fontSize = newFontSize;
        }

        public enum Axis
        {
            Horizontal,
            Vertical
        }
    }
}


#endif