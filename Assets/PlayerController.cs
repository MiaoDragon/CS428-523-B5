using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool controllable = false;
    public bool alive = true;
    public float horizontal = 0.0f;
    public float vertical = 0.0f;
    public float velocityScale = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        alive = true;
    }

    // Update is called once per frame
    void Update()
    {
        // if controllable, use wsad to control the player's motion
        vertical = Input.GetAxis("Vertical") * velocityScale;
        horizontal = Input.GetAxis("Horizontal") * velocityScale;
    }

    void SetAlive(bool val)
    {
        this.alive = val;
    }
}
