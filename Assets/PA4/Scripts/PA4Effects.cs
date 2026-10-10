using UnityEngine;

namespace BosquePA4
{
    public class PA4Effects : MonoBehaviour
    {
        public ParticleSystem[] pool;
        int nextEffect;
        public void Emit(Vector3 position)
        {
            if (pool.Length == 0)
            {
                return;
            }

            // Reutilizamos las partículas en vez de crear objetos en cada recogida.
            var particles = pool[nextEffect];
            nextEffect = (nextEffect + 1) % pool.Length;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.transform.position = position;
            particles.Play();
        }
    }
}
