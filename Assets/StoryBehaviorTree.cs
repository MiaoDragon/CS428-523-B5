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

    protected Node PreGame_Doll()
    {

        Node action = this.ST_ApproachAndWait(doll, this.wander1);
        return new Sequence(new LeafTrace("running doll pregame"), action);

    }

    protected Node PreGame_Player(GameObject player)
    {
        Node action = this.ST_ApproachAndWait(player, this.wander1);
        return new Sequence(new LeafTrace("running player pregame"), action);
    }

    protected Node PreGame_Loop(float timeout)
    {
        Debug.Log("here");
        Node dollNode = PreGame_Doll();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
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
