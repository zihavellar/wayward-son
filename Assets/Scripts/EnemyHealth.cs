using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace WaywardSon
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health")]
        public int maxHealth = 100;
        public int currentHealth;

        [Header("AI & Attack Settings")]
        public bool isAggressive = false;
        public float attackRange = 1.5f;
        public int attackDamage = 10;
        public float attackCooldown = 1.5f;

        private Transform playerTransform;
        private NavMeshAgent agent;
        private Renderer rend;
        private Color originalColor;
        private Coroutine flashCoroutine;
        private float nextAttackTime = 0f;
        private Vector3 initialSpawnPoint;

        private void Start()
        {
            currentHealth = maxHealth;
            initialSpawnPoint = transform.position;

            rend = GetComponent<Renderer>();
            if (rend != null)
            {
                originalColor = rend.material.color;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }

            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (isAggressive && playerTransform != null)
            {
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.SetDestination(playerTransform.position);
                }
                else
                {
                    Vector3 direction = (playerTransform.position - transform.position).normalized;
                    direction.y = 0;
                    transform.Translate(direction * 2f * Time.deltaTime, Space.World);
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(direction);
                    }
                }

                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance <= attackRange && Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    AttackPlayer();
                }
            }
        }

        private void AttackPlayer()
        {
            if (playerTransform == null) return;
            var playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            Debug.Log($"{gameObject.name} took {damage} damage! HP: {currentHealth}/{maxHealth}");

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRed());

            if (currentHealth <= 0)
            {
                Debug.Log($"{gameObject.name} defeated! Resetting HP and position.");
                currentHealth = maxHealth;
                
                // Warp back to start position to keep prototype looping
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.Warp(initialSpawnPoint);
                }
                else
                {
                    transform.position = initialSpawnPoint;
                }
            }
        }

        private IEnumerator FlashRed()
        {
            if (rend != null)
            {
                rend.material.color = Color.red;
                yield return new WaitForSeconds(0.15f);
                rend.material.color = originalColor;
            }
        }
    }
}
