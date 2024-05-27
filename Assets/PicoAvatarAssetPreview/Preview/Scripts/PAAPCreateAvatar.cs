using System;
using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Pico.Avatar;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.Serialization;

/*
 * Copy from CreateAvatar.cs, just rename.
 */

namespace AssemblyCSharp.Assets.AmzAvatar.TestTools
{
    public class PAAPCreateAvatar : MonoBehaviour
    {
        public string userId = "662230622642634752";
        public string avatarId = "";
        public string avatarStyleName;


        public AvatarManifestationType manifestationType = AvatarManifestationType.Full;
        public string handAssetId = "";//V3Test_Glove_Female_1
        [Tooltip("whether allow avatar meta loaded from local cache when network is weak.")]
        public bool allowAvatarMetaFromCache = false;
        public bool isMainAvatar = false;
        public DeviceInputReaderBuilderInputType inputType = DeviceInputReaderBuilderInputType.PicoXR;
        public bool allowEdit = false;
        public bool enableExpression = true;
        public bool enableFacialExpressionTransfer = false;
        public bool flipFollowMode = false;
        public AvatarLodLevel initLodLevel = AvatarLodLevel.Invalid;
        public AvatarLodLevel forceLodLevel = AvatarLodLevel.Invalid;
        public AvatarLodLevel maxLodLevel = AvatarLodLevel.Lod0;

        [HideInInspector]
        public uint animationFlags = 0;
        public AvatarHeadShowType headShowType = AvatarHeadShowType.Normal;
        public bool bodyCulling = false;
        public bool alignArmSpanByXButton = true;
        public bool viewJoints = false;
        public double delayTime = 1;
        public bool cameraTracking = false;
        public RecordBodyAnimLevel recordBodyAnimLevel = RecordBodyAnimLevel.Invalid;
        public JointType[] criticalJoints;
        public bool enablePlaceHolder = true;//change if needed.
        public bool loaded = false;
        public PAAPAvatarIKSettings ikSettings = null;
        
        //device actions
        public bool actionBasedControl = false;
        public InputActionProperty[] positionActions;
        public InputActionProperty[] rotationActions;
        public InputActionProperty[] buttonActions;

        [HideInInspector]
        public PicoAvatar avatar;
        [HideInInspector]
        public AvatarEntity avatarEntity;

        private AvatarBodyAnimController _bodyAnimController;
        private bool _animStarted = false;

        public bool loadByJson = false;

        public string[] changeClothId;

        public string[] changeClothJsonSpec;

        public bool customHandOpen = false;

        public Vector3 left_offset = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 right_offset = new Vector3(0.0f, 0.0f, 0.0f);

        private bool _customHandOpen = false;

        private Quaternion defaultLeftFootRot = new Quaternion();
        private Quaternion defaultRightFootRot = new Quaternion();

        public string jsonSpec = "";
        public bool useFaceTracker = false;
        
        public string materialProviderShaderName = null;

        private Vector3 cameraOffsetPosition = new Vector3(0.0f, 0.0f, 0.0f);

        private bool _isHeightAutoFitInitialized = false;

        private float _maxControllerDistance = 1.0f;

        public PAAPCreateAvatar()
        {

        }

        IEnumerator Start()
        {
            if (AvatarEnv.NeedLog(DebugLogMask.AvatarLoad))
            {
                AvatarEnv.Log(DebugLogMask.AvatarLoad, "CreateAvatar Start ");
            }

            //
            while (!PicoAvatarManager.canLoadAvatar)
            {
                yield return null;
            }

            if (ikSettings == null)
            {
                Debug.LogWarning("[CreateAvatar] If using IK, please add the 'AvatarIKSettings' prefab instance for 'IK Settings' in 'CreateAvatar' component.");
            }

            var capability = new AvatarCapabilities();
            capability.manifestationType = manifestationType;
            capability.controlSourceType = isMainAvatar ? ControlSourceType.MainPlayer : ControlSourceType.OtherPlayer;
            capability.bodyCulling = bodyCulling;
            capability.recordBodyAnimLevel = recordBodyAnimLevel;
            capability.enablePlaceHolder = enablePlaceHolder; //set Enable PlaceHolder.
            capability.autoStopIK = ikSettings ? ikSettings.autoStopIK : true; //set automatically stop ik when controller is far,idle,etc.
            capability.ikMode = ikSettings ? ikSettings.ikMode : AvatarIKMode.None;
            capability.handAssetId = handAssetId;
            capability.headShowType = headShowType;
            capability.initLodLevel = initLodLevel;
            capability.maxLodLevel = maxLodLevel;
            capability.forceLodLevel = forceLodLevel;
            capability.inputSourceType = inputType;

            if (isMainAvatar)
            {
                // Whether enable body tracking mode. 
                var avatarDebugToolGo = GameObject.Find("AvatarSDKDebugToolPanel");
                if (avatarDebugToolGo != null)
                {
                    // var avatarSDKDebugToolPanel = avatarDebugToolGo.GetComponent<AvatarSDKDebugToolPanel>();
                    // if (avatarSDKDebugToolPanel != null && 
                    //     avatarSDKDebugToolPanel.Config.GetLocalPropValueString(QAConfig.NameType.bodyTrackingMode).ToLower() == "true")
                    // {
                    //     capability.inputSourceType = DeviceInputReaderBuilderInputType.BodyTracking;
                    // }
                }
            }
           
           
            capability.enableExpression = enableExpression;
            
            if(allowEdit && string.IsNullOrEmpty(PicoAvatarApp.instance.localDebugSettings.debugConfigText))
            {
                capability.usage = AvatarCapabilities.Usage.AllowEdit;
            }
            if (enableFacialExpressionTransfer)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.EnableFaceExpressionTransfer;
            }
            if (enableFacialExpressionTransfer)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.EnableFaceExpressionTransfer;
            }
            if(flipFollowMode)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.FlipFollowMode;
            }
            if(allowAvatarMetaFromCache)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.AllowAvatarMetaFromCache;
            }

            while (Time.realtimeSinceStartup < delayTime)
            {
                yield return null;
            }

            Action<PicoAvatar, AvatarEntity> callback = (avatar, avatarEntity) =>
            {

                if (avatarEntity == null)
                {
                    AvatarEnv.Log(DebugLogMask.GeneralError, "CreateAvatar: Failed to load Avatar.");
                    this.loaded = true;
                    return;
                }

                //
                this.avatarStyleName = avatar.avatarStyleName;
                _bodyAnimController = avatarEntity?.bodyAnimController;
                if (_bodyAnimController == null)
                {
                    this.loaded = true;
                    return;
                }

                if (!isMainAvatar)
                {
                    if (_bodyAnimController != null && _bodyAnimController.bipedIKController != null)
                    {
                        _bodyAnimController.bipedIKController.SetValidHipsHeightRange(0.8f, 3.0f);
                        //_bodyAnimController.restoreToIdleWhenHeightInvalid = true;
                    }

                    
                    //avatar.ForceUpdate();
                    //avatar.StopAnimation();
                    //test change clothes animation
                    // var changeClothesLayer = _bodyAnimController.CreateAnimationLayerByName("ChangeClothes");
                    // Pico.Avatar.AvatarMask mask = new Pico.Avatar.AvatarMask();
                    // mask.SetJointPositionEnable(JointType.Hips,true);
                    // mask.SetJointPositionEnable(JointType.Root, true);
                    // changeClothesLayer.SetAvatarMask(mask);
                    // changeClothesLayer.PlayAnimationClip("walkingLeft", 0, 1, 0);
                }
                else
                {
                    _bodyAnimController = avatarEntity?.bodyAnimController;

                    InitAutoFitController();
                    if (_bodyAnimController != null)
                    {
                        if (avatarEntity.owner.capabilities.inputSourceType == DeviceInputReaderBuilderInputType.BodyTracking)
                        {
                            InitBodyTrackingUIManager(avatarEntity.deviceInputReader);
                        }

                        if (useFaceTracker)
                        {
                            _bodyAnimController.StartFaceTrack(true, true);
                        }

                        _bodyAnimController.bipedIKController.SetValidHipsHeightRange(0.25f, 3.0f);

                        //_bodyAnimController.setIKHandInvalidRegionEnable(true);

                        // _bodyAnimController.SetAvatarHeight(0.5f);

                        //test setting ik target
                        _bodyAnimController.bipedIKController.SetIKTrackingSource(IKEffectorType.Head,
                            IKTrackingSource.DeviceInput);
                        _bodyAnimController.bipedIKController.SetIKTrackingSource(IKEffectorType.LeftHand,
                            IKTrackingSource.DeviceInput);
                        _bodyAnimController.bipedIKController.SetIKTrackingSource(IKEffectorType.RightHand,
                            IKTrackingSource.DeviceInput);
                        //_bodyAnimController.bipedIKController.SetIKTrackingSource(IKEffectorType.LeftFoot, (int)IKTrackingSource.DeviceInput);
                        //_bodyAnimController.bipedIKController.SetIKTrackingSource(IKEffectorType.RightFoot, (int)IKTrackingSource.DeviceInput);

#if UNITY_EDITOR
                        _bodyAnimController.bipedIKController.SetIKAutoStopModeEnable(IKAutoStopMode.ControllerIdle,
                            false);
#endif

                        //_bodyAnimController.bipedIKController.SetProceduralFootstepEnable(false);
                        //_bodyAnimController.SetIKTrackingSource((int)IKEffectorType.LeftFoot, (int)IKTrackingSource.Custom);
                        //_bodyAnimController.SetIKTrackingSource((int)IKEffectorType.RightFoot, (int)IKTrackingSource.Custom);
                        //_bodyAnimController.SetIKEnable((int)IKEffectorType.RightHand, false);

                        //Quaternion avatarDefaultRot = avatarEntity.GetNativeXForm().orientation;
                        //XForm xForm = _bodyAnimController.GetJointXForm((uint)JointType.LeftFootAnkle);
                        //defaultLeftFootRot = avatarDefaultRot * xForm.orientation;
                        //xForm = _bodyAnimController.GetJointXForm((uint)JointType.RightFootAnkle);
                        //defaultRightFootRot = avatarDefaultRot *xForm.orientation;

                        if (_customHandOpen && isMainAvatar)
                        {
                            GameObject leftHandSkeleton = GameObject.Find("b_l_hand");
                            GameObject rightHandSkeleton = GameObject.Find("b_r_hand");

                            GameObject leftHandPose = GameObject.Find("b_l_hand_pose");
                            GameObject rightHandPose = GameObject.Find("b_r_hand_pose");
                            // Right
                            Vector3 right_up = new Vector3(0.0f, 0.0f, -1.0f);
                            Vector3 right_forward = new Vector3(-1.0f, 0.0f, 0.0f);
                            // Left
                            Vector3 left_up = new Vector3(0.0f, 0.0f, -1.0f);
                            Vector3 left_forward = new Vector3(1.0f, 0.0f, 0.0f);

                            bool state = avatar.entity.SetCustomHand(HandSide.Right, rightHandSkeleton, rightHandPose,
                                right_up, right_forward, right_offset);
                            state = avatar.entity.SetCustomHand(HandSide.Left, leftHandSkeleton, leftHandPose, left_up,
                                left_forward, left_offset);
                            if (state)
                            {
                                _bodyAnimController.bipedIKController.SetRotationLimitEnable(JointType.LeftHandWrist,
                                    false);
                                _bodyAnimController.bipedIKController.SetRotationLimitEnable(JointType.RightHandWrist,
                                    false);
                                _customHandOpen = false;
                            }
                        }
                    }
                }

                //
                this.loaded = true;
            };
            //jsonSpec = "{\"info\":{\"sex\": \"female\", \"background\": { \"color\": [255, 255, 0], \"image\": \"https://dfsedffe.png\"}}, \"graph\": {\"type\": \"PicoAvatar\", \"label\": \"general asset graph for pico avatar\", \"nodes\": {\"1\": { \"label\": \"Body\", \"metadata\": { \"tag\": \"Body_dev\", \"pins\": { \"root\": { \"type\": \"transform\", \"entityName\": \"root\"} }, \"uuid\": \"1351969406440325120\", \"colors\": { \"circleColor\": [1, 1, 1, 1]}, \"category\": \"Body\", \"textures\": { \"signature\": \"fsegisjf\"}, \"animations\": { \"gesture1\": \"fsegfsi\"}, \"blendshapes\": { \"mouthOpen\": 0.8}, \"incompatibleTags\": [\"hair_longBangs\"], \"boneDisplacements\": { \"nose\": 0.3} } },\"2\": { \"label\": \"Skeleton\", \"metadata\": { \"tag\": \"Skeleton\", \"pins\": { \"root\": { \"type\": \"transform\", \"entityName\": \"root\"} }, \"uuid\": \"1357496899997921280\", \"colors\": { \"circleColor\": [1, 1, 1, 1]}, \"category\": \"Skeleton\", \"textures\": { \"signature\": \"fsegisjf\"}, \"blendshapes\": { \"mouthOpen\": 0.8}, \"incompatibleTags\": [\"hair_longBangs\"], \"boneDisplacements\": { \"nose\": 0.3} } }}, \"directed\": true} }";
            if (loadByJson == true)
            {
                var escapedJsonSpec = jsonSpec.Replace("\\\"", "\"");
                avatar = PicoAvatarManager.instance.LoadAvatar(new AvatarLoadContext(userId, avatarId, escapedJsonSpec, capability), callback);
            }
            else
            {
                // set avatar style name
                capability.avatarStyleName = avatarStyleName;
                //
                avatar = PicoAvatarManager.instance.LoadAvatar(new AvatarLoadContext(userId, avatarId, null, capability), callback);
            }
            if(avatar == null)
            {
                UnityEngine.Debug.LogError("Failed to load avatar!");
                yield break;
            }

            avatar.criticalJoints = this.criticalJoints;

            if (!string.IsNullOrEmpty(materialProviderShaderName))
            {
                avatar.materialProvider = (AvatarLodLevel lodLevel, AvatarShaderType shaderType,PicoAvatarRenderMesh mesh) => {
                    return new Material(Shader.Find(materialProviderShaderName));
                };
            }

            //
            avatarEntity = avatar.entity;

            Transform avatarTransform = avatar?.transform;

            avatarTransform.SetParent(transform);
            avatarTransform.localPosition = Vector3.zero;
            avatarTransform.localRotation = Quaternion.identity;
            avatarTransform.localScale = Vector3.one;

            avatarEntity.actionBasedControl = actionBasedControl;
            avatarEntity.positionActions = positionActions;
            avatarEntity.rotationActions = rotationActions;
            avatarEntity.buttonActions = buttonActions;

            ikSettings?.updateIKTargetsConfig(avatarEntity.avatarIKTargetsConfig);
            
            if (cameraTracking && isMainAvatar && PicoAvatarManager.instance.avatarCamera != null)
            {
                PicoAvatarManager.instance.avatarCamera.trakingAvatar = avatar;
            }
            _customHandOpen = customHandOpen;
        }

        public void Update()
        {
            if(!_animStarted)
            {
                if(_bodyAnimController != null)
                {
                    _animStarted = _bodyAnimController.started;
                }
            }

            if (avatar != null)
            {
                //test play custom animation for local avatar
                //if (isLocalAvatar && avatar.isPlayingAnimation == false && avatar.isAnyEntityReady && ikMode != AvatarIKMode.FullBody)
                //{
                //    avatar.PlayAnimation("walking", 0);
                //}
            }

            if (_bodyAnimController != null && isMainAvatar)
            {
                if (customHandOpen && avatarEntity != null)
                {
                    AvatarCustomHandPose leftHandPose = avatarEntity.leftCustomHandPose;
                    if (leftHandPose != null)
                    {
                        leftHandPose.wristOffset = left_offset;
                    }
                    AvatarCustomHandPose rightHandPose = avatarEntity.rightCustomHandPose;
                    if (rightHandPose != null)
                    {
                        rightHandPose.wristOffset = right_offset;
                    }
                }

                if (ikSettings != null && ikSettings.isDirty)
                {
                    if (!_isHeightAutoFitInitialized && ikSettings.heightAutoFit.enableAutoFitHeight)
                    {
                        InitAutoFitController();
                    }

                    ikSettings.UpdateAvatarIKSettings(avatarEntity);
                }

                var _autoFitController = _bodyAnimController.autoFitController;
                if (_autoFitController != null && ikSettings != null &&
                    ikSettings.heightAutoFit.enableAutoFitHeight == true && 
                    ikSettings.heightAutoFit.cameraOffsetTarget != null)
                {
                    Vector3 pos = ikSettings.heightAutoFit.cameraOffsetTarget.transform.position;
                    if(Vector3.SqrMagnitude(pos - this.cameraOffsetPosition) > 1e-6)
                    {
                        _autoFitController.SetCurrentAvatarOffset(pos);
                        this.cameraOffsetPosition = pos;
                    }
                   
                }
                //test setting custom IK Targets

                //GameObject leftFootTarget = GameObject.Find("LeftFoot");
                //GameObject rightFootTarget = GameObject.Find("RightFoot");
                //if (leftFootTarget)
                //{
                //    XForm xForm;
                //    xForm.position = leftFootTarget.transform.localPosition;
                //    xForm.orientation = leftFootTarget.transform.localRotation * defaultLeftFootRot;
                //    xForm.scale = leftFootTarget.transform.localScale;
                //    _bodyAnimController.SetIKEffectorXForm((uint)IKEffectorType.LeftFoot, xForm);
                //}
                //if (rightFootTarget)
                //{
                //    XForm xForm;
                //    xForm.position = rightFootTarget.transform.localPosition;
                //    xForm.orientation = rightFootTarget.transform.localRotation * defaultRightFootRot;
                //    xForm.scale = rightFootTarget.transform.localScale;
                //    _bodyAnimController.SetIKEffectorXForm((uint)IKEffectorType.RightFoot, xForm);
                //}
            }

            if (alignArmSpanByXButton)
            {
                bool xButtonPressed = false;
                if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out xButtonPressed))
                {
                    if (xButtonPressed)
                    {
                       AlignAvatarArmSpan();
                    }
                }
            }
        }

        void InitAutoFitController()
        {
            if (_bodyAnimController == null || ikSettings == null)
                return;
            
            if (!ikSettings.heightAutoFit.enableAutoFitHeight || ikSettings.heightAutoFit.cameraOffsetTarget == null)
                return;

            //sdk event trigger
            var _autoFitController = _bodyAnimController.autoFitController;
            if (_autoFitController == null)
                return;

            //_autoFitController.SetMaxCrouchingTime(-1);
            _autoFitController.localAvatarHeightFittingEnable = true;
            _autoFitController.ClearAvatarOffsetChangedCallback(OnAvatarOffsetChangedCallBack);
            _autoFitController.AddAvatarOffsetChangedCallback(OnAvatarOffsetChangedCallBack);

            //app event trigger 
            var trigger = this.avatar.entity.gameObject.GetComponent<PicoAvatarAutoFitTrigger>();
            if (trigger == null)
                trigger = this.avatar.entity.gameObject.AddComponent<PicoAvatarAutoFitTrigger>();
            trigger.SetTriggerCallback(OnAppAutoFitTrigger);

            //when create avatar finished ,force trigger offset 
            Vector3 initPos = ikSettings.heightAutoFit.cameraOffsetTarget.transform.position;
            _autoFitController.SetCurrentAvatarOffset(initPos);
            _autoFitController.UpdateAvatarHeightOffset();
            Debug.Log("pav:_autoFitController.UpdateAvatarHeightOffset");

            _isHeightAutoFitInitialized = true;
        }

        void AlignAvatarArmSpan()
        {
            if(avatarEntity != null)
            {
                avatarEntity.AlignAvatarArmSpan();
            }
        }

        //sdk trigger changeOffset callBack
        void OnAvatarOffsetChangedCallBack(AvatarAutoFitController controller, Vector3 cameraOffsetPos)
        {
            if (ikSettings == null || !ikSettings.heightAutoFit.enableAutoFitHeight)
                return;
            
            //Debug.Log("pav:OnAvatarOffsetChanged:" + cameraOffsetPos.ToString());
            //更新相机offset
            RefreshCameraOffsetTargetPos(cameraOffsetPos);
            controller.SetCurrentAvatarOffset(cameraOffsetPos);
        }

        //app trigger callBack
        void OnAppAutoFitTrigger()
        {
            Debug.Log("pav:OnAppAutoFitTrigger:");
            var _autoFitController = _bodyAnimController.autoFitController;
            if (_autoFitController == null || ikSettings == null || ikSettings.heightAutoFit.cameraOffsetTarget == null)
                return;
            
            Vector3 initPos = ikSettings.heightAutoFit.cameraOffsetTarget.transform.position;
            _autoFitController.SetCurrentAvatarOffset(initPos);
            _autoFitController.UpdateAvatarHeightOffset();
        }
        //refresh camera offset
        void RefreshCameraOffsetTargetPos(Vector3 offset)
        {
            if (ikSettings == null || ikSettings.heightAutoFit.cameraOffsetTarget == null)
                return;

            ikSettings.heightAutoFit.cameraOffsetTarget.transform.position = offset;
            //Debug.Log("pav:cameraOffsetTarget pos:" + cameraOffsetTarget.transform.position.ToString());
        }

        public void ChangeMeshLOD(int lod)
        {
            if(avatar != null && avatar.entity != null)
            {
                avatar.entity.ForceLod((AvatarLodLevel)lod);
            }
        }

        public void ChangeCloth(int index)
        {
            loaded = false;
            UnloadAvatar();
            StartCoroutine(LoadAvatar(index));
            if (customHandOpen)
            {
                _customHandOpen = true;
            }
        }

        private void UnloadAvatar()
        {
            PicoAvatarManager.instance.UnloadAvatar(avatar);
            avatar = null;
        }

        private IEnumerator LoadAvatar(int index)
        {
            var capability = new AvatarCapabilities();
            capability.manifestationType = manifestationType;
            capability.controlSourceType = isMainAvatar ? ControlSourceType.MainPlayer : ControlSourceType.OtherPlayer;
            capability.bodyCulling = bodyCulling;
            capability.recordBodyAnimLevel = recordBodyAnimLevel;
            capability.handAssetId = handAssetId;
            capability.autoStopIK = ikSettings ? ikSettings.autoStopIK : true; //set automatically stop ik when controller is far,idle,etc.
            capability.ikMode = ikSettings ? ikSettings.ikMode : AvatarIKMode.FullBody;
            capability.headShowType = headShowType;
            capability.enableExpression = enableExpression;
            capability.inputSourceType = inputType;
                
            if (isMainAvatar)
            {
                // Whether enable body tracking mode. 
                // var avatarDebugToolGo = GameObject.Find("AvatarSDKDebugToolPanel");
                // if (avatarDebugToolGo != null)
                // {
                //     var avatarSDKDebugToolPanel = avatarDebugToolGo.GetComponent<AvatarSDKDebugToolPanel>();
                //     if (avatarSDKDebugToolPanel != null && 
                //         avatarSDKDebugToolPanel.Config.GetLocalPropValueString(QAConfig.NameType.bodyTrackingMode).ToLower() == "true")
                //     {
                //         capability.inputSourceType = DeviceInputReaderBuilderInputType.BodyTracking;
                //     }
                // }
            }
            if (allowEdit && string.IsNullOrEmpty(PicoAvatarApp.instance.localDebugSettings.debugConfigText))
            {
                capability.usage = AvatarCapabilities.Usage.AllowEdit;
            }
            if (enableFacialExpressionTransfer)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.EnableFaceExpressionTransfer;
            }
            if (flipFollowMode)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.FlipFollowMode;
            }

            while (Time.realtimeSinceStartup < delayTime)
            {
                yield return null;
            }

            Action<PicoAvatar, AvatarEntity> callback = (avatar, avatarEntity) =>
            {
                _bodyAnimController = avatarEntity?.bodyAnimController;
                if(_bodyAnimController == null)
                {
                    this.loaded = true;
                }

                if (!isMainAvatar)
                {
                    avatar.PlayAnimation("idle", 0, "BottomLayer");
                    avatar.ForceUpdate();
                }
                else
                {
                    if (avatar.capabilities.inputSourceType == DeviceInputReaderBuilderInputType.BodyTracking)
                    {
                        InitBodyTrackingUIManager(avatarEntity.deviceInputReader);
                    }

                    if (useFaceTracker)
                    {
                        _bodyAnimController.StartFaceTrack(true, true);
                    }
                    InitAutoFitController();

                    _bodyAnimController.autoFitController?.ApplyPreset(_bodyAnimController.autoFitController.presetStanding);

                    //_bodyAnimController.bipedIKController.SetValidHipsHeightRange(0.25f, 3.0f);

#if UNITY_EDITOR
                    _bodyAnimController.bipedIKController.SetIKAutoStopModeEnable(IKAutoStopMode.ControllerIdle, false);
#endif
                    //_bodyAnimController.SetIKAutoStopModeEnable((uint)IKAutoStopMode.ControllerIdle, false);

                    if (_customHandOpen && isMainAvatar)
                    {
                        GameObject leftHandSkeleton = GameObject.Find("b_l_hand");
                        GameObject rightHandSkeleton = GameObject.Find("b_r_hand");

                        GameObject leftHandPose = GameObject.Find("b_l_hand_pose");
                        GameObject rightHandPose = GameObject.Find("b_r_hand_pose");
                        // Right
                        Vector3 right_up = new Vector3(0.0f, 0.0f, -1.0f);
                        Vector3 right_forward = new Vector3(-1.0f, 0.0f, 0.0f);
                        // Left
                        Vector3 left_up = new Vector3(0.0f, 0.0f, -1.0f);
                        Vector3 left_forward = new Vector3(1.0f, 0.0f, 0.0f);

                        bool state = avatar.entity.SetCustomHand(HandSide.Right, rightHandSkeleton, rightHandPose, right_up, right_forward,right_offset);
                        state = avatar.entity.SetCustomHand(HandSide.Left, leftHandSkeleton, leftHandPose, left_up, left_forward,left_offset);
                        if (state)
                        {
                            _bodyAnimController.bipedIKController.SetRotationLimitEnable(JointType.LeftHandWrist, false);
                            _bodyAnimController.bipedIKController.SetRotationLimitEnable(JointType.RightHandWrist, false);
                            _customHandOpen = false;
                        }
                    }
                }

                // hide with scale.
                //avatar.transform.localScale = Vector3.zero;

                // show ite.
                CoroutineExecutor.DoDelayedWork(() => {
                    avatar.transform.localScale = Vector3.one;
                }, 0.01f);

                this.loaded = true;
            };
            index %= changeClothId.Length;
            if(loadByJson == false)
            {
                index %= changeClothId.Length;
                if (changeClothId.Length == 0)
                {
                    yield break;
                }
                avatar = PicoAvatarManager.instance.LoadAvatar(new AvatarLoadContext(userId, changeClothId[index], null, capability), callback);
            }
            else
            {
                if(changeClothJsonSpec.Length == 0)
                {
                    yield break;
                }
                index %= changeClothJsonSpec.Length;
                avatar = PicoAvatarManager.instance.LoadAvatar(new AvatarLoadContext(userId, changeClothId[index], changeClothJsonSpec[index], capability), callback);
            }
            
            avatar.criticalJoints = this.criticalJoints;
            //
            avatarEntity = avatar.entity;
            //

            Transform avatarTransform = avatar?.transform;

            avatarTransform.SetParent(transform);
            avatarTransform.localPosition = Vector3.zero;
            avatarTransform.localRotation = Quaternion.identity;
            avatarTransform.localScale = Vector3.one;

            ikSettings?.updateIKTargetsConfig(avatarEntity.avatarIKTargetsConfig);

            if (cameraTracking && isMainAvatar && PicoAvatarManager.instance.avatarCamera != null)
            {
                PicoAvatarManager.instance.avatarCamera.trakingAvatar = avatar;
            }
        }

        public IEnumerator loadAvatarFromJson(string avatarJson, System.Action<AvatarEntity> entityReadyCallback = null, bool enableFT  = false, bool enableLipSync = false)
        {
            var capability = new AvatarCapabilities();
            capability.manifestationType = manifestationType;
            capability.controlSourceType = isMainAvatar ? ControlSourceType.MainPlayer : ControlSourceType.OtherPlayer;
            capability.bodyCulling = bodyCulling;
            capability.recordBodyAnimLevel = recordBodyAnimLevel;
            capability.enablePlaceHolder = enablePlaceHolder; //set Enable PlaceHolder.
            capability.autoStopIK = ikSettings ? ikSettings.autoStopIK : true; //set automatically stop ik when controller is far,idle,etc.
            capability.ikMode = ikSettings ? ikSettings.ikMode : AvatarIKMode.FullBody;
            capability.headShowType = headShowType;
            capability.enableExpression = enableExpression;
            capability.inputSourceType = inputType;
            capability.forceLodLevel = forceLodLevel;
            
            if (isMainAvatar)
            {
                // Whether enable body tracking mode. 
                // var avatarDebugToolGo = GameObject.Find("AvatarSDKDebugToolPanel");
                // if (avatarDebugToolGo != null)
                // {
                //     var avatarSDKDebugToolPanel = avatarDebugToolGo.GetComponent<AvatarSDKDebugToolPanel>();
                //     if (avatarSDKDebugToolPanel != null && 
                //         avatarSDKDebugToolPanel.Config.GetLocalPropValueString(QAConfig.NameType.bodyTrackingMode).ToLower() == "true")
                //     {
                //         capability.inputSourceType = DeviceInputReaderBuilderInputType.BodyTracking;
                //     }
                // }
            }
            
            if (allowEdit && string.IsNullOrEmpty(PicoAvatarApp.instance.localDebugSettings.debugConfigText))
            {
                capability.usage = AvatarCapabilities.Usage.AllowEdit;
            }
            if (enableFacialExpressionTransfer)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.EnableFaceExpressionTransfer;
            }
            if (flipFollowMode)
            {
                capability.flags |= (uint)AvatarCapabilities.Flags.FlipFollowMode;
            }

            while (Time.realtimeSinceStartup < delayTime)
            {
                yield return null;
            }

            avatar = PicoAvatarManager.instance.LoadAvatar(new AvatarLoadContext(userId, "", avatarJson, capability),
                (avatar, avatarEntity) => {

                //
                this.avatarStyleName = avatar.avatarStyleName;
                _bodyAnimController = avatarEntity?.bodyAnimController;
                if (_bodyAnimController == null)
                {
                    this.loaded = true;
                    return;
                }

                if (!isMainAvatar)
                {
                    if (_bodyAnimController != null)
                    {
                        _bodyAnimController.bipedIKController?.SetValidHipsHeightRange(0.8f, 3.0f);
                        //_bodyAnimController.restoreToIdleWhenHeightInvalid = true;
                    }

                   avatar.PlayAnimation("idle", 0, "BottomLayer");
                   // force update animation data immediately
                   avatar.ForceUpdate();
                }
                else
                {
                    _bodyAnimController = avatarEntity?.bodyAnimController;
                    if (_bodyAnimController != null)
                    {
                        if (avatar.capabilities.inputSourceType == DeviceInputReaderBuilderInputType.BodyTracking)
                        {
                            InitBodyTrackingUIManager(avatarEntity.deviceInputReader);
                        }

                        if (useFaceTracker)
                        {
                            Debug.Log("CraeteAvatar client current useFt is " + enableFT + " uselipsync is " + enableLipSync);
                            _bodyAnimController.StartFaceTrack(enableLipSync, enableFT);
                        }
                        
                        _bodyAnimController.bipedIKController.SetValidHipsHeightRange(0.25f, 3.0f);

                        //defaultRightFootRot = avatarDefaultRot *xForm.orientation;

                        if (_customHandOpen && isMainAvatar)
                        {
                            GameObject leftHandSkeleton = GameObject.Find("b_l_hand");
                            GameObject rightHandSkeleton = GameObject.Find("b_r_hand");

                            GameObject leftHandPose = GameObject.Find("b_l_hand_pose");
                            GameObject rightHandPose = GameObject.Find("b_r_hand_pose");
                            // Right
                            Vector3 right_up = new Vector3(0.0f, 0.0f, -1.0f);
                            Vector3 right_forward = new Vector3(-1.0f, 0.0f, 0.0f);
                            // Left
                            Vector3 left_up = new Vector3(0.0f, 0.0f, -1.0f);
                            Vector3 left_forward = new Vector3(1.0f, 0.0f, 0.0f);

                            bool state = avatar.entity.SetCustomHand(HandSide.Right, rightHandSkeleton, rightHandPose, right_up, right_forward, Vector3.zero);
                            state = avatar.entity.SetCustomHand(HandSide.Left, leftHandSkeleton, leftHandPose, left_up, left_forward, Vector3.zero);
                            if (state)
                            {
                                _bodyAnimController.bipedIKController.SetRotationLimitEnable(JointType.LeftHandWrist, false);
                                _bodyAnimController.bipedIKController.SetRotationLimitEnable(JointType.RightHandWrist, false);
                                _customHandOpen = false;
                            }
                        }
                    }
                }
                //
                this.loaded = true;

                if(entityReadyCallback != null)
                {
                    entityReadyCallback(avatarEntity);
                }
            });
            
            if (avatar == null)
            {
                UnityEngine.Debug.LogError("Failed to load avatar!");
                yield break;
            }

            avatar.criticalJoints = this.criticalJoints;
            //
            avatarEntity = avatar.entity;

            Transform avatarTransform = avatar?.transform;

            avatarTransform.SetParent(transform);
            avatarTransform.localPosition = Vector3.zero;
            avatarTransform.localRotation = Quaternion.identity;
            avatarTransform.localScale = Vector3.one;

            avatarEntity.actionBasedControl = actionBasedControl;
            avatarEntity.positionActions = positionActions;
            avatarEntity.rotationActions = rotationActions;
            avatarEntity.buttonActions = buttonActions;

            ikSettings?.updateIKTargetsConfig(avatarEntity.avatarIKTargetsConfig);

            if (cameraTracking && isMainAvatar && PicoAvatarManager.instance.avatarCamera != null)
            {
                PicoAvatarManager.instance.avatarCamera.trakingAvatar = avatar;
            }
            _customHandOpen = customHandOpen;
        }

        /**
         * PutOnAsset. open editor and stop editor.
         */ 
        public IEnumerator Coroutine_PutOnAsset(string assetId, AvatarAssetType assetType)
        {
            if(!avatar || !avatar.isAnyEntityReady)
            {
                yield break;
            }
           
            var avatarEditState = avatar.GetAvatarEditState();
            var lastEditStatus = avatarEditState.status;
            avatarEditState.EnterState(0, (result) =>
            {
                avatarEditState.PutOnAsset(avatar.userId, assetId, (uint)assetType, "", "", 0, (nativeResult, desc) =>
                {
                    if (lastEditStatus == AvatarEditState.Status.None)
                    {
                        avatarEditState.ExitState();
                    }
                    //
                    avatarEditState = null;
                });
            });

            while(avatarEditState != null)
            {
                yield return null;
            }
        }

        // If using body tracking, init the body tracking UI
        internal void InitBodyTrackingUIManager(IDeviceInputReader deviceInputReader)
        {
            // if (deviceInputReader is BodyTrackingDeviceInputReader)
            // {
            //     BodyTrackingUIManager.Instance.ResetBodyTrackingDeviceInputReader((BodyTrackingDeviceInputReader)deviceInputReader);
            // }
        }
    }
}
