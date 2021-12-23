using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DollController : MonoBehaviour
{
    public int hp = 20;
    public bool alive = false;
    public Vector3 default_face;
    public GameObject eye;
    // Start is called before the first frame update
    void Start()
    {
        hp = 0;
        default_face = this.transform.forward * 2 + this.transform.position + this.transform.up * 9;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public int getHP()
    {
        return this.hp;
    }
}
