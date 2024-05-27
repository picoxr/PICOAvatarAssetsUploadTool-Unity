#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        internal class SkeletonMappingSettingHeadWidget : SkeletonMappingSettingWidget
        {
            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonMappingSettingHeadWidget.uxml";}
            public override JointType rootJointType { get => JointType.Neck; }
            public override SkeletonPanel.JointGroup jointGroup {get => SkeletonPanel.JointGroup.Head;}
#region Public Methods
            public override void AutoMappingJoints()
            {
                base.AutoMappingJoints(); 
                //TODO
            }

             /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();
               
                // rotation limit settings
                var group = mainElement.Q<Foldout>("Foldout_Head");
                if(group != null)
                {
                    foreach(JointType type in _headJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    group.value = true;
                    _groupJointTypeDic["Head"] = _headJointList;
                }

                _foldoutExtra = AddCustomSettingFoldout();
                return root;
            }

            public override List<JointType> GetJointTypesList()
            {
                return _headJointList;
            }

#endregion


#region Private Fields

            private List<JointType> _headJointList = new List<JointType>
            {
                JointType.Neck, JointType.Head
            };

#endregion
        }
    }
}
#endif