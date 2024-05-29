#if UNITY_EDITOR
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class DeveloperUploadToolLoginPanel : PavPanel
        {
            private const string TokenKey = "token";
            private const string AppidKey = "appid";

            public override string displayName
            {
                get => "DeveloperUploadToolLoginPanel";
            }
            public override string panelName
            {
                get => "DeveloperUploadToolLoginPanel";
            }
            public override string uxmlPathName
            {
                get => "Uxml/DeveloperUploadToolLoginPanel.uxml";
            }

            private static DeveloperUploadToolLoginPanel _instance;

            private HttpListener _listener = null;
            private CommonHttpRequest _loginRequest = null;

            private Button _chinaMainlandBtn;
            private Button _otherRegionsBtn;
            private Button _moreInfoBtn;
            private Button _loginBtn;
            private VisualElement _loginBg;
            private LabelAutoFit _titleNameLabel;
            
            private AssetServerType _currType = AssetServerType.China;

            private int _listernPort = 8080;
            private string _listernAdress = AssetServerProConfig.LocalListernAdress;
            private string _webLoginUrl = String.Empty;
            private readonly Color _regionSelectStyle = new(88/255f, 88/255f, 88/255f, 1);
            private readonly Color _regionUnSelectStyle = new(88/255f, 88/255f, 88/255f, 0);

            private const string ActiveStyle = "login__button--active";
            private const string NormalStyle = "login__button";
            
            private HttpListenerContext _context;
            
            private const string DocsUrl_CN = "https://developer-cn.picoxr.com/document/unity-avatar-uploader/get-started-with-avatar-uploader/";
            private const string DocsUrl_OVERSEA = "https://developer.picoxr.com/document/unity-avatar-uploader/get-started-with-avatar-uploader/";
            // 
            public static DeveloperUploadToolLoginPanel instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = Utils.LoadOrCreateAsset<DeveloperUploadToolLoginPanel>(
                            AssetBuilderConfig.instance.uiDataStorePath +
                            "PanelData/DeveloperUploadToolLoginPanel.asset");
                    }

                    return _instance;
                }
            }
            // whether DeveloperUploadToolLoginPanel is created.
            public static bool isValid => _instance != null;

            protected override bool BuildUIDOM(VisualElement parent) //SetVisualElements and BuildWithUxml
            {
                base.BuildUIDOM(parent);
                if (mainElement != null)
                {
                    _loginBtn = mainElement.Q<Button>("Login_Login_Entry");
                    _chinaMainlandBtn = mainElement.Q<Button>("Login_Region_China");
                    _otherRegionsBtn = mainElement.Q<Button>("Login_Region_Other");
                    _moreInfoBtn = mainElement.Q<Button>("Login_Info_More");
                    _titleNameLabel = mainElement.Q<LabelAutoFit>("Node-Title-Name");
                    _loginBg = mainElement.Q<VisualElement>("Node-Scale-Fit");
                }

                return true;
            }

            private bool _lastScaleFit = false;
            public bool ChangeScaleMode(bool scaleFit)
            {
                if (_loginBg == null || _lastScaleFit == scaleFit)
                    return false;
                _lastScaleFit = scaleFit;
                _loginBg.style.unityBackgroundScaleMode = scaleFit
                    ? new StyleEnum<ScaleMode>(ScaleMode.ScaleToFit)
                    : new StyleEnum<ScaleMode>(ScaleMode.ScaleAndCrop);
                return true;
            }

            protected override bool BindUIActions()
            {
                _currType = AssetServerType.China;
                _loginBtn.clicked += () =>
                {
                    /*if (_listener!= null && _listener.IsListening)
                    {
                        Debug.Log("alread listening, please goto web finish login process!");
                        return;
                    }*/
                    _listernPort = GetRandomUnusedPort();
                    //because cannot get web showing status, so allow relogin
                    StartListenWebResponse();

                    LoginUtils.SaveServerType(_currType);

                    AssetServerManager.instance.OpenDeveloperLoginWebsite(_listernPort);
                };
                _chinaMainlandBtn.clicked += () =>
                {
                    _chinaMainlandBtn.RemoveFromClassList(NormalStyle);
                    _chinaMainlandBtn.AddToClassList(ActiveStyle);
                    _otherRegionsBtn.RemoveFromClassList(ActiveStyle);
                    _otherRegionsBtn.AddToClassList(NormalStyle);
                    _currType = AssetServerType.China;
                };
                _otherRegionsBtn.clicked += () =>
                {
                    _otherRegionsBtn.RemoveFromClassList(NormalStyle);
                    _otherRegionsBtn.AddToClassList(ActiveStyle);
                    _chinaMainlandBtn.RemoveFromClassList(ActiveStyle);
                    _chinaMainlandBtn.AddToClassList(NormalStyle);
                    _currType = AssetServerType.Global;
                };
                _moreInfoBtn.clicked += () =>
                {
                    if (_currType == AssetServerType.China)
                    {
                        Application.OpenURL(DocsUrl_CN);
                    }
                    else
                    {
                        Application.OpenURL(DocsUrl_OVERSEA);
                    }
                   
                };
                return base.BindUIActions();
            }


            async void StartListenWebResponse()
            {
                if (_listener != null)
                {
                    _listener.Stop();
                }

                _listener = new HttpListener();

                _webLoginUrl = string.Format(_listernAdress, _listernPort);
                _listener.Prefixes.Add(_webLoginUrl);
                _listener.Start();
                _listener.BeginGetContext((result) =>
                {
                    if (_listener is { IsListening: true } && result.IsCompleted)
                    {
                        _context = _listener.EndGetContext(result);
                    }
                }, _listener);
            }

            public int GetRandomUnusedPort()
            {
                var tcpListener = new TcpListener(IPAddress.Any, 0);
                tcpListener.Start();
                var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
                tcpListener.Stop();
                return port;
            }


            private float _webReqInterval = 0f;
            public override void OnUpdate()
            {
                base.OnUpdate();
                if (_webReqInterval > 0)
                {
                    _webReqInterval += Time.deltaTime;
                    if (_webReqInterval > 0.2f)
                    {
                        _webReqInterval = 0;
                        _listener.BeginGetContext((result) =>
                        {
                            if (_listener is { IsListening: true } && result.IsCompleted)
                            {
                                _context = _listener.EndGetContext(result);
                                _context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                                _context.Response.AddHeader("Access-Control-Allow-Headers", "*");
                                _context.Response.AddHeader("Access-Control-Allow-Methods", "PUT,POST,GET,DELETE,OPTIONS");
                                _context.Response.AddHeader("Access-Control-Expose-Headers", "*");
                                _context.Response.Close();
                                _listener.Stop();
                            }
                        }, _listener);
                    }
                }
              
                if (_context != null)
                {
                    _context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                    _context.Response.AddHeader("Access-Control-Allow-Headers", "*");
                    _context.Response.AddHeader("Access-Control-Allow-Methods", "PUT,POST,GET,DELETE,OPTIONS");
                    _context.Response.AddHeader("Access-Control-Expose-Headers", "*");
                    if (_context.Request.HttpMethod.Equals("OPTIONS"))
                    {
                        _webReqInterval += Time.deltaTime;
                    }
                    
                    if (_context.Response.StatusCode == 200 && _context.Request.QueryString.Count > 1)
                    {
                        if (_context.Request.HttpMethod.Equals("GET"))
                        {
                            LoginUtils.SaveLoginToken(_context.Request.QueryString[TokenKey],
                                _context.Request.QueryString[AppidKey]);
                            if (MainMenuUIManager.instance.PAAB_OPEN == true)
                            {
                                MainWindow.SwitchWindow();
                            }
                            else
                            {
                                // MainRuntimeWindow.ShowMainWindow();
                            }
                        }
                        _context.Response.Close();
                    }
                    _context = null;
                }
            }
            
            public override void OnDestroy()
            {
                _listener?.Stop();
                _webLoginUrl = null;
                base.OnDestroy();
                _instance = null;
            }
        }
    }
}
#endif