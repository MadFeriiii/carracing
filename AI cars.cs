using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    public float maxSpeed = 150f;
    public float acceleration = 30f;
    public float steeringSensitivity = 5f;
    public float decelerationSpeed = 5f;
    public List<Transform> waypoints;
    public float waypointRadius = 5f;
    public float waypointRecheckDistance = 10f; // Distance to recheck for the nearest waypoint
    public float respawnTime = 5f; // Time to wait before respawning
    public float minSpeedForRespawn = 1f; // Minimum speed to consider the car stopped
    public float positionCheckInterval = 1f; // Interval to check the car's position
    public float minPositionChange = 0.5f; // Minimum position change to consider the car moving

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Audio")]
    public AudioSource engineSound;
    public Transform playerTransform;
    public float maxHearingDistance = 50f;

    [Header("Tire Effects")]
    public ParticleSystem frontLeftSmoke;
    public ParticleSystem frontRightSmoke;
    public ParticleSystem rearLeftSmoke;
    public ParticleSystem rearRightSmoke;

    private Rigidbody rb;
    private int currentWaypointIndex = 0;
    private float currentSpeed;
    private float previousSteerAngle = 0f; // To keep track of previous steering angle
    private float respawnTimer = 0f; // Timer to track respawn time
    private Vector3 lastPosition; // Last position of the car
    private float positionCheckTimer = 0f; // Timer to track position check interval

    private WheelFrictionCurve FLwheelFriction;
    private float FLWextremumSlip;
    private WheelFrictionCurve FRwheelFriction;
    private float FRWextremumSlip;
    private WheelFrictionCurve RLwheelFriction;
    private float RLWextremumSlip;
    private WheelFrictionCurve RRwheelFriction;
    private float RRWextremumSlip;

    private int currentGear = 1;
    private const int maxGear = 6;
    private const float gearChangeSpeed = 20f; // Speed interval for gear changes

    private bool raceStarted = false; // Flag to check if the race has started

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing!");
        }

        // Initial setup to calculate the drift value of the car
        FLwheelFriction = new WheelFrictionCurve();
        FLwheelFriction.extremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLWextremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLwheelFriction.extremumValue = frontLeftCollider.sidewaysFriction.extremumValue;
        FLwheelFriction.asymptoteSlip = frontLeftCollider.sidewaysFriction.asymptoteSlip;
        FLwheelFriction.asymptoteValue = frontLeftCollider.sidewaysFriction.asymptoteValue;
        FLwheelFriction.stiffness = frontLeftCollider.sidewaysFriction.stiffness;

        FRwheelFriction = new WheelFrictionCurve();
        FRwheelFriction.extremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRWextremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRwheelFriction.extremumValue = frontRightCollider.sidewaysFriction.extremumValue;
        FRwheelFriction.asymptoteSlip = frontRightCollider.sidewaysFriction.asymptoteSlip;
        FRwheelFriction.asymptoteValue = frontRightCollider.sidewaysFriction.asymptoteValue;
        FRwheelFriction.stiffness = frontRightCollider.sidewaysFriction.stiffness;

        RLwheelFriction = new WheelFrictionCurve();
        RLwheelFriction.extremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLWextremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLwheelFriction.extremumValue = rearLeftCollider.sidewaysFriction.extremumValue;
        RLwheelFriction.asymptoteSlip = rearLeftCollider.sidewaysFriction.asymptoteSlip;
        RLwheelFriction.asymptoteValue = rearLeftCollider.sidewaysFriction.asymptoteValue;
        RLwheelFriction.stiffness = rearLeftCollider.sidewaysFriction.stiffness;

        RRwheelFriction = new WheelFrictionCurve();
        RRwheelFriction.extremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRWextremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRwheelFriction.extremumValue = rearRightCollider.sidewaysFriction.extremumValue;
        RRwheelFriction.asymptoteSlip = rearRightCollider.sidewaysFriction.asymptoteSlip;
        RRwheelFriction.asymptoteValue = rearRightCollider.sidewaysFriction.asymptoteValue;
        RRwheelFriction.stiffness = rearRightCollider.sidewaysFriction.stiffness;
    }

    private void FixedUpdate()
    {
        if (!raceStarted || waypoints.Count == 0) return; // Check if the race has started

        Vector3 direction = waypoints[currentWaypointIndex].position - transform.position;
        direction.y = 0;
        float distanceToWaypoint = direction.magnitude;
        direction.Normalize();

        Vector3 localTarget = transform.InverseTransformPoint(waypoints[currentWaypointIndex].position);
        float steerAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

        // Apply steering sensitivity to ensure proper turning
        steerAngle = Mathf.Clamp(steerAngle * steeringSensitivity, -45f, 45f);

        // Only change steering if the angle change is significant
        if (Mathf.Abs(steerAngle - previousSteerAngle) > 1f) // Adjust this threshold as needed
        {
            previousSteerAngle = steerAngle;

            // Steer the front wheels
            frontLeftCollider.steerAngle = steerAngle;
            frontRightCollider.steerAngle = steerAngle;
        }

        // Adjust speed based on steering angle
        if (Mathf.Abs(steerAngle) > 10f) // Slow down in turns
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed / decelerationSpeed, Time.deltaTime);
        }
        else // Speed up on straights
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * acceleration);
        }

        // Apply motor torque to all wheels
        frontLeftCollider.motorTorque = currentSpeed;
        frontRightCollider.motorTorque = currentSpeed;
        rearLeftCollider.motorTorque = currentSpeed;
        rearRightCollider.motorTorque = currentSpeed;

        // Update wheel visuals
        UpdateWheelVisuals(frontLeftCollider, frontLeftMesh);
        UpdateWheelVisuals(frontRightCollider, frontRightMesh);
        UpdateWheelVisuals(rearLeftCollider, rearLeftMesh);
        UpdateWheelVisuals(rearRightCollider, rearRightMesh);

        // Update engine sound based on speed
        AdjustEngineSoundVolume();
        AdjustEngineSoundPitch(); // Call the new method to adjust pitch
        HandleGearChanges();

        // Emit smoke and skid marks
        EmitTireEffects();

        // Check if the car is close to the waypoint
        if (distanceToWaypoint < waypointRadius)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }

        // Recheck for the nearest waypoint if the car is far from the current waypoint
        if (distanceToWaypoint > waypointRecheckDistance)
        {
            currentWaypointIndex = FindNearestWaypoint();
        }

        // Check if the car has stopped or is stuck
        positionCheckTimer += Time.deltaTime;
        if (positionCheckTimer >= positionCheckInterval)
        {
            float positionChange = Vector3.Distance(transform.position, lastPosition);
            if (currentSpeed < minSpeedForRespawn && positionChange < minPositionChange)
            {
                respawnTimer += positionCheckTimer;
                if (respawnTimer >= respawnTime)
                {
                    RespawnAtNearestWaypoint();
                    respawnTimer = 0f;
                }
            }
            else
            {
                respawnTimer = 0f;
            }
            lastPosition = transform.position;
            positionCheckTimer = 0f;
        }
    }

    private void AdjustEngineSoundPitch()
    {
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, currentSpeed);
        float pitch = Mathf.Lerp(0.8f, 1.5f, speedFactor); // Adjusted pitch range
        engineSound.pitch = pitch;
    }

    private int FindNearestWaypoint()
    {
        int nearestIndex = currentWaypointIndex;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void UpdateWheelVisuals(WheelCollider collider, Transform mesh)
    {
        Quaternion rot;
        Vector3 pos;
        collider.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    private void AdjustEngineSoundVolume()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            float volume = Mathf.Clamp01(1 - (distanceToPlayer / maxHearingDistance));
            engineSound.volume = volume;
        }
    }

    private void HandleGearChanges()
    {
        int newGear = Mathf.Clamp(Mathf.FloorToInt(currentSpeed / gearChangeSpeed) + 1, 1, maxGear);
        if (newGear != currentGear)
        {
            currentGear = newGear;
            engineSound.pitch = 1 + (currentGear - 1) * 0.2f; // Adjust pitch based on gear
        }
    }

    private void EmitTireEffects()
    {
        if (Mathf.Abs(frontLeftCollider.steerAngle) > 20f || Mathf.Abs(frontRightCollider.steerAngle) > 20f) // If turning sharply
        {
            EmitSmoke(frontLeftSmoke);
            EmitSmoke(frontRightSmoke);
            EmitSmoke(rearLeftSmoke);
            EmitSmoke(rearRightSmoke);
        }
        else
        {
            StopSmoke(frontLeftSmoke);
            StopSmoke(frontRightSmoke);
            StopSmoke(rearLeftSmoke);
            StopSmoke(rearRightSmoke);
        }
    }

    private void EmitSmoke(ParticleSystem smoke)
    {
        if (!smoke.isPlaying)
        {
            smoke.Play();
        }
    }

    private void StopSmoke(ParticleSystem smoke)
    {
        if (smoke.isPlaying)
        {
            smoke.Stop();
        }
    }

    private void RespawnAtNearestWaypoint()
    {
        int nearestWaypointIndex = FindNearestWaypoint();
        Transform nearestWaypoint = waypoints[nearestWaypointIndex];

        // Find the next waypoint to determine the correct direction
        int nextWaypointIndex = (nearestWaypointIndex + 1) % waypoints.Count;
        Transform nextWaypoint = waypoints[nextWaypointIndex];

        // Calculate the direction from the nearest waypoint to the next waypoint
        Vector3 directionToNextWaypoint = (nextWaypoint.position - nearestWaypoint.position).normalized;

        // Set the car's position and rotation
        transform.position = nearestWaypoint.position;
        transform.rotation = Quaternion.LookRotation(directionToNextWaypoint);

        // Reset the car's velocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currentWaypointIndex = nearestWaypointIndex;
        Debug.Log("AI car respawned at nearest waypoint.");
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 1f);
                if (i > 0)
                {
                    Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
                }
            }
        }
    }

    public void StartRace()
    {
        raceStarted = true;
    }
}
