using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletObjectController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // check whether the bullet is out of the scene. If so remove it
        if (transform.position.x < -25 || transform.position.x > 25 || transform.position.z < -25 || transform.position.z > 25)
        {
            Destroy(this);
        }
    }
}
