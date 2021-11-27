using UnityEngine;
using System;
using System.Collections;
using TreeSharpPlus;

public class StoryBehaviorTree : MonoBehaviour
{
    public Transform wander1;
    public Transform wander2;
    public Transform wander3;
    public GameObject doll;
    public GameObject startLine;

    private BehaviorAgent behaviorAgent;
    // Use this for initialization
    void Start()
    {
        behaviorAgent = new BehaviorAgent(this.BuildTreeRoot());
        BehaviorManager.Instance.Register(behaviorAgent);
        behaviorAgent.StartBehavior();
    }

    // Update is called once per frame
    void Update()
    {

    }

    protected Node ST_ApproachAndWait(GameObject actor, Transform target)
    {
        Val<Vector3> position = Val.V(() => target.position);
        return new Sequence(actor.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(1000));
    }

    #region Doll-Related Functions
    /* pregame */
    protected Node PreGame_Doll(GameObject[] players)
    {

        //Node action = this.ST_ApproachAndWait(doll, this.wander1);
        //return new Sequence(new LeafTrace("running doll pregame"), action);

        // check whether player crosses line. If so, change status to dead
        Node[] checks = new Node[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            checks[i] = PreGame_Check_Doll(players[i]);
        }
        return new DecoratorLoop(new Sequence(checks));

    }
    protected Node PreGame_Check_Doll(GameObject player)
    {
        Func<bool> cross = () => (player.transform.position.z < startLine.transform.position.z);
        Func<bool> safe = () => (player.transform.position.z >= startLine.transform.position.z);

        // if cross, then set the player to dead
        Node alive = new Sequence(new LeafAssert(safe), new LeafTrace("Player didn't cross the line"));
        Node deadAction = new LeafInvoke(() => {player.GetComponent<PlayerController>().alive = false; return RunStatus.Success;});
        Node dead = new Sequence(new LeafAssert(cross), new LeafTrace("player has crossed the line"), deadAction, new LeafAssert(safe));
        // run again the check for safe so that we can reset the loop
        return new Selector(alive, dead);
    }
    #endregion

    #region Player-Related Functions
    /* pregame */
    protected Node PreGame_Player(GameObject player)
    {
        Node action = new Sequence(this.ST_ApproachAndWait(player, this.wander1));
        return CheckAlive_Player(player, action);
    }
    protected Node CheckAlive_Player(GameObject player, Node alive_node)
    {
        // if alive: do other stuff
        // otherwise: play dead
        Func<bool> alive = () => (player.GetComponent<PlayerController>().alive);
        Func<bool> dead = () => (!player.GetComponent<PlayerController>().alive);

        Node playDead = player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("DYING", 100);
        Node deadBehavior = new Sequence(new LeafAssert(dead), playDead);
        Node aliveBehavior = new Sequence(new LeafAssert(alive), alive_node);
        return new Selector(aliveBehavior, deadBehavior);
    }
    #endregion
    protected Node PreGame_Loop(float timeout)
    {
        Debug.Log("here");
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Node dollNode = PreGame_Doll(players);

        Debug.Log("after adding players");
        Node debugger = new LeafTrace("number of players: " + players.Length);
        Node[] totalNodes = new Node[players.Length + 1];
        totalNodes[0] = dollNode;
        for (int i=0; i<players.Length; i++)
        {
            Node playerNode = PreGame_Player(players[i]);
            totalNodes[i + 1] = playerNode;
        }
        Node parallelNode = new SequenceParallel(totalNodes);
        Node loopNode = new DecoratorLoop((int)(timeout / Time.deltaTime), parallelNode);
        return new Sequence(debugger, loopNode);
        //return new Sequence(participant.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(1000));
    }



    protected Node BuildTreeRoot()
    {
        //Node roaming = new DecoratorLoop(
        //    new Sequence(
        //        this.ST_ApproachAndWait(this.wander1),
        //        this.ST_ApproachAndWait(this.wander2),
        //        this.ST_ApproachAndWait(this.wander3)));
        //Node trigger = new DecoratorLoop(new LeafAssert(act));
        //Node root = new DecoratorLoop(new DecoratorForceStatus(RunStatus.Success, new SequenceParallel(trigger, roaming)));
        Debug.Log("building tree root");
        Node root = new DecoratorLoop(new DecoratorForceStatus(RunStatus.Success, 
                                      new Sequence(new LeafTrace("starting before loop"),  PreGame_Loop(10.0f))));
        Debug.Log("after building");

        return root;
    }
}
