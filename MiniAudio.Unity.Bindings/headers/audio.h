#ifndef MINIAUDIO_UNITY_BINDINGS_AUDIO_H
#define MINIAUDIO_UNITY_BINDINGS_AUDIO_H

#ifdef MINIAUDIO_EXPORTS
#define MINIAUDIO_API __declspec(dllexport)
#else
#define MINIAUDIO_API __declspec(dllimport)
#endif

#include "../miniaudio/miniaudio.h"
#include <cstdint>
#include <vector>

// All extern will follow UpperPascal casing to interop with C#.
// Everything else aside from classes and structs will follow snake_case.
extern "C" {

struct SoundLoadParameters {
    bool IsLooping;         // Should the sound loop over, great for music I guess
    float Volume;           // Linear volume between 0.0 to 1.0
    uint32_t StartTime;     // Where the sound will start (in ms).
    uint32_t EndTime;       // Where the sound will stop, if <= to the StartTime, the end of the clip will be used (in ms).
};

MINIAUDIO_API bool IsEngineInitialized();
MINIAUDIO_API void InitializeEngine();
MINIAUDIO_API uint32_t LoadSound(const wchar_t* path, SoundLoadParameters loadParams);
MINIAUDIO_API uint32_t UnsafeLoadSound(const wchar_t* path, uint32_t size, void* loadParameters);
MINIAUDIO_API void PlaySound(uint32_t handle);
MINIAUDIO_API void StopSound(uint32_t handle, bool rewind);
MINIAUDIO_API void SetSoundVolume(uint32_t handle, float volume);
MINIAUDIO_API bool IsSoundPlaying(uint32_t handle);
MINIAUDIO_API bool IsSoundFinished(uint32_t);
MINIAUDIO_API void ReleaseEngine();
}

/**
 * The AudioEngine will effectively store a sparse set unique paths and sounds.
 */
class AudioEngine {
public:
    AudioEngine();
    ~AudioEngine();
    size_t free_sound_count();
    uint32_t request_sound(const wchar_t* path, SoundLoadParameters load_params);
    void release_sound(uint32_t handle);
    void play_sound(uint32_t handle);
    void stop_sound(uint32_t handle, bool rewind);
    bool is_sound_playing(uint32_t handle);
	bool is_sound_finished(uint32_t handle);
	bool try_get_sound(uint32_t handle, ma_sound*& sound);
private:
    ma_engine primary_engine;
    std::vector<ma_sound *> sounds;
    std::vector<uint32_t> free_handles;
};


AudioEngine& get_engine();

#endif //MINIAUDIO_UNITY_BINDINGS_AUDIO_H
