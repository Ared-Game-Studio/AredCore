using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ared.Core.AudioManager.Data
{
    [CreateAssetMenu(menuName = "AudioManager/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class AudioData
        {
            [ReadOnly] public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }
        
        public List<AudioData> musics = new ();
        public List<AudioData> sfxs = new ();
        
        public AudioData GetMusic(string id) => musics.Find(x => x.id == id);
        public AudioData GetSfx(string id) => sfxs.Find(x => x.id == id);

        [HideInInspector] public string lastGeneratedHash = "";

        public string ComputeHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + HashList(musics);
                hash = hash * 31 + HashList(sfxs);
                return hash.ToString();
            }
        }

        private int HashList(List<AudioData> list)
        {
            unchecked
            {
                int h = 23;
                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    string name = entry.clip ? entry.clip.name : "null";
                    h = h * 31 + name.GetHashCode();
                    h = h * 31 + entry.volume.GetHashCode();
                }
                return h;
            }
        }

        public bool HasDuplicateNames(out string duplicateName)
        {
            var set = new HashSet<string>();
            foreach (var e in musics)
            {
                if (!e.clip) continue;
                if (!set.Add(e.id))
                {
                    duplicateName = e.id;
                    return true;
                }
            }

            foreach (var e in sfxs)
            {
                if (!e.clip) continue;
                if (!set.Add(e.id))
                {
                    duplicateName = e.id;
                    return true;
                }
            }

            duplicateName = null;
            return false;
        }
        
        public bool HasEmptyNames()
        {
            foreach (var e in musics)
            {
                if (!e.clip) continue;
                if (string.IsNullOrWhiteSpace(e.id))
                {
                    return true;
                }
            }

            foreach (var e in sfxs)
            {
                if (!e.clip) continue;
                if (string.IsNullOrWhiteSpace(e.id))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}