#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pico.Avatar;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class ConfigureBaseAnimationSetPanel : ConfigureAnimationSetPanel
        {
            public static ConfigureBaseAnimationSetPanel instance {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<ConfigureBaseAnimationSetPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/ConfigureBaseAnimationSetPanel.asset");
                    }
            
                    return _instance;
                }
            }
            
            // display name of the panel
            public override string displayName { get => "ConfigureBaseAnimationSet"; }
            public override string panelName { get => "ConfigureBaseAnimationSet"; }
            protected override AnimationSetType _animationSetType
            {
                get => AnimationSetType.Base;
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
                var labels = mainElement.Query<Label>(_ClipNameLabelText).ToList();

                foreach (var pair in clips)
                {
                    var label = labels.Find(label => label.text == pair.Key);
                    if (label != null)
                    {
                        var objField = label.parent.Q<ObjectField>();
                        objField.value = pair.Value;
                        continue;
                    }
                    
                    Debug.LogError("Unable to find the corresponding label and objectField " + pair.Key);
                }
            }

            public override void OnUpdate()
            {
                base.OnUpdate();

                UpdateRetargetToggle();
            }
#endregion

#region Protected/Private Mehtods

            // Start is called before the first frame update
            protected override bool BuildUIDOM(VisualElement parent)
            {
                var result = base.BuildUIDOM(parent);
                
                // hide addAnimationButton
                var addAnimationBtn = mainElement.Q<Button>("AddAnimation");
                if (addAnimationBtn != null)
                {
                    addAnimationBtn.style.display = DisplayStyle.None;
                }

                _renderItems.Clear();
                //
                AddRetargetCheckAllToggle();
                AddAnimationClips(_requiredAnimationClips, true);
                AddAnimationClips(_optionalAnimationClips, false);

                // set the first item margin top to be 12
                contentElement.Q<ObjectField>().parent.parent.style.marginTop = 12;

                return result;
            }

            private void AddRetargetCheckAllToggle()
            {
                var checkAllWidget = new AnimationRetargetCheckAllWidget();
                AddWidget(checkAllWidget);
                _retargetCheckAllToggle = checkAllWidget.mainElement.Q<Toggle>(_RetargetCheckAllToggleName);
                _retargetCheckAllToggle.RegisterValueChangedCallback(OnRetargetCheckAllToggleChanged);
                _retargetCheckAllToggle.parent.style.marginTop = 24;
            }

            private void AddAnimationClips(string[] animationClips, bool required)
            {
                for (int i = 0; i < animationClips.Length; ++i)
                {
                    var widget = new AnimationBaseSettingWidget();
                    AddWidget(widget);
                    widget.mainElement.Q<ObjectField>().parent.parent.style.marginRight = 32;
                    //
                    AnimationRenderItem renderItem = new AnimationRenderItem();
                    renderItem.animationNameLabel = widget.mainElement.Q<Label>(_ClipNameLabelText);
                    renderItem.animationNameLabel.text = animationClips[i];
                    renderItem.isRequired = required;
                    
                    var objectField = widget.mainElement.Q<ObjectField>();
                    renderItem.animationClipLabel = GetObjectFieldLabel(objectField);
                    UIUtils.AddVisualElementHoverMask(objectField, objectField);
                    objectField.RegisterValueChangedCallback((eve) =>
                    {
                        renderItem.retargetToggle.SetValueWithoutNotify(false);
                        renderItem.ResetStatus();
                        ResetWarningCheck();
                    });

                    renderItem.retargetToggle = widget.mainElement.Q<Toggle>(_RetargetToggleName);
                    renderItem.retargetToggle.RegisterValueChangedCallback((eve) =>
                    {
                        objectField.SetValueWithoutNotify(null);
                        renderItem.ResetStatus();
                        ResetWarningCheck();
                    });
                    //
                    _renderItems.Add(renderItem);
                }
            }

            //----------------------------------------------------------------------------------------------------------
            /// Animation Retarget
            
            private void OnRetargetCheckAllToggleChanged(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                {
                    _renderItems.ForEach(item => item.retargetToggle.value = true);
                }
                else
                {
                    _renderItems.ForEach(item => item.retargetToggle.value = false);
                }
            }

            private void UpdateRetargetToggle()
            {
                bool toCheckAll = true;
                bool toUncheckAll = true;
                foreach (var renderItem in _renderItems)
                {
                    Label label = renderItem.animationClipLabel;
                    if (renderItem.retargetToggle.value)
                    {
                        toUncheckAll = false;
                        label.text = _UseCustomizedClipText;
                    }
                    else
                    {
                        toCheckAll = false;
                        if (label.text == _UseCustomizedClipText)
                        {
                            label.text = renderItem.isRequired ? _RequiredText : _OptionalText;
                        }
                    }
                }

                if (toCheckAll)
                {
                    _retargetCheckAllToggle.SetValueWithoutNotify(true);
                }
                else if (toUncheckAll)
                {
                    _retargetCheckAllToggle.SetValueWithoutNotify(false);
                }
            }


            //----------------------------------------------------------------------------------------------------------


#endregion

#region Private Fields
            private static ConfigureBaseAnimationSetPanel _instance;
            private Toggle _retargetCheckAllToggle;
            private const string _RetargetCheckAllToggleName = "RetargetCheckAllToggle";
#endregion
        }
    }
}
#endif

