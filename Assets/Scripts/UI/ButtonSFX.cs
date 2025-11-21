using UnityEngine;

public class ButtonSFX : MonoBehaviour
{
    [SerializeField] private AudioClip clickSFX;
    [SerializeField] private float clickVolume = 1f;

    public void PlayClick()
    {
        if (clickSFX != null)
            SoundFXManager.instance.PlaySoundFXClip(clickSFX, transform, clickVolume);
    }
}
