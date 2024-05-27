#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using UnityEngine;

namespace Pico.AvatarAssetBuilder
{
    internal static class ALogWrapper
    {
        /// <summary>
        /// 创建Alog实例
        /// </summary>
        /// <param name="level">日志等级，小于此级别的日志不输出</param>
        /// <param name="sysLog">是否输出到系统中</param>
        /// <param name="instanceName">alog实例名</param>
        /// <param name="logFileDir">日志输出目录</param>
        /// <param name="logFileSizeEach">每个日志文件的大小</param>
        /// <param name="logFileSizeTotal">所有日志文件的大小</param>
        /// <param name="logFileExpDays">日志有效时间</param>
        /// <param name="cacheFileDir">日志缓存目录</param>
        /// <param name="cacheFileSizeEach">每个缓存文件的大小</param>
        /// <param name="cacheFileSizeTotal">总缓存文件的大小</param>
        /// <param name="appVersion">应用版本号</param>
        /// <param name="mode">模式？</param>
        /// <param name="timeFormat">日志中时间格式</param>
        /// <param name="prefixFormat">日志前缀格式</param>
        /// <param name="compress">压缩算法</param>
        /// <param name="symCrypt">加密算法</param>
        /// <param name="asymCrypt">加密算法</param>
        /// <param name="serverPublicKey">加密公钥</param>
        /// <returns>Alog持有的非托管指针</returns>
        internal static FileStream CreateLog(ALogLevel level, bool sysLog, 
            string instanceName,
            string logFileDir, long logFileSizeEach, long logFileSizeTotal, uint logFileExpDays,
            string cacheFileDir, long cacheFileSizeEach, long cacheFileSizeTotal,
            string appVersion, ALogMode mode, ALogTimeFormat timeFormat, ALogPrefixFormat prefixFormat,
            ALogCompressMode compress, ALogCryptMode symCrypt, ALogTransCryptMode asymCrypt,
            string serverPublicKey)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            IntPtr alog;
            var result = ALogCXXBridge.CXX_CreateALog(out alog,
                (uint)level, sysLog ? 1 : 0, instanceName,
                logFileDir, logFileSizeEach, logFileSizeTotal, logFileExpDays,
                cacheFileDir, cacheFileSizeEach, cacheFileSizeTotal,
                appVersion, (uint)mode, (uint)timeFormat, (uint)prefixFormat,
                (byte)compress, (byte)symCrypt, (byte)asymCrypt, serverPublicKey);
            
            if (result != 0)
            {
                Debug.LogError($"create alog instance error:{result}");
            }

            return new FileStream(alog, FileAccess.ReadWrite);
#else
            if (!Directory.Exists(Application.persistentDataPath + "/Log"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/Log");
            }

            string nowTime = DateTime.Now.Date.ToString("yyyy-MM-dd").Replace(" ", "_").
                Replace("/", "_").Replace(":", "_");
            FileInfo fileInfo = new FileInfo(Application.persistentDataPath + "/Log/" + nowTime + "_Log.txt");
            return fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
#endif
        }

        /// <summary>
        /// 销毁alog实例
        /// </summary>
        /// <param name="fs">alog持有的文件流</param>
        internal static void DestroyLog(FileStream fs)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_DestroyALog(out ptr);
#else
            fs.Close();
#endif
        }

        /// <summary>
        /// 输出日志（同步）
        /// </summary>
        /// <param name="ptr">alog实例</param>
        /// <param name="level">日志等级</param>
        /// <param name="tag">日志标签</param>
        /// <param name="content">日志内容</param>
        internal static void WriteLog(FileStream fs, ALogLevel level, string tag, string content)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_WriteALog(ptr, (uint)level, tag, content);
#else
            var encoding = new UTF8Encoding();
            fs.Write(encoding.GetBytes(content), 0, encoding.GetByteCount(content));
            fs.Flush();
#endif
        }

        /// <summary>
        /// 输出日志（同步）
        /// </summary>
        /// <param name="ptr">alog实例</param>
        /// <param name="level">日志等级</param>
        /// <param name="tag">日志标签</param>
        /// <param name="content">日志内容</param>
        internal static void WriteLog(FileStream fs, ALogLevel level, string tag, string content, string  stackTrace)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_WriteALog(ptr, (uint)level, tag, content);
#else
            string fullContent = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "【" + level + "】" + ": " + content + Environment.NewLine
                                 + stackTrace + Environment.NewLine + Environment.NewLine;
            if (fs.CanWrite)
            {
                fs.Position = fs.Length;
                fs.Write(Encoding.UTF8.GetBytes(fullContent), 0, Encoding.UTF8.GetByteCount(fullContent));
                fs.Flush();
            }
#endif
        }
        
        /// <summary>
        /// 输出日志（异步）
        /// </summary>
        /// <param name="ptr">alog实例</param>
        /// <param name="level">日志等级</param>
        /// <param name="tag">日志标签</param>
        /// <param name="content">日志内容</param>
        /// <param name="pid">进程id</param>
        /// <param name="timestampMs">日志时间戳</param>
        internal static void WriteLogAsync(IntPtr ptr, ALogLevel level, string tag, string content, int pid, ulong timestampMs)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            ALogCXXBridge.CXX_WriteALogAsync(ptr, (uint)level, tag, content, pid, timestampMs);
#endif
        }

        /// <summary>
        /// 立刻写入所有日志到文件（同步）
        /// </summary>
        /// <param name="fs">alog持有文件流</param>
        internal static void FlushLog(FileStream fs)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_ALogSyncFlush(ptr);
#endif
        }

        /// <summary>
        /// 立刻写入所有日志到文件（异步）
        /// <param name="fs">alog持有文件流<</param>
        /// </summary>
        internal static void FlushLogAsync(FileStream fs)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_ALogAsyncFlush(ptr);
#endif
        }

        /// <summary>
        /// 设置实例的有效日志级别
        /// </summary>
        /// <param name="fs">alog 持有的文件流</param>
        /// <param name="level">日志等级</param>
        internal static void SetLogLevel(FileStream fs, ALogLevel level)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_ALogSetLevel(ptr, (uint)level);
#endif
        }

        /// <summary>
        /// 是否输出日志到系统
        /// </summary>
        /// <param name="ptr">alog 持有的文件流</param>
        /// <param name="syslog">是否开启</param>
        internal static void SetSyslog(FileStream fs, bool syslog)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var ptr = fs.SafeFileHandle.DangerousGetHandle();
            ALogCXXBridge.CXX_ALogSetSyslog(ptr, syslog ? 1 : 0);
#endif
        }
    }
}
#endif