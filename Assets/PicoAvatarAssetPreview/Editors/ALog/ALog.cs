#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Pico.AvatarAssetPreview{
    public class ALog : IDisposable
    {
#region const field

        private const string NEVERDEFINED = "NEVER_DEFINED";
        
        private const long DEFAULT_LOG_FILE_SIZE = 20 * 1024 * 1024;
        private const long DEFAULT_LOG_FILE_SIZE_TOTAL = 200 * 1024 * 1024;
        private const uint DEFAULT_LOG_FILE_EXPIRED_DAY = 14;
        private const long DEFAULT_CACHE_FILE_SIZE = 65536;
        private const long DEFAULT_CACHE_FILE_SIZE_TOTAL = 196608;
        private const string DEFAULT_PUBLIC_KEY = "fecbb32b759120b672045f74edc41d159b6a426ffc863b9e0be9ad4be12824546f549959b838993a430344f15197221e87bd362298814c75f5068148b980306f";
        private const string TAG = "PicoAvatarAssetsOpenPlatform";

        private const bool ENABLE_CLOUD = false;
        
#endregion

#region fields

        private FileStream _alogRef;
        private ALogItemPool _pool;
        private Thread _thread;
        private bool _async;
        private ConcurrentQueue<ALogItem> _items;
        private int _pid;

        private bool _stop;
        
        private bool _disposed;
        
#endregion
        
        private ALog() {}

        ~ALog()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        public void Destroy()
        {
            DestroyLog(this);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _stop = true;

                while (_items.Count > 0)
                {
                    if (_items.TryDequeue(out var item))
                    {
                        ALogWrapper.WriteLog(_alogRef, item.level, item.tag, item.msg, item.stackTrace);
                        _pool.Recycle(item);
                    }
                }

                _pool = null;
                _items = null;

                _thread = null;
            }
            
            Flush();
            
            Destroy();

            _disposed = true;
        }
        
#region static function
        
        private static bool _javaInit;
        private static AndroidJavaClass _alogContext;
        
        private static ALog _instance;
        public static ALog Instance
        {
            get
            {
                if (null != _instance) return _instance;

                _instance = CreateLog("paap_single", Application.version, ALogLevel.Info, false, true, true);

                return _instance;
            }
        }
        
        // 主要是注册一个回捞日志的回调
        public static void InitJava()
        {
            if (_javaInit)
            {
                return;
            }
            
            _javaInit = true;
#if !UNITY_EDITOR && UNITY_ANDROID
            _alogContext = new AndroidJavaClass("com.bytedance.picoworlds.alog.ALogService");
            var unityContext =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            _javaInit = _alogContext.CallStatic<bool>("initALog", unityContext, ENABLE_CLOUD);
#endif
        }
        
        public static ALog CreateLog(string name, string versionInfo, ALogLevel level, bool syslog, bool compress = true, bool async = false)
        {
            InitJava();

            var logFileDir = System.IO.Path.Combine(Application.persistentDataPath, "logs");
            var cacheFileDir = System.IO.Path.Combine(Application.persistentDataPath, "ALOG");
            
            var alog = ALogWrapper.CreateLog(level, syslog, name,
                logFileDir, DEFAULT_LOG_FILE_SIZE, DEFAULT_LOG_FILE_SIZE_TOTAL, DEFAULT_LOG_FILE_EXPIRED_DAY,
                cacheFileDir, DEFAULT_CACHE_FILE_SIZE, DEFAULT_CACHE_FILE_SIZE_TOTAL,
                versionInfo, ALogMode.Safe, compress ? ALogTimeFormat.RAW : ALogTimeFormat.ISO_8601, ALogPrefixFormat.Legacy,
                compress ? ALogCompressMode.ZSTD : ALogCompressMode.NONE, 
                compress ? ALogCryptMode.TEA16 : ALogCryptMode.NONE,
                compress ? ALogTransCryptMode.SECP256K1 : ALogTransCryptMode.NONE,
                DEFAULT_PUBLIC_KEY);
            
            if (alog == default) return null;

            var log = new ALog();
            log._alogRef = alog;
            log._async = async;
            log._pid = Thread.CurrentThread.ManagedThreadId;
            
            if (async)
            {
                var items = new ConcurrentQueue<ALogItem>();
                log._items = items;

                var pool = new ALogItemPool();
                log._pool = pool;
                
                var thread = new Thread(log.Loop);
                thread.Name = "PicoAvatarAssetsOpenPlatform_Alog";
                thread.Start();
                
                log._thread = thread;
            }
            return log;
        }

        public static void DestroyLog(ALog alog)
        {
            if (null == alog) return;
            alog._stop = true;
            
            ALogWrapper.DestroyLog(alog._alogRef);
            alog = null;
        }
        
#endregion
        
#region setting

        public void SetLogLevel(ALogLevel logLevel)
        {
            ALogWrapper.SetLogLevel(_alogRef, logLevel);
        }

        public void EnableSyslog(bool enable)
        {
            ALogWrapper.SetSyslog(_alogRef, enable);
        }

#endregion

#region log out

        private void Loop()
        {
            while (!_stop)
            {
                if (_items.TryDequeue(out var item))
                {
                    //ALogWrapper.WriteLogAsync(_alogRef, item.level, item.tag, item.msg, item.tid, item.timestamp);
                    ALogWrapper.WriteLog(_alogRef, item.level, item.tag, item.msg, item.stackTrace);
                    _pool.Recycle(item);
                }
            }
        }
        
        private void PushLog(ALogLevel logLevel, string tag, string content)
        {
            var item = _pool.Obtain();
            item.msg = content;
            item.level = logLevel;
            item.tag = tag;
            item.tid = Thread.CurrentThread.ManagedThreadId;
            item.timestamp = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            _items.Enqueue(item);
        }
        
        private void PushLog(ALogLevel logLevel, string tag, string content, string stackTrace)
        {
            var item = _pool.Obtain();
            item.msg = content;
            item.stackTrace = stackTrace;
            item.level = logLevel;
            item.tag = tag;
            item.tid = Thread.CurrentThread.ManagedThreadId;
            item.timestamp = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            _items.Enqueue(item);
        }

#if !LOG_TRACE
		[Conditional(NEVERDEFINED)]
#endif
        public void V(string content)
        {
            Log(ALogLevel.Verbose, content);
        }

#if (!LOG_TRACE && !LOG_DEBUG)
		[Conditional(NEVERDEFINED)]
#endif
        public void D(string content)
        {
            Log(ALogLevel.Debug, content);
        }

#if !LOG_TRACE && !LOG_DEBUG && !LOG_INFO
		[Conditional(NEVERDEFINED)]
#endif
        public void I(string content)
        {
            Log(ALogLevel.Info, content);
        }
        
#if !LOG_TRACE && !LOG_DEBUG && !LOG_INFO && !LOG_WARNING
		[Conditional(NEVERDEFINED)]
#endif
        public void W(string content)
        {
            Log(ALogLevel.Warn, content);
        }
        
#if !LOG_TRACE && !LOG_DEBUG && !LOG_INFO && !LOG_WARNING && !LOG_ERROR
		[Conditional(NEVERDEFINED)]
#endif
        public void E(string content)
        {
            Log(ALogLevel.Error, content);
        }

#if !LOG_TRACE && !LOG_DEBUG && !LOG_INFO && !LOG_WARNING && !LOG_ERROR && !LOG_CRITICAL
		[Conditional(NEVERDEFINED)]
#endif
        public void F(string content)
        {
            Log(ALogLevel.Fatal, content);
        }

        public void LogFull(ALogLevel logLevel, string content, string stackTrace, bool showTag)
        {
            Log(logLevel, TAG, content, stackTrace);
        }

        public void Log(ALogLevel logLevel, string content)
        {
            Log(logLevel, TAG, content);
        }
        public void Log(ALogLevel logLevel, string tag, string content)
        {
            if (!_async)
            {
                ALogWrapper.WriteLog(_alogRef, logLevel, tag, content);
            }
            else
            {
                PushLog(logLevel, tag, content);
            }
        }

        public void Log(ALogLevel logLevel, string tag, string content, string stackTrace)
        {
            if (!_async)
            {
                ALogWrapper.WriteLog(_alogRef, logLevel, tag, content, stackTrace);
            }
            else
            {
                PushLog(logLevel, tag, content, stackTrace);
            }
        }
        
        private void FlushPool()
        {
            while (_items.TryDequeue(out var item))
            {
                ALogWrapper.WriteLog(_alogRef, item.level, item.tag, item.msg, item.stackTrace);
                _pool.Recycle(item);
            }
        }
        
        public void Flush()
        {
            FlushPool();
            ALogWrapper.FlushLog(_alogRef);
        }

        public void FlushAsync()
        {
            FlushPool();
            ALogWrapper.FlushLogAsync(_alogRef);
        }

        /// <summary>
        /// 主动上传日志文件
        /// </summary>
        /// <param name="startTime">ms</param>
        /// <param name="endTime">ms</param>
        public static void UploadFiles(long startTime, long endTime)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            _alogContext.CallStatic("UploadAlogInternal", "start", startTime, endTime);
#endif
        }

#endregion
        
    }
}
#endif