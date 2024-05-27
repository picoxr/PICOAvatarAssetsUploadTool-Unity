#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Pico.Avatar;
using Pico.AvatarAssetPreview;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class TopNavMenuBar : PavPanel
        {
            public override string displayName
            {
                get => "TopNavMenuBar";
            }
            public override string panelName
            {
                get => "TopNavMenuBar";
            }

            public override string uxmlPathName
            {
                get => "Uxml/TopNavMenuBar.uxml";
            }

            private static TopNavMenuBar _instance;

            public static TopNavMenuBar instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<TopNavMenuBar>(
                            AssetBuilderConfig.instance.uiDataStorePath + "PanelData/TopNavMenuBar.asset");
                    }

                    return _instance;
                }
            }

            private VisualElement NavButtonGroup;
            private StyleSheet commonStyle;
            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);
                NavButtonGroup = this.mainElement.Q<GroupBox>("NavMenu_GroupBox_Item");
                commonStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/PicoAvatarAssetPreview/Assets/Uss/Common.uss");
                return true;
            }
            
            public Button OnGenerateButton(Action<string, string> click, string chanelName, string panelName)
            {
                Button navBtn = new Button();
                navBtn.text = panelName;
                navBtn.clicked += () => { click(chanelName, panelName); };
                NavButtonGroup.Add(navBtn);
                return navBtn;
            }
            
            public Button OnGenerateButtonAndArrow(Action<string, string> click, string chanelName, string displayName, string panelName, bool needLable = false)
            {
                if (needLable)
                {
                    Label arrow = new Label();
                    arrow.text = ">";
                    arrow.style.fontSize = 20;
                    arrow.style.color = new StyleColor(new Color(255, 255, 255, 0.2f));
                    arrow.style.paddingBottom = 0;
                    arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
                    NavButtonGroup.Add(arrow);
                }
                
                Button navBtn = new Button();
                navBtn.text = displayName;
                navBtn.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                navBtn.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
                navBtn.style.borderBottomWidth = 0;
                navBtn.style.borderTopWidth = 0;
                navBtn.style.borderLeftWidth = 0;
                navBtn.style.borderRightWidth = 0;
                
                
                navBtn.clicked += () =>
                {
                    click(chanelName, panelName);
                };
                navBtn.styleSheets.Add(commonStyle);
                navBtn.AddToClassList("nav__button");
                
                NavButtonGroup.Add(navBtn);
              
                return navBtn;
            }
            
            public override void OnDestroy()
            {
                NavButtonGroup.Clear();
                base.OnDestroy();
                _instance = null;
            }
        }
    }
}
#endif