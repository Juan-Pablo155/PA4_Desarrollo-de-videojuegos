using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace BosquePA4
{
    // Solo se ejecuta al lanzar el build con -pa4test.
    public class PA4SmokeTest : MonoBehaviour
    {
        static bool started;
        bool failed;
        float deadline;
        void Awake()
        {
            deadline = Time.realtimeSinceStartup + 90;
            Application.logMessageReceived += OnLog;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                failed = true;
            }
        }

        void Update()
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError("PA4_TEST_TIMEOUT");
                Application.Quit(3);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (started || Array.IndexOf(Environment.GetCommandLineArgs(), "-pa4test") < 0)
            {
                return;
            }

            started = true;
            var g = new GameObject("Prueba automatica PA4");
            DontDestroyOnLoad(g);
            g.AddComponent<PA4SmokeTest>();
        }

        void Check(bool value, string name)
        {
            if (!value)
            {
                failed = true;
                Debug.LogError("PA4_TEST_FAIL: " + name);
            }
            else
            {
                Debug.Log("PA4_TEST_PASS: " + name);
            }
        }

        void Warp(PA4Game game, Vector3 pos)
        {
            var rb = game.player.GetComponent<Rigidbody>();
            rb.position = pos;
            game.player.transform.position = pos;
            rb.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        IEnumerator Start()
        {
            yield return null;
            var g = FindFirstObjectByType<PA4Game>();
            Check(g != null, "sistema de juego cargado");
            if (!g)
            {
                Application.Quit(2);
                yield break;
            }

            Check(g.State == PA4Game.Phase.Menu && Time.timeScale == 0, "inicio en menu");
            Check(FindFirstObjectByType<PA4Hud>() != null, "interfaz presente");
            Check(g.sound.ambience.clip && g.sound.pickup && g.sound.hurt && g.sound.win && g.sound.lose, "clips de audio asignados");
            g.Begin();
            yield return new WaitForSeconds(.5f);
            Check(g.State == PA4Game.Phase.Playing && g.player.enabled, "inicio de gameplay");
            Check(Physics.Raycast(g.player.transform.position, Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "colision con suelo");
            var body = g.player.GetComponent<Rigidbody>();
            var previous = body.position;
            g.player.enabled = false;
            body.linearVelocity = g.player.transform.forward * 2;
            yield return new WaitForSeconds(.3f);
            Check(Vector3.Distance(previous, body.position) > .15f, "movimiento fisico");
            body.linearVelocity = Vector3.zero;
            g.player.enabled = true;
            float remaining = g.Remaining;
            g.TogglePause();
            yield return new WaitForSecondsRealtime(.3f);
            Check(g.State == PA4Game.Phase.Paused && Mathf.Abs(g.Remaining - remaining) < .001f, "pausa congela tiempo");
            g.TogglePause();
            Check(!g.TryExtract(), "no hay victoria sin cristales");
            Check(!g.Collect(g.crystals[4]), "no se recoge a distancia");
            var guard = g.guards[0];
            Check(guard.GetComponent<NavMeshAgent>().isOnNavMesh, "enemigo sobre NavMesh");
            Warp(g, guard.transform.position + Vector3.up * 1.2f + guard.transform.forward * 4);
            yield return new WaitForSeconds(.5f);
            Check(guard.Mode == PA4Guard.Behaviour.Chase, "IA detecta y persigue");
            Warp(g, g.crystals[4].transform.position + Vector3.up * .2f);
            yield return new WaitForSeconds(3.5f);
            Check(guard.Mode == PA4Guard.Behaviour.Patrol, "IA vuelve a patrullar");
            int decisions = guard.Decisions;
            yield return new WaitForSeconds(.5f);
            Check(guard.Decisions - decisions <= 4, "IA limita frecuencia de decisiones");
            foreach (var enemy in g.guards)
            {
                enemy.enabled = false;
            }

            foreach (var crystal in g.crystals)
            {
                var path = new NavMeshPath();
                Check(NavMesh.CalculatePath(g.player.transform.position, crystal.transform.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "ruta a " + crystal.name);
                Warp(g, crystal.transform.position + Vector3.up * .2f);
                yield return null;
                int before = g.Collected;
                g.HoldInteract(true, 1.6f);
                Check(g.Collected == before + 1 && crystal.Taken, "interaccion recoge " + crystal.name);
                Check(!g.Collect(crystal), "no duplica cristal");
            }

            Check(g.effects.pool.Length == 3, "pool de particulas fijo");
            Warp(g, g.extraction.position);
            g.TryExtract();
            Check(g.State == PA4Game.Phase.Won, "victoria al regresar");
            g.Restart();
            yield return new WaitForSecondsRealtime(1);
            g = FindFirstObjectByType<PA4Game>();
            Check(g.State == PA4Game.Phase.Menu && g.Collected == 0 && g.Health == 3, "reinicio limpia estado");
            g.Begin();
            foreach (var enemy in g.guards)
            {
                enemy.enabled = false;
            }

            g.Damage();
            int health = g.Health;
            g.Damage();
            Check(g.Health == health, "invulnerabilidad temporal");
            yield return new WaitForSeconds(2.6f);
            g.Damage();
            yield return new WaitForSeconds(2.6f);
            g.Damage();
            Check(g.State == PA4Game.Phase.Lost && g.Health == 0, "derrota por vida");
            g.Restart();
            yield return new WaitForSecondsRealtime(1);
            g = FindFirstObjectByType<PA4Game>();
            g.Begin();
            g.AdvanceClock(301);
            Check(g.State == PA4Game.Phase.Lost && g.Remaining == 0, "derrota por tiempo");
            Debug.Log(failed ? "PA4_TESTS_FAILED" : "PA4_ALL_TESTS_PASSED");
            Application.Quit(failed ? 2 : 0);
        }
    }
}
