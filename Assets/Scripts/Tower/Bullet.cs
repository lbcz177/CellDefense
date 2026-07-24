using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]float speed;
    [SerializeField]float damage = 5;
    float lifeTime = 3f;
    Transform enemyTarget;

    void OnEnable()
    {
        Invoke("SelfReturn", lifeTime);
    }
    void Update()
    {
        if(enemyTarget == null)
        {
            ObjectPool.Instance.Return(gameObject);
            return;
        }
        transform.position = Vector3.MoveTowards(transform.position, enemyTarget.transform.position, speed * Time.deltaTime);

    }
    void OnDisable()
    {
        CancelInvoke();
    }
    void SelfReturn() { ObjectPool.Instance.Return(gameObject); }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyHealth>().TakeDamage(damage);
            ObjectPool.Instance.Return(gameObject);
        }
    }

    public void Init(Transform target, float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        this.enemyTarget = target;
    }
}