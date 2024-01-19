using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngineEx;

public class AudioMixerManager 
{
    public static AudioMixerPlayer GetPlayer(string category)
    {
        return AudioManager.GetPlayer<AudioMixerPlayer>(category);
    }


    public static bool CreatePlayer(string category, bool ignoreClear = false, string mixerPath = "")
    {
        var result = AudioManager.CreatePlayer<AudioMixerPlayer>(category, ignoreClear);
        var audioMixerPlayer = AudioManager.GetPlayer<AudioMixerPlayer>(category);
        BuildMixerPlayer(audioMixerPlayer, mixerPath);

        return result;
    }

    public static void BuildMixerPlayer(AudioMixerPlayer audioMixerPlayer, string mixerPath = "")
    {
        if (mixerPath != "")
        {
            AudioMixer mixer = ResManager.LoadRes(mixerPath) as AudioMixer;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master");
            audioMixerPlayer.AudioSourceComponent.outputAudioMixerGroup = groups[0];
        }
    }

    public static void DestroyAllPlayers()
    {
        AudioManager.DestroyAllPlayers<AudioMixerPlayer>();
    }
}
