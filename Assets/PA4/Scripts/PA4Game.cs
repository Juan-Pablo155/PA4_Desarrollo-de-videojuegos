using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BosquePA4
{
    [DefaultExecutionOrder(100)]
    public class PA4Game : MonoBehaviour
    {
        public enum Phase
        {
            Menu,
            Playing,
            Paused,
            Won,
            Lost
        }

        public FirstPersonController player;
        public Transform extraction;
        public PA4Crystal[] crystals;
        public PA4Guard[] guards;
        public PA4Audio sound;
        public PA4Effects effects;
        public float timeLimit = 300;
        public Phase State { get; private set; }
        public int Health { get; private set; } = 3;
        public int Collected => collectedCrystals.Count;
        public float Remaining { get; private set; }
        public string Message { get; private set; }
        public float HurtUntil { get; private set; }

        readonly HashSet<PA4Crystal> collectedCrystals = new HashSet<PA4Crystal>();
        Rigidbody body;
        Vector3 spawnPosition;
        float nextDamageTime;
        float messageUntil;
        float interactionTime;
        PA4Crystal nearby;
        public float InteractionProgress => interactionTime / 1.5f;
        public PA4Crystal Nearby => nearby;
        public bool Threatened { get; private set; }

        void Awake()
        {
            body = player.GetComponent<Rigidbody>();
            spawnPosition = player.transform.position;
            Remaining = timeLimit;
        }

        void Start()
        {
            SetState(Phase.Menu);
        }

        public void Begin()
        {
            if (State != Phase.Menu)
            {
                return;
            }

            SetState(Phase.Playing);
            Tell("Encuentra los 5 cristales cian. Evita a los centinelas.");
            sound.Cue(sound.start);
        }

        public void TogglePause()
        {
            if (State == Phase.Playing)
            {
                SetState(Phase.Paused);
            }
            else if (State == Phase.Paused)
            {
                SetState(Phase.Playing);
            }
        }

        void SetState(Phase phase)
        {
            State = phase;
            bool playing = phase == Phase.Playing;
            Time.timeScale = playing ? 1 : 0;
            player.enabled = playing;
            player.playerCanMove = playing;
            player.cameraCanMove = playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            sound.SetPlaying(playing);
        }

        public void Restart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Exit()
        {
            Time.timeScale = 1;
            Application.Quit();
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            if (State == Phase.Menu && Input.GetKeyDown(KeyCode.Return))
            {
                Begin();
            }

            if (State != Phase.Playing)
            {
                return;
            }

            AdvanceClock(Time.deltaTime);
            if (State != Phase.Playing)
            {
                return;
            }

            FindNearbyCrystal();
            HoldInteract(Input.GetKey(KeyCode.E), Time.deltaTime);
            CheckGuards();
            TryExtract();
            if (player.transform.position.y < spawnPosition.y - 35)
            {
                Damage();
                body.position = spawnPosition;
                body.linearVelocity = Vector3.zero;
            }

            if (Time.unscaledTime > messageUntil)
            {
                Message = "";
            }
        }

        void FindNearbyCrystal()
        {
            nearby = null;
            float closestDistance = 3.2f;
            foreach (var crystal in crystals)
            {
                if (crystal.Taken)
                {
                    continue;
                }

                float distance = Vector3.Distance(player.transform.position, crystal.transform.position);
                if (distance < closestDistance)
                {
                    nearby = crystal;
                    closestDistance = distance;
                }
            }
        }

        void CheckGuards()
        {
            Threatened = false;
            foreach (var guard in guards)
            {
                if (guard.Mode == PA4Guard.Behaviour.Chase)
                {
                    Threatened = true;
                }
            }
        }

        public void AdvanceClock(float delta)
        {
            if (State != Phase.Playing)
            {
                return;
            }

            Remaining = Mathf.Max(0, Remaining - delta);
            if (Remaining <= 0)
            {
                Tell("Se agotó el tiempo de la expedición.", 999);
                SetState(Phase.Lost);
                sound.Cue(sound.lose);
            }
        }

        public void HoldInteract(bool held, float delta)
        {
            if (State != Phase.Playing || nearby == null || !held)
            {
                interactionTime = 0;
                return;
            }

            interactionTime += delta;
            if (interactionTime >= 1.5f)
            {
                Collect(nearby);
                interactionTime = 0;
            }
        }

        public bool Collect(PA4Crystal crystal)
        {
            if (State != Phase.Playing || crystal == null || crystal.Taken || Vector3.Distance(player.transform.position, crystal.transform.position) > 3.2f)
            {
                return false;
            }

            if (!collectedCrystals.Add(crystal))
            {
                return false;
            }

            effects.Emit(crystal.transform.position);
            crystal.Take();
            sound.Cue(sound.pickup);
            Tell(Collected == crystals.Length ? "¡Todos los cristales! Regresa al mirador dorado." : "Cristal recuperado: " + Collected + " / " + crystals.Length);
            return true;
        }

        public void Damage()
        {
            if (State != Phase.Playing || Time.time < nextDamageTime)
            {
                return;
            }

            nextDamageTime = Time.time + 2.5f;
            HurtUntil = Time.unscaledTime + .45f;
            Health--;
            sound.Cue(sound.hurt);
            Tell("¡Te alcanzaron! Aléjate del centinela.");
            if (Health <= 0)
            {
                Tell("Los centinelas detuvieron la expedición.", 999);
                SetState(Phase.Lost);
                sound.Cue(sound.lose);
            }
        }

        public bool TryExtract()
        {
            if (State != Phase.Playing || Collected != crystals.Length || Vector3.Distance(player.transform.position, extraction.position) > 3)
            {
                return false;
            }

            SetState(Phase.Won);
            sound.Cue(sound.win);
            return true;
        }

        public Transform NextTarget()
        {
            if (Collected == crystals.Length)
            {
                return extraction;
            }

            Transform target = null;
            float closestDistance = float.MaxValue;
            foreach (var crystal in crystals)
            {
                if (!crystal.Taken)
                {
                    float distanceSquared = (crystal.transform.position - player.transform.position).sqrMagnitude;
                    if (distanceSquared < closestDistance)
                    {
                        closestDistance = distanceSquared;
                        target = crystal.transform;
                    }
                }
            }

            return target;
        }

        public void Tell(string text, float seconds = 4)
        {
            Message = text;
            messageUntil = Time.unscaledTime + seconds;
        }
    }
}
