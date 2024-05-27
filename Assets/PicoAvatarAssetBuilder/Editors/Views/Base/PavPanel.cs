#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = System.Object;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public delegate void OnPanelStoreEvent();
        
        
        /**
         * @brief Base class for editor panel which can be added to EditorWindow.
         */ 
        public class PavPanel : ScriptableObject
        {
            public static OnPanelStoreEvent OnPanelStore;
            
            // display name of the panel
            public virtual string displayName { get => ""; }

            //name of the panel(pair with paneltype do not modify)
            public virtual string panelName { get => ""; }
            
            // main element
            public VisualElement mainElement { get => _mainElement; }

            // content element named of "Content" in mainElement.
            public VisualElement contentElement { get; set; }

            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public virtual string uxmlPathName { get=>"";}

            public VisualTreeAsset visualTree  { get; set; }
            
            public bool IsVisible()
            {
                if (mainElement == null)
                    return false;

                return (mainElement.style.display == DisplayStyle.Flex);
            }

            public void ShowPanel()
            {
                mainElement.SetActive(true);
                OnShow();
            }

            public void HidePanel()
            {
                if (IsVisible())
                {
                    mainElement.SetActive(false);
                    OnHide();
                }
            }
            
            public void BuildAndBindUI(VisualElement parent)
            {
                BuildUIDOM(parent);
                BindUIActions();
            }
            
            

            // gets panel container.
            public IPavPanelContainer panelContainer { get => _panelContainer; }

            // Gets widget list in the panel.  The list MUST NOT BE modified outside PavPanel.
            public List<PavWidget> widgets { get => _widgets; }

            /**
             * @brief Detach the panel from current attached DOM. If derived class override the method, must invoke it.
             */
            public virtual void DetachFromDOM() //这个想办法写到manager里面？
            {
                if(_mainElement != null)
                {
                    _mainElement.RemoveFromHierarchy();
                    _mainElement = null;
                }
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                OnPanelStore -= OnPanelRestore;
            }
            
            public virtual void ShowTip(VisualElement target, string msg, float offsetX = 0, float offsetY = -9.67f, float tipMaxWidth = 250)
            {
            }

            public virtual void HideTip()
            {
            }

            /**
             * Attach to DOM.
             */
            public void AttachToDOM(VisualElement parentElement, IPavPanelContainer panelContainer)
            {
                if (mainElement == null)
                {
                    if (!BuildUIDOM(parentElement))
                    {
                        return;
                    }
                    BindUIActions();
                }
                //
                /*if (_mainElement != null && _mainElement.parent == null && parentElement != null)
                {
                    parentElement.Add(_mainElement);
                }*/
                //
                _panelContainer = panelContainer;
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                OnPanelStore += OnPanelRestore;
            }

            
            public void AttachToDOM(VisualElement parentElement, IPavPanelContainer panelContainer, params object[] initParams)
            {
                if (mainElement == null)
                {
                    if (!BuildUIDOM(parentElement))
                    {
                        return;
                    }
                    BindUIActions(initParams);
                }
                //
                /*if (_mainElement != null && _mainElement.parent == null && parentElement != null)
                {
                    parentElement.Add(_mainElement);
                }*/
                //
                _panelContainer = panelContainer;
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                OnPanelStore += OnPanelRestore;
            }
            
            /**
             * @breif Adds sub view.
             */
            public void AddWidget(PavWidget widget)
            {
                // sub view list.
                _widgets.Add(widget);

                // if ui has been created, should build and add uxml element of the sub view.
                if(_mainElement != null)
                {
                    var widgetElement = widget.BuildUIDOM();
                    if(widgetElement != null && !widget.CreateWithExistVisualElement())
                    {
                        contentElement.Add(widgetElement);
                    }
                }
            }

            /**
             * @brief dirty and save me
             */ 
            public virtual void SaveContext()
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (MainMenuUIManager.instance.PAAB_OPEN == true)
                {
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
                }
            }

            /**
             * @brief called when unity editor exit edit mode
             */ 
            public virtual void OnExitEditMode()
            {
                
            }

            /**
             * @brief called when panel restore
             */ 
            public virtual void OnPanelRestore()
            {
                
            }

            /// <summary>
            /// if panel has a button which named next,can implement this
            /// </summary>
            public virtual void OnNextStep()
            {
                
            }
#region Private Fields

            // main element.
            private VisualElement _mainElement = null;

            // content element.

            //  subview list.
            private List<PavWidget> _widgets = new List<PavWidget>();

            // panel container.
            private IPavPanelContainer _panelContainer;

#endregion


#region Protected/Private Mehtods

            /**
             * @brief build ui, build VisualTreeAsset from Uxml.
             */
            protected virtual bool BuildUIDOM(VisualElement parent)
            {
                if (_mainElement != null)
                {
                    return true;
                }

                
                // Import UXML Assets/PicoAvatarAssetBuilder/Editors/Views/
                var uiAssetPath = AssetBuilderConfig.instance.uiDataAssetsPath + uxmlPathName;
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiAssetPath);
                if (visualTree == null)
                {
                    UnityEngine.Debug.LogError(string.Format("ui asset file lost! {0}", uiAssetPath));
                    return false;
                }

                var childCount = parent.childCount;
                visualTree.CloneTree(parent);
                _mainElement = parent[childCount];
                // get content element
                contentElement = _mainElement.Q<VisualElement>("Content");

                // add subviews
                if (_widgets.Count > 0)
                {
                    foreach (var x in _widgets)
                    {
                        var subViewElement = x.BuildUIDOM();
                        if (subViewElement != null)
                        {
                            contentElement.Add(subViewElement);
                        }
                    }
                }
                //
                return true;
            }

            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            protected virtual bool BindUIActions()
            {
                if (_widgets.Count > 0)
                {
                    foreach (var x in _widgets)
                    {
                        if(x.mainElement != null)
                        {
                            x.BindUIActions();
                        }
                    }
                }
                return true;
            }
            
            protected virtual bool BindUIActions(params object[] initParams)
            {
                if (_widgets.Count > 0)
                {
                    foreach (var x in _widgets)
                    {
                        if(x.mainElement != null)
                        {
                            x.BindUIActions();
                        }
                    }
                }
                return true;
            }
            
            private void OnPlayModeStateChanged(PlayModeStateChange e)
            {
                if (e == PlayModeStateChange.ExitingEditMode)
                    OnExitEditMode();
            }

#endregion

            public virtual void OnEnable()
            {
            }

            /**
             * Notification that the panel will be destroyed. If derived class override the method, MUST invok it.
             */ 
            public virtual void OnDestroy()
            {
                // assure detach from dom
                DetachFromDOM();

                foreach(var x in _widgets)
                {
                    x.OnDestroy();
                }
                _widgets.Clear();

                //
                if(_mainElement != null)
                {
                    _mainElement.Unbind();
                    _mainElement = null;
                }
                contentElement = null;
                
            }
            public virtual void OnUpdate()
            {
            }
            public virtual void OnRemove()
            {

            }
            public virtual void OnShow()
            {
                
            }
            public virtual void OnHide()
            {
                
            }
        }
    }
}

#endif