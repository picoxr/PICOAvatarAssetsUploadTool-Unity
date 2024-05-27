#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * @brief Subview 
         */ 
        public class PavWidget 
        {
            public PavWidget()
            {
                _mainElement = null;
            }

            public PavWidget(VisualElement ve)
            {
                _mainElement = ve;
                if (_mainElement != null)
                    _createWithExistVisualElement = true;
            }
            
            
#region Public Properties
            
            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public virtual string uxmlPathName { get => ""; }

            // main element
            public VisualElement mainElement { get => _mainElement; }

            // whether is visible
            public bool visible
            {
                get => _visible;
                set{
                    SetVisible(value);
                }
            }

#endregion


#region Public Methods

            /**
             * @brief Sets whether the widget is visible.
             */
            public void SetVisible(bool visible_)
            {
                if(_visible != visible_)
                {
                    _visible = visible_;
                    //
                    if(mainElement != null)
                    {
                        mainElement.visible = _visible;
                    }
                }
            }

            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public virtual VisualElement BuildUIDOM()
            {
                if (!_createWithExistVisualElement)
                {
                    // Import UXML Assets/PicoAvatarAssetPreview/Editors/Views/
                    var uiAssetPath = AssetBuilderConfig.instance.uiDataAssetsPath + uxmlPathName;
                    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiAssetPath);
                    if (visualTree == null)
                    {
                        UnityEngine.Debug.LogError(string.Format("ui asset file lost! {0}", uiAssetPath));
                        return null;
                    }
                    _mainElement = visualTree.Instantiate();
                }
                
                _visible = _mainElement.visible;

                return _mainElement;
            }

            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            public virtual bool BindUIActions()
            {
                return true;
            }

            /**
             * @brief Detach the panel from current attached DOM. If derived class override the method, must invoke it.
             */
            public virtual void DetachFromDOM()
            {
                if(_mainElement != null && _mainElement.parent != null)
                {
                    _mainElement.parent.Remove(_mainElement);
                }
            }

            /**
             * Invoked when panel that manage the widget destroyed. If derived class override the method, MUST invoke it.
             */ 
            public virtual void OnDestroy()
            {
                DetachFromDOM();
                // 
                if (_mainElement != null)
                {
                    _mainElement.Unbind();
                    _mainElement = null;
                }
            }
            
            public bool IsVisible()
            {
                if (mainElement == null)
                    return false;

                return (mainElement.style.display == DisplayStyle.Flex);
            }
            
            public static void ShowVisualElement(VisualElement visualElement, bool state)
            {
                if (visualElement == null)
                    return;

                visualElement.style.display = (state) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            public void ShowWidget()
            {
                ShowVisualElement(mainElement, true);
            }

            public void HideWidget()
            {
                if (IsVisible())
                {
                    ShowVisualElement(mainElement, false);
                }
            }

            public bool CreateWithExistVisualElement()
            {
                return _createWithExistVisualElement;
            }

#endregion


#region Private Fields

            private VisualElement _mainElement;
            private bool _visible = true;
            private bool _createWithExistVisualElement = false;

#endregion


#region Private / Protected Methods


#endregion
        }
    }
}
#endif