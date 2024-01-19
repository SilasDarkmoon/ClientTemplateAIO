using System.Collections;
using UnityEngine;
using UnityEngineEx;
using UnityEngineEx.CoroutineTasks;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    protected float clipVolume = 1f;
    protected float? globalVolume = 1f;
    protected string category;
    public AudioSource audioSource = null;

    public bool isPlaying
    {
        get { return audioSource.isPlaying; }
    }

    public float ClipVolume
    {
        get { return clipVolume; }
        set
        {
            clipVolume = value;
            ApplyVolume();
        }
    }

    public string Category
    {
        get { return category; }
        set
        {
            category = value;
        }
    }

    public AudioSource AudioSourceComponent
    {
        get { return audioSource; }
        set
        {
            audioSource = value;
        }
    }

    public float GlobalVolume
    {
        get
        {
            if (!globalVolume.HasValue)
            {
                globalVolume = 1f;
            }

            return globalVolume.Value;
        }
        set
        {
            globalVolume = value;
            ApplyVolume();
        }
    }

    public float ClipLength
    {
        get
        {
            if (audioSource.clip != null)
                return audioSource.clip.length;
            return 0;
        }
    }

    public float RemainingClipLength
    {
        get
        {
            if (audioSource.loop)
                return -1;
            if (audioSource.clip != null)
                return audioSource.clip.length - audioSource.time;
            return 0;
        }
    }

    public IEnumerator PlayAudioBase(string path, float volume, bool loop = false)
    {
        ClipVolume = volume;
        var audioClipAsync = ResManager.LoadResAsync(path, typeof(AudioClip));
        yield return audioClipAsync;
        var audioClip = audioClipAsync.Result as AudioClip;
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            ApplyVolume();
            audioSource.Play();
            audioSource.loop = loop;
            yield return new AudioPlayEndYieldInstruction(audioSource);
        }
        else
        {
            PlatDependant.LogError("Audio clip not found, path :" + path);
        }
    }

    public void PlayAudio(string path, float volume, bool loop = false, bool instantly = false)
    {
        ClipVolume = volume;
        if (instantly == true)
        {
            var audioClip = ResManager.LoadRes(path, typeof(AudioClip)) as AudioClip;
            if (audioClip != null)
            {
                audioSource.clip = audioClip;
                ApplyVolume();
                audioSource.Play();
                audioSource.loop = loop;
            }
            else
            {
                PlatDependant.LogError("Instantly Audio clip not found, path :" + path);
            }
            return;
        }
        StartCoroutine(PlayAudioBase(path, volume, loop));
    }

    public void Stop()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = null;
    }

    public void ApplyVolume()
    {
        audioSource.volume = clipVolume * GlobalVolume * AudioManager.GetAudioVolume(category);
    }

    public void Pause()
    {
        if (audioSource == null) return;
        audioSource.Pause();
    }

    public void UnPause()
    {
        if (audioSource == null) return;
        audioSource.UnPause();
    }
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void OnDestroy()
    {
        AudioManager.DestroyPlayer(this.Category);
    }
}
