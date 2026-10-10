using UnityEngine;
using UnityEngine.AI;

namespace BosquePA4
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PA4Guard : MonoBehaviour
    {
        public enum Behaviour
        {
            Patrol,
            Chase
        }

        public PA4Game game;
        public Vector3[] waypoints;
        public Transform eye;
        public Behaviour Mode { get; private set; }

        NavMeshAgent agent;
        int waypointIndex;
        float nextDecision;
        float lastSeen = -100;
        Vector3 lastPlayerPosition;
        public int Decisions { get; private set; }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            // El NavMesh de la escena debe estar activo antes que el agente.
            agent.enabled = true;
        }

        void Update()
        {
            if (!agent.isOnNavMesh || game.State != PA4Game.Phase.Playing)
            {
                return;
            }

            if (Time.time < nextDecision)
            {
                return;
            }

            // Revisamos la IA cinco veces por segundo para reducir cálculos.
            nextDecision = Time.time + 0.2f;
            Decisions++;
            Vector3 target = game.player.transform.position;
            float distanceToPlayer = Vector3.Distance(transform.position, target);
            bool canSeePlayer = distanceToPlayer < 14 && CanSeePlayer(target);
            if (canSeePlayer)
            {
                lastSeen = Time.time;
                lastPlayerPosition = target;
            }

            Mode = Time.time - lastSeen < 3 ? Behaviour.Chase : Behaviour.Patrol;
            agent.speed = Mode == Behaviour.Chase ? 4.25f : 2;
            if (Mode == Behaviour.Chase)
            {
                agent.SetDestination(lastPlayerPosition);
            }
            else if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 1.2f))
            {
                if (waypoints.Length > 0)
                {
                    agent.SetDestination(waypoints[waypointIndex]);
                    waypointIndex = (waypointIndex + 1) % waypoints.Length;
                }
            }

            if (distanceToPlayer < 2 && canSeePlayer)
            {
                game.Damage();
            }

            if (eye)
            {
                eye.localScale = Vector3.one * (Mode == Behaviour.Chase ? .65f : .42f);
            }
        }

        bool CanSeePlayer(Vector3 target)
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            if (Physics.Linecast(origin, target + Vector3.up * .3f, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<FirstPersonController>() != null;
            }

            return true;
        }
    }
}
