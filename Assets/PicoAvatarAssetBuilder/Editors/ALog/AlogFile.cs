#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetBuilder
    {
        public class ALogFile
    {
        private ALog _alog;
        private long _currentTime;

        private static ALogFile _instance;
        private static readonly object syslock = new object();
        public bool enableStackTrace = false;
        private bool _init = false;
        public static ALogFile Instance
        {
            get
            {
                lock (syslock)
                {
                    if (_instance == null)
                    {
                        _instance = new();
                    }
                }

                return _instance;
            }
        }
        public void Init()
        {
            if (_init) return;
            _init = true;
            var alogLevel = LogLevel2ALogLevel(0);

#if BUILD_TYPE_DEVELOPMENT || DEVELOPMENT_BUILD || true
            var syslog = true;
            var compress = false;
#else
            var syslog = false;
            var compress = true;
#endif
            _currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 2000;
            _alog = ALog.CreateLog("PAAP", GenerateVersionInfo(), alogLevel, syslog, compress, true);
            
            Application.logMessageReceived += HandleLog;
            if (null == _alog) return;
        }

        public void UnInit()
        {
            _init = false;
            Application.logMessageReceived -= HandleLog;
            _alog?.Destroy();
            _alog = null;
        }
        

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (enableStackTrace)
            {
                OnLog(LogLevel2ALogLevel(type), logString, stackTrace);
            }
            else
            {
                OnLog(LogLevel2ALogLevel(type), logString);
            }
          
        }

        private void OnLog(ALogLevel level, string message)
        {
            _alog?.Log(level, "paap", message);
        }
        
        private void OnLog(ALogLevel level, string message, string stackTrace)
        {
            _alog?.Log(level, "paap", message, stackTrace);
        }
        
        private ALogLevel LogLevel2ALogLevel(LogType level)
        {
            var alogLevel = ALogLevel.None;
            switch (level)
            {
                case LogType.Error:
                    alogLevel = ALogLevel.Error;
                    break;
                case LogType.Assert:
                    alogLevel = ALogLevel.Verbose;
                    break;
                case LogType.Warning:
                    alogLevel = ALogLevel.Warn;
                    break;
                case LogType.Log:
                    alogLevel = ALogLevel.Info;
                    break;
                case LogType.Exception:
                    alogLevel = ALogLevel.Debug;
                    break;
            }

            return alogLevel;
        }
        
        private string GenerateVersionInfo()
        {
            return "1.0.0";
        }
        
        public void UploadLogs()
        {
            ALog.UploadFiles(0, _currentTime);
        }
    }
    }
}
#endif