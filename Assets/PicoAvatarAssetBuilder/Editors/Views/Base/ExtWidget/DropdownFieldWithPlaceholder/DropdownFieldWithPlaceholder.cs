#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;

namespace Pico.AvatarAssetBuilder
{
    public class DropdownFieldWithPlaceholder
    {
        private Label placeholderLabel;
        private VisualElement popupTextElement;
        
        private float placeholderOpicity = 0.45f;
        private float placeholderErrorOpicity = 0.95f;
        private Color inputTextColor;
        private string placeholder;

        public DropdownField dropdownField
        {
            get;
            private set;
        }
        
        public Color errorColor { get; set; } = new Color(1, 87 / 255f, 82 / 255f, 1);
        
        public DropdownFieldWithPlaceholder(DropdownField df, string placeHolder)
        {
            dropdownField = df;
            placeholder = placeHolder;
            CreatePlaceHolder();
            InitEvent();
            OnValueChange(null);
        }
        
        public void ShowErrorOnce()
        {
            placeholderLabel.style.color = errorColor;
            placeholderLabel.style.opacity = placeholderErrorOpicity;
            popupTextElement.style.color = errorColor;
        }
        
        private void CreatePlaceHolder()
        {
            // TODO 有没有更好的查找方法
            popupTextElement = dropdownField.FindChildRecursive<TextElement>();
            if (popupTextElement == null)
            {
                Debug.LogWarning("Can not find popupTextElement");
                return;
            }
            
            placeholderLabel = popupTextElement.FindChildRecursive("placeholder") as Label;
            if (placeholderLabel == null)
            {
                placeholderLabel = new Label();
                placeholderLabel.text = placeholder;
                placeholderLabel.name = "placeholder";
            }
            
            popupTextElement.Add(placeholderLabel);
            placeholderLabel.style.marginLeft = popupTextElement.style.marginLeft;
            placeholderLabel.style.marginRight = popupTextElement.style.marginRight;
            placeholderLabel.style.marginTop = popupTextElement.style.marginTop;
            placeholderLabel.style.marginBottom = popupTextElement.style.marginBottom;
            placeholderLabel.style.fontSize = popupTextElement.style.fontSize;
            placeholderLabel.style.height = Length.Percent(100);
            placeholderLabel.style.width = Length.Percent(100);
            placeholderLabel.style.position = Position.Absolute;
            placeholderLabel.style.opacity = placeholderOpicity;
            placeholderLabel.style.color = Color.white;
            inputTextColor = dropdownField.resolvedStyle.color;
        }
        
        private void InitEvent()
        {
            dropdownField.RegisterCallback<FocusInEvent>(evt => OnFocusIn());
            dropdownField.RegisterCallback<FocusOutEvent>(evt => OnFocusOut());
            dropdownField.RegisterCallback<ChangeEvent<string>>(OnValueChange);
        }
        
        void OnFocusIn()
        {
            placeholderLabel.SetVisibility(false);
        }
            
        void OnFocusOut()
        {
            if (dropdownField.index == -1)
            {
                placeholderLabel.SetVisibility(true);
            }
        }
        
        void OnValueChange(ChangeEvent<string> evt)
        {
            placeholderLabel.SetVisibility(dropdownField.index == -1);
            
            placeholderLabel.style.opacity = placeholderOpicity;
            placeholderLabel.style.color = Color.white;
            popupTextElement.style.color = inputTextColor;
        }
    }
}
#endif