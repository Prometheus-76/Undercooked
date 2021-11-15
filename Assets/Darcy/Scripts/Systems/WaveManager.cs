using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Responsible for spawning and tracking enemies and kills, as well as the spawn budget

public class WaveManager : MonoBehaviour
{
    public static float waveProgress;
    public static int waveNumber;

    public LayerMask spawnerLayer;
    public LayerMask environmentLayers;
    public LayerMask enemyLayer;
    public int minSpawnDistance;
    public int maxSpawnDistance;
    private List<Transform> validSpawnPoints;
    private List<GameObject> waveEnemies;

    public static int eliminatedWaveEnemies;
    private int waveEnemyCount;

    public GameObject[] enemyPrefabs;
    private List<GameObject> upgradeStationInstances;
    private List<int> upgradeStationSpawnOrder;
    private int lastSpawnLocation;

    public float budgetInstancePercentAllowed;
    public int waveSpawnBudget;
    public int spawnBudgetIncreasePerWave;
    private int currentWaveCost;

    public static bool gameStarted { get; private set; }
    public bool waveActive;
    public int timeBetweenWaves;
    private float intermissionStartTime;
    private float waveStartTime;

    public Transform enemyParent;
    private Transform playerTransform;
    private Transform mainCameraTransform;
    private PlayerStats playerStats;

    // Start is called before the first frame update
    void Start()
    {
        #region Initialisation

        waveProgress = 0f;
        waveNumber = 0;

        playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        mainCameraTransform = Camera.main.GetComponent<Transform>();

        upgradeStationInstances = new List<GameObject>();
        upgradeStationSpawnOrder = new List<int>();

        validSpawnPoints = new List<Transform>();
        waveEnemies = new List<GameObject>();
        waveEnemyCount = 1;

        lastSpawnLocation = -1;
        GameObject[] upgradeStations = GameObject.FindGameObjectsWithTag("UpgradeStation");
        foreach (GameObject station in upgradeStations)
        {
            upgradeStationInstances.Add(station);
        }

        #endregion

        gameStarted = false;
        waveActive = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameStarted)
        {
            if (waveActive)
            {
                UpdateSpawnPoints();

                // Shrink list to fit all enemies that are currently alive
                for (int i = 0; i < waveEnemies.Count; i++)
                {
                    if (waveEnemies[i] == null)
                    {
                        waveEnemies.RemoveAt(i);
                        i -= 1;
                    }
                }

                // If the wave is currently occurring and there is a valid place to spawn an enemy
                if (waveEnemies.Count > 0 && validSpawnPoints.Count > 0)
                {
                    // Determine cost of current enemies spawned in
                    int currentActiveEnemyCost = 0;
                    int remainingEnemies = 0;
                    for (int i = 0; i < waveEnemies.Count; i++)
                    {
                        if (waveEnemies[i].activeInHierarchy)
                        {
                            currentActiveEnemyCost += waveEnemies[i].GetComponent<Enemy>().spawnCost;
                        }
                        else
                        {
                            remainingEnemies += 1;
                        }
                    }

                    // Spawn more enemies if there isn't enough
                    if ((float)currentActiveEnemyCost / (float)currentWaveCost < budgetInstancePercentAllowed && remainingEnemies > 0)
                    {
                        SpawnEnemy();
                    }
                }
            }

            if (eliminatedWaveEnemies >= waveEnemyCount && waveActive)
            {
                waveActive = false;
                WaveEnded();
            }

            if (waveActive == false && playerStats.currentRunTime >= waveStartTime)
            {
                NewWave();
            }
        }

        #region Update UI

        waveProgress = (float)eliminatedWaveEnemies / (float)waveEnemyCount;
        waveProgress = Mathf.Clamp01(waveProgress);
        UserInterfaceHUD.waveProgress = Mathf.Floor(Mathf.Clamp01(waveProgress) * 1000f) / 1000f;

        waveNumber = Mathf.Max(1, waveNumber);
        UserInterfaceHUD.waveNumber = waveNumber;

        if (playerStats.currentRunTime < waveStartTime)
        {
            UserInterfaceHUD.intermissionProgress = Mathf.Clamp01((playerStats.currentRunTime - intermissionStartTime) / (waveStartTime - intermissionStartTime));
        }
        else
        {
            UserInterfaceHUD.intermissionProgress = -1f;
        }

        UserInterfaceHUD.intermissionDuration = timeBetweenWaves;

        #endregion
    }

    void UpdateSpawnPoints()
    {
        validSpawnPoints.Clear();

        // Cast maximum sphere
        Collider[] spawnPointColliders = Physics.OverlapSphere(playerTransform.position, maxSpawnDistance, spawnerLayer);

        // Add valid spawners to list
        for (int i = 0; i < spawnPointColliders.Length; i++)
        {
            Vector3 spawnerPosition = spawnPointColliders[i].transform.position;

            // If the spawn point is outside the minimum range
            if (Vector3.Distance(spawnerPosition, playerTransform.position) > minSpawnDistance)
            {
                // If the spawn point is outside the player's vision
                Vector3 cameraToSpawner = spawnerPosition - mainCameraTransform.position;
                if (Vector3.Dot(cameraToSpawner.normalized, mainCameraTransform.forward) < 0f || Physics.Linecast(mainCameraTransform.position, spawnerPosition, environmentLayers))
                {
                    // If there is no enemy at this point currently
                    if (Physics.OverlapSphere(spawnerPosition, 3f, enemyLayer).Length <= 0)
                    {
                        // Add the spawn point to the list
                        validSpawnPoints.Add(spawnPointColliders[i].transform);
                    }
                }
            }
        }
    }

    void ChooseWaveEnemies()
    {
        int bananaCount = enemyPrefabs[0].GetComponent<BananaBunchEnemy>().bananaCount;

        currentWaveCost = 0;
        waveEnemyCount = 0;
        waveEnemies.Clear();
        eliminatedWaveEnemies = 0;

        while (currentWaveCost < waveSpawnBudget)
        {
            // Add random enemy to distributed spawn list and update budget
            int enemyIndex = Random.Range(0, 6);

            // Convert weighted randomness to flat keyed indices (0 - banana, 1 - strawberry, 2 - onion)
            switch (enemyIndex)
            {
                case 0:
                    enemyIndex = 0;
                    break;
                case 1:
                    enemyIndex = 0;
                    break;
                case 2:
                    enemyIndex = 0;
                    break;
                case 3:
                    enemyIndex = 1;
                    break;
                case 4:
                    enemyIndex = 1;
                    break;
                case 5:
                    enemyIndex = 2;
                    break;
            }

            // Sum all enemies in this wave
            waveEnemyCount += (enemyIndex == 0) ? bananaCount + 1 : 1;
            currentWaveCost += enemyPrefabs[enemyIndex].GetComponent<Enemy>().spawnCost;

            // Spawn enemy, disable it and add to the list
            GameObject enemyInstance = Instantiate<GameObject>(enemyPrefabs[enemyIndex], Vector3.zero, Quaternion.identity, enemyParent);
            waveEnemies.Add(enemyInstance);
            enemyInstance.SetActive(false);
        }
    }

    void SpawnEnemy()
    {
        // If there are enemies available to spawn in this wave
        if (waveEnemies.Count > 0)
        {
            int spawnPoint = Random.Range(0, validSpawnPoints.Count);

            for (int i = 0; i < waveEnemies.Count; i++)
            {
                // If the enemy hasn't been spawned in yet
                if (waveEnemies[i].activeInHierarchy == false)
                {
                    // Spawn the enemy
                    waveEnemies[i].transform.position = validSpawnPoints[spawnPoint].position;
                    waveEnemies[i].SetActive(true);
                    break;
                }
            }
        }
    }

    void WaveEnded()
    {
        // Set intermission period
        intermissionStartTime = playerStats.currentRunTime;
        waveStartTime = playerStats.currentRunTime + timeBetweenWaves;

        // Set next wave budget
        waveSpawnBudget += spawnBudgetIncreasePerWave;
        playerStats.CalculateDifficultyLevel();

        #region Upgrade Station Spawns

        if (upgradeStationSpawnOrder.Count <= 0)
        {
            upgradeStationSpawnOrder.Clear();

            // Create spawn order
            List<int> tempOrderList = new List<int>();
            for (int i = 0; i < upgradeStationInstances.Count; i++)
            {
                tempOrderList.Add(i);
                Debug.Log(i);
            }

            Debug.Log("-");
            // Bag randomiser for spawn order, does not account for closed off areas due to barriers
            while (tempOrderList.Count > 0)
            {
                int index = Random.Range(0, tempOrderList.Count);
                upgradeStationSpawnOrder.Add(tempOrderList[index]);
                Debug.Log(tempOrderList[index]);
                tempOrderList.RemoveAt(index);
            }

            // Prevent double ups between sequences
            if (upgradeStationSpawnOrder[0] == lastSpawnLocation)
            {
                upgradeStationSpawnOrder.Reverse();
            }
        }

        if (upgradeStationSpawnOrder.Count > 0)
        {
            // Turn upgrade station on
            upgradeStationInstances[upgradeStationSpawnOrder[0]].GetComponent<UpgradeStation>().isInteractable = true;
            lastSpawnLocation = upgradeStationSpawnOrder[0];
            upgradeStationSpawnOrder.RemoveAt(0);
        }

        #endregion
    }

    void NewWave()
    {
        // Turn all upgrade stations off
        for (int i = 0; i < upgradeStationInstances.Count; i++)
        {
            upgradeStationInstances[i].GetComponent<UpgradeStation>().isInteractable = false;
        }

        waveNumber += 1;
        waveActive = true;
        ChooseWaveEnemies();
    }

    public void StartWaveCycle()
    {
        waveNumber = 0;
        NewWave();
        gameStarted = true;
    }
}
