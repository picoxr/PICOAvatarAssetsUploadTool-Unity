#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Serialization;
using System;
using AssemblyCSharp.Assets.AmzAvatar.TestTools;
using Pico.Avatar;
using Pico.AvatarAssetPreview;

public enum MaterialTyple
{
    Official,
    Custom,
    None
}
namespace UIToolkitDemo
{
    // high-level manager for the various parts of the Main Menu UI. Here we use one master UXML and one UIDocument.
    // We allow the individual parts of the user interface to have separate UIDocuments if needed (but not shown in this example).
    
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuRuntimeUIManager : MonoBehaviour
    {
        
        UIDocument m_MainMenuDocument;
        public UIDocument MainMenuDocument => m_MainMenuDocument;

        VisualElement m_rootVisualElement;
        public VisualElement  RootVisualElement => m_rootVisualElement;

        VisualElement m_editVisualElement;
        public VisualElement  EditVisualElement => m_editVisualElement;
        
        VisualElement m_runTimeVisualElement;
        public VisualElement  RuntimeVisualElement => m_runTimeVisualElement;

        VisualElement m_lodVisualElement;
        public VisualElement LodVisualElement => m_lodVisualElement;
        
        DropdownField m_lodDropdownField;
        public DropdownField LodDropdownField => m_lodDropdownField;

        VisualElement m_materialTypeVisualElement;
        Button m_officialMaterialButton;
        Button m_customMaterialButton;
        private MaterialTyple m_currentMaterialType = MaterialTyple.None;

        private PAAPRuntimeManager manager;
        
        void OnEnable()
        {
            m_MainMenuDocument = GetComponent<UIDocument>();
            m_rootVisualElement = m_MainMenuDocument.rootVisualElement;
            
            m_materialTypeVisualElement = m_rootVisualElement.Q<VisualElement>("MaterialType_Element");
            m_officialMaterialButton = m_rootVisualElement.Q<Button>("OfficialMaterialButton");
            m_customMaterialButton = m_rootVisualElement.Q<Button>("CustomMaterialButton");
        }

        void Start()
        {
            Time.timeScale = 1f;
            m_editVisualElement = m_rootVisualElement.Q<VisualElement>("Edit_Element");
            m_runTimeVisualElement = m_rootVisualElement.Q<VisualElement>("RunTime_Element");
            
            MainRuntimeWindow.instance.rootVisualElement = m_editVisualElement;
            LoginRuntimeWindow.instance.rootVisualElement = m_editVisualElement;
            MainRuntimeWindow.ShowMainWindow();
            
            // find the manager.
            manager = GameObject.FindObjectOfType<PAAPRuntimeManager>();

            // lod
            m_lodVisualElement = m_rootVisualElement.Q<VisualElement>("Lod_Element");
            m_lodDropdownField = m_rootVisualElement.Q<DropdownField>("LodDropdown");
            m_lodDropdownField.RegisterValueChangedCallback((evt) =>
            {
                string selectedOption = evt.newValue;
                
                if (selectedOption == "Lod0")
                {
                    if (manager)
                    {
                        manager.currentLodLevel = AvatarLodLevel.Lod0;
                        manager.ChangeAvatarLod();
                    }
                }
                else if (selectedOption == "Lod1")
                {
                    if (manager)
                    {
                        manager.currentLodLevel = AvatarLodLevel.Lod1;
                        manager.ChangeAvatarLod();
                    }
                }
                else if (selectedOption == "Lod2")
                {
                    if (manager)
                    {
                        manager.currentLodLevel = AvatarLodLevel.Lod2;
                        manager.ChangeAvatarLod();
                    }
                }
            });
            LodVisualElement.style.display = DisplayStyle.Flex;
            
            // material Type
            m_officialMaterialButton.clicked += () =>
            {
                SwitchMaterialType(MaterialTyple.Official);
            };
            
            m_customMaterialButton.clicked += () =>
            {
                SwitchMaterialType(MaterialTyple.Custom);
            };
        }

        public void SwitchMaterialType(MaterialTyple type)
        {
            if (!manager || !manager.createAvatar || !manager.createAvatar.avatar || !manager.createAvatar.avatar.isAnyEntityReady)
            {
                return;
            }
            if (m_currentMaterialType == type)
            {
                return;
            }
            if (type == MaterialTyple.Official)
            {
                m_officialMaterialButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                m_customMaterialButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                m_currentMaterialType = MaterialTyple.Official;
                if (manager)
                {
                    manager.avatarApp.renderSettings.useCustomMaterial = false;
                    manager.ReLoadAvatarFromSpec();
                }
            }
            else if (type == MaterialTyple.Custom)
            {
                m_officialMaterialButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 0);
                m_customMaterialButton.style.backgroundColor = (Color) new Color32(61, 61, 61, 255);
                m_currentMaterialType = MaterialTyple.Custom;
                if (manager)
                {
                    manager.avatarApp.renderSettings.useCustomMaterial = true;
                    manager.ReLoadAvatarFromSpec();
                }
            }
        }

    }
}
#endif