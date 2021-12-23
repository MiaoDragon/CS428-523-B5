using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Spawner : MonoBehaviour
{
    public int numberToSpawn;
    public List<GameObject> spawnPool;
    public GameObject quad;

    void Start()
    {
        spawnObjects();
    }

    public void spawnObjects()
    {
        destroyObjects();
        int randomItem = 0;
        GameObject toSpawn;
        BoxCollider c = quad.GetComponent<BoxCollider>();
        float screenX, screenZ;
        Vector3 pos;
        for (int i = 0; i < numberToSpawn; i++)
        {
            randomItem = Random.Range(0, spawnPool.Count);
            toSpawn = spawnPool[randomItem];

            screenX = Random.Range(quad.transform.position.x-quad.transform.localScale.x/2, quad.transform.position.x + quad.transform.localScale.x/2);
            screenZ = Random.Range(quad.transform.position.z - quad.transform.localScale.z / 2, quad.transform.position.z + quad.transform.localScale.z / 2);
            pos = new Vector3(screenX, toSpawn.transform.position.y, screenZ);
            Debug.Log("screenX: " + screenX.ToString());
            Debug.Log("screenZ: " + screenZ.ToString());

            GameObject instantiated = Instantiate(toSpawn, pos, toSpawn.transform.rotation);
            instantiated.SetActive(true);
        }
    }
    private void destroyObjects()
    {
        foreach (GameObject o in GameObject.FindGameObjectsWithTag("Spawnable"))
        {
            Destroy(o);
        }
    }


}