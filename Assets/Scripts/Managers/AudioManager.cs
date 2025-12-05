using UnityEngine;
using UnityEngine.Audio;
using RTS.Core.Services;
using RTSGame.Settings;
using System.Collections.Generic;

namespace RTSGame.Managers
{
    /// <summary>
    /// Manages all audio in the game including music, SFX, UI sounds, and voice.
    /// Implements the IAudioService interface.
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Audio Mixer (Optional)")]
        [SerializeField] private AudioMixerGroup masterMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;
        [SerializeField] private AudioMixerGroup voiceMixerGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Audio Clips (Example)")]
        [SerializeField] private AudioClip[] musicClips;
        [SerializeField] private AudioClip[] sfxClips;
        [SerializeField] private AudioClip[] uiClips;
        [SerializeField] private AudioClip[] voiceClips;

        // Audio clip dictionaries for quick lookup
        private Dictionary<string, AudioClip> musicDictionary = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> uiDictionary = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> voiceDictionary = new Dictionary<string, AudioClip>();

        // Current settings
        private AudioSettings currentSettings;

        // Volume properties
        public float MasterVolume
        {
            get => currentSettings?.MasterVolume ?? 1f;
            set
            {
                if (currentSettings != null)
                {
                    currentSettings.MasterVolume = Mathf.Clamp01(value);
                    ApplyVolumeSettings();
                }
            }
        }

        public float MusicVolume
        {
            get => currentSettings?.MusicVolume ?? 0.7f;
            set
            {
                if (currentSettings != null)
                {
                    currentSettings.MusicVolume = Mathf.Clamp01(value);
                    if (musicSource != null)
                        musicSource.volume = value * MasterVolume;
                }
            }
        }

        public float SFXVolume
        {
            get => currentSettings?.SFXVolume ?? 0.8f;
            set
            {
                if (currentSettings != null)
                {
                    currentSettings.SFXVolume = Mathf.Clamp01(value);
                    // SFX volume is applied per-clip when playing
                }
            }
        }

        public float UIVolume
        {
            get => currentSettings?.UIVolume ?? 0.6f;
            set
            {
                if (currentSettings != null)
                {
                    currentSettings.UIVolume = Mathf.Clamp01(value);
                    if (uiSource != null)
                        uiSource.volume = value * MasterVolume;
                }
            }
        }

        public float VoiceVolume
        {
            get => currentSettings?.VoiceVolume ?? 0.9f;
            set
            {
                if (currentSettings != null)
                {
                    currentSettings.VoiceVolume = Mathf.Clamp01(value);
                    if (voiceSource != null)
                        voiceSource.volume = value * MasterVolume;
                }
            }
        }

        // Audio settings properties
        public float SpatialBlend
        {
            get => currentSettings?.SpatialBlend ?? 1f;
            set
            {
                if (currentSettings != null)
                    currentSettings.SpatialBlend = Mathf.Clamp01(value);
            }
        }

        public DynamicRange DynamicRange
        {
            get => currentSettings?.DynamicRange ?? DynamicRange.Normal;
            set
            {
                if (currentSettings != null)
                    currentSettings.DynamicRange = value;
            }
        }

        public BattleSFXIntensity BattleSFXIntensity
        {
            get => currentSettings?.BattleSFXIntensity ?? BattleSFXIntensity.Normal;
            set
            {
                if (currentSettings != null)
                    currentSettings.BattleSFXIntensity = value;
            }
        }

        public UnitVoiceStyle UnitVoiceStyle
        {
            get => currentSettings?.UnitVoices ?? UnitVoiceStyle.Classic;
            set
            {
                if (currentSettings != null)
                    currentSettings.UnitVoices = value;
            }
        }

        public bool AlertNotifications
        {
            get => currentSettings?.AlertNotifications ?? true;
            set
            {
                if (currentSettings != null)
                    currentSettings.AlertNotifications = value;
            }
        }

        public string CurrentAudioDevice => "Default"; // Unity doesn't expose this easily

        private void Awake()
        {
            // Create audio sources if they don't exist
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                if (musicMixerGroup != null)
                    musicSource.outputAudioMixerGroup = musicMixerGroup;
            }

            if (uiSource == null)
            {
                GameObject uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
                uiSource.spatialBlend = 0f; // 2D sound
                if (uiMixerGroup != null)
                    uiSource.outputAudioMixerGroup = uiMixerGroup;
            }

            if (voiceSource == null)
            {
                GameObject voiceObj = new GameObject("VoiceSource");
                voiceObj.transform.SetParent(transform);
                voiceSource = voiceObj.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
                voiceSource.spatialBlend = 0f; // 2D sound
                if (voiceMixerGroup != null)
                    voiceSource.outputAudioMixerGroup = voiceMixerGroup;
            }

            // Initialize dictionaries
            InitializeAudioClipDictionaries();

            // Initialize with default settings
            currentSettings = new AudioSettings();
            ApplyVolumeSettings();
        }

        private void InitializeAudioClipDictionaries()
        {
            if (musicClips != null)
            {
                foreach (var clip in musicClips)
                {
                    if (clip != null)
                        musicDictionary[clip.name] = clip;
                }
            }

            if (sfxClips != null)
            {
                foreach (var clip in sfxClips)
                {
                    if (clip != null)
                        sfxDictionary[clip.name] = clip;
                }
            }

            if (uiClips != null)
            {
                foreach (var clip in uiClips)
                {
                    if (clip != null)
                        uiDictionary[clip.name] = clip;
                }
            }

            if (voiceClips != null)
            {
                foreach (var clip in voiceClips)
                {
                    if (clip != null)
                        voiceDictionary[clip.name] = clip;
                }
            }
        }

        public void ApplySettings(AudioSettings settings)
        {
            if (settings == null) return;

            currentSettings = settings;
            ApplyVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
            if (musicSource != null)
                musicSource.volume = MusicVolume * MasterVolume;

            if (uiSource != null)
                uiSource.volume = UIVolume * MasterVolume;

            if (voiceSource != null)
                voiceSource.volume = VoiceVolume * MasterVolume;
        }

        public void PlayMusic(string musicName)
        {
            if (musicSource == null) return;

            if (musicDictionary.TryGetValue(musicName, out AudioClip clip))
            {
                if (musicSource.clip == clip && musicSource.isPlaying)
                    return;

                musicSource.clip = clip;
                musicSource.Play();
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Music clip '{musicName}' not found!");
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlaySFX(string sfxName, Vector3 position = default)
        {
            if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
            {
                float volume = SFXVolume * MasterVolume;

                if (position != default)
                {
                    // Play 3D sound at position
                    AudioSource.PlayClipAtPoint(clip, position, volume);
                }
                else
                {
                    // Play 2D sound
                    AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
                }
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX clip '{sfxName}' not found!");
            }
        }

        public void PlayUISFX(string sfxName)
        {
            if (uiSource == null) return;

            if (uiDictionary.TryGetValue(sfxName, out AudioClip clip))
            {
                uiSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] UI SFX clip '{sfxName}' not found!");
            }
        }

        public void PlayVoice(string voiceName)
        {
            if (voiceSource == null) return;

            if (voiceDictionary.TryGetValue(voiceName, out AudioClip clip))
            {
                voiceSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Voice clip '{voiceName}' not found!");
            }
        }

        public string[] GetAvailableAudioDevices()
        {
            // Unity doesn't expose audio devices easily in the default API
            // This would require platform-specific code or plugins
            return new string[] { "Default" };
        }

        public void SetAudioDevice(string deviceName)
        {
            // Placeholder - Unity doesn't expose this in the standard API
            Debug.Log($"[AudioManager] Audio device selection: {deviceName}");
        }

        public void RefreshAudioSources()
        {
            ApplyVolumeSettings();
        }
    }
}
