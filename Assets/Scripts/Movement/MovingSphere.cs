using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSphere : MonoBehaviour
{
    [SerializeField]
    Rect allowedArea = new Rect(-5f, -5f, 10f, 10f);

    [SerializeField, Range(0, 100)]
    float maxSpeed = 10f;

    [SerializeField, Range(0, 100)]
    float maxAcceleration = 10f;

    [SerializeField, Range(0f, 1f)]
    float bounciness = 0.5f;

    Vector2 inputMovement;
    Vector3 velocity;

    public void OnMove(InputValue value)
    {
        inputMovement = value.Get<Vector2>();
        inputMovement = Vector2.ClampMagnitude(inputMovement, 1f);
    }

    private void Update()
    {
        CalculateVelocity();

        Vector3 displacement = velocity * Time.deltaTime;
        Vector3 newPosition = transform.localPosition + displacement;

        newPosition = ClampNewPosition(newPosition);
        transform.localPosition = newPosition;
    }

    void CalculateVelocity()
    {
        Vector3 desiredVelocity = new Vector3(inputMovement.x, 0f, inputMovement.y) * maxSpeed;
        float maxSpeedChange = maxAcceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        /* Shorthand for the following
        if (velocity.x < desiredVelocity.x)
        {
            velocity.x += Mathf.Min(velocity.x + maxSpeedChange, desiredVelocity.x);
        }
        else if (velocity.x > desiredVelocity.x)
        {
            velocity.x -= Mathf.Max(velocity.x - maxSpeedChange, desiredVelocity.x);
        }
        if (velocity.z < desiredVelocity.z)
        {
            velocity.z += Mathf.Min(velocity.z + maxSpeedChange, desiredVelocity.z);
        }
        else if (velocity.z > desiredVelocity.z)
        {
            velocity.z -= Mathf.Max(velocity.z - maxSpeedChange, desiredVelocity.z);
        }
        
         */
    }

    Vector3 ClampNewPosition(Vector3 newPosition)
    {
        if (newPosition.x < allowedArea.xMin)
        {
            newPosition.x = allowedArea.xMin;
            velocity.x = -velocity.x * bounciness;
        }
        else if (newPosition.x > allowedArea.xMax)
        {
            newPosition.x = allowedArea.xMax;
            velocity.x = -velocity.x * bounciness;
        }

        if (newPosition.z < allowedArea.yMin)
        {
            newPosition.z = allowedArea.yMin;
            velocity.z = -velocity.z * bounciness;
        }
        else if (newPosition.z > allowedArea.yMax)
        {
            newPosition.z = allowedArea.yMax;
            velocity.z = -velocity.z * bounciness;
        }
        return newPosition;
    }
}
