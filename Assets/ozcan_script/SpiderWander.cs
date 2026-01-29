using UnityEngine;
using UnityEngine.AI;

public class SpiderWander : MonoBehaviour
{
    public Transform roomCenter;
    public float wanderRadius = 4f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    private NavMeshAgent agent;
    private float waitTimer;
    private float nextWait;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("[SpiderWander] NavMeshAgent yok!", this);
            enabled = false;
            return;
        }

        waitTimer = 0f;
        nextWait = Random.Range(waitTimeMin, waitTimeMax);
    }

    void Start()
    {
        // Start'ta bir kez hedef dene
        TryPickNewDestination();
    }

    void Update()
    {
        // Agent kapalıysa çık
        if (!agent.enabled) return;

        // ✅ EN KRİTİK: NavMesh üstünde değilse remainingDistance'a ASLA dokunma
        if (!agent.isOnNavMesh) return;

        // Yol bekleniyorsa çık
        if (agent.pathPending) return;

        // Hedefe geldiyse bekle sonra yeni hedef seç
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= nextWait)
            {
                TryPickNewDestination();
            }
        }
    }

    void TryPickNewDestination()
    {
        waitTimer = 0f;
        nextWait = Random.Range(waitTimeMin, waitTimeMax);

        Vector3 center = roomCenter ? roomCenter.position : transform.position;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * wanderRadius;
            randomPoint.y = center.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh)
                    agent.SetDestination(hit.position);

                return;
            }
        }
    }

    public void DisableNavForPhysics()
    {
        if (agent) agent.enabled = false;
    }
}
