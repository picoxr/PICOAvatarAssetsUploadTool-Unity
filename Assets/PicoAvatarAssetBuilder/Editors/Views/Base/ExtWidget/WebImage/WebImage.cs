#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Pico.AvatarAssetBuilder
{
    public enum ImageFileExtension
    {
        None = 0,
        JPG = 255216,
        GIF = 7173,
        PNG = 13780,
    }

    public enum WebImageStatus
    {
        Empty,
        Loading,
        Success,
        ExtensionNotMatch,
        Failed,
    }
    
    public class WebImage
    {
        public Action<bool> onTextureLoad;
        private string url;
        private UnityWebRequest request;
        private VisualElement img;
        private ImageFileExtension fileTypeToCheck;
        private Texture2D tex;
        private float aspect = -1;
        private int rawFileSize = 0;

        public string URL => url;
        public Texture2D texture => tex;

        public WebImageStatus status
        {
            get;
            private set;
        } = WebImageStatus.Empty;
        
        public int textureFileSize => rawFileSize;

        public WebImage(VisualElement img)
        {
            this.img = img;
        }

        public void OnDestroy()
        {
            Cancel();
        }

        public void SetAspect(float aspect)
        {
            this.aspect = aspect;
            if (status == WebImageStatus.Success)
            {
                var newTex = ProcessTextureWithAspect(tex);
                if (newTex != tex)
                {
                    GameObject.DestroyImmediate(tex);
                    tex = newTex;
                    tex.hideFlags = HideFlags.DontSaveInEditor;
                    img.style.backgroundImage = tex;
                }
            }
        }

        public void SetActive(bool value)
        {
            img.SetActive(value);
        }

        public void SetTexture(string url, ImageFileExtension fileType = ImageFileExtension.None)
        {
            ClearTexture();
            this.url = url;
            fileTypeToCheck = fileType;
            LoadTextureByUrl(this.url);
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(url);
        }

        public void ClearTexture()
        {
            Cancel();
            img.style.backgroundImage = null;
        }

        private Texture2D ProcessTextureWithAspect(Texture2D sourceTex)
        {
            if (sourceTex == null)
                return null;
            
            if (aspect == -1)
                return sourceTex;
            
            var newTex = UIUtils.ClipTextureWithAspect(sourceTex, aspect);
            return newTex;
        }

        private void Cancel()
        {
            //Debug.Log($"WebImage Canceled : url[{url}], status[{status}]");
            url = "";
            if (request != null)
            {
                request.Abort();
                request.Dispose();
            }
            
            if (tex != null)
            {
                GameObject.DestroyImmediate(tex);
                tex = null;
            }
            
            request = null;
            status = WebImageStatus.Empty;
            rawFileSize = 0;
        }
        
        public void LoadTextureByUrl(string url, int timeOut = 10)
        {
            request = UnityWebRequestTexture.GetTexture(url);
            request.timeout = timeOut;
            var handler = request.SendWebRequest();
            status = WebImageStatus.Loading;
            handler.completed += operation =>
            {
                if (string.IsNullOrEmpty(URL) || request == null)
                    return;

                if (request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    status = WebImageStatus.ExtensionNotMatch;
                    //Debug.LogWarning($"Load image {url} error : DataProcessingError");
                    onTextureLoad?.Invoke(false);
                }
                else if (request.result != UnityWebRequest.Result.Success)
                {
                    status = WebImageStatus.Failed;
                    //Debug.LogWarning($"Load image {url} error [{request.result}] : {request.error}");
                    onTextureLoad?.Invoke(false);
                }
                else
                {
                    if (fileTypeToCheck != ImageFileExtension.None)
                    {
                        string fileType = "";
                        if (request.downloadHandler.data.Length < 2)
                        {
                            status = WebImageStatus.ExtensionNotMatch;
                            Debug.LogError($"Load image {url} error : data length = {request.downloadHandler.data.Length}");
                            onTextureLoad?.Invoke(false);
                            return;
                        }

                        fileType = request.downloadHandler.data[0].ToString() + request.downloadHandler.data[1].ToString();

                        if (((int)fileTypeToCheck).ToString() != fileType)
                        {
                            status = WebImageStatus.ExtensionNotMatch;
                            Debug.LogError($"Load image {url} error : file type [{((int)fileTypeToCheck).ToString()} : {fileType}] not match");
                            onTextureLoad?.Invoke(false);
                            return;
                        }
                    }
                    
                    if (tex != null)
                        GameObject.DestroyImmediate(tex);
                    
                    var rawTex = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    tex = ProcessTextureWithAspect(rawTex);
                    if (tex == null)
                    {
                        status = WebImageStatus.Failed;
                        Debug.LogError($"Load image {url} failed");
                        return;
                    }

                    rawFileSize = request.downloadHandler.data.Length;
                    tex.hideFlags = HideFlags.DontSaveInEditor;
                    status = WebImageStatus.Success;
                    img.style.backgroundImage = tex;
                    request = null;
                    onTextureLoad?.Invoke(true);
                }
            };
        }
    }
}
#endif