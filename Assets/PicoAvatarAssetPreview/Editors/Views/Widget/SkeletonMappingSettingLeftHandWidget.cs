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
        internal class SkeletonMappingSettingLeftHandWidget : SkeletonMappingSettingWidget
        {
            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetPreview/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonMappingSettingLeftHandWidget.uxml";}
            public override JointType rootJointType { get => JointType.LeftHandWrist; }
            public override SkeletonPanel.JointGroup jointGroup {get => SkeletonPanel.JointGroup.LeftHand;}
#region Public Methods

            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();
               
                // rotation limit settings
                var group = mainElement.Q<Foldout>("Foldout_LeftHandThumb");
                if(group != null)
                {
                    foreach(JointType type in _leftHandThumbJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    group.value = true;
                    _groupJointTypeDic["LeftHandThumb"] = _leftHandThumbJointList;
                }
                _fingersList.Add(_leftHandThumbJointList);

                group = mainElement.Q<Foldout>("Foldout_LeftHandIndex");
                if(group != null)
                {
                    foreach(JointType type in _leftHandIndexJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["LeftHandIndex"] = _leftHandIndexJointList;
                }
                _fingersList.Add(_leftHandIndexJointList);

                group = mainElement.Q<Foldout>("Foldout_LeftHandMiddle");
                if(group != null)
                {
                    foreach(JointType type in _leftHandMiddleJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["LeftHandMiddle"] = _leftHandMiddleJointList;
                }
                _fingersList.Add(_leftHandMiddleJointList);

                group = mainElement.Q<Foldout>("Foldout_LeftHandRing");
                if(group != null)
                {
                    foreach(JointType type in _leftHandRingJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["LeftHandRing"] = _leftHandRingJointList;
                }
                _fingersList.Add(_leftHandRingJointList);

                group = mainElement.Q<Foldout>("Foldout_LeftHandPinky");
                if(group != null)
                {
                    foreach(JointType type in _leftHandPinkyJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["LeftHandPinky"] = _leftHandPinkyJointList;
                }
                _fingersList.Add(_leftHandPinkyJointList);
                
                _foldoutExtra = AddCustomSettingFoldout();
                return root;
            }

            public override bool BindUIActions()
            {
                if(!base.BindUIActions())
                {
                    return false;
                }
                var jointList = GetJointTypesList();
                foreach(JointType jointType in jointList)
                {
                    if(!_jointObjects.ContainsKey(jointType))
                    {
                        continue;
                    }
                    var jointObject = _jointObjects[jointType];
                    if (jointObject != null)
                    {
                        jointObject.RegisterValueChangedCallback((eve) =>
                        {
                            AutoMappingChildren(jointType, (Transform)eve.newValue);
                        });
                    }
                }
                return true;
            }
            
            public override List<JointType> GetJointTypesList()
            {
                return _leftHandThumbJointList.Concat(_leftHandIndexJointList).Concat(_leftHandMiddleJointList).Concat(_leftHandRingJointList).Concat(_leftHandPinkyJointList).ToList<JointType>();
            }

#endregion


#region Private Fields
            private List<JointType> _leftHandThumbJointList = new List<JointType>
            {
            JointType.LeftHandThumbTrapezium, JointType.LeftHandThumbMetacarpal, JointType.LeftHandThumbProximal, JointType.LeftHandThumbDistal, JointType.LeftHandThumbTip
            };

            private List<JointType> _leftHandIndexJointList = new List<JointType>
            {
            JointType.LeftHandIndexMetacarpal, JointType.LeftHandIndexProximal,  JointType.LeftHandIndexIntermediate, JointType.LeftHandIndexDistal, JointType.LeftHandIndexTip
            };

            private List<JointType> _leftHandMiddleJointList = new List<JointType>
            {
            JointType.LeftHandMiddleMetacarpal, JointType.LeftHandMiddleProximal,  JointType.LeftHandMiddleIntermediate, JointType.LeftHandMiddleDistal, JointType.LeftHandMiddleTip
            };

            private List<JointType> _leftHandRingJointList = new List<JointType>
            {
            JointType.LeftHandRingMetacarpal, JointType.LeftHandRingProximal,  JointType.LeftHandRingIntermediate, JointType.LeftHandRingDistal, JointType.LeftHandRingTip
            };

            private List<JointType> _leftHandPinkyJointList = new List<JointType>
            {
            JointType.LeftHandPinkyMetacarpal, JointType.LeftHandPinkyProximal,  JointType.LeftHandPinkyIntermediate, JointType.LeftHandPinkyDistal, JointType.LeftHandPinkyTip
            };

            private List<List<JointType>> _fingersList = new List<List<JointType>>();
#endregion
        }
    }
}
#endif