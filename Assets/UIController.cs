using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UIController : MonoBehaviour
{
    public Text DollHPText;
    public Text playerCount;
    public Text announcement;
    public GameObject doll;
    public GameObject players;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DollHPText.text = "Doll HP: " + doll.GetComponent<DollController>().getHP().ToString();
        int pcount = 0 ;
        for (int i = 0; i < players.transform.childCount; i++){
            if (players.transform.GetChild(i).GetComponent<PlayerController>().isAlive()){
                pcount++;
            }
        }
        playerCount.text = "Alive Players: " + pcount.ToString();

        if (doll.GetComponent<DollController>().getHP() < 1){
           announcement.text = "Players win!";
        }

        if (pcount < 1){
           announcement.text = "Doll Wins.";
        }
    }
}
