using UnityEngine;

namespace BosquePA4
{
    public class PA4Audio : MonoBehaviour
    {
        public AudioSource ambience, fx;
        public AudioClip pickup, hurt, win, lose, start;
        public void Cue(AudioClip clip)
        {
            if (clip)
            {
                fx.PlayOneShot(clip);
            }
        }

        public void SetPlaying(bool playing)
        {
            ambience.volume = playing ? .22f : .09f;
        }
    }
}
