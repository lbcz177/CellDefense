using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField]GameObject prefab;
    [SerializeField]int initialSize;
    Queue<GameObject> pool = new Queue<GameObject>();
    public static ObjectPool Instance { get; private set; }
    void Awake()
    {
        Instance = this;
        for(int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject Get()
    {
        if(pool.Count == 0)
        {
            GameObject obj1 = Instantiate(prefab);
            obj1.SetActive(false);
            pool.Enqueue(obj1);
        }
        GameObject obj = pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}