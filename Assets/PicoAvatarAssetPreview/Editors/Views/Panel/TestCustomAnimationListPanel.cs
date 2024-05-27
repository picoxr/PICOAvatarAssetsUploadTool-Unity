#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pico.Avatar;
using Pico.AvatarAssetPreview.Protocol;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using CharacterInfo = Pico.AvatarAssetPreview.Protocol.CharacterInfo;
using Label = UnityEngine.UIElements.Label;

namespace Pico.AvatarAssetPreview
{
    public class TestCustomAnimationListPanel : PavPanel
    {
        private PavScrollView animList;
        private CharacterInfo characterInfo;

        public override string displayName { get => "Animation"; }
        public override string panelName { get => "TestCustomAnimationListPanel"; }
        public override string uxmlPathName { get => "Uxml/TestCustomAnimationListPanel.uxml"; }
        
        public CharacterInfo CharacterInfo => characterInfo;
        
        private static TestCustomAnimationListPanel _instance;
        
        private List<AnimationTestCellData> animDatas = new List<AnimationTestCellData>();

        public static TestCustomAnimationListPanel instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.LoadOrCreateAsset<TestCustomAnimationListPanel>(
                        AssetBuilderConfig.instance.uiDataStorePath + "PanelData/TestCustomAnimationListPanel.asset");
                }
                return _instance;
            }
        }


        public void SetPanelData(CharacterInfo data, List<string> animClipNames)
        {
            characterInfo = data;
            
            animDatas.Clear();
            animClipNames.Insert(0, "StopAnimation");
            animDatas.Clear();
            for (int i = 0; i < animClipNames.Count; i++)
            {
                animDatas.Add(new AnimationTestCellData()
                {
                    isBaseAnim = true,
                    isStopBtn = i == 0,
                    name = animClipNames[i]
                });
            }
            
            animList.Refresh();
        }

#region base function

        protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
        {
            base.BuildUIDOM(parent);

            InitElements();
                
            return true;
        }
        
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            animList.OnDestroy();
            _instance = null;
        }
#endregion

#region 私有函数


        private void InitElements()
        {
            var animListSV = mainElement.Q<ScrollView>("animList");
            animList = new PavScrollView(this, animListSV);
            animList.Direction = ScrollViewDirection.Vertical;
            animList.WrapMode = Wrap.NoWrap;
            
            animList.CellCount = CellCount;
            animList.CellAtIndex = CellAtIndex;
            animList.DataAtIndex = DataAtIndex;
        }
        
        

        private int CellCount()
        {
            return animDatas.Count;
        }
        
        private Type CellAtIndex(int index)
        {
            return typeof(AnimtionTestCell);
        }

        private PavScrollViewCellDataBase DataAtIndex(int index)
        {
            return animDatas[index];
        }
#endregion

        public bool CheckNavShowWarningWhenSelfIsShow()
        {
            return false;
        }

        public void OnRefresh()
        {
            
        }
        
        public bool IsRefreshVisible()
        {
            return false;
        }
    }
}
#endif