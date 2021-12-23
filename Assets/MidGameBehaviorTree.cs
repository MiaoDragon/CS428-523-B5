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

    public float green_light_time = 5.0f;
    public float red_light_time = 5.0f;
    public float total_time = 100.0f;

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

    public Node GoToStatic(GameObject player, Func<Vector3> get_target_func)
    {
        Vector3 saved_target = player.transform.position;
        Func<RunStatus> set_target_value = () =>
        {
            Vector3 target = get_target_func();
            saved_target = target;

            return RunStatus.Success;
        };
        Func<Vector3> new_get_target_func = () =>
        {
            return saved_target;
        };
        return new Sequence(new LeafInvoke(set_target_value), player.GetComponent<BehaviorMecanim>().Node_GoTo(new_get_target_func));
    }

    public Node GoUpToDistance(GameObject player, Func<Vector3> get_target_func, float distance)
    {
        Vector3 previous_position = player.transform.position;
        float traveled_distance = 0.0f;

        Func<RunStatus> set_prev_position = () =>
        {
            previous_position = player.transform.position;
            return RunStatus.Success;
        };

        Func<Vector3> new_get_target_func = () =>
        {
            Vector3 target = get_target_func();
            // if distance has passed, then stop
            Vector3 travel = player.transform.position - previous_position;
            traveled_distance += travel.magnitude;
            if (traveled_distance >= distance)
            {
                return player.transform.position;
            }
            previous_position = player.transform.position;
            return target;
        };
        return new Sequence(new LeafInvoke(set_prev_position), player.GetComponent<BehaviorMecanim>().Node_GoTo(new_get_target_func));
    }


    protected Node check_hit(GameObject player, Node action)
    {
        // if the player is hit, then play the hit action
        // otherwise, go do action
        Func<bool> hit_func = () => (player.GetComponent<PlayerController>().hit);
        Func<RunStatus> before_anim_func = () =>
        {
            player.GetComponent<PlayerController>().moving = true;
            return RunStatus.Success;
        };
        Node hit_anim_node = player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("DUCK", 2000);

        Func<RunStatus> reset_hit_func = () =>
        {
            player.GetComponent<PlayerController>().hit = false;
            player.GetComponent<PlayerController>().moving = false;
            return RunStatus.Success;
        };
        Node before_anim_node = new LeafInvoke(before_anim_func);
        Node reset_hit_node = new LeafInvoke(reset_hit_func);
        Node stop_anim = player.GetComponent<BehaviorMecanim>().Node_BodyAnimation("DUCK", false);

        Node check_hit_node = new IfElseNode(hit_func, new Sequence(before_anim_node, hit_anim_node, reset_hit_node), 
            new Sequence(new LeafTrace("player " + player.name + " check hit false, stop animation and take action"), stop_anim, action));
        return check_hit_node;
    }

    protected Node GoToAndHit(GameObject player, GameObject target_player)
    {
        // the player go to the other player and hit him from behind
        // when the player is close enough to the other player, hit the other player
        // if the player is too far, go to the player's direction for some distance instead
        float radius = 2.0f;
        float distance = 6.0f;
        Func<Vector3> get_target_func = () =>
        {
            Vector3 direction = target_player.transform.position - player.transform.position;
            direction = direction.normalized;
            Vector3 target_location = target_player.transform.position - direction * radius * 0.8f;
            direction = target_location - player.transform.position;
            direction = direction.normalized;
            // cap the target location by the distance
            float distance_to_cur = Mathf.Min((target_location - player.transform.position).magnitude, distance);
            return player.transform.position + direction * distance_to_cur;
        };
        Func<bool> check_within_radius_f = () =>
        {
            Vector3 direction = target_player.transform.position - player.transform.position;
            return direction.magnitude <= radius;
        };
        Func<RunStatus> before_move = () =>
        {
            player.GetComponent<PlayerController>().moving = true;
            return RunStatus.Success;
        };

        Func<RunStatus> after_move = () =>
        {
            player.GetComponent<PlayerController>().moving = false;
            return RunStatus.Success;
        };

        Node walk = new Sequence(new LeafInvoke(before_move), GoUpToDistance(player, get_target_func, distance), new LeafInvoke(after_move));
        Node face = player.GetComponent<BehaviorMecanim>().Node_OrientTowards(Val.V(() => (target_player.transform.position)));
        Node hit = player.GetComponent<BehaviorMecanim>().ST_PlayHandGesture("HITSTEALTH", Val.V(() => ((long)(500))));
        Func<RunStatus> after_hit = () =>
        {
            target_player.GetComponent<PlayerController>().hit = true;
            return RunStatus.Success;
        };


        Node hit_node = new Sequence(new LeafInvoke(before_move), face, hit, new LeafInvoke(after_hit), new LeafInvoke(after_move));
        Node check_node = new IfElseNode(check_within_radius_f, hit_node, new LeafAssert(() => (true)));
        Node start_node = new IfElseNode(check_within_radius_f, hit_node, new Sequence(walk, check_node));
        return start_node;
        //return new Sequence(walk, hit, new LeafInvoke(after_hit));
    }

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

        Func<Vector3> get_target = () => (player.transform.position + direction * distance);
        //player.GetComponent<PlayerController>().moving = true;  // is moving

        Node move_node = GoToStatic(player, get_target);

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

        NodeWeight[] green_light_actions = new NodeWeight[4];

        // action 1: random move
        green_light_actions[0] = new NodeWeight(0.7f, walk_forward_player(player));
        // action 2: random move within startline
        green_light_actions[1] = new NodeWeight(0.2f, new LeafWait(1000));

        green_light_actions[2] = new NodeWeight(0.1f, pick_and_throw(player));

        // action 4: hit someone
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject selected_player = players[0];
        while (true)
        {
            // sample one player until it's not the current player
            int idx = UnityEngine.Random.Range(0, players.Length);
            if (!players[idx].GetComponent<PlayerController>().alive)
            {
                continue;
            }
            if (players[idx].name == player.name)
            {
                continue;
            }
            selected_player = players[idx];
            break;
        }

        green_light_actions[3] = new NodeWeight(0.15f, GoToAndHit(player, selected_player));



        Node alive_action = new SelectorShuffle(green_light_actions);


        Node alive_node = new Sequence(alive_action);

        // check if hit
        Node total_node = check_hit(player, alive_node);

        return CheckAlive_Player(player, total_node);
    }

    protected Node green_light_doll_node()
    {
        Val<Vector3> target = Val.V(() => (doll.GetComponent<DollController>().default_face));
        return doll.GetComponent<BehaviorMecanim>().Node_HeadLookTurnFirst(target);
    }

    protected Node green_light_behavior()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        Node[] totalNodes = new Node[players.Length+1];
        //totalNodes[players.Length] = green_light_doll_node();
        for (int i = 0; i < players.Length; i++)
        {
            Node playerNode = new DecoratorLoop(green_light_player_node(players[i]));
            totalNodes[i] = playerNode;
        }
        Node dollNode = green_light_doll_node();
        totalNodes[players.Length] = dollNode;

        Func<RunStatus> set_color_func = () => { screen.GetComponent<MeshRenderer>().material.color = Color.green; return RunStatus.Success; };
        Node set_color = new LeafInvoke(set_color_func);
        return new Sequence(set_color, new SequenceParallel(totalNodes));

    }


    protected Node CheckAlive_Player(GameObject player, Node alive_node)
    {
        // if alive: do other stuff
        // otherwise: play dead
        Func<bool> alive = () => (player.GetComponent<PlayerController>().alive);
        Node stop_anim = player.GetComponent<BehaviorMecanim>().Node_BodyAnimation("DUCK", false);
        Node playDead = player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("DYING", 100);
        return new IfElseNode(alive, alive_node, new Sequence(stop_anim, playDead));
    }

    

    protected Node human_hit(GameObject player)
    {
        // find the nearest player that current player is facing
        float radius = 2.0f;
        float distance = 6.0f;
        float angle_radius = Mathf.PI / 180.0f * 90.0f;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Func<RunStatus> before_hit_func = () =>
        {
            player.GetComponent<PlayerController>().moving = true;
            return RunStatus.Success;
        };
        Node hit = player.GetComponent<BehaviorMecanim>().ST_PlayHandGesture("HITSTEALTH", Val.V(() => ((long)(500))));
        Func<RunStatus> after_hit_func = () =>
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].name == player.name)
                {
                    continue;
                }
                Vector3 direction = players[i].transform.position - player.transform.position;
                float angle = Mathf.Acos(Vector3.Dot(direction.normalized, player.transform.forward));
                Debug.Log("angle: " + (angle * 180.0f / Mathf.PI));
                if ((direction.magnitude <= radius) && (angle <= angle_radius))
                {
                    players[i].GetComponent<PlayerController>().hit = true;
                }
            }
            return RunStatus.Success;
        };

        return new Sequence(new LeafInvoke(before_hit_func), hit, new LeafInvoke(after_hit_func));
        //return new Sequence(walk, hit, new LeafInvoke(after_hit));
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
            bool p_trigger = player.GetComponent<PlayerController>().p_triggered;

            return ((!p_trigger) && (!space) && Mathf.Abs(horizontal) < 1e-8f && Mathf.Abs(vertical) < 1e-8f);
        };

        Func<bool> moving = () =>
        {
            float horizontal = player.GetComponent<PlayerController>().horizontal;
            float vertical = player.GetComponent<PlayerController>().vertical;
            bool space = player.GetComponent<PlayerController>().space_triggered;
            bool p_trigger = player.GetComponent<PlayerController>().p_triggered;
            return !((!p_trigger) && (!space) && Mathf.Abs(horizontal) < 1e-8f && Mathf.Abs(vertical) < 1e-8f);
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
        Func<bool> p_triggered = () => (player.GetComponent<PlayerController>().p_triggered);
        Func<bool> p_not_triggered = () => (!player.GetComponent<PlayerController>().p_triggered);

        Node move_action = player.GetComponent<BehaviorMecanim>().Node_GoTo(get_target);
        Node pick_and_throw_action = pick_and_throw(player);

        Node human_move = new Sequence(new LeafInvoke(before_move_func), move_action, new LeafInvoke(after_move_func));
        Node human_pick_and_throw = new Sequence(new LeafAssert(space_triggered),
                                    new LeafInvoke(before_move_func), pick_and_throw_action, new LeafInvoke(after_move_func));

        Node human_attack = new Sequence(new LeafAssert(p_triggered),
                                         new LeafInvoke(before_move_func), human_hit(player), new LeafInvoke(after_move_func));

        Node human_node = new Selector(human_pick_and_throw, human_attack, human_move);

        Node total_node = check_hit(player, human_node);
        return total_node;
    }

    
    protected Node red_light_player_node(GameObject player)
    {

        if (player.GetComponent<PlayerController>().controllable)
        {
            return CheckAlive_Player(player, human_move(player));
        }

        NodeWeight[] red_light_actions = new NodeWeight[3];

        // action 1: random move
        red_light_actions[0] = new NodeWeight(0.05f, walk_forward_player(player));
        // action 2: random move within startline
        red_light_actions[1] = new NodeWeight(0.9f, new LeafWait(1000));

        // action 4: hit someone
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject selected_player = players[0];
        while (true)
        {
            // sample one player until it's not the current player
            int idx = UnityEngine.Random.Range(0, players.Length);
            if (!players[idx].GetComponent<PlayerController>().alive)
            {
                continue;
            }
            if (players[idx].name == player.name)
            {
                continue;
            }
            selected_player = players[idx];
            break;
        }

        red_light_actions[2] = new NodeWeight(0.05f, GoToAndHit(player, selected_player));

        Node alive_action = new SelectorShuffle(red_light_actions);

        Node alive_node = new Sequence(alive_action);
        Node total_node = check_hit(player, alive_node);

        return CheckAlive_Player(player, total_node);
    }

    Func<bool> raycast_check(GameObject player)
    {
        Func<bool> raycast_f = () =>
        {
            // check if the ray cast from doll's eye to the player intersects with anything else
            Vector3 player_relative_transform = player.transform.position - doll.GetComponent<DollController>().eye.transform.position;
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(doll.GetComponent<DollController>().eye.transform.position,
                            player_relative_transform.normalized, out hitInfo, player_relative_transform.magnitude);
            // get info about the cloest collider
            if (hit)
            {
                if (hitInfo.collider.gameObject.tag != "Player" && hitInfo.collider.gameObject.tag != "PlayerContext")
                {
                    return false;
                }
                return true;
            }
            return true;
        };
        return raycast_f;
    }

    protected Node look_and_shoot_doll(GameObject player)
    {
        // doll looks at the player that should be set dead, and then shoot a bullet
        // after that, set the player to dead
        Val<Vector3> direction_target = Val.V(() => new Vector3(player.transform.position.x, 1.0f, player.transform.position.z));

        Node lookNode = doll.GetComponent<BehaviorMecanim>().Node_HeadLookTurnFirst(direction_target);

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
            vel = vel / vel.magnitude;
            float forceScale = 10000.0f;
            rb.AddForce(vel * forceScale, ForceMode.Acceleration);
            //rb.velocity = vel * velScale;
            return RunStatus.Success;
        };
        Node shooting_node = new LeafInvoke(shooting_action);
        Node deadAction = new LeafInvoke(() => { player.GetComponent<PlayerController>().alive = false; return RunStatus.Success; });
        Node action = new Sequence(lookNode, shooting_node, deadAction);
        Node check_and_action = new IfElseNode(raycast_check(player), action, new LeafAssert(() => (true)));
        return check_and_action;
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
        Node green_light_node = new LoopUntilTimeOut(green_light_behavior(), (long)(green_light_time*1000));
        Node red_light_node = new LoopUntilTimeOut(red_light_behavior(), (long)(red_light_time*1000));
        Node root_sequence = new Sequence(green_light_node, red_light_node, new LeafWait(100));
        Node loop_node = new LoopUntilTimeOut(root_sequence, (long)(total_time*1000)+100);

        //Node after_math_node = new shoot_all_players_after();
        return loop_node;
    }
}
