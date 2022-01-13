using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSpherePhysics : MonoBehaviour
{
    [SerializeField, Range(0, 100)]
    float maxSpeed = 10f;

    [SerializeField, Range(0, 100)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0, 10)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    Vector2 inputMovement;
    Vector3 velocity, desiredVelocity;
    Vector3 contactNormal;
    Rigidbody body;

    bool wantedToJump;
    int groundContactCount;
    bool OnGround => groundContactCount > 0;

    int jumpPhase;
    float minGroundDotProduct;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    public void OnMove(InputValue value)
    {
        inputMovement = value.Get<Vector2>();
        inputMovement = Vector2.ClampMagnitude(inputMovement, 1f);
        desiredVelocity = new Vector3(inputMovement.x, 0f, inputMovement.y) * maxSpeed;
    }

    public void OnJump(InputValue value)
    {
        wantedToJump |= true;
    }

    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();
        if (wantedToJump)
        {
            wantedToJump = false;
            Jump();
        }

        body.velocity = velocity;
        ClearState();
    }


    void UpdateState()
    {
        velocity = body.velocity;
        if (OnGround)
        {
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
            
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }


    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = CalculateJumpSpeed(jumpHeight);
            velocity += contactNormal * jumpSpeed;
        }
    }

    float CalculateJumpSpeed(float height)
    {
        float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * height);
        float alignedSpeed = Vector3.Dot(velocity, contactNormal);
        if (alignedSpeed > 0f)
        {
            //TODO: Still not sure why the jumpSpeed - alignedSpeed calculation is happening--preventing infinite vertical acceleration when spamming jump and having multiple air jumps?
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        return jumpSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckIfTouchingAnyGround(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckIfTouchingAnyGround(collision);
    }

    void CheckIfTouchingAnyGround(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }

    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float adjustedX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float adjustedZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (adjustedX - currentX) + zAxis * (adjustedZ - currentZ);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        // TODO: I don't understand the trig. behind this calculation. This is apparently supposed to be the equivalent of Vector3.ProjectOnPlane() (in our situation).
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }
}
