#if UNITY_EDITOR
using Pico.Avatar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        [Serializable]
        public class PaabAnimationImportSetting : PaabAssetImportSetting
        {
            // asset import setting type.
            public override AssetImportSettingType settingType { get => AssetImportSettingType.Animation; }

            // use list to preserve its original order
            [SerializeField]
            public List<KeyValuePair<string, AnimationClip>> animationClips = new List<KeyValuePair<string, AnimationClip>>();
            
            // used to get the custom animation sets online
            public string characterId;
            
            public bool isBasicAnimationSet;
            
            public static string walking = "walking";
            public static string walkingFwd = "walkingForward";
            public static string fist = "fist";
            public static string lHandFist = "lHandFist";
            public static string rHandFist = "rHandFist";
            

            /**
             * @brief Build from json object. Derived class SHOULD override the method.
             */
            public override void FromJsonObject(Dictionary<string, object> jsonObject)
            {
                object tmpObj = null;
                jsonObject.TryGetValue("config", out tmpObj);
                var tmpObjDict = tmpObj as Dictionary<string, object>;
                if (tmpObjDict == null)
                {
                    Debug.LogError("Unable to find the key 'config' in the json");
                    return;
                }
                //
                object obj = null;
                tmpObjDict.TryGetValue("animations", out obj);
                var objDict = obj as Dictionary<string, string>;
                if (objDict == null)
                {
                    Debug.LogError("Unable to find the key 'animations' in the json");
                    return;
                }

                // remove walking/lHandFist/rHandFist in dictionary
                objDict.Remove(walking);
                objDict.Remove(lHandFist);
                objDict.Remove(rHandFist);

                animationClips.Clear();   
                foreach (var item in objDict)
                {
                    animationClips.Add(new KeyValuePair<string, AnimationClip>(item.Key, null));
                }
            }
            
            public override void ToJsonObject(Dictionary<string, object> jsonObject)
            {
                var config = new Dictionary<string,object>();
                var animations = new Dictionary<string, string>();
                for (int i = 0; i < animationClips.Count; i++)
                {
                    animations.Add(animationClips[i].Key, "anim/" + animationClips[i].Key + ".animaz");
                }
                config["animations"] = animations;
                jsonObject["config"] = config;
            }

            static public List<KeyValuePair<string, AnimationClip>> PostProcessAnimationClips(in List<KeyValuePair<string, AnimationClip>> clips)
            {
                var newAnimationClips = new List<KeyValuePair<string, AnimationClip>>(clips);
                // add walking with clip same as walkingForward
                var walkingFwdItem = newAnimationClips.Find(clip => clip.Key == walkingFwd);
                if (!string.IsNullOrEmpty(walkingFwdItem.Key))
                {
                    newAnimationClips.Add(new KeyValuePair<string, AnimationClip>(walking, walkingFwdItem.Value));
                }
                // add lHandFist and rHandFist with clip same as fist
                var fistItem = newAnimationClips.Find(clip => clip.Key == fist);
                if (!string.IsNullOrEmpty(fistItem.Key))
                {
                    newAnimationClips.Add(new KeyValuePair<string, AnimationClip>(lHandFist, fistItem.Value));
                    newAnimationClips.Add(new KeyValuePair<string, AnimationClip>(rHandFist, fistItem.Value));
                }
                return newAnimationClips;
            }

            static public void AddExtraToRetargetClips(List<string> toRetargetClips)
            {
                // add walking to retarget
                if (toRetargetClips.Contains(walkingFwd))
                {
                    toRetargetClips.Add(walking);
                }

                // add lHandFist and rHandFist to retarget
                if (toRetargetClips.Contains(fist))
                {
                    toRetargetClips.Add(lHandFist);
                    toRetargetClips.Add(rHandFist);
                }
            }
        }
    }
}
#endif