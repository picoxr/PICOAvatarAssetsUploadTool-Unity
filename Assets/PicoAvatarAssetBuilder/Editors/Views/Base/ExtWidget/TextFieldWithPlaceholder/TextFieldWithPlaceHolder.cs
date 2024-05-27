#if UNITY_EDITOR
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Pico.AvatarAssetBuilder
{
    public class TextFieldWithPlaceHolder
    {
        private Label placeholderLabel;
        private VisualElement inputText;

        private float placeholderOpicity = 0.45f;
        private float placeholderErrorOpicity = 0.95f;
        private Color inputTextColor;
        private string placeholder;
        private bool readOnly;
        // 双击选中相关
        private bool doubleClickSelectAll = false;
        private bool doubleClickFlag = false;
        private DateTime doubleClickDT = DateTime.Now;

        public TextField textField
        {
            get;
            private set;
        }

        public bool ReadOnly
        {
            get { return readOnly; }
            set
            {
                readOnly = value;
                if (textField != null)
                    textField.isReadOnly = readOnly;
            }
        }

        public bool DoubleClickSelectAll
        {
            get { return doubleClickSelectAll; }
            set
            {
                doubleClickSelectAll = value;
                if (doubleClickSelectAll)
                {
                    textField.doubleClickSelectsWord = false;
                    textField.RegisterCallback<ClickEvent>(DoubleClickProcessor);
                }
                else
                {
                    textField.doubleClickSelectsWord = true;
                    textField.UnregisterCallback<ClickEvent>(DoubleClickProcessor);
                }
            }
        } 

        public Color errorColor { get; set; } = new Color(1, 87 / 255f, 82 / 255f, 1);

        public TextFieldWithPlaceHolder(TextField tf, string placeHolder)
        {
            textField = tf;
            placeholder = placeHolder;
            readOnly = textField.isReadOnly;
            CreatePlaceHolder();
            InitEvent();
        }
        
        public TextFieldWithPlaceHolder(TextField tf, string placeHolder, bool readOnly) : this(tf, placeHolder)
        {
            ReadOnly = readOnly;
        }

        public void ShowErrorOnce()
        {
            placeholderLabel.style.color = errorColor;
            placeholderLabel.style.opacity = placeholderErrorOpicity;
            inputText.style.color = errorColor;
        }

        public void ResetError()
        {
            placeholderLabel.style.opacity = placeholderOpicity;
            placeholderLabel.style.color = Color.white;
            inputText.style.color = inputTextColor;
        }

        public void SetNormalTextColor(Color color)
        {
            inputTextColor = color;
        }


        private void CreatePlaceHolder()
        {
            placeholderLabel = textField.FindChildRecursive("placeholder") as Label;
            if (placeholderLabel == null)
            {
                placeholderLabel = new Label();
                placeholderLabel.text = placeholder;
                placeholderLabel.name = "placeholder";
            }
            
            inputText = textField.FindChildRecursive("unity-text-input");
            if (inputText != null)
            {
                inputText.Add(placeholderLabel);
                placeholderLabel.style.marginLeft = inputText.style.marginLeft;
                placeholderLabel.style.marginRight = inputText.style.marginRight;
                placeholderLabel.style.marginTop = inputText.style.marginTop;
                placeholderLabel.style.marginBottom = inputText.style.marginBottom;
                placeholderLabel.style.fontSize = inputText.style.fontSize;
                placeholderLabel.style.height = Length.Percent(100);
                placeholderLabel.style.width = Length.Percent(100);
                placeholderLabel.style.position = Position.Absolute;
                placeholderLabel.style.opacity = placeholderOpicity;
                placeholderLabel.style.color = Color.white;
                inputTextColor = textField.resolvedStyle.color;
            }
            else
            {
                Debug.LogError("Create TextFieldWithPlaceHolder failed!!");
            }
        }

        private void InitEvent()
        {
            textField.RegisterCallback<FocusInEvent>(evt => onFocusIn());
            textField.RegisterCallback<FocusOutEvent>(evt => onFocusOut());
            textField.RegisterValueChangedCallback(onValueChange);
        }
        
        void onFocusIn()
        {
            if (!textField.isReadOnly)
                placeholderLabel.SetVisibility(false);
        }
            
        void onFocusOut()
        {
            if (string.IsNullOrEmpty(textField.text))
            {
                placeholderLabel.SetVisibility(true);
            }
        }
            
        void onValueChange(ChangeEvent<string> evt)
        {
            if (textField.focusController.focusedElement == textField)
            {
                placeholderLabel.SetVisibility(false);
            }
            else
            {
                placeholderLabel.SetVisibility(string.IsNullOrEmpty(evt.newValue));
            }

            ResetError();
        }

        private void DoubleClickProcessor(ClickEvent evt)
        {
            var ts = DateTime.Now - DateTime.Today;
            var lastTs = doubleClickDT - DateTime.Today;
            if (doubleClickFlag && ts.TotalMilliseconds - lastTs.TotalMilliseconds > 300)
                doubleClickFlag = false;
            
            if (!doubleClickFlag)
            {
                doubleClickFlag = true;
                doubleClickDT = DateTime.Now;
            }
            else
            {
                textField.SelectAll();
                doubleClickFlag = false;
            }
        }
    }
}
#endif