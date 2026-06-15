using UnityEngine;

namespace WaywardSon
{
    public class Projectile : MonoBehaviour
    {
        public float speed = 20f;
        public int damage = 10;
        public float lifetime = 5f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ignore trigger colliders and player
            if (other.isTrigger || other.CompareTag("Player")) return;

            // Check if enemy
            var enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
