using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Managers
{
    [System.Serializable]
    public class SoundData
    {
        public string name;      // 호출 시 사용할 이름 (예: "PlayerDie", "Fire_Rifle")
        public AudioClip clip;   // 실제 오디오 파일
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }

    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Sound Lists")]
        [SerializeField] private List<SoundData> sfxList = new List<SoundData>();
        [SerializeField] private List<SoundData> bgmList = new List<SoundData>();

        [Header("Settings")]
        [SerializeField] private int sfxPoolSize = 20;

        private Dictionary<string, SoundData> sfxDictionary = new Dictionary<string, SoundData>();
        private Dictionary<string, SoundData> bgmDictionary = new Dictionary<string, SoundData>();

        private AudioSource bgmSource;
        private List<AudioSource> sfxSources = new List<AudioSource>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeDictionaries();
            InitializeAudioSources();
        }

        private void InitializeDictionaries()
        {
            foreach (var sfx in sfxList)
            {
                if (!sfxDictionary.ContainsKey(sfx.name))
                {
                    sfxDictionary.Add(sfx.name, sfx);
                }
            }

            foreach (var bgm in bgmList)
            {
                if (!bgmDictionary.ContainsKey(bgm.name))
                {
                    bgmDictionary.Add(bgm.name, bgm);
                }
            }
        }

        private void InitializeAudioSources()
        {
            // BGM Source
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;

            // SFX Pool
            GameObject sfxPoolObj = new GameObject("SFX_Pool");
            sfxPoolObj.transform.SetParent(transform);

            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_Source_{i}");
                sfxObj.transform.SetParent(sfxPoolObj.transform);
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxSources.Add(source);
            }
        }

        private AudioSource GetSFXSource()
        {
            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // 모든 소스가 사용 중이면 첫 번째 소스 반환 (또는 새로 생성하여 확장 가능)
            return sfxSources[0];
        }

        public void PlaySFX(string name, Vector3 position)
        {
            if (sfxDictionary.TryGetValue(name, out SoundData data))
            {
                Debug.Log($"[SoundManager] Playing SFX: {name}");
                AudioSource source = GetSFXSource();
                source.transform.position = position;
                source.clip = data.clip;
                source.volume = data.volume;
                source.pitch = data.pitch;
                source.spatialBlend = 1f; // 3D Sound
                source.Play();
                StartCoroutine(ReturnToPool(source, data.clip.length));
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {name}");
            }
        }

        public void PlaySFX(string name)
        {
            if (sfxDictionary.TryGetValue(name, out SoundData data))
            {
                AudioSource source = GetSFXSource();
                source.transform.localPosition = Vector3.zero; // Attach to manager/listener or reset
                source.clip = data.clip;
                source.volume = data.volume;
                source.pitch = data.pitch;
                source.spatialBlend = 0f; // 2D Sound
                source.Play();
                StartCoroutine(ReturnToPool(source, data.clip.length));
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {name}");
            }
        }

        private System.Collections.IEnumerator ReturnToPool(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (source != null && !source.loop) 
            {
                source.Stop();
                source.clip = null;
            }
        }

        public AudioSource PlayLoopingSFX(string name, Vector3 position)
        {
            if (sfxDictionary.TryGetValue(name, out SoundData data))
            {
                AudioSource source = GetSFXSource();
                source.transform.position = position;
                source.clip = data.clip;
                source.volume = data.volume;
                source.pitch = data.pitch;
                source.spatialBlend = 1f; // 3D Sound
                source.loop = true;
                source.Play();
                return source;
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {name}");
                return null;
            }
        }

        public void StopLoopingSFX(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.loop = false;
                source.clip = null; // Release ref
            }
        }

        public void PlayBGM(string name)
        {
            if (bgmDictionary.TryGetValue(name, out SoundData data))
            {
                if (bgmSource.clip == data.clip && bgmSource.isPlaying) return;

                bgmSource.clip = data.clip;
                bgmSource.volume = data.volume;
                bgmSource.pitch = data.pitch;
                bgmSource.Play();
            }
            else
            {
                Debug.LogWarning($"[SoundManager] BGM not found: {name}");
            }
        }

        public void StopBGM()
        {
            bgmSource.Stop();
        }
    }
}
