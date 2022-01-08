using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityBox : GravitySource
{
    [SerializeField]
    float gravity = 9.81f;

    [SerializeField]
    Vector3 boundingVector = Vector3.one;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        boundingVector = Vector3.Max(Vector3.zero, boundingVector);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one); // Why not use transform.localScale for this?
        Gizmos.DrawWireCube(Vector3.zero, boundingVector * 2f);
    }

    public override Vector3 GetGravity(Vector3 position)
    {
        return Physics.gravity;
    }
}
