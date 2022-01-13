using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
    [SerializeField]
    bool floatToSleep = false;

    [SerializeField]
    Color bodyAwake = Color.red;

    [SerializeField]
    Color bodyFloatDelaying = Color.yellow;

    Color originalColor;
    float floatDelay;
    Rigidbody body;
    MeshRenderer rend;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        rend = GetComponent<MeshRenderer>();
        originalColor = rend.material.color;
    }

    private void FixedUpdate()
    {
        if (floatToSleep)
        {
            if (body.IsSleeping())
            {
                floatDelay = 0f;
                rend.material.color = originalColor;
                return;
            }

            if (body.velocity.sqrMagnitude < 0.0001f)
            {
                floatDelay += Time.deltaTime;
                rend.material.color = bodyFloatDelaying;
                if (floatDelay >= 1f)
                {
                    return;
                }
            }
            else
            {
                floatDelay = 0f;
            }
        }

        rend.material.color = bodyAwake;
        body.AddForce(CustomGravity.GetGravity(body.position), ForceMode.Acceleration);
    }
}
