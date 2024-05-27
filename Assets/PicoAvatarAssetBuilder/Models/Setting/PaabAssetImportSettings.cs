#if UNITY_EDITOR
using Pico.Avatar;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetBuilder
    {

        public enum AssetStatus
        {
            Uncheck,
            Ready,
        }
        
        public enum OperationType
        {
            Create = 0,
            Update,
            CreateAsset,
            UpdateAsset
        };
        

        /**
         * @brief Settings used to import asset. composed with one more AssetImportSetting.
         */
        public class PaabAssetImportSettings : ScriptableObject
        {
            #region Public Types
            // listener for the object. AssetImportPanel may need to be notified with destroy event.
            public interface Listener
            {
                void OnAssetImportSettingsDestroy(PaabAssetImportSettings target);
            }
            #endregion

            #region Public Properties

            // asset import settings type.
            public AssetImportSettingsType assetImportSettingsType { get => _assetImportSettingsType; }

            // asset name in basicInfoSetting
            public string assetName { get => (basicInfoSetting == null ? "" : basicInfoSetting.assetName); }

            // listener for destroy event.
            public Listener listener { get; set; }
            
            // asset status
            public AssetStatus assetStatus
            {
                get
                {
                    return _assetStatus;
                }

                set
                {
                    _assetStatus = value;
                }
            }
            
            public OperationType opType { get; set; }

            // Basic and common information of the asset.
            [SerializeField]
            public PaabBasicInfoImportSetting basicInfoSetting;

            [SerializeField]
            public PaabAssetImportSetting[] settingItems;

#endregion


#region Public Methods

            /**
             * Sets asset type name.
             */
            public void SetAssetTypeName(AssetImportSettingsType assetTypeName) { _assetImportSettingsType = assetTypeName; }

            /**
             * @brief Gets import setting with the type.
             */
            public T GetImportSetting<T>(bool createIfNull) where T : PaabAssetImportSetting, new()
            {
                // basic info is especial field.
                if (typeof(T) == typeof(PaabBasicInfoImportSetting))
                {
                    if (basicInfoSetting == null && createIfNull)
                    {
                        basicInfoSetting = new PaabBasicInfoImportSetting();
                    }
                    return basicInfoSetting as T;
                }

                // find in setting items.
                if (settingItems != null)
                {
                    foreach (var x in settingItems)
                    {
                        var obj = x as T;
                        if (obj != null)
                        {
                            return obj;
                        }
                    }
                }
                //
                if (createIfNull)
                {
                    int count = settingItems == null ? 0 : settingItems.Length;
                    System.Array.Resize<PaabAssetImportSetting>(ref settingItems, count + 1);
                    // create instance.
                    var obj = new T();
                    settingItems[count] = obj;
                    return obj;
                }

                return null;
            }


            /**
             * @brief Build from json text.
             */
            public void FromJsonText(string jsonText)
            {
                //
                try
                {
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);
                    if (basicInfoSetting == null)
                    {
                        basicInfoSetting = new PaabBasicInfoImportSetting();
                    }
                    basicInfoSetting.FromJsonObject(jsonObject);

                    // TODO: parse other

                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError(e.Message);
                }
            }

            /**
             * @brief build json text.
             */
            public string ToJsonText()
            {
                var jsonObj = new Dictionary<string, object>();

                if (basicInfoSetting != null)
                {
                    basicInfoSetting.ToJsonObject(jsonObj);
                }
                // add other settin items.
                if (settingItems != null)
                {
                    foreach (var x in settingItems)
                    {
                        x.ToJsonObject(jsonObj);
                    }
                }

                return JsonConvert.SerializeObject(jsonObj);
            }

            /**
             * @brief Fill asset meta proto json object. Derived class SHOULD override and invoke the method.
             */
            public void FillAssetMetaProtoJsonObject(Dictionary<string, object> assetMetaJsonObject)
            {
                if(basicInfoSetting == null || settingItems == null)
                {
                    return;
                }

                // fill basic info first.
                basicInfoSetting.FillAssetMetaProtoJsonObject(assetMetaJsonObject, this);

                // fill meta proto for other setting items
                foreach(var x in settingItems)
                {
                    x.FillAssetMetaProtoJsonObject(assetMetaJsonObject, this);
                }
            }

            /**
             * @brief When destroyed, should notify owner.
             */
            private void OnDestroy()
            {
                if(listener != null)
                {
                    listener.OnAssetImportSettingsDestroy(this);
                }
                //UnityEngine.Debug.Log("PaabAssetImportSetting destroyed.");
            }
            #endregion


            #region Private Fields

            // Basic and common information of the asset.
            [SerializeField]
            private AssetImportSettingsType _assetImportSettingsType;

            private AssetStatus _assetStatus = AssetStatus.Uncheck;

#endregion

        }
    }
}
#endif