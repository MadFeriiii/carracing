using UnityEngine;

public class CombinedCameraFollow : MonoBehaviour
{
    public Transform target;        // The car's transform
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 5, -10); // Default offset, adjust as needed
    public float maxSpeed = 50f;    // Maximum speed of the car
    public float baseFOV = 60f;     // Base field of view
    public float maxFOV = 90f;      // Maximum field of view
    public float maxTiltAngle = 10f;// Maximum tilt angle during turns
    public float shakeAmount = 0.1f;// Amount of camera shake

    private Camera cam;
    private Vector3 initialPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        initialPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        // Calculate the desired position behind the car
        Vector3 desiredPosition = target.position + target.rotation * offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Rotate the camera to look at the car
        transform.LookAt(target);

        // Adjust the field of view based on the car's speed
        float speed = target.GetComponent<Rigidbody>().linearVelocity.magnitude;
        cam.fieldOfView = Mathf.Lerp(baseFOV, maxFOV, speed / maxSpeed);

        // Tilt the camera based on the car's steering
        float horizontalInput = Input.GetAxis("Horizontal");
        float tiltAngle = maxTiltAngle * horizontalInput;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, -tiltAngle);

        // Simple shake effect based on car's speed
        if (speed > 10)  // Adjust the threshold as needed
        {
            transform.localPosition = initialPosition + Random.insideUnitSphere * shakeAmount;
        }
        else
        {
            transform.localPosition = initialPosition;
        }
    }

}
