using UnityEngine;

public class Projectile : MonoBehaviour
{
    private GameObject _target;
    private float _damage;

    public void Initialize(GameObject target, float damage)
    {
        _target = target;
        _damage = damage;
        // Add logic to move the projectile towards the target
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == _target)
        {
            Debug.Log($"Projectile hit target and dealt {_damage} damage.");
            // Add damage application logic here
            Destroy(gameObject);
        }
    }
}