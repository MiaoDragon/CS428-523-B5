using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RootMotion.FinalIK;

using TreeSharpPlus;

public class MidGameBehaviorTree : MonoBehaviour
{
    public GameObject doll;
    public FullBodyBipedEffector leftHand;
    public FullBodyBipedEffector rightHand;
    public FullBodyBipedEffector rightFoot;
    public FullBodyBipedEffector leftFoot;
    public GameObject endLine;
    public GameObject bulletPrefab;
    public GameObject screen;

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

    //protected Node green_light_doll_node()
    //{
    //    // check if the doll is alive

    //}

    protected Node pick_and_throw(GameObject player)
    {
        Func<RunStatus> before_action_func = () =>
        {
            player.GetComponent<PlayerController>().moving = true;
            // spawn a stone
            //player.GetComponent<PlayerController>().stone = Instantiate(player.GetComponent<PlayerController>().stonePrefab, 
            //                                                            player.GetComponent<PlayerController>().stone_transform);
            player.GetComponent<PlayerController>().stone.SetActive(true);
            player.GetComponent<PlayerController>().stone.GetComponent<Rigidbody>().isKinematic = false;

            //player.GetComponent<PlayerController>().stone.GetComponent<Rigidbody>().isKinematic = true;
            return RunStatus.Success;
        };
        Func<RunStatus> after_action_func = () =>
        {
            player.GetComponent<PlayerController>().moving = false;
            return RunStatus.Success;
        };

        // pick up the stone
        Node pick_up = player.GetComponent<BehaviorMecanim>().Node_StartInteraction(rightHand,
                        player.GetComponent<PlayerController>().stone.GetComponent<InteractionObject>());
        // throw the stone
        Node throw_it = player.GetComponent<BehaviorMecanim>().Node_HandAnimation("WAVE", Val.V(() => (true)));
        Node after_throw = new Sequence(
            player.GetComponent<BehaviorMecanim>().Node_HandAnimation("WAVE", Val.V(() => (false))),
            player.GetComponent<BehaviorMecanim>().Node_StopInteraction(rightHand)
            );
        Func<RunStatus> after_throw_ball_func = () =>
        {
            // compute direction to doll
            Vector3 direction = doll.transform.position - player.transform.position;
            direction = direction / direction.magnitude;
            player.GetComponent<PlayerController>().stone.GetComponent<Rigidbody>().AddForce(direction * 20000.0f, ForceMode.Acceleration);
            // doll hp decreases
            doll.GetComponent<DollController>().hp -= 2;
            return RunStatus.Success;
        };

        Node action_node = new Sequence(new LeafInvoke(before_action_func), pick_up, new LeafWait(500), throw_it, new LeafWait(100), after_throw, new LeafWait(100), new LeafInvoke(after_throw_ball_func),
                                        new LeafInvoke(after_action_func));
        return action_node;
    }

    protected Node walk_forward_player(GameObject player)
    {
        // walk in the z decreasing direction
        Vector3 direction = new Vector3(0, 0, -1);
        // randomly generate a distance in 0~3
        float max_distance = 2.0f;
        float distance = UnityEngine.Random.value * max_distance;

        Val<Vector3> target = Val.V(() => (player.transform.position + direction * distance));

        //player.GetComponent<PlayerController>().moving = true;  // is moving

        Node move_node = player.GetComponent<BehaviorMecanim>().Node_GoTo(target);

        Func<RunStatus> before_move_func = () =>
        {
            player.GetComponent<PlayerController>().moving = true;
            return RunStatus.Success;
        };

        Func<RunStatus> after_move_func = () =>
        {
            player.GetComponent<PlayerController>().moving = false;
            return RunStatus.Success;
        };

        return new Sequence(new LeafInvoke(before_move_func), move_node, new LeafInvoke(after_move_func));
    }


    protected Node green_light_player_node(GameObject player)
    {
        if (player.GetComponent<PlayerController>().controllable)
        {
            return CheckAlive_Player(player, human_move(player));
        }

        NodeWeight[] green_light_actions = new NodeWeight[3];

        // action 1: random move
        green_light_actions[0] = new NodeWeight(0.7f, walk_forward_player(player));
        // action 2: random move within startline
        green_light_actions[1] = new NodeWeight(0.2f, new LeafWait(1000));

        green_light_actions[2] = new NodeWeight(0.1f, pick_and_throw(player));

        Node alive_action = new SelectorShuffle(green_light_actions);


        Node alive_node = new Sequence(alive_action);

        return CheckAlive_Player(player, alive_node);
    }

    protected Node green_light_behavior()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Node[] totalNodes = new Node[players.Length];
        //totalNodes[players.Length] = green_light_doll_node();
        for (int i = 0; i < players.Length; i++)
        {
            Node playerNode = new DecoratorLoop(green_light_player_node(players[i]));
            totalNodes[i] = playerNode;
        }
        Func<RunStatus> set_color_func = () => { screen.GetComponent<MeshRenderer>().material.color = Color.green; return RunStatus.Success; };
        Node set_color = new LeafInvoke(set_color_func);
        return new Sequence(set_color, new SequenceParallel(totalNodes));

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

    protected Node human_move(GameObject player)
    {
        // if motion has 0 value, then don't move
        // otherwise move
        Func<bool> not_moving = () =>
        {
            float horizontal = player.GetComponent<PlayerController>().horizontal;
            float vertical = player.GetComponent<PlayerController>().vertical;
            bool space = player.GetComponent<PlayerController>().space_triggered;
            return ((!space) && Mathf.Abs(horizontal) < 1e-5f && Mathf.Abs(vertical) < 1e-5f);
        };

        Func<bool> moving = () =>
        {
            float horizontal = player.GetComponent<PlayerController>().horizontal;
            float vertical = player.GetComponent<PlayerController>().vertical;
            bool space = player.GetComponent<PlayerController>().space_triggered;
            return !((!space) && Mathf.Abs(horizontal) < 1e-5f && Mathf.Abs(vertical) < 1e-5f);
        };


        Func<Vector3> get_target = () =>
        {
            float horizontal = player.GetComponent<PlayerController>().horizontal;
            float vertical = player.GetComponent<PlayerController>().vertical;
            Vector3 delta_transform = player.transform.forward * vertical + player.transform.right * horizontal;
            Vector3 target = player.transform.position + delta_transform;
            return target;
        };

        Func<RunStatus> before_move_func = () =>
        {
            player.GetComponent<PlayerController>().moving = true;
            return RunStatus.Success;
        };

        Func<RunStatus> after_move_func = () =>
        {
            player.GetComponent<PlayerController>().moving = false;
            return RunStatus.Success;
        };

        Func<bool> space_triggered = () => (player.GetComponent<PlayerController>().space_triggered);
        Func<bool> space_not_triggered = () => (!player.GetComponent<PlayerController>().space_triggered);

        Node move_action = player.GetComponent<BehaviorMecanim>().Node_GoTo(Val.V(get_target));
        Node pick_and_throw_action = pick_and_throw(player);

        Node human_move = new Sequence(new LeafAssert(moving), new LeafAssert(space_not_triggered),  
                                    new LeafInvoke(before_move_func), move_action, new LeafInvoke(after_move_func));
        Node human_pick_and_throw = new Sequence(new LeafAssert(moving), new LeafAssert(space_triggered), 
                                    new LeafInvoke(before_move_func), pick_and_throw_action, new LeafInvoke(after_move_func));
        Node human_not_move = new Sequence(new LeafAssert(not_moving), new LeafInvoke(after_move_func));  // set the moving to false
        Node human_node = new Selector(human_move, human_pick_and_throw, human_not_move);
        return human_node;
    }

    protected Node red_light_player_node(GameObject player)
    {

        if (player.GetComponent<PlayerController>().controllable)
        {
            return CheckAlive_Player(player, human_move(player));
        }

        NodeWeight[] red_light_actions = new NodeWeight[2];

        // action 1: random move
        red_light_actions[0] = new NodeWeight(0.1f, walk_forward_player(player));
        // action 2: random move within startline
        red_light_actions[1] = new NodeWeight(0.9f, new LeafWait(1000));

        Node alive_action = new SelectorShuffle(red_light_actions);

        Node alive_node = new Sequence(alive_action);

        return CheckAlive_Player(player, alive_node);
    }

    protected Node look_and_shoot_doll(GameObject player)
    {
        // doll looks at the player that should be set dead, and then shoot a bullet
        // after that, set the player to dead
        Val<Vector3> direction_target = Val.V(() => new Vector3(player.transform.position.x, 9.0f, player.transform.position.z));

        Node lookNode = doll.GetComponent<BehaviorMecanim>().Node_HeadLook(direction_target);

        Func<RunStatus> shooting_action = () =>
        {
            var bullet = Instantiate(bulletPrefab, doll.transform.position +
                                    Vector3.up * doll.transform.localScale.y / 2 -
                                    Vector3.right * doll.transform.localScale.z / 2, Quaternion.identity);
            bullet.SetActive(true);
            var rb = bullet.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            Vector3 vel = player.transform.position - bullet.transform.position;
            Debug.Log("velocity: " + vel.ToString());
            vel = vel / vel.magnitude;
            float forceScale = 10000.0f;
            rb.AddForce(vel * forceScale, ForceMode.Acceleration);
            //rb.velocity = vel * velScale;
            return RunStatus.Success;
        };
        Node shooting_node = new LeafInvoke(shooting_action);
        Node deadAction = new LeafInvoke(() => { player.GetComponent<PlayerController>().alive = false; return RunStatus.Success; });
        Node action = new Sequence(lookNode, shooting_node, deadAction);
        return action;
    }

    protected Node red_light_doll_check(GameObject player)
    {
        // check if player move. If move then shoot
        Func<bool> cross = () => (player.transform.position.z <= endLine.transform.position.z);
        Func<bool> not_cross = () => (player.transform.position.z > endLine.transform.position.z);
        Func<bool> alive_before = () => (player.GetComponent<PlayerController>().alive);
        Func<bool> dead_before = () => (!player.GetComponent<PlayerController>().alive);
        Func<bool> moving = () => (player.GetComponent<PlayerController>().moving);
        Func<bool> not_moving = () => (!player.GetComponent<PlayerController>().moving);

        // if cross then safe
        Node alive_node = new Selector(new LeafAssert(dead_before), new LeafAssert(cross), new LeafAssert(not_moving));

        // if not cross and move, then shoot
        


        // if cross, then set the player to dead
        //Node deadAction = new LeafInvoke(() => {player.GetComponent<PlayerController>().alive = false; return RunStatus.Success;});
        Node deadAction = look_and_shoot_doll(player);
        Node dead_node = new Sequence(new LeafAssert(alive_before), new LeafAssert(not_cross), new LeafAssert(moving), deadAction, new LeafAssert(alive_before));
        // run again the check for safe so that we can reset the loop
        return new Selector(alive_node, dead_node);
    }
    
    protected Node red_light_doll_node()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Node[] checks = new Node[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            checks[i] = red_light_doll_check(players[i]);
        }
        return new DecoratorLoop(new Sequence(checks));


    }


    protected Node red_light_behavior()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Node[] totalNodes = new Node[players.Length+1];
        totalNodes[players.Length] = red_light_doll_node();
        for (int i = 0; i < players.Length; i++)
        {
            Node playerNode = new DecoratorLoop(red_light_player_node(players[i]));
            totalNodes[i] = playerNode;
        }

        // set color to red
        Func<RunStatus> set_color_func = () => { screen.GetComponent<MeshRenderer>().material.color = Color.red; return RunStatus.Success; };
        Node set_color = new LeafInvoke(set_color_func);
        return new Sequence(set_color, new SequenceParallel(totalNodes));

        //return new SequenceParallel(totalNodes);

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

        // loop until duration has passed
        Node green_light_node = new LoopUntilTimeOut(green_light_behavior(), 5000);
        Node red_light_node = new LoopUntilTimeOut(red_light_behavior(), 5000);
        Node root_sequence = new Sequence(green_light_node, red_light_node);
        Node loop_node = new LoopUntilTimeOut(root_sequence, 100000);

        //Node after_math_node = new shoot_all_players_after();
        return loop_node;
    }
}
