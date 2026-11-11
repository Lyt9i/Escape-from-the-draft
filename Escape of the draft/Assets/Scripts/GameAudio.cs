using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    [SerializeField] private AudioClip paperMove;
    [SerializeField] private AudioClip steps;
    [SerializeField] private AudioClip star;
    [SerializeField] private AudioClip door;
    [SerializeField] private AudioClip paperTurn;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private AudioSource effects;
    private AudioSource footsteps;

    private void Awake()
    {
        effects = GetComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;

        footsteps = gameObject.AddComponent<AudioSource>();
        footsteps.playOnAwake = false;
        footsteps.spatialBlend = 0f;
        footsteps.loop = true;
    }

    public void PlayPaperMove() => PlayEffect(paperMove);
    public void PlayStar() => PlayEffect(star);
    public void PlayDoor() => PlayEffect(door);
    public void PlayPaperTurn() => PlayEffect(paperTurn);

    public void StartSteps()
    {
        if (steps == null)
            return;

        footsteps.clip = steps;
        footsteps.volume = volume;
        footsteps.Play();
    }

    public void StopSteps() => footsteps.Stop();

    public void StopAll()
    {
        effects.Stop();
        footsteps.Stop();
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            effects.Pause();
            footsteps.Pause();
        }
        else
        {
            effects.UnPause();
            footsteps.UnPause();
        }
    }

    private void PlayEffect(AudioClip clip)
    {
        if (clip != null)
            effects.PlayOneShot(clip, volume);
    }
}
