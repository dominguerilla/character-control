using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityBox : GravitySource
{
    [SerializeField]
    float gravity = 9.81f;

    [SerializeField]
    Vector3 boundingVector = Vector3.one;

    [SerializeField, Min(0f)]
    float innerRadius = 0f, innerFalloffDistance = 0f;

    float innerFalloffFactor;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        boundingVector = Vector3.Max(Vector3.zero, boundingVector);
        float maxInner = Mathf.Min(boundingVector.x, boundingVector.y, boundingVector.z);
        innerRadius = Mathf.Min(innerRadius, maxInner);
        innerFalloffDistance = Mathf.Max(
            Mathf.Min(innerFalloffDistance, maxInner),
            innerRadius
        );
        innerFalloffFactor = 1f / (innerFalloffDistance - innerRadius);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one); // Why not use transform.localScale for this?
        Gizmos.DrawWireCube(Vector3.zero, boundingVector * 2f);

        if(innerFalloffDistance > innerRadius)
        {
            Vector3 innerDimensions = boundingVector - new Vector3(innerRadius, innerRadius, innerRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(Vector3.zero, innerDimensions * 2f);
        }
        if(innerRadius > 0)
        {
            Vector3 innerFalloffDimensions = boundingVector - new Vector3(innerFalloffDistance, innerFalloffDistance, innerFalloffDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, innerFalloffDimensions * 2f);
        }

    }

    float GetGravityComponent(float coordinate, float distance)
    {
        if(distance > innerFalloffDistance)
        {
            return 0f;
        }
        float g = gravity;
        if(distance > innerRadius)
        {
            g *= 1f - (distance - innerRadius) * innerFalloffFactor;
        }
        return coordinate > 0f ? -g : g;
    }

    public override Vector3 GetGravity(Vector3 position)
    {
        position = transform.InverseTransformDirection(position - transform.position);
        Vector3 gravityVector = Vector3.zero;
        Vector3 distances;
        distances.x = boundingVector.x - Mathf.Abs(position.x);
        distances.y = boundingVector.y - Mathf.Abs(position.y);
        distances.z = boundingVector.z - Mathf.Abs(position.z);
        //TODO: Is there a clearer way to structure this?
        if(distances.x < distances.y)
        {
            if(distances.x < distances.z)
            {
                gravityVector.x = GetGravityComponent(position.x, distances.x);
            }
            else
            {
                gravityVector.z = GetGravityComponent(position.z, distances.z);
            }
        }else if(distances.y < distances.z)
        {
            gravityVector.y = GetGravityComponent(position.y, distances.y);
        }
        else
        {
            gravityVector.z = GetGravityComponent(position.z, distances.z);
        }
        return transform.TransformDirection(gravityVector);
    }
}
