using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSphereCustomGravity : PlayerAction
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
    Vector3 upAxis, rightAxis, forwardAxis;
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
        body.useGravity = false;
        meshRenderer = GetComponent<Renderer>();
        OnValidate();
    }

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    #region Input
    public override void Register(PlayerControls controls)
    {
        base.Register(controls);
        controls.Gameplay.Move.performed += OnMove;
        controls.Gameplay.Move.canceled += _ => ResetInputMovement();
        controls.Gameplay.Jump.performed += OnJump;
        
    }
    public override void Unregister()
    {
        base.Unregister();
        if (controls == null) return;
        controls.Gameplay.Move.performed -= OnMove;
        controls.Gameplay.Jump.performed -= OnJump;
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        inputMovement = ctx.ReadValue<Vector2>();
        inputMovement = Vector2.ClampMagnitude(inputMovement, 1f);
        desiredVelocity = new Vector3(inputMovement.x, 0f, inputMovement.y) * maxSpeed;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        wantedToJump |= true;
    }

    public void ResetInputMovement()
    {
        inputMovement = Vector2.zero;
        //inputMovement = Vector2.ClampMagnitude(inputMovement, 1f);
        desiredVelocity = new Vector3(inputMovement.x, 0f, inputMovement.y) * maxSpeed;
    }
    #endregion

    private void Update()
    {
        if (playerInputSpace)
        {
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }
    }
    private void FixedUpdate()
    {
        Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
        UpdateState();
        AdjustVelocity();
        if (wantedToJump)
        {
            wantedToJump = false;
            Jump(gravity);
        }

        velocity += gravity * Time.deltaTime;
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
            contactNormal = upAxis;
        }
    }

    void ClearState()
    {
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }


    void Jump(Vector3 gravity)
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
        jumpDirection = (jumpDirection + upAxis).normalized;

        float jumpSpeed = CalculateJumpSpeed(jumpHeight, jumpDirection, gravity);
        velocity += jumpDirection * jumpSpeed;
        
    }

    float CalculateJumpSpeed(float height, Vector3 surfaceNormal, Vector3 gravity)
    {
        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * height);
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

    /// <summary>
    /// Updates groundContactCount and contactNormal based on the given collision.
    /// </summary>
    void EvaluateCollision(Collision collision)
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float upDot = Vector3.Dot(upAxis, normal);
            if (upDot >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (upDot > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }

    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
        Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float adjustedX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float adjustedZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (adjustedX - currentX) + zAxis * (adjustedZ - currentZ);
    }

    /// <summary>
    /// Projects `direction` onto the plane specified by its normal, returning a normalized value.
    /// </summary>
    Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 planeNormal)
    {
        return (direction - planeNormal * Vector3.Dot(direction, planeNormal)).normalized;
    }

    /// <summary>
    /// Attempts to 'snap' this object to the ground and returns true if it does.
    /// Also adjusts this.velocity to move along the new contactNormal if snapping is successful.
    /// </summary>
    /// <returns></returns>
    bool SnapToGround()
    {
        if (physicsStepsSinceLastGrounded > 1 || physicsStepsSinceLastJumped <= 2)
        {
            return false;
        }

        // If you're going fast enough, you won't SnapToGround()
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }

        // If there's no ground to fall on
        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask))
        {
            return false;
        }

        // if upDot is low, the hit ground is too steep to snap to.
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }

        // snap!
        groundContactCount = 1;
        contactNormal = hit.normal;

        // If dot > 0f, then the current velocity is adjusted so that it travels perpendicular to the ground surface with the same speed
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    /// <summary>
    /// Returns minStairsDotProduct if layer includes the stairsMask, minGroundDotProduct otherwise.
    /// </summary>
    float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
    }

    /// <summary>
    /// Returns true if this GameObject is 'stuck' in a crevasse and is oriented (mostly) up, false otherwise
    /// </summary>
    /// <returns></returns>
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

    #region DEBUG
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        DrawOrientationAxes();
    }
    void DrawDesiredVelocityGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 xGizmo = new Vector3(desiredVelocity.x, 0, 0);
        Vector3 zGizmo = new Vector3(0, 0, desiredVelocity.z);
        Gizmos.DrawLine(Vector3.zero, xGizmo);
        Gizmos.DrawLine(Vector3.zero, zGizmo);
    }

    void DrawOrientationAxes()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.zero, upAxis);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, rightAxis);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, forwardAxis);

    }
    #endregion
}
