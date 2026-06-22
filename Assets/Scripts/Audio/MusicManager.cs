using System.Collections;
using UnityEngine;

namespace UndertaleLiDAR.Audio
{
    /// <summary>
    /// BGM と効果音の再生を一元管理する (DRY: 音の出口はここだけ)。
    /// AudioSource を自前で生成し、他層は AudioClip を渡すだけでよい (SRP)。
    /// クリップ未設定でも null 安全に動作する (実機/アセット無しでも開発できる)。
    /// </summary>
    public sealed class MusicManager : MonoBehaviour
    {
        [Header("クリップ (任意・未設定なら無音)")]
        [SerializeField] private AudioClip _defaultBgm;
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.8f;
        [Tooltip("開始時に defaultBgm を自動再生する")]
        [SerializeField] private bool _playOnStart = true;

        private AudioSource _bgm;
        private AudioSource _sfx;
        private Coroutine _fade;

        private void Awake()
        {
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
            _bgm.volume = _bgmVolume;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false;
            _sfx.playOnAwake = false;
            _sfx.volume = _sfxVolume;
        }

        private void Start()
        {
            if (_playOnStart && _defaultBgm != null)
            {
                PlayBgm(_defaultBgm);
            }
        }

        /// <summary>BGM を差し替えて再生する。</summary>
        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null || _bgm == null)
            {
                return;
            }
            if (_fade != null) { StopCoroutine(_fade); _fade = null; }
            _bgm.clip = clip;
            _bgm.loop = loop;
            _bgm.volume = _bgmVolume;
            _bgm.Play();
        }

        public void StopBgm()
        {
            if (_bgm != null) _bgm.Stop();
        }

        /// <summary>BGM を seconds 秒かけてフェードアウトして停止する。</summary>
        public void FadeOutBgm(float seconds)
        {
            if (_bgm == null || !_bgm.isPlaying)
            {
                return;
            }
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeRoutine(seconds));
        }

        private IEnumerator FadeRoutine(float seconds)
        {
            float start = _bgm.volume;
            float t = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _bgm.volume = Mathf.Lerp(start, 0f, t / seconds);
                yield return null;
            }
            _bgm.Stop();
            _bgm.volume = _bgmVolume;
            _fade = null;
        }

        /// <summary>効果音を 1 発再生する (セリフのブリップ音など)。</summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _sfx == null)
            {
                return;
            }
            _sfx.PlayOneShot(clip, Mathf.Clamp01(_sfxVolume * volumeScale));
        }
    }
}
