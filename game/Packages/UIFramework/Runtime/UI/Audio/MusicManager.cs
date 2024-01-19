using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public static class MusicManager
{
    private static List<string> PlayList;
    private static float Volume;
    private static bool isPlaying;
    private static bool IsSet = false;

    private static string category = "music";

    public static void Reset()
    {
        IsSet = false;
    }
    public static void Set()
    {
        IsSet = true;
        PlayList = null;
        Volume = 0.4f;
        isPlaying = false;
    }

    public static void SetPlayList(List<string> playList)
    {
        if (AudioManager.GetPlayer(category) != null && AudioManager.GetPlayer(category).isPlaying)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(FadeOut());
            seq.AppendCallback(() =>
            {
                PlayList = playList;
            });
        }
        else
        {
            PlayList = playList;
        }
    }

    public static void SetVolume(float volume)
    {
        Volume = volume;
    }

    public static void Play(string[] playList = null)
    {
        if (!IsSet)
        {
            Set();
        }
        if (playList != null)
        {
            SetPlayList(new List<string>(playList));
        }
        if (AudioManager.GetPlayer(category) == null)
        {
            AudioManager.CreatePlayer(category, true);
            isPlaying = false;
        }
        if (!isPlaying)
        {
            isPlaying = true;
            AudioManager.GetPlayer(category).StartCoroutine(PlayMusic());
        }
    }

    public static void Stop()
    {
        isPlaying = false;
        if (AudioManager.GetPlayer(category) == null) return;
        AudioManager.GetPlayer(category).Stop();
    }

    public static void Pause()
    {
        isPlaying = false;
        if (AudioManager.GetPlayer(category) == null) return;
        AudioManager.GetPlayer(category).Pause();
    }

    public static void UnPause()
    {
        isPlaying = true;
        if (AudioManager.GetPlayer(category) == null) return;
        AudioManager.GetPlayer(category).UnPause();
    }

    public static void DestroyPlayer()
    {
        Stop();
        AudioManager.DestroyPlayer(category);
    }

    public static Sequence FadeOut()
    {
        Sequence seq = DOTween.Sequence();
        var target = AudioManager.GetPlayer(category);
        seq.Append(DOTween.To(() => target.GlobalVolume, x => target.GlobalVolume = x, 0, 2f).SetTarget(target));
        seq.AppendCallback(Stop);
        return seq;
    }

    private static IEnumerator PlayMusic()
    {
        while (isPlaying)
        {
            if (PlayList != null && PlayList.Count > 0)
            {
                int playIndex = Random.Range(0, PlayList.Count);
                yield return AudioManager.GetPlayer(category).PlayAudioBase(PlayList[playIndex], Volume);
            }
            yield return null;
        }
    }

}
