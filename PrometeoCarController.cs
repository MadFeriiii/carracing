using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrometeoCarController : MonoBehaviour
{
    //CAR SETUP
    [Space(20)]
    [Space(10)]
    [Range(20, 190)]
    public int maxSpeed = 90; //The maximum speed that the car can reach in km/h.
    [Range(10, 120)]
    public int maxReverseSpeed = 45; //The maximum speed that the car can reach while going on reverse in km/h.
    [Range(1, 10)]
    public int accelerationMultiplier = 2; // How fast the car can accelerate. 1 is a slow acceleration and 10 is the fastest.
    [Space(10)]
    [Range(10, 45)]
    public int maxSteeringAngle = 27; // The maximum angle that the tires can reach while rotating the steering wheel.
    [Range(0.1f, 1f)]
    public float steeringSpeed = 0.5f; // How fast the steering wheel turns.
    [Space(10)]
    public bool turboActive = false; // New turbo active flag
    [Range(1, 10)]
    public float turboMultiplier = 1f; // New turbo multiplier
    public int brakeForce = 350; // The strength of the wheel brakes.
    [Range(1, 10)]
    public int decelerationMultiplier = 2; // How fast the car decelerates when the user is not using the throttle.
    [Range(1, 10)]
    public int handbrakeDriftMultiplier = 5; // How much grip the car loses when the user hit the handbrake.
    [Space(10)]
    public Vector3 bodyMassCenter; // This is a vector that contains the center of mass of the car.
    public int numberOfGears = 5; // Number of gears, adjustable in the inspector
    public Text gearText; // UI Text to display the current gear
    private int currentGear = 1; // Current gear
    //private float gearChangeSpeed = 20f; // Speed interval for gear changes
    public AudioSource carEngineIdleSound; // Audio source for idle engine sound
    public AudioSource carEngineSlowSound; // Audio source for slow engine sound
    public AudioSource carEngineMediumSound; // Audio source for medium speed engine sound
    public AudioSource carEngineHighSound; // Audio source for high speed engine sound

    //WHEELS
    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;
    [Space(10)]
    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;
    [Space(10)]
    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;
    [Space(10)]
    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;

    //PARTICLE SYSTEMS
    [Space(20)]
    public bool useEffects = false;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    [Space(10)]
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    //SPEED TEXT (UI)
    [Space(20)]
    public bool useUI = false;
    public Text carSpeedText; // Used to store the UI object that is going to show the speed of the car.

    //SOUNDS
    [Space(20)]
    public bool useSounds = false;
    public AudioSource carEngineSound; // This variable stores the sound of the car engine.
    public AudioSource tireScreechSound; // This variable stores the sound of the tire screech (when the car is drifting).
    float initialCarEngineSoundPitch; // Used to store the initial pitch of the car engine sound.

    //CONTROLS
    [Space(20)]
    public bool useTouchControls = false;
    public GameObject throttleButton;
    PrometeoTouchInput throttlePTI;
    public GameObject reverseButton;
    PrometeoTouchInput reversePTI;
    public GameObject turnRightButton;
    PrometeoTouchInput turnRightPTI;
    public GameObject turnLeftButton;
    PrometeoTouchInput turnLeftPTI;
    public GameObject turboButton; // New turbo button
    PrometeoTouchInput turboPTI; // New turbo
    public GameObject handbrakeButton;
    PrometeoTouchInput handbrakePTI;

    //CAR DATA
    [HideInInspector]
    public float carSpeed; // Used to store the speed of the car.
    [HideInInspector]
    public bool isDrifting; // Used to know whether the car is drifting or not.
    [HideInInspector]
    public bool isTractionLocked; // Used to know whether the traction of the car is locked or not.
    

    public bool isPlayer; // To distinguish between player and AI cars
    public RaceManager raceManager;

    public AudioSource engineSound;
    public float maxHearingDistance = 50f;

    //PRIVATE VARIABLES
    Rigidbody carRigidbody; // Stores the car's rigidbody.
    float steeringAxis; // Used to know whether the steering wheel has reached the maximum value. It goes from -1 to 1.
    float throttleAxis; // Used to know whether the throttle has reached the maximum value. It goes from -1 to 1.
    float driftingAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;
    bool touchControlsSetup = false;
    public ParticleSystem turboEffect; // New turbo effect
    public Camera mainCamera; // Reference to the main camera
    float originalFOV; // Original camera FOV
    float turboFOV = 100f; // Increased FOV during turbo
    public AudioSource turboSound;
    public Slider turboBar; // UI element for the turbo bar
    public float turboDuration = 2f; // Duration of the turbo
    private float currentTurboTime; // Current time left for turbo
    //private bool isRecharging= false; // Flag to check if turbo is recharging
    public float rechargeRate = 0.5f; // Rate at which the turbo bar recharges
    private bool raceStarted = false;
    private WheelFrictionCurve FLwheelFriction;
    private WheelFrictionCurve FRwheelFriction;
    private WheelFrictionCurve RLwheelFriction;
    private WheelFrictionCurve RRwheelFriction;
    public float[] gearSpeedRanges;
    private float FLWextremumSlip;
    private float FRWextremumSlip;
    private float RLWextremumSlip;
    private float RRWextremumSlip;
    // New properties for CarAudio integration
    public float RevsFactor { get; private set; }
    public float AccelInput { get; private set; }
    public float AvgSkid { get; private set; }
    public float SpeedFactor { get; private set; }
    public AudioSource gearChangeSound;
    // Start is called before the first frame update
    void Start()
    {
        if (gearText != null)
        {
            gearText.text = "Gear: " + currentGear;
        }
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        if (gearChangeSound == null)
        {
            Debug.LogWarning("Gear change sound is not assigned!");
        }
        if (carEngineSound != null)
        {
            initialCarEngineSoundPitch = carEngineSound.pitch;
        }

        if (useUI)
        {
            InvokeRepeating("CarSpeedUI", 0f, 0.1f);
        }
        else if (!useUI)
        {
            if (carSpeedText != null)
            {
                carSpeedText.text = "0";
            }
        }

        // Stop all engine sounds initially
        StopAllEngineSounds();

        if (!useEffects)
        {
            if (RLWParticleSystem != null)
            {
                RLWParticleSystem.Stop();
            }
            if (RRWParticleSystem != null)
            {
                RRWParticleSystem.Stop();
            }
            if (RLWTireSkid != null)
            {
                RLWTireSkid.emitting = false;
            }
            if (RRWTireSkid != null)
            {
                RRWTireSkid.emitting = false;
            }
        }

        if (useTouchControls)
        {
            if (throttleButton != null && reverseButton != null &&
                turnRightButton != null && turnLeftButton != null &&
                handbrakeButton != null && turboButton != null) // Check for turbo button
            {
                throttlePTI = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePTI = reverseButton.GetComponent<PrometeoTouchInput>();
                turnLeftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();
                turnRightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();
                handbrakePTI = handbrakeButton.GetComponent<PrometeoTouchInput>();
                turboPTI = turboButton.GetComponent<PrometeoTouchInput>(); // Initialize turbo touch input
                touchControlsSetup = true;
            }
            else
            {
                String ex = "Touch controls are not completely set up. You must drag and drop your scene buttons in the" +
                            " PrometeoCarController component.";
                Debug.LogWarning(ex);
            }
        }
        if (mainCamera != null)
        {
            originalFOV = mainCamera.fieldOfView;
        }

        // Initialize turbo bar
        if (turboBar != null)
        {
            turboBar.maxValue = turboDuration;
            turboBar.value = turboDuration;
            currentTurboTime = turboDuration;
        }

        // Initialize turbo sound
        if (turboSound == null)
        {
            Debug.LogWarning("Turbo sound is not assigned!");
        }
    }


    // Update is called once per frame
    // Update is called once per frame
    void Update()
    {
        if (!raceStarted) return;

        HandleGearChanges();

        // Update new properties
        RevsFactor = Mathf.InverseLerp(0, maxSpeed, carSpeed);
        AccelInput = Input.GetAxis("Vertical");
        AvgSkid = (Mathf.Abs(localVelocityX) > 2.5f) ? 1f : 0f;
        SpeedFactor = Mathf.InverseLerp(0, maxSpeed, carSpeed);

        //CAR DATA
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        // Adjust engine sound pitch based on car speed
        if (useSounds)
        {
            float speedFactor = Mathf.InverseLerp(0, maxSpeed, carSpeed);
            float pitch = Mathf.Lerp(initialCarEngineSoundPitch, 2f, speedFactor);

            if (carSpeed < 30f)
            {
                FadeEngineSound(carEngineSlowSound, carEngineIdleSound, pitch);
            }
            else if (carSpeed >= 30f && carSpeed < 90f)
            {
                FadeEngineSound(carEngineIdleSound, carEngineSlowSound, pitch);
                FadeEngineSound(carEngineMediumSound, carEngineSlowSound, pitch);
            }
            else if (carSpeed >= 90f && carSpeed < 140f)
            {
                FadeEngineSound(carEngineSlowSound, carEngineMediumSound, pitch);
                FadeEngineSound(carEngineHighSound, carEngineMediumSound, pitch);
            }
            else if (carSpeed >= 140f)
            {
                FadeEngineSound(carEngineMediumSound, carEngineHighSound, pitch);
            }

            if (isDrifting || (isTractionLocked && Mathf.Abs(carSpeed) > 12f))
            {
                if (!tireScreechSound.isPlaying)
                {
                    tireScreechSound.Play();
                }
            }
            else if (!isDrifting && (!isTractionLocked || Mathf.Abs(carSpeed) < 12f))
            {
                tireScreechSound.Stop();
            }
        }

        if (useTouchControls && touchControlsSetup)
        {
            if (throttlePTI.buttonPressed)
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoForward();
            }
            if (reversePTI.buttonPressed)
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoReverse();
            }

            if (turnLeftPTI.buttonPressed)
            {
                TurnLeft();
            }
            if (turnRightPTI.buttonPressed)
            {
                TurnRight();
            }
            if (handbrakePTI.buttonPressed)
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                Handbrake();
            }
            if (!handbrakePTI.buttonPressed)
            {
                RecoverTraction();
            }
            if ((!throttlePTI.buttonPressed && !reversePTI.buttonPressed))
            {
                ThrottleOff();
            }
            if ((!reversePTI.buttonPressed && !throttlePTI.buttonPressed) && !handbrakePTI.buttonPressed && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
            if (!turnLeftPTI.buttonPressed && !turnRightPTI.buttonPressed && steeringAxis != 0f)
            {
                ResetSteeringAngle();
            }
            if (turboPTI.buttonPressed)
            {
                ActivateTurbo();
            }
            else if (turboActive)
            {
                DeactivateTurbo();
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.W))
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoForward();
            }
            if (Input.GetKey(KeyCode.S))
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoReverse();
            }

            if (Input.GetKey(KeyCode.A))
            {
                TurnLeft();
            }
            if (Input.GetKey(KeyCode.D))
            {
                TurnRight();
            }
            if (Input.GetKey(KeyCode.Space))
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                Handbrake();
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                RecoverTraction();
            }
            if ((!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W)))
            {
                ThrottleOff();
            }
            if ((!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W)) && !Input.GetKey(KeyCode.Space) && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
            if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && steeringAxis != 0f)
            {
                ResetSteeringAngle();
            }

            if (Input.GetKey(KeyCode.T))
            {
                ActivateTurbo();
            }
            else if (turboActive)
            {
                DeactivateTurbo();
            }
        }

        AnimateWheelMeshes();
    }

    public void StartRace()
    {
        raceStarted = true;
        Debug.Log("Race started for " + gameObject.name);
    }
    private void PlayEngineSound(AudioSource engineSound, float pitch)
    {
        if (!engineSound.isPlaying)
        {
            StopAllEngineSounds();
            engineSound.Play();
        }
        engineSound.pitch = pitch;
    }
   
    private void HandleGearChanges()
    {
        if (gearSpeedRanges == null || gearSpeedRanges.Length == 0)
        {
            Debug.LogWarning("Gear speed ranges are not initialized.");
            return;
        }

        for (int i = 0; i < gearSpeedRanges.Length - 1; i++)
        {
            if (carSpeed >= gearSpeedRanges[i] && carSpeed < gearSpeedRanges[i + 1])
            {
                if (currentGear != i + 1)
                {
                    currentGear = i + 1;
                    if (gearText != null)
                    {
                        gearText.text = "Gear: " + currentGear;
                    }
                    if (carEngineSound != null)
                    {
                        carEngineSound.pitch = initialCarEngineSoundPitch + (currentGear - 1) * 0.2f;
                    }
                    StartCoroutine(ModifySpeedDuringGearChange());
                }
                break;
            }
        }
    }

    private IEnumerator ModifySpeedDuringGearChange()
    {
        if (gearChangeSound != null)
        {
            gearChangeSound.Play();
        }
        yield return new WaitForSeconds(0.5f);
    }

    // This method converts the car speed data from float to string, and then set the text of the UI carSpeedText with this value.
    public void CarSpeedUI()
    {
        if (useUI)
        {
            try
            {
                float absoluteCarSpeed = Mathf.Abs(carSpeed);
                carSpeedText.text = Mathf.RoundToInt(absoluteCarSpeed).ToString();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
    }



    private void PlayEngineSound(AudioSource engineSound)
    {
        if (!engineSound.isPlaying)
        {
            StopAllEngineSounds();
            engineSound.Play();
        }
    }

    private void StopAllEngineSounds()
    {
        if (carEngineIdleSound != null) carEngineIdleSound.Stop();
        if (carEngineSlowSound != null) carEngineSlowSound.Stop();
        if (carEngineMediumSound != null) carEngineMediumSound.Stop();
        if (carEngineHighSound != null) carEngineHighSound.Stop();
    }

    //
    //STEERING METHODS
    //

    //The following method turns the front car wheels to the left. The speed of this movement will depend on the steeringSpeed variable.
    public void TurnLeft()
    {
        steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
        if (steeringAxis < -1f)
        {
            steeringAxis = -1f;
        }
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void TurnRight()
    {
        steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
        if (steeringAxis > 1f)
        {
            steeringAxis = 1f;
        }
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void ResetSteeringAngle()
    {
        if (steeringAxis < 0f)
        {
            steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
        }
        else if (steeringAxis > 0f)
        {
            steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
        }
        if (Mathf.Abs(frontLeftCollider.steerAngle) < 1f)
        {
            steeringAxis = 0f;
        }
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    void AnimateWheelMeshes()
    {
        try
        {
            Quaternion FLWRotation;
            Vector3 FLWPosition;
            frontLeftCollider.GetWorldPose(out FLWPosition, out FLWRotation);
            frontLeftMesh.transform.position = FLWPosition;
            frontLeftMesh.transform.rotation = FLWRotation;

            Quaternion FRWRotation;
            Vector3 FRWPosition;
            frontRightCollider.GetWorldPose(out FRWPosition, out FRWRotation);
            frontRightMesh.transform.position = FRWPosition;
            frontRightMesh.transform.rotation = FRWRotation;

            Quaternion RLWRotation;
            Vector3 RLWPosition;
            rearLeftCollider.GetWorldPose(out RLWPosition, out RLWRotation);
            rearLeftMesh.transform.position = RLWPosition;
            rearLeftMesh.transform.rotation = RLWRotation;

            Quaternion RRWRotation;
            Vector3 RRWPosition;
            rearRightCollider.GetWorldPose(out RRWPosition, out RRWRotation);
            rearRightMesh.transform.position = RRWPosition;
            rearRightMesh.transform.rotation = RRWRotation;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
        }
    }

    //
    //ENGINE AND BRAKING METHODS
    //

    // This method apply positive torque to the wheels in order to go forward.
    public void GoForward()
    {
        //If the forces aplied to the rigidbody in the 'x' asis are greater than
        //3f, it means that the car is losing traction, then the car will start emitting particle systems.
        if (Mathf.Abs(localVelocityX) > 2.5f)
        {
            isDrifting = true;
            DriftCarPS();
        }
        else
        {
            isDrifting = false;
            DriftCarPS();
        }
        // The following part sets the throttle power to 1 smoothly.
        throttleAxis = throttleAxis + (Time.deltaTime * 3f);
        if (throttleAxis > 1f)
        {
            throttleAxis = 1f;
        }
        //If the car is going backwards, then apply brakes in order to avoid strange
        //behaviours. If the local velocity in the 'z' axis is less than -1f, then it
        //is safe to apply positive torque to go forward.
        if (localVelocityZ < -1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.RoundToInt(carSpeed) < maxSpeed)
            {
                //Apply positive torque in all wheels to go forward if maxSpeed has not been reached.
                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
            }
            else
            {
                // If the maxSpeed has been reached, then stop applying torque to the wheels.
                // IMPORTANT: The maxSpeed variable should be considered as an approximation; the speed of the car
                // could be a bit higher than expected.
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    // This method apply negative torque to the wheels in order to go backwards.
    public void GoReverse()
    {
        //If the forces aplied to the rigidbody in the 'x' asis are greater than
        //3f, it means that the car is losing traction, then the car will start emitting particle systems.
        if (Mathf.Abs(localVelocityX) > 2.5f)
        {
            isDrifting = true;
            DriftCarPS();
        }
        else
        {
            isDrifting = false;
            DriftCarPS();
        }
        // The following part sets the throttle power to -1 smoothly.
        throttleAxis = throttleAxis - (Time.deltaTime * 3f);
        if (throttleAxis < -1f)
        {
            throttleAxis = -1f;
        }
        //If the car is still going forward, then apply brakes in order to avoid strange
        //behaviours. If the local velocity in the 'z' axis is greater than 1f, then it
        //is safe to apply negative torque to go reverse.
        if (localVelocityZ > 1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
            {
                //Apply negative torque in all wheels to go in reverse if maxReverseSpeed has not been reached.
                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
            }
            else
            {
                //If the maxReverseSpeed has been reached, then stop applying torque to the wheels.
                // IMPORTANT: The maxReverseSpeed variable should be considered as an approximation; the speed of the car
                // could be a bit higher than expected.
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    //The following function set the motor torque to 0 (in case the user is not pressing either W or S).
    public void ThrottleOff()
    {
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
    }

    // The following method decelerates the speed of the car according to the decelerationMultiplier variable, where
    // 1 is the slowest and 10 is the fastest deceleration. This method is called by the function InvokeRepeating,
    // usually every 0.1f when the user is not pressing W (throttle), S (reverse) or Space bar (handbrake).
    public void DecelerateCar()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f)
        {
            isDrifting = true;
            DriftCarPS();
        }
        else
        {
            isDrifting = false;
            DriftCarPS();
        }
        // The following part resets the throttle power to 0 smoothly.
        if (throttleAxis != 0f)
        {
            if (throttleAxis > 0f)
            {
                throttleAxis = throttleAxis - (Time.deltaTime * 10f);
            }
            else if (throttleAxis < 0f)
            {
                throttleAxis = throttleAxis + (Time.deltaTime * 10f);
            }
            if (Mathf.Abs(throttleAxis) < 0.15f)
            {
                throttleAxis = 0f;
            }
        }
        carRigidbody.linearVelocity = carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));
        // Since we want to decelerate the car, we are going to remove the torque from the wheels of the car.
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
        // If the magnitude of the car's velocity is less than 0.25f (very slow velocity), then stop the car completely and
        // also cancel the invoke of this method.
        if (carRigidbody.linearVelocity.magnitude < 0.25f)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
    }

    // This function applies brake torque to the wheels according to the brake force given by the user.
    public void Brakes()
    {
        frontLeftCollider.brakeTorque = brakeForce;
        frontRightCollider.brakeTorque = brakeForce;
        rearLeftCollider.brakeTorque = brakeForce;
        rearRightCollider.brakeTorque = brakeForce;
    }

    // This function is used to make the car lose traction. By using this, the car will start drifting. The amount of traction lost
    // will depend on the handbrakeDriftMultiplier variable. If this value is small, then the car will not drift too much, but if
    // it is high, then you could make the car to feel like going on ice.
    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");
        // We are going to start losing traction smoothly, there is were our 'driftingAxis' variable takes
        // place. This variable will start from 0 and will reach a top value of 1, which means that the maximum
        // drifting value has been reached. It will increase smoothly by using the variable Time.deltaTime.
        driftingAxis = driftingAxis + (Time.deltaTime);
        float secureStartingPoint = driftingAxis * FLWextremumSlip * handbrakeDriftMultiplier;

        if (secureStartingPoint < FLWextremumSlip)
        {
            driftingAxis = FLWextremumSlip / (FLWextremumSlip * handbrakeDriftMultiplier);
        }
        if (driftingAxis > 1f)
        {
            driftingAxis = 1f;
        }
        //If the forces aplied to the rigidbody in the 'x' asis are greater than
        //3f, it means that the car lost its traction, then the car will start emitting particle systems.
        if (Mathf.Abs(localVelocityX) > 2.5f)
        {
            isDrifting = true;
        }
        else
        {
            isDrifting = false;
        }
        //If the 'driftingAxis' value is not 1f, it means that the wheels have not reach their maximum drifting
        //value, so, we are going to continue increasing the sideways friction of the wheels until driftingAxis
        // = 1f.
        if (driftingAxis < 1f)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;

            FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontRightCollider.sidewaysFriction = FRwheelFriction;

            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;

            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
        }

        // Whenever the player uses the handbrake, it means that the wheels are locked, so we set 'isTractionLocked = true'
        // and, as a consequense, the car starts to emit trails to simulate the wheel skids.
        isTractionLocked = true;
        DriftCarPS();

    }

    // This function is used to emit both the particle systems of the tires' smoke and the trail renderers of the tire skids
    // depending on the value of the bool variables 'isDrifting' and 'isTractionLocked'.
    public void DriftCarPS()
    {

        if (useEffects)
        {
            try
            {
                if (isDrifting)
                {
                    RLWParticleSystem.Play();
                    RRWParticleSystem.Play();
                }
                else if (!isDrifting)
                {
                    RLWParticleSystem.Stop();
                    RRWParticleSystem.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }

            try
            {
                if ((isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f)
                {
                    RLWTireSkid.emitting = true;
                    RRWTireSkid.emitting = true;
                }
                else
                {
                    RLWTireSkid.emitting = false;
                    RRWTireSkid.emitting = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
        else if (!useEffects)
        {
            if (RLWParticleSystem != null)
            {
                RLWParticleSystem.Stop();
            }
            if (RRWParticleSystem != null)
            {
                RRWParticleSystem.Stop();
            }
            if (RLWTireSkid != null)
            {
                RLWTireSkid.emitting = false;
            }
            if (RRWTireSkid != null)
            {
                RRWTireSkid.emitting = false;
            }
        }

    }


    void ActivateTurbo()
    {
        if (!turboActive && currentTurboTime > 0 && localVelocityZ > 0)
        {
            turboActive = true;
            StartCoroutine(SmoothTurboIncrease());
            Debug.Log("Turbo activated");

            // Play turbo effect
            if (turboEffect != null)
            {
                turboEffect.Play();
            }

            // Increase camera FOV
            if (mainCamera != null)
            {
                mainCamera.fieldOfView = turboFOV;
            }

            // Play turbo sound
            if (turboSound != null && !turboSound.isPlaying)
            {
                turboSound.Play();
            }

            // Increase engine sound pitch
            if (carEngineSound != null)
            {
                carEngineSound.pitch *= 1.5f;
            }
        }
    }

    IEnumerator SmoothTurboIncrease()
    {
        while (turboActive && currentTurboTime > 0)
        {
            carRigidbody.AddForce(transform.forward * accelerationMultiplier * turboMultiplier * 100f * Time.deltaTime, ForceMode.Acceleration);
            currentTurboTime -= Time.deltaTime;
            if (turboBar != null)
            {
                turboBar.value = currentTurboTime;
            }
            yield return null;
        }
        DeactivateTurbo();
    }

    void DeactivateTurbo()
    {
        turboActive = false;
        Debug.Log("Turbo deactivated");

        // Stop turbo effect
        if (turboEffect != null)
        {
            turboEffect.Stop();
        }

        // Reset camera FOV
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = originalFOV;
        }

        // Stop turbo sound
        if (turboSound != null && turboSound.isPlaying)
        {
            turboSound.Stop();
        }

        // Reset engine sound pitch
        if (carEngineSound != null)
        {
            carEngineSound.pitch /= 1.5f;
        }

        // Start recharging turbo
        StartCoroutine(RechargeTurbo());
    }
    IEnumerator RechargeTurbo()
    {
    //    isRecharging = true;
        while (currentTurboTime < turboDuration)
        {
            currentTurboTime += rechargeRate * Time.deltaTime;
            if (turboBar != null)
            {
                turboBar.value = currentTurboTime;
            }
            yield return null;
        }
        currentTurboTime = turboDuration;
     //   isRecharging = false;
    }
    public void RechargeTurbo(float amount)
    {
        currentTurboTime = Mathf.Min(currentTurboTime + amount, turboDuration);
        if (turboBar != null)
        {
            turboBar.value = currentTurboTime;
        }
    }


    // This function is used to recover the traction of the car when the user has stopped using the car's handbrake.
    public void RecoverTraction()
    {
        isTractionLocked = false;
        driftingAxis = driftingAxis - (Time.deltaTime / 1.5f);
        if (driftingAxis < 0f)
        {
            driftingAxis = 0f;
        }

        //If the 'driftingAxis' value is not 0f, it means that the wheels have not recovered their traction.
        //We are going to continue decreasing the sideways friction of the wheels until we reach the initial
        // car's grip.
        if (FLwheelFriction.extremumSlip > FLWextremumSlip)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;

            FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontRightCollider.sidewaysFriction = FRwheelFriction;

            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;

            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;

            Invoke("RecoverTraction", Time.deltaTime);

        }
        else if (FLwheelFriction.extremumSlip < FLWextremumSlip)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;

            FRwheelFriction.extremumSlip = FRWextremumSlip;
            frontRightCollider.sidewaysFriction = FRwheelFriction;

            RLwheelFriction.extremumSlip = RLWextremumSlip;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;

            RRwheelFriction.extremumSlip = RRWextremumSlip;
            rearRightCollider.sidewaysFriction = RRwheelFriction;

            driftingAxis = 0f;
        }
    }
    public void InstantRechargeTurbo(float amount)
    {
        currentTurboTime = Mathf.Min(currentTurboTime + amount, turboDuration);
        if (turboBar != null)
        {
            turboBar.value = currentTurboTime;
        }
        Debug.Log("Turbo instantly recharged by " + amount);
    }
    private void FadeEngineSound(AudioSource currentSound, AudioSource nextSound, float pitch)
    {
        if (!nextSound.isPlaying)
        {
            nextSound.Play();
        }

        currentSound.volume = Mathf.Lerp(currentSound.volume, 0f, Time.deltaTime * 2f);
        nextSound.volume = Mathf.Lerp(nextSound.volume, 1f, Time.deltaTime * 2f);

        nextSound.pitch = pitch;
    }


}

