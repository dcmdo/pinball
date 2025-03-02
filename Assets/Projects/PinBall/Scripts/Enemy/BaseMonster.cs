using UnityEngine;
using UnityEngine.AI;

namespace Cool.Dcm.Game.PinBall.Enemy
{
    public abstract class BaseMonster : MonoBehaviour
    {
        [Header("基础属性")]
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float attackRange = 1.5f;
        [SerializeField] protected float knockbackResistance = 0.5f;

        protected NavMeshAgent navAgent;
        protected Transform attackTarget;
        protected bool isAttacking;

        protected virtual void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = attackRange;
        }

        public virtual void Initialize(Transform target)
        {
            attackTarget = target;
            navAgent.SetDestination(attackTarget.position);
        }

        protected virtual void Update()
        {
            if (!isAttacking && navAgent.remainingDistance <= attackRange)
            {
                StartAttack();
            }
        }

        protected virtual void StartAttack()
        {
            isAttacking = true;
            navAgent.isStopped = true;
            // 触发攻击动画/逻辑
        }

        public virtual void ApplyKnockback(Vector3 force)
        {
            navAgent.velocity = force * (1 - knockbackResistance);
            isAttacking = false;
            navAgent.isStopped = false;
        }

        protected virtual void OnDestroy()
        {
            if (navAgent != null)
            {
                Destroy(navAgent);
            }
        }
    }
}
