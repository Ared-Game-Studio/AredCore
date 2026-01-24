using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ared.Core.AudioManager.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ared.Core.AudioManager
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Library")]
        [SerializeField] private AudioLibrary library;
        
        [Header("Settings")]
        [SerializeField] private int initialPoolSize = 10;
        
        private AudioSource _mainMusicSource;
        
        private Dictionary<string, AudioLibrary.AudioData> _sfxMap;
        private Dictionary<string, AudioLibrary.AudioData> _musicMap;
        private Queue<AudioSource> _pool;
        private HashSet<AudioSource> _inUse;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            _sfxMap = new Dictionary<string, AudioLibrary.AudioData>();
            _musicMap = new Dictionary<string, AudioLibrary.AudioData>();
            _pool = new Queue<AudioSource>();
            _inUse = new HashSet<AudioSource>();
            
            _mainMusicSource = gameObject.AddComponent<AudioSource>();
            _mainMusicSource.loop = true;
            
            RebuildCache();
            BuildPool();
        }
        
        private void RebuildCache()
        {
            _musicMap.Clear();
            _sfxMap.Clear();

            if (!library) return;

            foreach (AudioLibrary.AudioData audioData in library.musics)
            {
                if (audioData == null || string.IsNullOrEmpty(audioData.id)) continue;
                _musicMap[audioData.id] = audioData;
            }

            foreach (AudioLibrary.AudioData audioData in library.sfxs)
            {
                if (audioData == null || string.IsNullOrEmpty(audioData.id)) continue;
                _sfxMap[audioData.id] = audioData;
            }
        }
        
        private void BuildPool()
        {
            for (int i = 0; i < Mathf.Max(1, initialPoolSize); i++)
            {
                var src = CreateNewPooledSource();
                _pool.Enqueue(src);
            }
        }
        
        private AudioSource CreateNewPooledSource()
        {
            GameObject newAudioSource = new GameObject("AudioSource");
            newAudioSource.transform.SetParent(transform);
            AudioSource src = newAudioSource.AddComponent<AudioSource>();
            newAudioSource.SetActive(false);
            return src;
        }
        
        private AudioSource GetPooledSource()
        {
            AudioSource src = _pool.Count > 0 ? _pool.Dequeue() : CreateNewPooledSource();
            _inUse.Add(src);
            src.gameObject.SetActive(true);
            return src;
        }
        
        private IEnumerator ReleaseWhenDone(AudioSource src, float duration)
        {
            yield return new WaitForSeconds(duration + 0.05f);

            if (src is null) yield break;

            src.Stop();
            src.clip = null;
            src.gameObject.SetActive(false);

            _inUse.Remove(src);
            _pool.Enqueue(src);
        }
        
        // ------------- PUBLIC STRING API (Internal use) -------------
        
        public static void PlaySfx(string id, Vector3? position = null, float delay = 0f)
        {
            if (!Instance || Instance.library == null) return;
            Instance.StartCoroutine(Instance.PlaySfxRoutine(id, position, delay));
        }

        public static void PlayMusic(string id, bool isMain = false, float delay = 0f)
        {
            if (!Instance || Instance.library == null) return;
            Instance.StartCoroutine(Instance.PlayMusicRoutine(id, isMain, delay));
        }

        private IEnumerator PlaySfxRoutine(string id, Vector3? position, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (!_sfxMap.TryGetValue(id, out var entry) || entry.clip is null) yield break;

            AudioSource src = GetPooledSource();
            
            if (position.HasValue)
            {
                src.transform.position = position.Value;
                src.spatialBlend = 1f;
            }
            else
            {
                src.transform.position = transform.position;
                src.spatialBlend = 0f;
            }
            
            src.volume = entry.volume;
            src.PlayOneShot(entry.clip);

            StartCoroutine(ReleaseWhenDone(src, entry.clip.length));
        }

        private IEnumerator PlayMusicRoutine(string id, bool isMain, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (!_musicMap.TryGetValue(id, out var entry) || entry.clip is null) yield break;

            if (isMain)
            {
                if (_mainMusicSource.isPlaying)
                {
                    _mainMusicSource.Stop();
                }

                _mainMusicSource.clip = entry.clip;
                _mainMusicSource.volume = entry.volume;
                _mainMusicSource.Play();
            }
            else
            {
                var src = GetPooledSource();
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = entry.volume;
                src.PlayOneShot(entry.clip);

                StartCoroutine(ReleaseWhenDone(src, entry.clip.length));
            }
        }
        
        


#if UNITY_EDITOR
        private const string LibraryAssetPath = "Assets/Audio/AudioLibrary.asset";
        [MenuItem("Ared/AudioManager/Create Audio Manager")]
        private static void CreateInScene()
        {
            GameObject go = new GameObject("_AudioManager_");
            var manager = go.AddComponent<AudioManager>();
            
            //set config asset if exists
            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryAssetPath);
            if (library != null)
            {
                manager.library = library;
                EditorUtility.SetDirty(go);
            }
            
            Selection.activeGameObject = go;
        }
#endif
    }
}