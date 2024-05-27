#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Pico.Avatar;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        [Serializable]
        public class PaabSkeletonImportSetting : PaabAssetImportSetting
        {
            [Serializable]
            public class PaabAvatarJoint
            {
                //name of joint
                public string jointName;
                //transform in scene.
                public Transform JointTransform = null;
                //type of avatar joint mapped to standard skeleton
                public JointType jointType = JointType.Invalid;
                //min rotation angle relative to TPose
                public Vector3 minAngle = new Vector3(-180, -180, -180);
                //max rotation angle relative to TPose
                public Vector3 maxAngle = new Vector3(180, 180, 180);
                //joint twist  axis
                public Vector3 axis = Vector3.left;
                //joint up axis (bend normal)
                public Vector3 up = Vector3.up;
                //twist weight, only apply to some certain twist joints
                public float twistWeight = 0.0f;
                //joint default local orientation
                public Quaternion defaultLocalOrientation = Quaternion.identity;
                //enable status of joint pos, rot and scale
                public byte jointTransformProperty = 0;
            }

            public class PaabAvatarCustomJoint
            {
                //name of joint
                public string jointName;
                //transform in scene.
                public Transform jointTransform = null;
                //enable status of joint pos, rot and scale
                public byte jointMaskBits = 0;
                //group of the joint
                public SkeletonPanel.JointGroup jointGroup = SkeletonPanel.JointGroup.Invalid;
            }
            // asset import setting type.
            public override AssetImportSettingType settingType { get => AssetImportSettingType.Skeleton; }

            [Tooltip("tag for style name")]
            [SerializeField]
            public string styleTagName = "2.0";

            [SerializeField]
            public Dictionary<JointType, PaabAvatarJoint> avatarJoints = new Dictionary<JointType, PaabAvatarJoint>();
            public Dictionary<string, PaabAvatarCustomJoint> avatarCustomJoints = new Dictionary<string, PaabAvatarCustomJoint>();

            //max arm stretch ratio
            [SerializeField]
            public float armStretch = 0.0f;

            //max leg stretch ratio
            [SerializeField]
            public float legStretch = 0.0f;

            //min hips height
            [SerializeField]
            public float minHipsHeight = 0.25f;

            public string skeletonName = "";

            public string skeletonRootName = "";

            //public GameObject skeletonRootGO = null;

            private static Vector3 GetVector3(Dictionary<string, object> obj, string key)
            {
                var value = obj.GetValueOrDefault(key, null);
                if (value != null)
                {
                    var fs = value as float[];
                    return new Vector3(fs[0], fs[1], fs[2]);
                }
                return Vector3.zero;
            }

            private static Quaternion GetQuaternion(Dictionary<string, object> obj, string key)
            {
                var value = obj.GetValueOrDefault(key, null);
                if (value != null)
                {
                    var fs = value as float[];
                    return new Quaternion(fs[0], fs[1], fs[2], fs[3]);
                }
                return Quaternion.identity;
            }

            /**
             * @brief Build from json object. Derived class SHOULD override the method.
             */
            public override void FromJsonObject(Dictionary<string, object> jsonObject)
            {
                var obj = jsonObject.GetValueOrDefault("skeletonSetting", null);
                if (obj != null)
                {
                    var skeletonSetting = obj as Dictionary<string, object>;
                    armStretch = (float) skeletonSetting.GetValueOrDefault("armStretch", 0);
                    legStretch = (float) skeletonSetting.GetValueOrDefault("legStretch", 0);
                    minHipsHeight = (float) skeletonSetting.GetValueOrDefault("minHipsHeight", 0);

                    obj = skeletonSetting.GetValueOrDefault("joints", null);
                    if (obj != null)
                    {
                        var joints = obj as Dictionary<string, object>;
                        avatarJoints = new Dictionary<JointType, PaabAvatarJoint>();
                        foreach (var i in joints)
                        {
                            var joint = new PaabAvatarJoint();
                            var jointDic = i.Value as Dictionary<string, object>;
                            joint.jointName = (string) jointDic.GetValueOrDefault("jointName", "");
                            joint.jointType = (JointType) (int) jointDic.GetValueOrDefault("jointType", 0);
                            joint.minAngle = GetVector3(jointDic, "minAngle");
                            joint.maxAngle = GetVector3(jointDic, "maxAngle");
                            joint.axis = GetVector3(jointDic, "axis");
                            joint.up = GetVector3(jointDic, "up");
                            joint.twistWeight = (float) jointDic.GetValueOrDefault("twistWeight", 0.0f);
                            joint.defaultLocalOrientation = GetQuaternion(jointDic, "defaultLocalOrientation");
                            joint.jointTransformProperty = (byte) jointDic.GetValueOrDefault("jointMaskBits", 0);
                        }
                    }

                    obj = skeletonSetting.GetValueOrDefault("customJoints", null);
                    if (obj != null)
                    {
                        var customJoints = obj as Dictionary<string, object>;
                        avatarCustomJoints = new Dictionary<string, PaabAvatarCustomJoint>();
                        foreach (var i in customJoints)
                        {
                            var joint = new PaabAvatarCustomJoint();
                            var jointDic = i.Value as Dictionary<string, object>;
                            joint.jointName = (string) jointDic.GetValueOrDefault("jointName", "");
                            joint.jointMaskBits = (byte) jointDic.GetValueOrDefault("jointMaskBits", 0);
                        }
                    }
                }
            }

            /**
             * @brief Serialize to json object. Derived class SHOULD override the method.
             */
            public override void ToJsonObject(Dictionary<string, object> jsonObject)
            {
                var joints = new Dictionary<string, object>();
                foreach (var i in avatarJoints)
                {
                    var joint = i.Value;
                    var jointDic = new Dictionary<string, object>();
                    jointDic["jointName"] = string.IsNullOrEmpty(joint.jointName) ? "" : joint.jointName;
                    jointDic["jointType"] = (int) joint.jointType;
                    jointDic["minAngle"] = new float[] { joint.minAngle.x, joint.minAngle.y, joint.minAngle.z };
                    jointDic["maxAngle"] = new float[] { joint.maxAngle.x, joint.maxAngle.y, joint.maxAngle.z };
                    jointDic["axis"] = new float[] { joint.axis.x, joint.axis.y, joint.axis.z };
                    jointDic["up"] = new float[] { joint.up.x, joint.up.y, joint.up.z };
                    jointDic["twistWeight"] = joint.twistWeight;
                    jointDic["defaultLocalOrientation"] = new float[] { joint.defaultLocalOrientation.x, joint.defaultLocalOrientation.y, joint.defaultLocalOrientation.z, joint.defaultLocalOrientation.w };
                    jointDic["jointMaskBits"] = joint.jointTransformProperty;
                    joints[i.Key.ToString()] = jointDic;
                }

                var customJoints = new Dictionary<string, object>();
                foreach (var i in avatarCustomJoints)
                {
                    var joint = i.Value;
                    var jointDic = new Dictionary<string, object>();
                    jointDic["jointName"] = string.IsNullOrEmpty(joint.jointName) ? "" : joint.jointName;
                    jointDic["jointMaskBits"] = joint.jointMaskBits;
                    customJoints[i.Key.ToString()] = jointDic;
                }

                var skeletonSetting = new Dictionary<string, object>();
                skeletonSetting["joints"] = joints;
                skeletonSetting["customJoints"] = customJoints;
                skeletonSetting["armStretch"] = armStretch;
                skeletonSetting["legStretch"] = legStretch;
                skeletonSetting["minHipsHeight"] = minHipsHeight;

                jsonObject["skeletonSetting"] = skeletonSetting;

                var config = new Dictionary<string,object>();
                config["assetPath"] = "prefab/" + skeletonName + "_skeleton.prefab";
                config["skeletonMapper"] = "skeleton/skeleton.map";
                config["avatarMask"] = "mask/fullBone.mask";
                config["assetType"] = "Skeleton";
                config["skeletonVersion"] = styleTagName;
                jsonObject["config"] = config;
            }
        }
    }
}
#endif