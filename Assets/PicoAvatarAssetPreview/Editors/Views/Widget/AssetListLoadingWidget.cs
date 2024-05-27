#if UNITY_EDITOR
using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico.AvatarAssetPreview
{
    public class AssetListLoadingWidget : PavWidget
    {
        private VisualElement loadingIcon;
        private float rotation = 0;
#if UNITY_EDITOR
        private EditorCoroutine animCoroutine;
#endif
        private DateTime dt;
        private float autoHideTimeOut = 15f;
        private float timer = 0;
        
        public AssetListLoadingWidget(VisualElement ve) : base(ve)
        {
            ve.pickingMode = PickingMode.Ignore;
        }
        
        
        public AssetListLoadingWidget(VisualElement ve, float timeOut) : base(ve)
        {
            ve.pickingMode = PickingMode.Ignore;
            autoHideTimeOut = timeOut;
        }
        
        public override string uxmlPathName { get => "UxmlWidget/AssetListLoading.uxml"; }

        public override VisualElement BuildUIDOM()
        {
            var root = base.BuildUIDOM();
            loadingIcon = root.Q("loadingIcon");
            BeginRotate();
            
            return root;
        }

        public void ShowLoading()
        {
            timer = 0;
            loadingIcon.SetVisibility(true);
        }

        public void HideLoading()
        {
            timer = 0;
            loadingIcon.SetVisibility(false);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (animCoroutine!= null)
                EditorCoroutineUtility.StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        private void BeginRotate()
        {
            //if (!Application.isPlaying)
            {
                if (animCoroutine == null)
                {
                    timer = 0;
                    dt = DateTime.Now;
                    animCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(EditorRotate());
                }
            }
        }

        IEnumerator EditorRotate()
        {
            while (true)
            {
                float deltaTime = (float)((DateTime.Now - dt).TotalMilliseconds) / 1000f;
                dt = DateTime.Now;
                rotation += deltaTime * 600;
                rotation %= 360;
                loadingIcon.style.rotate = new Rotate(rotation);

                if (autoHideTimeOut > 0 && mainElement.IsVisible())
                {
                    timer += deltaTime;
                    if (timer >= autoHideTimeOut)
                        HideLoading();
                }
                yield return null;
            }
        }
    }
}
#endif