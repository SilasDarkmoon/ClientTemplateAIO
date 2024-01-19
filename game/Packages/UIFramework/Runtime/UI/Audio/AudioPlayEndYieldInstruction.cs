using UnityEngine;

public class AudioPlayEndYieldInstruction : CustomYieldInstruction
{
    private AudioSource audioSource;
    private AudioClip clip;

    public AudioPlayEndYieldInstruction(AudioSource audioSource)
    {
        this.audioSource = audioSource;
        this.clip = audioSource.clip;
    }

    public override bool keepWaiting
    {
        get
        {
            bool needBreak = audioSource.isPlaying == false || audioSource.clip != clip;
            return !needBreak;
        }
    }
}
