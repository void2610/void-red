using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace Void2610.UnityTemplate
{
    public class SeManager : SingletonMonoBehaviour<SeManager>
    {
        [System.Serializable]
        public class SoundData
        {
            public string name;
            public AudioClip audioClip;
            public float volume = 1.0f;
        }

        [SerializeField] private AudioMixerGroup seMixerGroup;
        [SerializeField] private SoundData[] soundData;

        private readonly AudioSource[] _seAudioSourceList = new AudioSource[20];
        private float _seVolume = 0.5f;

        public float SeVolume
        {
            get => _seVolume;
            set
            {
                _seVolume = value;
                if (value <= 0.0f)
                {
                    value = 0.0001f;
                }
                seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(value) * 20);
                PlayerPrefs.SetFloat("SeVolume", value);
            }
        }

        public void PlaySe(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            var audioSource = GetUnusedAudioSource();
            if (!clip)
            {
                Debug.LogError("AudioClip could not be found.");
                return;
            }
            if (!audioSource)
            {
                Debug.LogWarning("There is no available AudioSource.");
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.Play();
        }

        public void PlaySe(string seName, float volume = 1.0f, float pitch = -1.0f)
        {
            var data = this.soundData.FirstOrDefault(t => t.name == seName);
            var audioSource = GetUnusedAudioSource();
            if (data == null) return;
            if (!audioSource) return;

            audioSource.clip = data.audioClip;
            audioSource.volume = data.volume * volume;
            audioSource.pitch = pitch < 0.0f ? Random.Range(0.8f, 1.2f) : pitch;
            audioSource.Play();
        }
    
        public void WaitAndPlaySe(string seName, float time, float volume = 1.0f, float pitch = 1.0f)
        {
            WaitAndPlaySeAsync(seName, time, volume, pitch).Forget();
        }
    
        private async UniTaskVoid WaitAndPlaySeAsync(string seName, float time, float volume = 1.0f, float pitch = 1.0f)
        {
            await UniTask.Delay((int)(time * 1000));
            PlaySe(seName, volume, pitch);
        }

        private AudioSource GetUnusedAudioSource() => _seAudioSourceList.FirstOrDefault(t => t.isPlaying == false);

        protected override void Awake()
        {
            for (var i = 0; i < _seAudioSourceList.Length; ++i)
            {
                _seAudioSourceList[i] = gameObject.AddComponent<AudioSource>();
                _seAudioSourceList[i].outputAudioMixerGroup = seMixerGroup;
            }
        }
        
        private void Start()
        {
            SeVolume = PlayerPrefs.GetFloat("SeVolume", 0.5f);
            seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(_seVolume) * 20);
        }
    }
}