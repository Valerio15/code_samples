using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ManageAudioPitch : MonoBehaviour
{
    public bool makePredictable;
    public static ManageAudioPitch Instance;
    private AudioSource audioSource;
    private char currentCharacter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void RandomizePitch(float pitch)
    {
        if (!makePredictable)
        {
            audioSource.pitch = Random.Range(pitch, pitch + .5f);
        }
        else
        {
            int hashCode = currentCharacter.GetHashCode();

            int maxPitchInt = (int)((pitch + .5f) * 100);
            int minPitchInt = (int)(pitch * 100);

            int pitchRangeInt = maxPitchInt - minPitchInt;

            if (pitchRangeInt != 0)
            {
                int predictablePitchInt = (hashCode % pitchRangeInt) + minPitchInt;
                float predictablePitch = predictablePitchInt / 100f;
                audioSource.pitch = predictablePitch;
            }
            else
            {
                audioSource.pitch = pitch;
            }
        }
    }

    public void SetCurrentCharacter(char character)
    {
        currentCharacter = character;
    }
}
