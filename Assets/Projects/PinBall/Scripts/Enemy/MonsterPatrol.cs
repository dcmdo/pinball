using UnityEngine;
using UnityEngine.AI;

namespace Cool.Dcm.Game.PinBall.Enemy
{
public class MonsterPatrol : BaseMonster
    {
        private MonsterConfig config;
        private int currentPatrolIndex;
        private Vector3[] worldPatrolPoints;
        // private NavMeshAgent navAgent;
        // private Transform attackTarget;
        // private bool isAttacking;

        public void Initialize(MonsterConfig monsterConfig, Transform target)
        {
            config = monsterConfig;
            attackTarget = target;
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.speed = config.moveSpeed;
            navAgent.stoppingDistance = 0.5f;
            navAgent.acceleration = 8f;
            
            ConvertPatrolPointsToWorldSpace();
            currentPatrolIndex = 0;
            SetNextDestination();
        }

        private void ConvertPatrolPointsToWorldSpace()
        {
            worldPatrolPoints = new Vector3[config.patrolPoints.Count];
            for (int i = 0; i < config.patrolPoints.Count; i++)
            {
                worldPatrolPoints[i] = transform.TransformPoint(config.patrolPoints[i]);
            }
        }

        private void SetNextDestination()
        {
            if (worldPatrolPoints.Length == 0) return;
            navAgent.SetDestination(worldPatrolPoints[currentPatrolIndex]);
        }

        // private void Update()
        // {
        //     if (!navAgent.pathPending && navAgent.remainingDistance < 0.1f)
        //     {
        //         currentPatrolIndex = (currentPatrolIndex + 1) % worldPatrolPoints.Length;
        //         SetNextDestination();
        //     }
        // }

        void OnDrawGizmosSelected()
        {
            if (config == null || config.patrolPoints == null) return;

            Gizmos.color = Color.red;
            foreach (var point in config.patrolPoints)
            {
                Gizmos.DrawSphere(transform.TransformPoint(point), 0.2f);
            }
        }
    }
}
