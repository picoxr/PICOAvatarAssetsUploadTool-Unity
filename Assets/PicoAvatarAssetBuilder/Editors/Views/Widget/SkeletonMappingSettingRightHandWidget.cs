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
    namespace AvatarAssetBuilder
    {
        internal class SkeletonMappingSettingRightHandWidget : SkeletonMappingSettingWidget
        {        
            // gets uxml path name. relative to AssetBuilderConfig.uiDataAssetsPath. default root: "Assets/PicoAvatarAssetBuilder/Editors/Views/"
            public override string uxmlPathName { get => "UxmlWidget/SkeletonMappingSettingRightHandWidget.uxml";}
            public override JointType rootJointType { get => JointType.RightHandWrist; }
            public override SkeletonPanel.JointGroup jointGroup {get => SkeletonPanel.JointGroup.RightHand;}
#region Public Methods

            /**
             * @brief Build ui element of the sub view.
             * @return root uxml element of the sub view.
             */
            public override VisualElement BuildUIDOM()
            {
                var root = base.BuildUIDOM();
               
                // rotation limit settings
                var group = mainElement.Q<Foldout>("Foldout_RightHandThumb");
                if(group != null)
                {
                    foreach(JointType type in _rightHandThumbJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    group.value = true;
                    _groupJointTypeDic["RightHandThumb"] = _rightHandThumbJointList;
                }
                _fingersList.Add(_rightHandThumbJointList);

                group = mainElement.Q<Foldout>("Foldout_RightHandIndex");
                if(group != null)
                {
                    foreach(JointType type in _rightHandIndexJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["RightHandIndex"] = _rightHandIndexJointList;
                }
                _fingersList.Add(_rightHandIndexJointList);

                group = mainElement.Q<Foldout>("Foldout_RightHandMiddle");
                if(group != null)
                {
                    foreach(JointType type in _rightHandMiddleJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["RightHandMiddle"] = _rightHandMiddleJointList;
                }
                _fingersList.Add(_rightHandMiddleJointList);

                group = mainElement.Q<Foldout>("Foldout_RightHandRing");
                if(group != null)
                {
                    foreach(JointType type in _rightHandRingJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["RightHandRing"] = _rightHandRingJointList;
                }
                _fingersList.Add(_rightHandRingJointList);

                group = mainElement.Q<Foldout>("Foldout_RightHandPinky");
                if(group != null)
                {
                    foreach(JointType type in _rightHandPinkyJointList)
                    {
                        AddMappingSettingItem(ref group, type);
                    }
                    _groupJointTypeDic["RightHandPinky"] = _rightHandPinkyJointList;
                }
                _fingersList.Add(_rightHandPinkyJointList);

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
                return _rightHandThumbJointList.Concat(_rightHandIndexJointList).Concat(_rightHandMiddleJointList).Concat(_rightHandRingJointList).Concat(_rightHandPinkyJointList).ToList<JointType>();
            }

#endregion


#region Private Fields
            private List<JointType> _rightHandThumbJointList = new List<JointType>
            {
            JointType.RightHandThumbTrapezium, JointType.RightHandThumbMetacarpal, JointType.RightHandThumbProximal, JointType.RightHandThumbDistal, JointType.RightHandThumbTip
            };

            private List<JointType> _rightHandIndexJointList = new List<JointType>
            {
            JointType.RightHandIndexMetacarpal, JointType.RightHandIndexProximal,  JointType.RightHandIndexIntermediate, JointType.RightHandIndexDistal, JointType.RightHandIndexTip
            };

            private List<JointType> _rightHandMiddleJointList = new List<JointType>
            {
            JointType.RightHandMiddleMetacarpal, JointType.RightHandMiddleProximal,  JointType.RightHandMiddleIntermediate, JointType.RightHandMiddleDistal, JointType.RightHandMiddleTip
            };

            private List<JointType> _rightHandRingJointList = new List<JointType>
            {
            JointType.RightHandRingMetacarpal, JointType.RightHandRingProximal,  JointType.RightHandRingIntermediate, JointType.RightHandRingDistal, JointType.RightHandRingTip
            };

            private List<JointType> _rightHandPinkyJointList = new List<JointType>
            {
            JointType.RightHandPinkyMetacarpal, JointType.RightHandPinkyProximal,  JointType.RightHandPinkyIntermediate, JointType.RightHandPinkyDistal, JointType.RightHandPinkyTip
            };

            private List<List<JointType>> _fingersList = new List<List<JointType>>();

#endregion
        }
    }
}
#endif