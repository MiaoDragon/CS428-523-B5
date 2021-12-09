using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TreeSharpPlus;
using RootMotion.FinalIK;

public class PlayerController : MonoBehaviour
{
    public bool controllable = false;
    public bool alive = true;
    public bool moving = false;
    public GameObject stonePrefab;
    public Transform stone_transform;
    public GameObject stone;
    public float horizontal = 0.0f;
    public float vertical = 0.0f;
    public float velocityScale = 1.0f;
    public bool space_triggered = false;

    public InteractionObject rightKickPoint;
    // Start is called before the first frame update
    void Start()
    {
        alive = true;
        moving = false;
       stone.SetActive(false);


    }

    // Update is called once per frame
    void Update()
    {
        // if controllable, use wsad to control the player's motion
        vertical = Input.GetAxis("Vertical") * velocityScale;
        horizontal = Input.GetAxis("Horizontal") * velocityScale;
        if (Input.GetKey("space"))
        {
            space_triggered = true;
        }
        if (Input.GetKeyUp("space"))
        {
            space_triggered = false;
        }

        if (stone.transform.position.x < -25 || stone.transform.position.x > 25 ||
            stone.transform.position.z < -25 || stone.transform.position.z > 25 ||
            stone.transform.position.y > 25)
        {
            stone.transform.position = stone_transform.position;
            stone.transform.rotation = stone_transform.rotation;
            stone.GetComponent<Rigidbody>().velocity = Vector3.zero;
            stone.GetComponent<Rigidbody>().isKinematic = true;
            stone.SetActive(false);
        }

    }

    void SetAlive(bool val)
    {
        this.alive = val;
    }
}
