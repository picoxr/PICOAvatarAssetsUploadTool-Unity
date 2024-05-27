#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico.AvatarAssetBuilder
{
    public class AccessorySelectionPanel : PavPanel
    {
        #region Properties
        public override string displayName { get => "AccessorySelectionPanel"; }
        public override string panelName { get => "AccessorySelectionPanel"; }
        public override string uxmlPathName { get => "Uxml/AccessorySelectionPanel.uxml"; }

        public static string femalePresetConfigPath
        {
            get
            {
                return AssetBuilderConfig.instance.uiDataEditorConfigPath + "FacePresetConfigJson/FemaleFacePresets.json";
            }
        }
        public static string malePresetConfigPath
        {
            get
            {
                return AssetBuilderConfig.instance.uiDataEditorConfigPath + "FacePresetConfigJson/MaleFacePresets.json";
            }
        }

        private const string k_FemaleButton = "femaleButton";
        private const string k_MaleButton = "maleButton";

        private const string k_ButtonSelected = "ButtonSelected";
        private const string k_ButtonUnSelected = "ButtonUnSelected";

        private const string k_FacePresetsScrollView = "facePresetsScrollView";

        private PavScrollView facePresetsScrollView = null;

        public Gender curGender { get; private set; }

        public Dictionary<Gender, FacePresetList> facePresetDict = new Dictionary<Gender, FacePresetList>();

        private static AccessorySelectionPanel _instance;
        public static AccessorySelectionPanel instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.LoadOrCreateAsset<AccessorySelectionPanel>(
                        AssetBuilderConfig.instance.uiDataStorePath + "PanelData/AccessorySelectionPanel.asset");
                }

                return _instance;
            }
        }

        // whether singleton object has been created.
        public static bool isValid => _instance != null;

        #endregion Properties


        #region Public Methods
        public override void OnEnable()
        {
            base.OnEnable();
            SwitchGenderUI(curGender);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (facePresetsScrollView != null)
            {
                facePresetsScrollView.OnDestroy();
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected override bool BuildUIDOM(VisualElement parent)
        {
            if (!base.BuildUIDOM(parent))
            {
                return false;
            }

            InitUI();
            LoadPresetConfigData();

            return true;
        }

        protected override bool BindUIActions()
        {
            if (!base.BindUIActions())
            {
                return false;
            }

            var femaleButton = mainElement.Q<Button>(k_FemaleButton);
            var maleButton = mainElement.Q<Button>(k_MaleButton);
            femaleButton.clicked += () =>
            {
                SwitchGenderUI(Gender.Female);
            };
            maleButton.clicked += () =>
            {
                SwitchGenderUI(Gender.Male);
            };

            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            SwitchGenderUI(curGender);
        }

        #endregion Public Methods


        #region Private Methods

        public int CellCount()
        {
            return facePresetDict[curGender].facePresets.Length;
        }

        public Type CellAtIndex(int index)
        {
            return typeof(FacePresetCell);
        }

        public PavScrollViewCellDataBase DataAtIndex(int index)
        {
            if (facePresetDict[curGender].facePresets.Length <= 0 || index < 0)
            {
                return null;
            }

            return facePresetDict[curGender].facePresets[index];
        }

        private void LoadPresetConfigData()
        {
            facePresetDict[Gender.Female] = JsonUtility.FromJson<FacePresetList>(File.ReadAllText(femalePresetConfigPath));
            facePresetDict[Gender.Male] = JsonUtility.FromJson<FacePresetList>(File.ReadAllText(malePresetConfigPath));
        }

        private void SavePresetConfigDataTo(string jsonConfigPath, Gender gender)
        {
            var femalePresetStr = JsonUtility.ToJson(facePresetDict[Gender.Female]);
            var malePresetStr = JsonUtility.ToJson(facePresetDict[Gender.Male]);
        }

        private void InitUI()
        {
            curGender = Gender.Female; // Default UI, set Female, DO NOT change to Male as it correspond to uxml settings

            var scrollView = mainElement.Q<ScrollView>("FacePresetScrollView");
            facePresetsScrollView = new PavScrollView(this, scrollView);
            facePresetsScrollView.Direction = ScrollViewDirection.Vertical;
            facePresetsScrollView.CellCount = CellCount;
            facePresetsScrollView.CellAtIndex = CellAtIndex;
            facePresetsScrollView.DataAtIndex = DataAtIndex;

            SwitchGenderUI(curGender);
        }

        private void SwitchGenderUI(Gender gender)
        {
            if (mainElement == null)
            {
                return;
            }

            var femaleButton = mainElement.Q<Button>(k_FemaleButton);
            var maleButton = mainElement.Q<Button>(k_MaleButton);

            if (gender == Gender.Female)
            {
                if (curGender != Gender.Female)
                {
                    curGender = Gender.Female;

                    // switch female UI showing

                    femaleButton.ElementAt(0).style.visibility = Visibility.Visible;
                    femaleButton.EnableInClassList(k_ButtonSelected, true);
                    femaleButton.EnableInClassList(k_ButtonUnSelected, false);

                    maleButton.ElementAt(0).style.visibility = Visibility.Hidden;
                    maleButton.EnableInClassList(k_ButtonSelected, false);
                    maleButton.EnableInClassList(k_ButtonUnSelected, true);

                    SwitchGenderScrollView();
                }
            }
            else if (gender == Gender.Male)
            {
                if (curGender != Gender.Male)
                {
                    curGender = Gender.Male;

                    // switch male UI showing

                    femaleButton.ElementAt(0).style.visibility = Visibility.Hidden;
                    femaleButton.EnableInClassList(k_ButtonSelected, false);
                    femaleButton.EnableInClassList(k_ButtonUnSelected, true);

                    maleButton.ElementAt(0).style.visibility = Visibility.Visible;
                    maleButton.EnableInClassList(k_ButtonSelected, true);
                    maleButton.EnableInClassList(k_ButtonUnSelected, false);

                    SwitchGenderScrollView();
                }
            }
        }

        private void SwitchGenderScrollView()
        {
            //var facePresets = facePresetDict[gender].facePresets;
            //foreach (var facePreset in facePresets)
            //{

            //}
            facePresetsScrollView.Refresh();
        }

        #endregion Private Methods
    }

    public enum Gender
    {
        None = -1,
        Female,
        Male,
        Count
    }

    [Serializable]
    public class FacePreset : PavScrollViewCellDataBase
    {
        public string assetName;
        public string assetId;
    }

    [Serializable]
    public class FacePresetList
    {
        public FacePreset[] facePresets;
    }

    public class FacePresetCell : PavScrollViewCell
    {
        public override string AssetPath
        {
            get
            {
                return AssetBuilderConfig.instance.uiDataAssetsPath + "UxmlWidget/FacePresetCell.uxml";
            }
        }

        private GroupBox contentGroupBox;
        private Button contentButton;

        public override void OnInit()
        {
            base.OnInit();
            contentGroupBox = _cellVisualElement.Q<GroupBox>("facePresetCellGroupBox");
            contentButton = _cellVisualElement.Q<Button>("facePresetCellButton");

            contentButton.RegisterCallback<ClickEvent>(@event =>
            {
                OnContentButtonClick();
            });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void RefreshCell()
        {
            var data = GetData<FacePreset>();
            if (null == data)
            {
                return;
            }

            contentButton.text = data.assetName;
        }

        private void OnContentButtonClick()
        {
            var data = GetData<FacePreset>();
            if (null == data)
            {
                return;
            }

            Debug.LogFormat("[INFO] Accessory id: {0}", data.assetId);
            // TODO: face preset info get here, send back to the unity editor window
        }
    }
}

#endif
