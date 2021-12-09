using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RootMotion.FinalIK;

using TreeSharpPlus;

public class EndGameBehaviorTree : MonoBehaviour
{
    public GameObject doll;
    public FullBodyBipedEffector leftHand;
    public FullBodyBipedEffector rightHand;
    public FullBodyBipedEffector rightFoot;
    public FullBodyBipedEffector leftFoot;

    private BehaviorAgent behaviorAgent;

    // Start is called before the first frame update
    void Start()
    {
        //behaviorAgent = new BehaviorAgent(this.BuildTreeRoot());
        //BehaviorManager.Instance.Register(behaviorAgent);
        //behaviorAgent.StartBehavior();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected Node endgame_player_cross_line_behavior(GameObject player)
    {
        // celebrate action: move close to doll and kick the doll
        Func<Vector3> get_doll_position =
            () =>
            {
                // get vector pointing from player to doll
                Vector3 player_to_doll = doll.transform.position - player.transform.position;
                player_to_doll = player_to_doll / player_to_doll.magnitude;
                Vector3 target_pos = doll.transform.position - player_to_doll * 3.0f;
                return target_pos;
            };
        Node move_to_target = player.GetComponent<BehaviorMecanim>().Node_GoTo(Val.V(get_doll_position));
        Node face_doll = player.GetComponent<BehaviorMecanim>().ST_TurnToFace(Val.V(()=>(doll.transform.position)));
        InteractionObject kickPoint = player.GetComponent<PlayerController>().rightKickPoint;

        Node kick_doll = player.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightFoot, kickPoint);
        Node loop_kick = new DecoratorLoop(
                                new Sequence(new LeafWait(900), kick_doll, new LeafWait(1000),
                                      player.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightFoot)));
        return new Sequence(move_to_target, face_doll, loop_kick);
    }

    protected Node endgame_player_beat_doll_behavior(GameObject player)
    {

        Node danceNode = player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("BREAKDANCE", Val.V(() => ((long)2000)));
        Node loop_breakdance = new DecoratorLoop(new Sequence(danceNode));
        return loop_breakdance;
    }

    protected Node ending_player_crossed_line(GameObject player)
    {
        // alive players celebrate
        return endgame_player_cross_line_behavior(player);

    }

    protected Node ending_player_beat_doll(GameObject player)
    {
        // alive players celebrate more
        // alive players celebrate

        return endgame_player_beat_doll_behavior(player);

    }

    protected Node ending_doll_win()
    {

        Node loop_breakdance = new DecoratorLoop(doll.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("BREAKDANCE", 2000));
        return loop_breakdance;
    }

    protected Node ending_draw()
    {
        return new DecoratorLoop(new LeafWait(100));
    }

    protected Node endgame_doll_check_hp(Node alive_behavior, Node dead_behavior)
    {
        Func<bool> alive = () => {return doll.GetComponent<DollController>().getHP() > 0; };
        Func<bool> dead = () => (doll.GetComponent<DollController>().getHP() == 0);
        //Func<bool> dead = () => (!doll.GetComponent<DollController>().alive);

        Node alive_node = new Sequence(new LeafAssert(alive), alive_behavior);
        Node dead_node = new Sequence(new LeafAssert(dead), dead_behavior);
        Node res_node = new Selector(alive_node, dead_node);
        return res_node;
    }

    protected Node endgame_checked_alive(GameObject player)
    {
        // alive: execute the action
        // dead: do not exectue
        Func<bool> is_alive = () => (player.GetComponent<PlayerController>().alive);
        Func<bool> is_dead = () => (!player.GetComponent<PlayerController>().alive);
        
        Node cross_line = ending_player_crossed_line(player);
        Node beat_doll = ending_player_beat_doll(player);
        Node doll_check = endgame_doll_check_hp(cross_line, beat_doll);
        Node alive_node = new Sequence(new LeafAssert(is_alive), doll_check);
        Node dead_node = new Sequence(new LeafAssert(is_dead), new LeafWait(100));
        return new Selector(alive_node, dead_node);
    }


    protected Node endgame_check_alive(GameObject player)
    {
        Func<bool> alive = () => (player.GetComponent<PlayerController>().alive);
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Node[] totalNodes = new Node[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            Node playerNode = endgame_checked_alive(players[i]);
            totalNodes[i] = playerNode;
        }
        Node res_node = new Sequence(new LeafAssert(alive),  new SequenceParallel(totalNodes));
        return res_node;
    }

    protected Node endgame_player_dead()
    {
        // executing this node means all players are dead
        return endgame_doll_check_hp(ending_doll_win(), ending_draw());
    }

    public Node BuildTreeRoot()
    {
        //Node roaming = new DecoratorLoop(
        //    new Sequence(
        //        this.ST_ApproachAndWait(this.wander1),
        //        this.ST_ApproachAndWait(this.wander2),
        //        this.ST_ApproachAndWait(this.wander3)));
        //Node trigger = new DecoratorLoop(new LeafAssert(act));
        //Node root = new DecoratorLoop(new DecoratorForceStatus(RunStatus.Success, new SequenceParallel(trigger, roaming)));
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Node player_dead_node = endgame_player_dead();

        Node[] totalNodes = new Node[players.Length + 1];
        totalNodes[players.Length] = player_dead_node;
        for (int i = 0; i < players.Length; i++)
        {
            Node playerNode = endgame_check_alive(players[i]);
            totalNodes[i] = playerNode;
        }
        Node selection_node = new Selector(totalNodes);


        return selection_node;
    }
}
