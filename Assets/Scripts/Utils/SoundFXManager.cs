using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [SerializeField] private AudioSource soundFXObject;
    private List<AudioSource> activeAudioSources = new List<AudioSource>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();

        activeAudioSources.Add(audioSource);

        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
        StartCoroutine(RemoveFromListAfterDelay(audioSource, clipLength));
    }

    public void PlayRandomSoundFXClip(AudioClip[] audioClips, Transform spawnTransform, float volume)
    {
        int rand = Random.Range(0, audioClips.Length);
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClips[rand];
        audioSource.volume = volume;
        audioSource.Play();

        activeAudioSources.Add(audioSource);

        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
        StartCoroutine(RemoveFromListAfterDelay(audioSource, clipLength));
    }

    // ✅ Pause all current sound effects
    public void PauseAllSoundFX()
    {
        foreach (AudioSource source in activeAudioSources)
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }
        }
    }

    // ✅ Resume all paused sound effects
    public void ResumeAllSoundFX()
    {
        foreach (AudioSource source in activeAudioSources)
        {
            if (source != null && !source.isPlaying)
            {
                source.UnPause();
            }
        }
    }

    // ✅ Clean up list after sounds are destroyed
    private System.Collections.IEnumerator RemoveFromListAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeAudioSources.Remove(source);
    }
}
