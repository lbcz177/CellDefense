using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFindWay : MonoBehaviour
{
    Transform[] waypoints;
    int currentPointIndex = 0;
    public EnemyData enemyData;
    // Start is called before the first frame update
    void Start()
    {
        GameObject pathObj = GameObject.Find("Path");
        if (pathObj == null)
        {
            return;
        }
        Transform pathParent = pathObj.transform;
        int childCount = pathParent.childCount;
        waypoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            waypoints[i] = pathParent.GetChild(i);
        }
    }

    public void SetWaypoints(Transform[] points)
    {
        waypoints = points;
    }

    // Update is called once per frame
    void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Vector3 targetPosition = waypoints[currentPointIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, enemyData.speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentPointIndex++;
            if (currentPointIndex >= waypoints.Length)
            {
                GameManager.Instance.TakeDamage(enemyData.damage);
                Destroy(gameObject);
            }
        }
    }
}