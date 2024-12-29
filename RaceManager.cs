using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceManager : MonoBehaviour
{
    public Transform startFinishPoint;
    public List<Transform> checkpointTransforms; // Additional checkpoints to prevent cheating
    public Text countdownText;
    public Text lapText;
    public Text bestTimeText;
    public Text currentTimeText;
    public int totalLaps = 3;

    private float countdownTime = 5f;
    private bool raceStarted = false;
    private float raceStartTime;
    //private float bestTime = Mathf.Infinity;
    private float currentTime;
    private Dictionary<GameObject, int> carLapCounters = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, float> carBestTimes = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, int> carCheckpointCounters = new Dictionary<GameObject, int>();

    void Start()
    {
        Debug.Log("RaceManager started");

        // Verify that the start/finish point is assigned
        if (startFinishPoint == null)
        {
            Debug.LogError("Start/Finish point is not assigned!");
        }
        else
        {
            Debug.Log("Start/Finish point assigned: " + startFinishPoint.name);
        }

        // Verify that checkpoints are assigned
        if (checkpointTransforms == null || checkpointTransforms.Count == 0)
        {
            Debug.LogError("No checkpoints assigned!");
        }
        else
        {
            Debug.Log("Checkpoints assigned: " + checkpointTransforms.Count);
            foreach (var checkpoint in checkpointTransforms)
            {
                Debug.Log("Checkpoint: " + checkpoint.name);
            }
        }

        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        Debug.Log("Countdown started");
        while (countdownTime > 0)
        {
            countdownText.text = countdownTime.ToString("F0");
            Debug.Log("Countdown: " + countdownTime);
            yield return new WaitForSeconds(1f);
            countdownTime--;
        }

        countdownText.text = "GO!";
        Debug.Log("Race started");
        raceStarted = true;
        raceStartTime = Time.time;
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

        // Start the race for all AI cars
        AICarController[] aiCars = FindObjectsByType<AICarController>(FindObjectsSortMode.None);
        foreach (AICarController aiCar in aiCars)
        {
            aiCar.StartRace();
            Debug.Log(aiCar.name + " started racing");
        }

        // Start the race for the player car
        PrometeoCarController playerCar = FindFirstObjectByType<PrometeoCarController>();
        if (playerCar != null)
        {
            playerCar.StartRace();
            Debug.Log(playerCar.name + " started racing");
        }
    }

    void Update()
    {
        if (raceStarted)
        {
            currentTime = Time.time - raceStartTime;
            currentTimeText.text = "Current Time: " + currentTime.ToString("F2");

            foreach (var car in carLapCounters.Keys)
            {
                if (carLapCounters[car] > totalLaps)
                {
                    raceStarted = false;
                    float carTime = Time.time - raceStartTime;
                    if (carTime < carBestTimes[car])
                    {
                        carBestTimes[car] = carTime;
                        bestTimeText.text = "Best Time: " + carTime.ToString("F2");
                        Debug.Log(car.name + " achieved a new best time: " + carTime.ToString("F2"));
                    }
                    Debug.Log(car.name + " finished the race!");
                }
            }

            AdjustEngineSounds();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter called with " + other.name);
        if (other.CompareTag("Player") || other.CompareTag("Car"))
        {
            GameObject car = other.gameObject;
            if (!carLapCounters.ContainsKey(car))
            {
                carLapCounters[car] = 0;
                carBestTimes[car] = Mathf.Infinity;
                carCheckpointCounters[car] = 0;
                Debug.Log(car.name + " added to race");
            }

            if (other.transform == startFinishPoint)
            {
                if (carCheckpointCounters[car] == checkpointTransforms.Count)
                {
                    carLapCounters[car]++;
                    carCheckpointCounters[car] = 0;
                    Debug.Log(car.name + " completed lap " + carLapCounters[car]);
                    if (carLapCounters[car] <= totalLaps)
                    {
                        lapText.text = "Lap: " + carLapCounters[car] + "/" + totalLaps;
                    }
                }
            }
            else
            {
                int checkpointIndex = checkpointTransforms.IndexOf(other.transform);
                if (checkpointIndex == carCheckpointCounters[car])
                {
                    carCheckpointCounters[car]++;
                    Debug.Log(car.name + " passed checkpoint " + checkpointIndex);
                }
            }
        }
    }
    
    public void OnCheckpointReached(PrometeoCarController car, Transform checkpoint)
    {
        var carInfo = carInfos[car.name];
        if (checkpoint == checkpointTransforms[carInfo.checkpointIndex])
        {
            carInfo.checkpointIndex++;
            if (carInfo.checkpointIndex >= checkpointTransforms.Count)
            {
                carInfo.checkpointIndex = 0;
                carInfo.currentLap++;
                carInfo.lapStartTime = Time.time;

                if (carInfo.currentLap > totalLaps)
                {
                    raceStarted = false;
                    carInfos[car.name].finished = true;
                    Debug.Log(car.name + " - Finished!\n");
                }
            }
        }
        else
        {
            Debug.LogWarning("Possible cheating detected for " + car.name + ": Skipped a checkpoint");
        }
    }

    private void AdjustEngineSounds()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        AICarController[] aiCars = FindObjectsByType<AICarController>(FindObjectsSortMode.None);

        foreach (AICarController aiCar in aiCars)
        {
            float closestDistance = Mathf.Infinity;
            foreach (GameObject player in players)
            {
                float distance = Vector3.Distance(aiCar.transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
            float volume = Mathf.Clamp01(1 - (closestDistance / aiCar.maxHearingDistance));
            aiCar.engineSound.volume = volume;
        }

        PrometeoCarController playerCar = FindFirstObjectByType<PrometeoCarController>();
        if (playerCar != null && playerCar.engineSound != null)
        {
            float playerVolume = Mathf.Clamp01(1 - (Vector3.Distance(playerCar.transform.position, FindClosestPlayer(playerCar).transform.position) / playerCar.maxHearingDistance));
            playerCar.engineSound.volume = playerVolume;
        }
    }

    private GameObject FindClosestPlayer(PrometeoCarController car)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(car.transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestPlayer = player;
                closestDistance = distance;
            }
        }
        return closestPlayer;
    }

    private Dictionary<string, CarInfo> carInfos = new Dictionary<string, CarInfo>(); // Dictionary to store car info

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time - minutes * 60);
        int milliseconds = Mathf.FloorToInt((time - minutes * 60 - seconds) * 1000);
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    public class CarInfo
    {
        public PrometeoCarController car;
        public int currentLap;
        public int checkpointIndex;
        public float lapStartTime;
        public bool finished = false;
    }
}
