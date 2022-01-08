using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySphere : GravitySource
{
    [SerializeField]
    float gravity = 9.81f;

    [SerializeField, Min(0f)]
    float outerRadius = 20f, outerFalloffRadius = 30f;

    float outerFalloffFactor;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        outerFalloffRadius = Mathf.Max(outerRadius, outerFalloffRadius);
        outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
    }

    public override Vector3 GetGravity(Vector3 position)
    {
        Vector3 gravityDirection = transform.position - position;
        float distance = gravityDirection.magnitude;
        if ( distance > outerFalloffRadius)
        {
            return Vector3.zero;
        }
        float g = gravity / distance;
        if(distance > outerRadius)
        {
            g *= 1 - (distance - outerRadius) * outerFalloffFactor;
        }
        return gravityDirection * g;
    }

    private void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, outerRadius);

        if(outerFalloffRadius > outerRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p, outerFalloffRadius);
        }

    }
}
