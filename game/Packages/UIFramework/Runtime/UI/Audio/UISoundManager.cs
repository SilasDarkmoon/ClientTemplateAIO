using UnityEngine;

public static class UISoundManager
{
    private static float UISoundVolume = 1.0f;

    private static string category = "ui";
    // 是否立即播放
    public static bool IsInstantlyPlay = true;
    public static void Play(string file, float volume = -1f, bool loop = false)
    {
        if (volume == 0f) volume = UISoundVolume;
        if (AudioManager.GetPlayer(category) == null) AudioManager.CreatePlayer(category, true);
        AudioManager.GetPlayer(category).PlayAudio("Game/Audio/UI/" + file, (float)volume, loop, IsInstantlyPlay);
    }
    public static void Stop()
    {
        if (AudioManager.GetPlayer(category) != null) AudioManager.GetPlayer(category).Stop();
    }
}
