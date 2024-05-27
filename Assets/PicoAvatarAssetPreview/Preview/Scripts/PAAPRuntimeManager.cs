#if UNITY_EDITOR
using System;
using System.Collections;
using Pico.Avatar;
using Pico.AvatarAssetBuilder;
using UIToolkitDemo;
using UnityEngine;
using UnityEditor;

namespace AssemblyCSharp.Assets.AmzAvatar.TestTools
{
    [CustomEditor(typeof(PAAPRuntimeManager))]
    public class PAAPRuntimeManagerEditor : Editor
    {
        PAAPRuntimeManager mTarget;
        
        private void OnEnable()
        {
            mTarget = target as PAAPRuntimeManager;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying && mTarget)
            {
                if (GUILayout.Button("LoadAvatar From Spec"))
                {
                    mTarget.avatarId = "default";
                    mTarget.LoadAvatarFromSpec(mTarget.avatarSpec);
                }
                // if (GUILayout.Button("Change AvatarLod"))
                //     mTarget.ChangeAvatarLod();
            }
        }
    }
    
    public class PAAPRuntimeManager : MonoBehaviour
    {
        [HideInInspector]
        public AvatarLodLevel currentLodLevel = AvatarLodLevel.Lod0;
        public PAAPCreateAvatar createAvatar;
        public PicoAvatarApp avatarApp;
        public MainMenuRuntimeUIManager menuManager;
        public Camera mainCamera;
        [SerializeField]
        public String avatarSpec = "{\"info\":{\"sex\":\"male\",\"status\":\"Online\",\"tag_list\":[\"Common\"],\"continent\":\"EU\",\"background\":{\"image\":\"https://dfsedffe.png\",\"end_color\":[133,182,255],\"start_color\":[148,111,253]},\"avatar_type\":\"preset\"},\"avatar\":{\"body\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-Bone\",\"floatIdParams\":[]},\"head\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-BS\",\"floatIdParams\":[]},\"skin\":{\"color\":\"\",\"white\":0,\"softening\":0},\"assetPins\":[],\"nextWearTimeStamp\":25},\"avatarStyle\":\"PicoAvatar3\"}";

        
        [Header("　")]
        // for component or customAnimation.
        [HideInInspector] public string assetName;
        [HideInInspector] public string assetPath;
        [HideInInspector] public string assetConfig;
        [HideInInspector] public Pico.AvatarAssetBuilder.PaabBasicInfoImportSetting assetBasicInfoSetting = null;
        [HideInInspector] public Pico.AvatarAssetBuilder.PaabComponentImportSetting assetComponentSetting = null;
        [HideInInspector] public Pico.AvatarAssetBuilder.PaabAnimationImportSetting assetAnimationSetting = null;

        // for avatar.
        [SerializeField]
        [HideInInspector] public String avatarName = "";
        [HideInInspector] public String avatarId = "default";
        [HideInInspector] public bool fromPresetPanel = false;

        [HideInInspector] public Pico.AvatarAssetBuilder.Protocol.CharacterInfo characterinfo;

        private AvatarAnimationLayer _previewAnimationLayer = null;

        private void Start()
        {
            if (createAvatar != null)
            {
                createAvatar.jsonSpec = avatarSpec;
                if (avatarSpec != "")
                    createAvatar.gameObject.SetActive(true);
            }

            if (assetComponentSetting.componentType.ToString() != "")
            {
                assetPath = $"{CharacterUtil.LocalAvatarCachePath}/{avatarName}/{CharacterUtil.ComponentFolderName}/{assetComponentSetting.componentType.ToString()}/{assetName}";
            }

            if (assetAnimationSetting.characterId != "")
            {
                assetPath = $"{CharacterUtil.LocalAvatarCachePath}/{avatarName}/{CharacterUtil.AnimationSetFolderName}/{CharacterUtil.CustomAnimationSetFolderName}/{assetName}";
            }
        }

        public void ReLoadAvatarFromSpec()
        {
            this.avatarSpec = createAvatar.avatar.GetAvatarSpecification();
            if(!createAvatar.avatar || !createAvatar.avatar.isAnyEntityReady)
            {
                return;
            }
            if (createAvatar.avatar != null && createAvatar.avatar.isAnyEntityReady)
            {
                PicoAvatarManager.instance.UnloadAvatar(createAvatar.avatar);
            }
            createAvatar.forceLodLevel = currentLodLevel;
            LoadAvatarAsync(createAvatar, this.avatarSpec);
        }

        public void LoadAvatarFromSpec(string spec)
        {
            if (spec == "") return;
            if (createAvatar.jsonSpec == spec) return;
            createAvatar.forceLodLevel = currentLodLevel;
            if (createAvatar.gameObject.activeSelf == false)
            {
                avatarSpec = spec;
                createAvatar.jsonSpec = spec;
                this.avatarSpec = spec;
                createAvatar.avatarId = this.avatarId;
                createAvatar.gameObject.SetActive(true);   
                menuManager.LodVisualElement.SetActive(true);
            }
            else if (createAvatar != null)
            {
                createAvatar.gameObject.SetActive(true);
                if (createAvatar.avatarId == this.avatarId && this.avatarId != "default" && this.fromPresetPanel == false) 
                    return;
                if (createAvatar.avatar != null && createAvatar.avatar.isAnyEntityReady)
                {
                    createAvatar.avatarId = this.avatarId;
                    createAvatar.jsonSpec = spec;
                    this.avatarSpec = spec;
                    PicoAvatarManager.instance.UnloadAvatar(createAvatar.avatar);
                    LoadAvatarAsync(createAvatar, spec);
                }
            }
        }

        public void ChangeAvatarLod()
        {
            if (createAvatar != null)
            {
                createAvatar.ChangeMeshLOD((int)currentLodLevel);
            }
        }

        private void LoadAvatarAsync(PAAPCreateAvatar createAvatar, string avatarSpecJson)
        {
            StartCoroutine(createAvatar.loadAvatarFromJson(avatarSpecJson, (avatarEntity) => {

                _previewAnimationLayer = avatarEntity.bodyAnimController.CreateAnimationLayerByName("AnimationPreview");
                Pico.Avatar.AvatarMask mask = new Pico.Avatar.AvatarMask(avatarEntity.bodyAnimController);
                mask.SetAllJointsPositionEnable(true);
                mask.SetAllJointsRotationEnable(true);
                mask.SetAllJointsScaleEnable(true);
                _previewAnimationLayer.SetAvatarMask(mask);
                _previewAnimationLayer.SetLayerBlendMode(AnimLayerBlendMode.Override);

                menuManager.LodVisualElement.SetActive(true);
                
                // update camera position.
                Vector3 headPos = avatarEntity.bodyAnimController.GetJointWorldXForm(JointType.Head).position;
                Vector3 rootPos = avatarEntity.bodyAnimController.GetJointWorldXForm(JointType.Root).position;
                
                float chaHeight = Vector3.Distance(headPos, rootPos);
                UnityEngine.Debug.Log("cha Height : " + chaHeight);
                
                Vector3 leftHandWristPos = avatarEntity.bodyAnimController.GetJointWorldXForm(JointType.LeftHandWrist).position;
                Vector3 rightHandWristPos = avatarEntity.bodyAnimController.GetJointWorldXForm(JointType.RightHandWrist).position;
                
                float chaLength = Vector3.Distance(leftHandWristPos, rightHandWristPos);
                UnityEngine.Debug.Log("cha Length : " + chaLength);
                
                float maxValue = Mathf.Max(chaHeight, chaLength);
                
                // if (maxValue >= 1.2f && maxValue <= 2.5f)
                // {
                //     Vector3 tempPos = mainCamera.transform.localPosition;
                //     tempPos.z = (maxValue - 1.48f) * 4.0f + 1.5f;
                //     mainCamera.transform.localPosition = tempPos;
                // }
            }, false, false));
        }

        public void PutOnAsset(string assetUuid, AvatarAssetType type)
        {
            this.StartCoroutine(Coroutine_PutOnAsset(assetUuid, type));
        } 
            
        IEnumerator Coroutine_PutOnAsset(string assetId, AvatarAssetType assetType )
        {
            if(string.IsNullOrEmpty(assetId))
            {
                yield break;
            }

            if (createAvatar != null)
            {
                yield return createAvatar.Coroutine_PutOnAsset(assetId, assetType);
            }
        }

        public void PlayAnimationByName(string name)
        {
            if (createAvatar != null)
            {
                //createAvatar.avatar.PlayAnimation(name);
                if (_previewAnimationLayer != null)
                {
                    _previewAnimationLayer.PlayAnimationClip(name, 0, 1, 0);
                }
                else
                {
                    _previewAnimationLayer = createAvatar.avatarEntity.bodyAnimController.CreateAnimationLayerByName("AnimationPreview");
                    Pico.Avatar.AvatarMask mask = new Pico.Avatar.AvatarMask(createAvatar.avatarEntity.bodyAnimController);
                    mask.SetAllJointsPositionEnable(true);
                    mask.SetAllJointsRotationEnable(true);
                    mask.SetAllJointsScaleEnable(true);
                    _previewAnimationLayer.SetAvatarMask(mask);
                    _previewAnimationLayer.SetLayerBlendMode(AnimLayerBlendMode.Override);
                    
                    if (_previewAnimationLayer != null)
                    {
                        _previewAnimationLayer.PlayAnimationClip(name, 0, 1, 0);
                    }
                }
                // disable ik when play animation
                for (int i = 0; i < (int)IKEffectorType.Count; ++i)
                {
                    createAvatar.avatarEntity.bodyAnimController.bipedIKController?.SetIKEnable((IKEffectorType)i, false);
                }
            }
        }
        
        public void StopAnimation()
        {
            if(createAvatar != null)
            {
                //createAvatar.avatar.StopAnimation();
                if (_previewAnimationLayer != null)
                {
                    _previewAnimationLayer.StopAnimation(0);
                }

                // enable ik when stop animation
                for (int i = 0; i < (int)IKEffectorType.Count; ++i)
                {
                    createAvatar.avatarEntity.bodyAnimController.bipedIKController?.SetIKEnable((IKEffectorType)i, true);
                }
            }
        }
        
        public void AddAnimationSet(string assetId)
        {
            if(createAvatar != null)
            {
                createAvatar.avatar.AddAnimationSet(assetId);
            }
        }

        public void SwitchMaterialType(MaterialTyple materialTyple)
        {
            menuManager.SwitchMaterialType(materialTyple);
        }
        
        private void OnDestroy()
        {
            avatarSpec = "{\"info\":{\"sex\":\"male\",\"status\":\"Online\",\"tag_list\":[\"Common\"],\"continent\":\"EU\",\"background\":{\"image\":\"https://dfsedffe.png\",\"end_color\":[133,182,255],\"start_color\":[148,111,253]},\"avatar_type\":\"preset\"},\"avatar\":{\"body\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-Bone\",\"floatIdParams\":[]},\"head\":{\"version\":1,\"perParams\":[],\"technique\":\"Pico2-BS\",\"floatIdParams\":[]},\"skin\":{\"color\":\"\",\"white\":0,\"softening\":0},\"assetPins\":[],\"nextWearTimeStamp\":25},\"avatarStyle\":\"PicoAvatar3\"}";
            avatarName = "";
        }
    }
}
#endif