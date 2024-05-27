#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using Pico.Avatar;
using Pico.AvatarAssetBuilder.Protocol;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class ConfigureCustomAnimationSetPanel : ConfigureAnimationSetPanel
        {
            public static ConfigureCustomAnimationSetPanel instance {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<ConfigureCustomAnimationSetPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/ConfigureCustomAnimationSetPanel.asset");
                    }
            
                    return _instance;
                }
            }
            
            // display name of the panel
            public override string displayName { get => "ConfigureCustomAnimationSet"; }
            public override string panelName { get => "ConfigureCustomAnimationSet"; }
            protected override AnimationSetType _animationSetType
            {
                get => AnimationSetType.Custom;
            }
            
#region Public Methods
            /**
             * Notification that the panel will be destroyed. If derived class override the method, MUST invoke it.
             */
            public override void OnDestroy()
            {
                base.OnDestroy();
                //
                if(_instance == this)
                {
                    _instance = null;
                }
            }

            public override void BindOrUpdateFromData(PaabAssetImportSettings importConfig)
            {
                base.BindOrUpdateFromData(importConfig);
                
                var clips = _animationSetting.animationClips;
                
                // clear all original children widgets
                _renderItems.Clear();
                contentElement.Query("AnimationCustomSettingWidget").ForEach(ve => ve.parent.parent.Remove(ve.parent));

                foreach (var item in clips)
                {
                    AddCustomAnimationInternal(item.Key, item.Value);
                }
                
                AddDefaultAnimationItemIfEmpty();
                HideFirstMinusButton();
            }
#endregion
            
#region Protected/Private Mehtods

            // Start is called before the first frame update
            protected override bool BuildUIDOM(VisualElement parent)
            {
                var result = base.BuildUIDOM(parent);
                
                _addAnimationButton = mainElement.Q<Button>("AddAnimation");
                _addAnimationButton.clicked += AddCustomAnimation;
                UIUtils.AddVisualElementHoverMask(_addAnimationButton, _addAnimationButton);

                AddDefaultAnimationItemIfEmpty();
                HideFirstMinusButton();

                return result;
            }

            /**
             * @brief Bind ui events. Derived class SHOULD override the method.
             * Invoked from EditorWindowBase.ShowMe after build the ui elements.
             */
            protected override bool BindUIActions()
            {
                if (!base.BindUIActions())
                {
                    return false;
                }
                
                return true;
            }

            private void AddDefaultAnimationItemIfEmpty()
            {
                if (mainElement.Q<Button>("MinusButton") == null)
                {
                    AddCustomAnimationInternal();
                }
            }
            
            /**
             * @brief Hide first MinusButton 
             */
            private void HideFirstMinusButton()
            {
                var firstBtn = mainElement.Q<Button>("MinusButton");
                firstBtn.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                // first item margin
                firstBtn.parent.style.marginTop = 24;
            }

            private void AddCustomAnimation()
            {
                if (_renderItems.Count >= _MaxAnimationNumInSet)
                {
                    Debug.Log("The number of animations in an animation set has reached the maximum number " + _MaxAnimationNumInSet.ToString());
                    return;
                }
                
                AddCustomAnimationInternal("", null);
                ResetWarningCheck();

                if (_renderItems.Count == _MaxAnimationNumInSet)
                {
                    DisableAddAnimation();
                }
            }
            
            private void AddCustomAnimationInternal(string animationName = "", AnimationClip clip = null)
            {
                var widget = new AnimationCustomSettingWidget();
                AddWidget(widget);
                widget.mainElement.Q<ObjectField>().parent.style.marginRight = 12;
                widget.mainElement.Q<ObjectField>().parent.parent.style.marginRight = 32;
                widget.mainElement.Q<TextField>().value = animationName;
                widget.mainElement.Q<ObjectField>().value = clip;

                AnimationRenderItem renderItem = new AnimationRenderItem();
                renderItem.animationNameTextField = widget.mainElement.Q<TextField>();
                // add mask to its parent(group box)
                UIUtils.AddVisualElementHoverMask(renderItem.animationNameTextField.parent, renderItem.animationNameTextField.parent);
                renderItem.BuildTextFieldHolder();
                renderItem.animationNameTextField.RegisterValueChangedCallback((evt) =>
                {
                    renderItem.ResetStatus();
                    ResetWarningCheck();
                });

                var objectField = widget.mainElement.Q<ObjectField>();
                UIUtils.AddVisualElementHoverMask(objectField, objectField);
                renderItem.animationClipLabel = GetObjectFieldLabel(objectField);
                renderItem.isRequired = true;
                objectField.RegisterValueChangedCallback((eve) =>
                {
                    renderItem.ResetStatus();
                    ResetWarningCheck();
                });
                _renderItems.Add(renderItem);
                
                // bind click event on the minus button
                var minusBtn = widget.mainElement.Q<Button>("MinusButton");
                UIUtils.AddVisualElementHoverMask(minusBtn, minusBtn);
                minusBtn.clicked += () =>
                {
                    _renderItems.RemoveAll(item => item.animationClipLabel == GetObjectFieldLabel(widget.mainElement.Q<ObjectField>()));
                    widget.DetachFromDOM();
                    ResetWarningCheck();
                    EnableAddAnimation();
                };
            }

            private void DisableAddAnimation()
            {
                _addAnimationButton.SetEnabled(false);
            }

            private void EnableAddAnimation()
            {
                _addAnimationButton.SetEnabled(true);
            }
#endregion
            
                    
#region Private Fields
            private static ConfigureCustomAnimationSetPanel _instance;
            private const int _MaxAnimationNumInSet = 50;
            private Button _addAnimationButton;
#endregion
        }
    }
}

#endif