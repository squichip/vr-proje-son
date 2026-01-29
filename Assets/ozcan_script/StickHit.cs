using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class StickHit_Final : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Örümceklerin tag'i")]
    public string spiderTag = "Spider";

    [Header("Force Tuning")]
    [Tooltip("En az vurma kuvveti (sopa yavaşken bile)")]
    public float baseImpulse = 1.5f;

    [Tooltip("Sopa hızına göre ekstra kuvvet çarpanı")]
    public float velocityMultiplier = 0.35f;

    [Tooltip("Kuvvetin üst limiti (uçmayı keser)")]
    public float maxImpulse = 3.0f;

    [Tooltip("Yukarı fırlamasın diye Y bileşenini sınırla (0-0.25 iyi)")]
    public float maxUp = 0.12f;

    [Header("Physics On Hit (YOL A)")]
    public bool disableKinematicOnHit = true;
    public bool enableGravityOnHit = true;

    [Header("Per-Spider Cooldown")]
    [Tooltip("Aynı örümceğe art arda force basmayı engeller (sn)")]
    public float perSpiderCooldown = 0.15f;

    private Vector3 _lastPos;
    private Vector3 _velocity;

    private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>();

    private void Start()
    {
        _lastPos = transform.position;
    }

    private void FixedUpdate()
    {
        _velocity = (transform.position - _lastPos) / Time.fixedDeltaTime;
        _lastPos = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        if (!other.CompareTag(spiderTag))
            return;

        int id = other.GetInstanceID();
        if (_lastHitTime.TryGetValue(id, out float lastT))
        {
            if (Time.time - lastT < perSpiderCooldown)
                return;
        }
        _lastHitTime[id] = Time.time;

        // 1) NavMesh/Wander kapat (dolaşma + fizik kavga etmesin)
        SpiderWander wander = other.GetComponent<SpiderWander>();
        if (wander != null) wander.DisableNavForPhysics();

        NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled) agent.enabled = false;

        // 2) Rigidbody al
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        // 3) Fizik moduna geçir (YOL A)
        if (disableKinematicOnHit) rb.isKinematic = false;
        if (enableGravityOnHit) rb.useGravity = true;

        // 4) Vuruş yönü = sopanın anlık hareket yönü
        Vector3 dir = (_velocity.sqrMagnitude > 0.0001f) ? _velocity.normalized : transform.forward;

        // Yukarı bileşeni sınırla (uçmayı azaltır)
        dir.y = Mathf.Clamp(dir.y, -0.05f, maxUp);
        dir.Normalize();

        // 5) Kuvvet hesapla (uçmayı kesmek için clamp)
        float speed = _velocity.magnitude;
        float impulse = baseImpulse + (speed * velocityMultiplier);
        impulse = Mathf.Clamp(impulse, 0f, maxImpulse);

        // 6) Çarpma noktasından it (daha gerçekçi)
        Vector3 hitPoint = collision.GetContact(0).point;
        rb.AddForceAtPosition(dir * impulse, hitPoint, ForceMode.Impulse);

        // 7) Fazla dönmesin diye küçük dengeleme (opsiyonel ama iyi)
        rb.angularVelocity *= 0.6f;
    }
}
