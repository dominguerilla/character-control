using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TODO: Refactor this as a PlayerAction. Right now, it needs its own PlayerInput.
/// </summary>
[RequireComponent(typeof(Camera), typeof(PlayerInput))]
public class OrbitingCamera : MonoBehaviour
{
    [SerializeField]
    Transform focusObject = default;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    [SerializeField]
    bool invertX, invertY;

    [SerializeField, Min(0f)]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    [SerializeField, Min(0f)]
    float upAlignmentSpeed = 360f;

    [SerializeField]
	LayerMask obstructionMask = -1;

    Vector2 orbitAngles = new Vector2(45f, 0f);
    Vector3 focusPosition, previousFocusPosition;
    float interpolator;
    float lastManualRotationTime;
    Vector2 inputMovement;
    Camera regularCamera;

    Quaternion gravityAlignment = Quaternion.identity;
    Quaternion orbitRotation;

    //TODO: Study how this is calculated. What does FOV mean for cameras? Aspect? nearClipPlane? Why use Tan()?
    Vector3 CameraHalfExtends {
        get{
            Vector3 halfExtends = new Vector3();
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }

    #region Monobehaviour Overrides
    private void Awake()
    {
        regularCamera = GetComponent<Camera>();
        UpdateFocusPosition();
        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
    }
    private void LateUpdate()
    {
        UpdateGravityAlignment();
        UpdateFocusPosition();
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            orbitRotation = Quaternion.Euler(orbitAngles);
        }
        Quaternion lookRotation = gravityAlignment * orbitRotation;
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPosition - lookDirection * distance;

        // TODO: Refactor!
        // What is rectOffset?
        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focusObject.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        // TODO: Read the documentation for Boxcast
        if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, obstructionMask))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    private void UpdateGravityAlignment()
    {
        Vector3 fromUp = gravityAlignment * Vector3.up;
        Vector3 toUp = CustomGravity.GetUpAxis(focusPosition);

        float dot = Mathf.Clamp(Vector3.Dot(fromUp, toUp), -1f, 1f);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        float maxAngle = upAlignmentSpeed * Time.deltaTime;

        Quaternion newAlignment = Quaternion.FromToRotation(fromUp, toUp) * gravityAlignment;
        if (angle < maxAngle)
        {
            gravityAlignment = newAlignment;
        }
        else
        {
            gravityAlignment = Quaternion.SlerpUnclamped(gravityAlignment, newAlignment, maxAngle / angle);
        }
        
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(focusPosition, focusRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(focusPosition, focusObject.position);
    }

    private void OnValidate(){
        if(maxVerticalAngle < minVerticalAngle){
            maxVerticalAngle = minVerticalAngle;
        }
    }
    #endregion

    private void OnLook(InputValue value){
        inputMovement = value.Get<Vector2>();
        inputMovement = Vector2.ClampMagnitude(inputMovement, 1f);
        inputMovement = Invert(inputMovement);
        inputMovement = SwitchAxes(inputMovement);
    }

    Vector2 Invert(Vector2 vector){
        float x = invertX ? -1f : 1f;
        float y = invertY ? -1f : 1f;
        return new Vector2(vector.x * x, vector.y * y);
    }
    Vector2 SwitchAxes(Vector2 vector){
        return new Vector2(vector.y, vector.x);
    }

    /**
    * Returns true of the orbitAngles were manually adjusted, false otherwise.
    */
    bool ManualRotation(){
        const float e = 0.001f;
        if (inputMovement.x < -e || inputMovement.x > e || inputMovement.y < -e || inputMovement.y > e){
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * inputMovement;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    bool AutomaticRotation(){
        if(Time.unscaledTime - lastManualRotationTime < alignDelay){
            return false;
        }

        Vector3 alignedDelta = Quaternion.Inverse(gravityAlignment) * (focusPosition - previousFocusPosition);
        Vector2 movement = new Vector2(
            alignedDelta.x,
            alignedDelta.z
        );
        float sqrMagnitude = movement.sqrMagnitude;
        if(sqrMagnitude < 0.000001f){
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(sqrMagnitude));
        float absDelta = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, sqrMagnitude);
        if(absDelta < alignSmoothRange){
            rotationChange *= absDelta/alignSmoothRange;
        }else if(180f - absDelta < alignSmoothRange){
            rotationChange *= (180f - absDelta)/alignSmoothRange;
        }

        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    static float GetAngle(Vector2 direction){
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle;
    }
    void UpdateFocusPosition()
    {
        previousFocusPosition = focusPosition;
        Vector3 targetPoint = focusObject.position;
        if (focusRadius > 0f)
        {
            float currentDistance = Vector3.Distance(targetPoint, focusPosition);
            interpolator = 1f;
            if (currentDistance > 0.01f && focusCentering > 0f)
            {
                interpolator = Mathf.Pow(1 - focusCentering, Time.unscaledDeltaTime);
            }
            if (currentDistance > focusRadius)
            {
                // This helps keep the target in view; helpful if the target moves very fast between one frame and the next.
                interpolator = Mathf.Min(interpolator, focusRadius / currentDistance);
            }
            focusPosition = Vector3.Lerp(targetPoint, focusPosition, interpolator);

        }
        else
        {
            focusPosition = focusObject.position;
        }
    }

    void ConstrainAngles(){
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        //Because we adjust orbitAngles incrementally, this should do.
        if(orbitAngles.y < 0f){
            orbitAngles.y += 360f;
        }else if(orbitAngles.y >= 360f){
            orbitAngles.y -= 360f;
        }
    }

    #region DEBUG
    public float GetCurrentDistanceFromTarget()
    {
        return Vector3.Distance(focusObject.position, focusPosition);
    }

    public float GetInterpolator()
    {
        return interpolator;
    }

    public float GetRadiusDistanceRatio(){
        return focusRadius / GetCurrentDistanceFromTarget();
    }
    #endregion
}
