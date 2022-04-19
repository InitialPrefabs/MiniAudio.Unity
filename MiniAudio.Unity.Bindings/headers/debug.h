#ifndef MINIAUDIO_UNITY_BINDINGS_DEBUG_H
#define MINIAUDIO_UNITY_BINDINGS_DEBUG_H

#ifdef DEBUG_EXPORTS
#define DEBUG_API __declspec(dllexport)
#else
#define DEBUG_API __declspec(dllimport)
#endif

extern "C" {

typedef void(*log_info_ptr)(const char* message);  // static log_info_ptr debug_log;
typedef void(*log_warn_ptr)(const char* message);  // static log_warn_ptr debug_warn;
typedef void(*log_error_ptr)(const char* message); // static log_error_ptr debug_error;

// Hook this in with Debug.Log from Unity.
DEBUG_API void InitializeLogger(log_info_ptr log_ptr, log_warn_ptr warn_ptr, log_error_ptr error_ptr);
};

#endif //MINIAUDIO_UNITY_BINDINGS_DEBUG_H
