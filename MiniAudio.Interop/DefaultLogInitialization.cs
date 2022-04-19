using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniAudio.Interop {

    public static class DefaultLogInitialization {

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogHandler(string message);

        static LogHandler DebugLogHandler;
        static LogHandler DebugWarnHandler;
        static LogHandler DebugErrorHandler;

        static IntPtr logFunctionPtr;
        static IntPtr warnFunctionPtr;
        static IntPtr errorFunctionPtr;

        static void Log(string msg) {
            Debug.Log(msg);
        }

        static void Warn(string warn) {
            Debug.LogWarning(warn);
        }

        static void Error(string error) {
            Debug.LogError(error);
        }

        public static void InitializeLibrary() {
#if UNITY_EDITOR_WIN && MINIAUDIO_DEVELOP
            InitHandler = LibraryHandler.GetDelegate<LoggerInitializationHandler>(ConstantImports.MiniAudioHandle, "InitializeLogger");
#endif
            DebugLogHandler = Log;
            DebugWarnHandler = Warn;
            DebugErrorHandler = Error;

            logFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugLogHandler);
            warnFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugWarnHandler);
            errorFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugErrorHandler);
            InitializeLogger(logFunctionPtr, warnFunctionPtr, errorFunctionPtr);
        }

#if UNITY_EDITOR_WIN && MINIAUDIO_DEVELOP
        public delegate void LoggerInitializationHandler(IntPtr log, IntPtr warn, IntPtr error);
        static LoggerInitializationHandler InitHandler;

        static void InitializeLogger(IntPtr log, IntPtr warn, IntPtr error) {
            InitHandler?.Invoke(log, warn, error);
        }
#else
        [DllImport("MiniAudio_Unity_Bindings.dll")]
        static extern void InitializeLogger(IntPtr log, IntPtr warn, IntPtr error);
#endif
    }
}
