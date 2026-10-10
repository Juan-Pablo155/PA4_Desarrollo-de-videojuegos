using UnityEngine;

namespace BosquePA4
{
    public class PA4Crystal : MonoBehaviour
    {
        public Transform model;
        public bool Taken { get; private set; }

        Vector3 basePosition;
        void Awake()
        {
            basePosition = model.localPosition;
        }

        void Update()
        {
            if (Taken)
            {
                return;
            }

            model.Rotate(0, 35 * Time.deltaTime, 0, Space.World);
            model.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * 2) * .12f);
        }

        public void Take()
        {
            Taken = true;
            gameObject.SetActive(false);
        }
    }
}
