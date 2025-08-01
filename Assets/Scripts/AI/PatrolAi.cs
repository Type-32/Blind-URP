using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    public enum State
    {
        Patrol,
        Chase,
        Attack,
        Distracted
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class PatrolAi : MonoBehaviour
    {
        // --- Public Fields (visible in Inspector) ---
        [Header("Core Behavior")] public Transform player;

        [Header("Perception")] public float detectionRadius = 15f;
        [Range(0, 360)] public float fieldOfViewAngle = 90f;

        [Header("Combat")] public float attackRange = 8f;

        // NEW: Section for distraction parameters
        [Header("Distraction Behavior")] [Tooltip("How long the AI stays distracted at the location.")]
        public float distractionDuration = 5f;

        [Header("Movement Speeds")] public float patrolSpeed = 3.5f;
        public float chaseSpeed = 5f;
        public float distractionSpeed = 4.5f; // NEW: Speed when moving to a distraction

        [Header("Patrol Behavior")] public List<Transform> patrolPoints;

        // --- Private Fields ---
        private NavMeshAgent agent;
        private int patrolIndex = 0;
        private State currentState;
        private Coroutine distractionCoroutine; // NEW: To manage the distraction timer

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        void Start()
        {
            if (player == null)
            {
                Debug.LogError("AI setup error: Player not found.", this);
                this.enabled = false;
                return;
            }

            ChangeState(State.Patrol);
        }

        // --- Public Method to Trigger Distraction ---
        /// <summary>
        /// Distracts the AI, causing it to investigate a position.
        /// Only works if the AI is currently patrolling.
        /// </summary>
        /// <param name="distractionPosition">The world position to investigate.</param>
        public void Distract(Vector3 distractionPosition)
        {
            // NEW: Only allow distraction if currently patrolling
            if (currentState == State.Patrol)
            {
                // Set the target for the new state BEFORE changing to it
                agent.destination = distractionPosition;
                ChangeState(State.Distracted);
            }
        }

        void ChangeState(State newState)
        {
            if (currentState == newState) return;

            // NEW: Clean up any running timers when changing state
            if (distractionCoroutine != null)
            {
                StopCoroutine(distractionCoroutine);
                distractionCoroutine = null;
            }

            currentState = newState;

            switch (currentState)
            {
                case State.Patrol:
                    agent.speed = patrolSpeed;
                    agent.isStopped = false;
                    SetNextPatrolPoint();
                    break;

                case State.Distracted: // NEW: Setup for the distracted state
                    agent.speed = distractionSpeed;
                    agent.isStopped = false;
                    // Destination is set in the Distract() function before this is called
                    break;

                case State.Chase:
                    agent.speed = chaseSpeed;
                    agent.isStopped = false;
                    break;

                case State.Attack:
                    agent.destination = transform.position; // Lock position
                    agent.isStopped = true;
                    break;
            }
        }

        void Update()
        {
            // The main logic loop, which now includes the distracted state
            switch (currentState)
            {
                case State.Patrol:
                    UpdatePatrolState();
                    break;
                case State.Distracted: // NEW: Handle logic for the distracted state
                    UpdateDistractedState();
                    break;
                case State.Chase:
                    UpdateChaseState();
                    break;
                case State.Attack:
                    UpdateAttackState();
                    break;
            }
        }

        // --- State-specific Update Logic ---

        private void UpdatePatrolState()
        {
            // Highest priority: check for the player
            if (CanSeePlayer())
            {
                ChangeState(State.Chase);
                return;
            }

            // Continue patrolling
            if (IsAtDestination())
            {
                SetNextPatrolPoint();
            }
        }

        // NEW: The logic for the distracted state
        private void UpdateDistractedState()
        {
            // Highest priority: check for the player. This can interrupt the distraction.
            if (CanSeePlayer())
            {
                ChangeState(State.Chase);
                return;
            }

            // When the AI arrives at the distraction point, it waits for a duration.
            // We check if the coroutine is null to ensure we only start it once.
            if (IsAtDestination() && distractionCoroutine == null)
            {
                distractionCoroutine = StartCoroutine(DistractionWaitTimer());
            }
        }

        // MODIFIED: This state is now a trap. No exit.
        private void UpdateChaseState()
        {
            // Constantly update destination to follow the player
            agent.destination = player.position;

            // Condition to switch to attack: Are we in range?
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(State.Attack);
            }
        }

        // MODIFIED: This state is now a trap. No exit.
        private void UpdateAttackState()
        {
            // Always face the player when attacking
            transform.LookAt(player);

            // Even though we're "trapped", the player could move out of range.
            // If so, we must chase them again.
            if (Vector3.Distance(transform.position, player.position) > attackRange)
            {
                ChangeState(State.Chase);
            }
            else
            {
                // Stay in place and perform the attack action
                PerformAttack();
            }
        }

        // --- Timers and Helper Methods ---

        // NEW: Coroutine to handle the waiting period for a distraction
        private IEnumerator DistractionWaitTimer()
        {
            yield return new WaitForSeconds(distractionDuration);

            // After waiting, if we haven't been interrupted by seeing the player,
            // return to patrolling.
            ChangeState(State.Patrol);
        }

        private bool IsAtDestination()
        {
            // A helper to check if the agent has reached its destination
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        }

        // Unchanged perception logic
        private bool CanSeePlayer()
        {
            if (player == null) return false;
            if (Vector3.Distance(transform.position, player.position) > detectionRadius) return false;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToPlayer) > fieldOfViewAngle / 2) return false;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRadius))
            {
                if (hit.transform == player) return true;
            }

            return false;
        }

        void SetNextPatrolPoint()
        {
            if (patrolPoints.Count == 0) return;
            agent.destination = patrolPoints[patrolIndex].position;
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
        }

        void PerformAttack()
        {
            Debug.Log(gameObject.name + " is attacking the player!");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward;
            Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, fovLine1 * detectionRadius);
            Gizmos.DrawRay(transform.position, fovLine2 * detectionRadius);
        }
    }
}