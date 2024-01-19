using System.Collections;
using UnityEngine;
using UnityEngineEx;
using UnityEngineEx.CoroutineTasks;

[RequireComponent(typeof(AudioSource))]
public class AudioMixerPlayer : AudioPlayer
{
    private string audioPath;
    private float runTime;
    private float endTime;
    public void PlayAudioInstantly(string path, float volume, bool loop = false, float pitch = 1, bool isPlayAwake = false)
    {
        StartCoroutine(PlayAudio(path, volume, loop, pitch, isPlayAwake));
    }
    public void PlayAudioScheduledInstantly(string path, float volume, float startTime, float playTime, bool loop = false, float pitch = 1, bool isPlayAwake = false)
    {
        StartCoroutine(PlayAudioScheduled(path, volume, startTime, playTime, loop, pitch, isPlayAwake));
    }
    public IEnumerator PlayAudio(string path, float volume, bool loop = false, float pitch = 1, bool isPlayAwake = false)
    {
        ClipVolume = volume;
        endTime = 0;
        //var clip = ResManager.LoadRes(path, typeof(AudioClip)) as AudioClip;
        CoroutineWork work = ResManager.LoadResAsync(path, typeof(AudioClip));
        yield return work;
        if (work.Done)
        {
            yield return new WaitForSeconds(0.1f);
            audioPath = path;
            audioSource.clip = work.Result as AudioClip;
            ApplyVolume();
            audioSource.time = 0;
            audioSource.pitch = pitch;
            AudioMixerConfig(pitch);
            audioSource.Play();
            audioSource.loop = loop;
            audioSource.playOnAwake = isPlayAwake;
            yield return new AudioPlayEndYieldInstruction(audioSource);
        }
        else
        {
            PlatDependant.LogError("Audio clip not found, path :" + path);
        }
    }

    public void AudioMixerConfig(float pitch = 1)
    {
        if (audioSource.outputAudioMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup.audioMixer.SetFloat("MasterPitch", 1f / pitch);
            audioSource.outputAudioMixerGroup.audioMixer.SetFloat("MasterVolume", pitch);
        }
    }

    private void AudioScheduledConfigure(float startTime, float playTime, bool loop = false, float pitch = 1, bool isPlayAwake = false)
    {
        var expand = (pitch < 1) ? (1 / pitch) : 1;
        ApplyVolume();
        audioSource.time = startTime;
        audioSource.pitch = pitch;

        AudioMixerConfig(pitch);
        audioSource.Play();
        audioSource.SetScheduledEndTime((AudioSettings.dspTime + playTime) * expand);
        audioSource.loop = loop;
        audioSource.playOnAwake = isPlayAwake;
    }

    public IEnumerator PlayAudioScheduled(string path, float volume, float startTime, float playTime, bool loop = false, float pitch = 1, bool isPlayAwake = false)
    {
        ClipVolume = volume;
        endTime = playTime;
        if (audioPath == path && audioSource.clip)
        {
            AudioScheduledConfigure(startTime, playTime, loop, pitch, isPlayAwake);
            yield return new AudioPlayEndYieldInstruction(audioSource);
        }
        else
        {
            var clip = ResManager.LoadRes(path, typeof(AudioClip)) as AudioClip;
            if (clip)
            {
                audioPath = path;
                audioSource.clip = clip;
                AudioScheduledConfigure(startTime, playTime, loop, pitch, isPlayAwake);
                yield return new AudioPlayEndYieldInstruction(audioSource);
            }
            else
            {
                PlatDependant.LogError("Audio clip not found, path :" + path);
            }
        }
    }
    public void IsPlayingAudioClip(bool isPlaying)
    {
        if (audioSource != null)
        {
            if (isPlaying)
            {
                if (endTime > 0)
                {
                    audioSource.time = runTime;
                    audioSource.Play();
                    audioSource.SetScheduledEndTime(AudioSettings.dspTime + endTime);
                }
                else
                {
                    audioSource.Play();
                }
            }
            else
            {
                runTime = audioSource.time;
                audioSource.Pause();
            }
        }
    }

    private void OnDestroy()
    {
        AudioManager.DestroyPlayer(this.Category, gameObject);
        audioPath = null;
    }
}
