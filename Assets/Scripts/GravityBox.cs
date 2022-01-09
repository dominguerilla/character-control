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

    [SerializeField, Min(0f)]
    float outerDistance = 0f, outerFalloffDistance = 0f;

    float innerFalloffFactor, outerFalloffFactor;

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
        outerFalloffDistance = Mathf.Max(outerFalloffDistance, outerDistance);
        innerFalloffFactor = 1f / (innerFalloffDistance - innerRadius);
        outerFalloffFactor = 1f / (outerFalloffDistance - outerDistance);
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
        if(outerDistance > 0f)
        {
            Gizmos.color = Color.yellow;
            DrawGizmosOuterCube(outerDistance);
        }
        if(outerFalloffDistance > outerDistance)
        {
            Gizmos.color = Color.cyan;
            DrawGizmosOuterCube(outerFalloffDistance);
        }
    }

    void DrawGizmosRect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    void DrawGizmosOuterCube(float distance)
    {
        Vector3 a, b, c, d;

        // Draw the right face (+X)
        a.y = b.y = boundingVector.y;
        d.y = c.y = -boundingVector.y;
        b.z = c.z = boundingVector.z;
        d.z = a.z = -boundingVector.z;
        a.x = b.x = c.x = d.x = boundingVector.x + distance;
        DrawGizmosRect(a, b, c, d);

        // Draw the left face (-X)
        a.x = b.x = c.x = d.x = -a.x;
        DrawGizmosRect(a, b, c, d);

        // Draw the top face (+Y)
        a.x = d.x = boundingVector.x;
        b.x = c.x = -boundingVector.x;
        a.z = b.z = boundingVector.z;
        c.z = d.z = -boundingVector.z;
        a.y = b.y = c.y = d.y = boundingVector.y + distance;
        DrawGizmosRect(a, b, c, d);

        // Draw the bottom face (-Y)
        a.y = b.y = c.y = d.y = -a.y;
        DrawGizmosRect(a, b, c, d);

        // Draw the forward face (+Z)
        a.y = b.y = boundingVector.y;
        c.y = d.y = -boundingVector.y;
        a.x = d.x = -boundingVector.x;
        b.x = c.x = boundingVector.x;
        a.z = b.z = c.z = d.z = boundingVector.z + distance;
        DrawGizmosRect(a, b, c, d);

        // Draw the toward face (-Z)
        a.z = b.z = c.z = d.z = -a.z;
        DrawGizmosRect(a, b, c, d);

        // this is the sqrt of 1/3; we are offsetting each boundary point in all three dimensions equally
        // TODO: study the math. Might have to do with Pythagorean's Theorem, but in 3 dimensions?
        distance *= 0.5773502692f;
        Vector3 size = boundingVector;
        size.x = 2f * (boundingVector.x + distance);
        size.y = 2f * (boundingVector.y + distance);
        size.z = 2f * (boundingVector.z + distance);
        Gizmos.DrawWireCube(Vector3.zero, size);
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
