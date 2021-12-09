using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DollController : MonoBehaviour
{
    public int hp = 20;
    public bool alive = false;
    // Start is called before the first frame update
    void Start()
    {
        hp = 20;
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
