#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading;
using Pico.Avatar;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using Color = UnityEngine.Color;

namespace Pico.AvatarAssetBuilder
{
    public static class UIUtils
    {
        public const float DefaultCharacterIconAspect = 0.75f; // 角色图片比例3：4
        public const float DefaultAssetIconAspect = 1f; // 资源图片比例1：1

        public static string TmpIconDir
        {
            get
            {
                if (!Directory.Exists($"{AvatarEnv.cacheSpacePath}/Tmp") || !Directory.Exists($"{AvatarEnv.cacheSpacePath}/Tmp/Icon"))
                    Directory.CreateDirectory($"{AvatarEnv.cacheSpacePath}/Tmp/Icon");

                return $"{AvatarEnv.cacheSpacePath}/Tmp/Icon";
            }
        }
        
        /// <summary>
        /// 设置TextField的placeholder文字
        /// </summary>
        /// <param name="textField"></param>
        /// <param name="placeholder"></param>
        /// <param name="placeholderOpacity"></param>
        /// <param name="normalOpacity"></param>
        public static void SetTextFieldPlaceHolder(TextField textField, string placeholder,
            float placeholderOpacity = 0.4f, float normalOpacity = 0.95f)

        {
           
            Label placeholderLabel = textField.FindChildRecursive("placeholder") as Label;
            if (placeholderLabel == null)
            {
                placeholderLabel = new Label();
                placeholderLabel.text = placeholder;
                placeholderLabel.name = "placeholder";
            }
            
            var input = textField.FindChildRecursive("unity-text-input");
            if (input != null)
            {
                input.Add(placeholderLabel);
                placeholderLabel.style.marginLeft = input.style.marginLeft;
                placeholderLabel.style.marginRight = input.style.marginRight;
                placeholderLabel.style.marginTop = input.style.marginTop;
                placeholderLabel.style.marginBottom = input.style.marginBottom;
                placeholderLabel.style.fontSize = input.style.fontSize;
                placeholderLabel.style.height = Length.Percent(100);
                placeholderLabel.style.width = Length.Percent(100);
                placeholderLabel.style.position = Position.Absolute;
                placeholderLabel.style.opacity = placeholderOpacity;
            }
            else
            {
                return;
            }
            
            
            textField.RegisterCallback<FocusInEvent>(evt => onFocusIn());
            textField.RegisterCallback<FocusOutEvent>(evt => onFocusOut());
            textField.RegisterValueChangedCallback(onValueChange);
            
            void onFocusIn()
            {
                placeholderLabel.SetActive(false);
            }
            
            void onFocusOut()
            {
                if (string.IsNullOrEmpty(textField.text))
                {
                    placeholderLabel.SetActive(true);
                }
            }
            
            void onValueChange(ChangeEvent<string> evt)
            {
                if (textField.focusController.focusedElement == textField)
                {
                    placeholderLabel.SetActive(false);
                }
                else
                {
                    placeholderLabel.SetActive(string.IsNullOrEmpty(evt.newValue));
                }
            }
        }
        
        /// <summary>
        /// 设置VisualElement的hover颜色
        /// </summary>
        /// <param name="ve"></param>
        public static void SetVisualElementMoveHoverAndUnhoverColor(VisualElement ve)
        {
            Color? rawColor = null;
            Color maskColor = new Color(1f, 1f, 1f, 0.08f);
            
            ve.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            ve.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        
            void OnMouseEnter(MouseEnterEvent @event)
            {
                rawColor ??= ve.resolvedStyle.backgroundColor;
                
                Color newColor = new Color(rawColor.Value.r * (1 - maskColor.a) + maskColor.r * maskColor.a,
                    rawColor.Value.g * (1 - maskColor.a) + maskColor.g * maskColor.a,
                    rawColor.Value.b * (1 - maskColor.a) + maskColor.b * maskColor.a, rawColor.Value.a + maskColor.a);
                ve.style.backgroundColor = newColor;
            }
            
            void OnMouseLeave(MouseLeaveEvent @event)
            {
                rawColor ??= ve.resolvedStyle.backgroundColor;
                ve.style.backgroundColor = rawColor.Value;
            }
            
        }

        public static void AddVisualElementHoverMask(VisualElement maskTarget, VisualElement triggerTarget = null, bool attachToMaskTargetParent = false, float alpha = 0.08f)
        {
            if (maskTarget.FindChildRecursive("hoverMask") != null)
                return;
            
            if (triggerTarget == null)
                triggerTarget = maskTarget; 
            
            VisualElement ve = new VisualElement();
            ve.name = "hoverMask";
            if (attachToMaskTargetParent)
                maskTarget.parent.Add(ve);
            else
                maskTarget.Add(ve);
            
            ve.pickingMode = PickingMode.Ignore;
            ve.style.position = Position.Absolute;
            ve.style.left = 0;
            ve.style.right = 0;
            ve.style.top = 0;
            ve.style.bottom = 0;
            ve.style.borderBottomLeftRadius = maskTarget.resolvedStyle.borderBottomLeftRadius;
            ve.style.borderBottomRightRadius = maskTarget.resolvedStyle.borderBottomRightRadius;
            ve.style.borderTopLeftRadius = maskTarget.resolvedStyle.borderTopLeftRadius;
            ve.style.borderTopRightRadius = maskTarget.resolvedStyle.borderTopRightRadius;
            ve.style.backgroundColor = new Color(1, 1, 1, alpha);
            
            triggerTarget.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            triggerTarget.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            ve.SetActive(false);
            void OnMouseEnter(MouseEnterEvent @event)
            {
                ve.SetActive(true);
                ve.style.opacity = 1;
            }
            
            void OnMouseLeave(MouseLeaveEvent @event)
            {
                ve.style.opacity = 0;
                ve.SetActive(false);
            }
        }

        /// <summary>
        /// 设置DropdownField的placeholder文字
        /// </summary>
        /// <param name="dropdownField"></param>
        /// <param name="placeholder"></param>
        /// <param name="placeholderOpacity"></param>
        /// <param name="normalOpacity"></param>
        public static void SetDropdownFieldPlaceHolder(DropdownField dropdownField, string placeholder, float placeholderOpacity = 0.4f, float normalOpacity = 0.95f)
        {
            // TODO 有没有更好的查找方法
            var popupTextElement = dropdownField.FindChildRecursive<TextElement>();
            if (popupTextElement == null)
            {
                Debug.LogWarning("Can not find popupTextElement");
                return;
            }
            
            dropdownField.RegisterCallback<ChangeEvent<string>>(evt => UpdateDropdownPlaceholder(dropdownField, placeholder, placeholderOpacity, normalOpacity));
            UpdateDropdownPlaceholder(dropdownField, placeholder, placeholderOpacity, normalOpacity);
            void UpdateDropdownPlaceholder(DropdownField dropdownField, string placeholder, float placeholderOpacity, float normalOpacity)
            {
                if (string.IsNullOrEmpty(dropdownField.text) || dropdownField.text == placeholder)
                {
                    dropdownField.SetValueWithoutNotify(placeholder);
                    popupTextElement.style.opacity = placeholderOpacity;
                }
                else
                {
                    popupTextElement.style.opacity = normalOpacity;
                }
            }
        }

        /// <summary>
        /// 给VisualElement添加hover tip
        /// </summary>
        /// <param name="ve"></param>
        /// <param name="msg"></param>
        public static void SetTipText(VisualElement ve, string msg, float tipMaxWidth = 250, PavPanel panel = null)
        {
            ve.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            ve.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            
            ve.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            ve.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            
            void OnMouseEnter(MouseEnterEvent @event)
            {
                if (panel == null)
                    MainPanel.instance.ShowTip(ve, msg, tipMaxWidth: tipMaxWidth, offsetX: 0, offsetY: -9.67f);
                else
                    panel.ShowTip(ve, msg, tipMaxWidth: tipMaxWidth, offsetX: 0, offsetY: -9.67f);
            }
            
            void OnMouseLeave(MouseLeaveEvent @event)
            {
                if (panel == null)
                    MainPanel.instance.HideTip();
                else
                    panel.HideTip();
            }
        }

        /// <summary>
        /// 设置texture分辨率
        /// </summary>
        /// <param name="origTex"></param>
        /// <param name="aspect"></param>
        /// <returns></returns>
        public static Texture2D ClipTextureWithAspect(Texture2D origTex, float aspect)
        {
            int origTexWidth = origTex.width;
            int origTexHeight = origTex.height;
            float origAspect = (float)origTexWidth / (float)origTexHeight;

            if (Mathf.Abs(aspect - origAspect) < 0.01)
            {
                return origTex;
            }

            int newTexWidth = origTexWidth;
            int newTexHeight = origTexHeight;
            int rowStartIndex = 0, rowEndIndex = origTexHeight;
            int colStartIndex = 0, colEndIndex = origTexWidth;
            if (aspect <= origAspect)
            {
                newTexWidth = Mathf.RoundToInt(origTexHeight * aspect);
                if (newTexWidth % 2 == 1)
                    newTexWidth--;
                
                int offset = origTexWidth - newTexWidth;
                if (offset % 2 == 0)
                {
                    colStartIndex += offset / 2;
                    colEndIndex -= offset / 2;
                }
                else
                {
                    colStartIndex += (offset - 1) / 2;
                    colEndIndex -= (offset - 1) / 2;
                }
            }
            else
            {
                newTexHeight = Mathf.RoundToInt(origTexWidth / aspect);
                if (newTexHeight % 2 == 1)
                    newTexHeight--;
                
                int offset = origTexHeight - newTexHeight;
                if (offset % 2 == 0)
                {
                    rowStartIndex += offset / 2;
                    rowEndIndex -= offset / 2;
                }
                else
                {
                    rowStartIndex += (offset - 1) / 2;
                    rowEndIndex -= (offset - 1) / 2;
                }
            }

            Texture2D newTex = new Texture2D(newTexWidth, newTexHeight, origTex.format, false);
            for (int r = rowStartIndex; r < rowEndIndex; r++)
            {
                for (int c = colStartIndex; c < colEndIndex; c++)
                {
                    int targetRow = r - rowStartIndex;
                    int targetCol = c - colStartIndex;
                    Color color = origTex.GetPixel(c, r);
                    newTex.SetPixel(targetCol, targetRow, color);
                }
            }

            newTex.wrapMode = TextureWrapMode.Clamp;
            
            newTex.Apply();
            return newTex;
        }

        public static string SaveTextureToTmpPathWithAspect(string sourcePath, string name, float aspect)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.Log($"Can not find texture at path [{sourcePath}]");
                return sourcePath;
            }

            try
            {
                byte[] imgBytes = File.ReadAllBytes(sourcePath);
                Texture2D rawTex = new Texture2D(1, 1);
                rawTex.LoadImage(imgBytes);

                var fileName = Path.GetFileName(sourcePath);
                var tex = ClipTextureWithAspect(rawTex, aspect);
                var path = $"{GetTmpTextureDir()}/{fileName}";
                return SaveTextureToFile(path, tex);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveTextureToTmpPathWithAspect [{sourcePath}] failed : {e.Message} \n {e.StackTrace}");
                return "";
            }
        }
        
        public static string SaveTextureToFile(string filePath, Texture2D texture)
        {
            try
            {
                var bytes = texture.EncodeToPNG();
                var file = File.Open(filePath, FileMode.Create);
                var binary = new BinaryWriter(file);
                binary.Write(bytes);
                file.Close();
                
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveTextureToFile to dir [{filePath}] failed : {e.Message} \n {e.StackTrace}");
                return "";
            }
        }

        public static string GetTmpTextureDir()
        {
            var dateName = DateTime.Today.ToString("yyyy-MM-dd");
            var datePath = $"{TmpIconDir}/{dateName}";
            if (!Directory.Exists(datePath))
                Directory.CreateDirectory(datePath);

            var guid = GUID.Generate().ToString();
            var finalDir = $"{datePath}/{guid}";
            Directory.CreateDirectory(finalDir);
            return finalDir;
        }
        
        


#region 功能扩展

        public static VisualElement FindChildRecursive(this VisualElement element, string name)
        {
            if (element == null)
                return null;

            if (element.name == name)
                return element;

            for (int i = 0; i < element.childCount; i++)
            {
                var result = FindChildRecursive(element[i], name);
                if (result != null)
                    return result;
            }

            return null;
        }
        
        public static VisualElement FindChildRecursive<T>(this VisualElement element) where T : class
        {
            if (element == null)
                return null;
        
            if (element.GetFirstOfType<T>() != null)
                return element;
        
            for (int i = 0; i < element.childCount; i++)
            {
                var result = FindChildRecursive<T>(element[i]);
                if (result != null)
                    return result;
            }
        
            return null;
        }

        public static void SetActive(this VisualElement element, bool value)
        {
            if (element == null)
                return;

            element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public static void SetVisibility(this VisualElement element, bool value)
        {
            if (element == null)
                return;

            element.style.visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public static bool IsVisible(this VisualElement element)
        {
            if (element == null)
                return false;

            return element.style.visibility == Visibility.Visible;
        }
        

        #endregion
        

        

        
    }
}
#endif