using System.Collections.Generic;
using UnityEngine;

public class spawning : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject[] objectsToSpawn;
    [SerializeField] private List<Transform> _spawnPoints;
    [SerializeField, Min(0.01f)] private float spawnInterval = 2f;
    [SerializeField] private float minimumX = -7f;
    [SerializeField] private float maximumX = 7f;

    [Header("Pooling")]
    [SerializeField, Min(1)] private int poolSizePerPrefab = 10;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 4f;
    [SerializeField] private float despawnY = -12f;

    private float timer;

    private readonly List<Queue<GameObject>> pools =
        new List<Queue<GameObject>>();

    private readonly List<GameObject> prefabs =
        new List<GameObject>();

    private readonly List<ActiveObject> activeObjects =
        new List<ActiveObject>();

    private Transform poolRoot;

    private struct ActiveObject
    {
        public GameObject instance;
        public int poolIndex;

        public ActiveObject(GameObject instance, int poolIndex)
        {
            this.instance = instance;
            this.poolIndex = poolIndex;
        }
    }

    private void Awake()
    {
        // Create objects under an inactive parent so they
        // do not activate before being added to the pool.
        GameObject root = new GameObject("Object Pool");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        poolRoot = root.transform;

        if (objectsToSpawn == null)
            return;

        foreach (GameObject prefab in objectsToSpawn)
        {
            if (prefab == null)
                continue;

            int poolIndex = pools.Count;

            prefabs.Add(prefab);
            pools.Add(new Queue<GameObject>());

            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                pools[poolIndex].Enqueue(
                    CreatePooledObject(poolIndex)
                );
            }
        }

        root.SetActive(true);
    }

    private void Update()
    {
        MoveObjects();

        if (pools.Count == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= Mathf.Max(0.01f, spawnInterval))
        {
            timer = 0f;
            SpawnObject();
        }
    }

    private GameObject CreatePooledObject(int poolIndex)
    {
        GameObject instance = Instantiate(
            prefabs[poolIndex],
            poolRoot
        );

        instance.SetActive(false);
        return instance;
    }

    private void SpawnObject()
{
    if (pools.Count == 0)
        return;

    if (_spawnPoints == null || _spawnPoints.Count == 0)
    {
        Debug.LogWarning("No spawn points assigned.", this);
        return;
    }

    // Choose a random spawn point.
    int spawnPointIndex = Random.Range(0, _spawnPoints.Count);
    Transform spawnPoint = _spawnPoints[spawnPointIndex];

    if (spawnPoint == null)
    {
        Debug.LogWarning("The selected spawn point is empty.", this);
        return;
    }

    // Choose a random prefab pool.
    int poolIndex = Random.Range(0, pools.Count);
    Queue<GameObject> pool = pools[poolIndex];

    GameObject instance = pool.Count > 0
        ? pool.Dequeue()
        : CreatePooledObject(poolIndex);

    instance.transform.SetPositionAndRotation(
        spawnPoint.position,
        prefabs[poolIndex].transform.rotation
    );

    instance.transform.localScale =
        prefabs[poolIndex].transform.localScale;

    activeObjects.Add(
        new ActiveObject(instance, poolIndex)
    );

    instance.SetActive(true);
}

    private void MoveObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject instance = activeObjects[i].instance;

            instance.transform.position +=
                Vector3.down * movementSpeed * Time.deltaTime;

            if (instance.transform.position.y <= despawnY)
            {
                ReturnAtIndex(i);
            }
        }
    }

    // Call this instead of Destroy when collecting
    // or removing a spawned object.
    public void ReturnToPool(GameObject instance)
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i].instance == instance)
            {
                ReturnAtIndex(i);
                return;
            }
        }
    }

    private void ReturnAtIndex(int index)
    {
        ActiveObject item = activeObjects[index];

        activeObjects.RemoveAt(index);

        item.instance.SetActive(false);
        pools[item.poolIndex].Enqueue(item.instance);
    }
}