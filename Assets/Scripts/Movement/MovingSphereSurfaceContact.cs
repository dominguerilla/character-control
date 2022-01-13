using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSphereSurfaceContact : MonoBehaviour
{

    [SerializeField]
    Transform playerInputSpace = default;

    [SerializeField, Range(0, 100)]
    float maxSpeed = 10f;

    [SerializeField, Range(0, 100)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0, 10)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f, maxStairsAngle = 50f;

    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;

    [SerializeField, Min(0f)]
    float probeDistance = 1f;

    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;

    Vector2 inputMovement;
    Vector3 velocity, desiredVelocity;
    Vector3 contactNormal, steepNormal;
    Rigidbody body;
    Renderer meshRenderer;

    bool wantedToJump;
    int groundContactCount, steepContactCount;
    bool OnGround => groundContactCount > 0;
    bool OnSteep => steepContactCount > 0;

    int jumpPhase;
    int physicsStepsSinceLastGrounded, physicsStepsSinceLastJumped;
    float minGroundDotProduct, minStairsDotProduct;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<Renderer>();
        OnValidate();
    }

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    public void OnMove(InputValue value)
    {
        inputMovement = value.Get<Vector2>();
        inputMovement = Vector2.ClampMagnitude(inputMovement, 1f);
        if(playerInputSpace){
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            desiredVelocity = (forward * inputMovement.y + right * inputMovement.x)* maxSpeed;
        }else{
            desiredVelocity = new Vector3(inputMovement.x, 0f, inputMovement.y) * maxSpeed;
        }
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
        physicsStepsSinceLastGrounded += 1;
        physicsStepsSinceLastJumped += 1;
        velocity = body.velocity;
        if (OnGround || SnapToGround() || CheckSteepContacts())
        {
            physicsStepsSinceLastGrounded = 0;

            // prevent false landing
            if (physicsStepsSinceLastJumped > 1)
            {
                jumpPhase = 0;
            }

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
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }


    void Jump()
    {
        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            // Reset air jumps after a wall jump
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            // if the agent falls off an edge and tries to airjump
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }

        jumpPhase += 1;
        physicsStepsSinceLastJumped = 0;
        
        // Add upward bias
        jumpDirection = (jumpDirection + Vector3.up).normalized;

        float jumpSpeed = CalculateJumpSpeed(jumpHeight, jumpDirection);
        velocity += jumpDirection * jumpSpeed;
        
    }

    float CalculateJumpSpeed(float height, Vector3 surfaceNormal)
    {
        float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * height);
        float alignedSpeed = Vector3.Dot(velocity, surfaceNormal);
        if (alignedSpeed > 0f)
        {
            //TODO: Still not sure why the jumpSpeed - alignedSpeed calculation is happening--preventing infinite vertical acceleration when spamming jump and having multiple air jumps?
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        return jumpSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normal;
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

    bool SnapToGround()
    {
        if (physicsStepsSinceLastGrounded > 1 || physicsStepsSinceLastJumped <= 2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask))
        {
            return false;
        }
        if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }

        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        // TODO: Again; not understanding the (velocity - hit.normal * dot) bit. What is the result of subtracting two directional vectors, one of them scaled by the dot product of the two?
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
    }

    bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }
}
