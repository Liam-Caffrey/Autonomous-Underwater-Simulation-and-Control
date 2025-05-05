using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public GameObject fish1Prefab;
    public GameObject fish2Prefab;
    public LayerMask terrainLayer; 

    private int totalFish1 = 100;
    private int totalFish2 = 100;

    void Start()
    {
        SpawnFish(fish1Prefab, totalFish1, new Vector3(0.5f, 90f, 0f), Vector3.zero, true); 
        SpawnFish(fish2Prefab, totalFish2, new Vector3(-90f, -90f, 0f), Vector3.zero, false);  
    }

    void SpawnFish(GameObject fishPrefab, int count, Vector3 rotation, Vector3 yOffset, bool isFish1)
    {
        for (int i = 0; i < count; i++)
        {
            int coinFlip = Random.Range(0, 2);

            Vector3 randomPosition = coinFlip == 1 ? GetRandomPositionType1() : GetRandomPositionType2();
            randomPosition += yOffset; 

            randomPosition.y = Random.Range(0f, -8f);

            if (IsPositionClear(randomPosition))
            {
                Instantiate(fishPrefab, randomPosition, Quaternion.Euler(rotation));
            }
            else
            {
                i--;  
            }
        }
    }

    bool IsPositionClear(Vector3 position)
    {
       
        float fishColliderSize = 1f; 
        Collider[] colliders = Physics.OverlapBox(position, new Vector3(fishColliderSize, fishColliderSize, fishColliderSize), Quaternion.identity, terrainLayer);

        return colliders.Length == 0;  
    }

    Vector3 GetRandomPositionType1()
    {
       
        float x = Random.Range(-44f, 387f);
        float z = Random.Range(0f, 400f);
        return new Vector3(x, 0, z); 
    }

    Vector3 GetRandomPositionType2()
    {
        
        float x = Random.Range(-420f, -11f);
        float z = Random.Range(-480f, 0f);
        return new Vector3(x, 0, z);
    }
}
