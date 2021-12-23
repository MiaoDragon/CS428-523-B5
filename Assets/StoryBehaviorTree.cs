using UnityEngine;
using System;
using System.Collections;
using TreeSharpPlus;
using RootMotion.FinalIK;

public class StoryBehaviorTree : MonoBehaviour
{
    public Transform wander1;
    public Transform wander2;
    public Transform wander3;
    public GameObject doll;
    public Transform doll_head;
    public GameObject startLine;
    public GameObject bulletPrefab;

    public FullBodyBipedEffector leftHand;
    public FullBodyBipedEffector rightHand;
    public FullBodyBipedEffector rightFoot;
    public FullBodyBipedEffector leftFoot;

    public float total_time = 20.0f;
    private BehaviorAgent behaviorAgent;
    // Use this for initialization
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
        Node[] checks = new Node[players.Length+1];
        Val<Vector3> target = Val.V(() => (doll.GetComponent<DollController>().default_face));
        checks[0] = doll.GetComponent<BehaviorMecanim>().Node_HeadLookTurnFirst(target);
        for (int i = 0; i < players.Length; i++)
        {
            checks[i+1] = PreGame_Check_Doll(players[i]);
        }
        return new DecoratorLoop(new Sequence(checks));

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
                Debug.Log("hit object is: " + hitInfo.collider.gameObject.name);
                if (hitInfo.collider.gameObject.tag != "Player" && hitInfo.collider.gameObject.tag != "PlayerContext")
                {
                    Debug.Log("raycast evaluate to be false");
                    return false;
                }
                Debug.Log("raycast evaluate to be true");
                return true;
            }
            Debug.Log("raycast evaluate to be true");
            return true;
        };
        return raycast_f;
    }

    protected Node PreGame_Look_Shoot_Doll(GameObject player)
    {
        // doll looks at the player that should be set dead, and then shoot a bullet
        // after that, set the player to dead
        Val<Vector3> direction_target = Val.V(()=>new Vector3(player.transform.position.x, 1.0f, player.transform.position.z));

        Node lookNode = doll.GetComponent<BehaviorMecanim>().Node_HeadLookTurnFirst(direction_target);
        //Node lookNode = doll.GetComponent<BehaviorMecanim>().Node_HeadLook(direction_target);

        Func<RunStatus> shooting_action = () =>
        {
            var bullet = Instantiate(bulletPrefab, doll.transform.position + 
                                    Vector3.up*doll.transform.localScale.y/2 - 
                                    Vector3.right*doll.transform.localScale.z/2, Quaternion.identity);
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
        Node check_and_action = new IfElseNode(raycast_check(player), action, new LeafAssert(() => (true)));
        return check_and_action;
    }

    protected Node PreGame_Check_Doll(GameObject player)
    {
        Func<bool> cross = () => (player.transform.position.z < startLine.transform.position.z);
        Func<bool> safe = () => (player.transform.position.z >= startLine.transform.position.z);
        Func<bool> alive_before = () => (player.GetComponent<PlayerController>().alive);
        Func<bool> dead_before = () => (!player.GetComponent<PlayerController>().alive);

        // if cross, then set the player to dead
        Node alive = new Selector(new LeafAssert(dead_before), new LeafAssert(safe));//, new LeafTrace("Player didn't cross the line"));
        //Node deadAction = new LeafInvoke(() => {player.GetComponent<PlayerController>().alive = false; return RunStatus.Success;});
        Node deadAction = PreGame_Look_Shoot_Doll(player);
        Node dead = new Sequence(new LeafTrace("dead..."), new LeafAssert(alive_before), new LeafAssert(cross), deadAction, new LeafAssert(alive_before));
        // run again the check for safe so that we can reset the loop
        return new Selector(alive, dead);
    }
    #endregion

    #region Player-Related Functions
    /* pregame */
    protected Node PreGame_Player(GameObject player)
    {
        //Node action = new Sequence(this.ST_ApproachAndWait(player, this.wander1));

        Node action = PreGame_Action_Player(player);
        return new DecoratorLoop(CheckAlive_Player(player, action));
    }
    protected Node CheckAlive_Player(GameObject player, Node alive_node)
    {
        // if alive: do other stuff
        // otherwise: play dead
        Func<bool> alive = () => (player.GetComponent<PlayerController>().alive);
        Node stop_anim = player.GetComponent<BehaviorMecanim>().Node_BodyAnimation("DUCK", false);
        Node playDead = player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("DYING", 100);
        Node deadBehavior = new Sequence(stop_anim, playDead);
        Node aliveBehavior = alive_node;
        return new IfElseNode(alive, aliveBehavior, deadBehavior);
    }

    protected Node RandomGoToRadius_Player(GameObject player, float radius)
    {
        // generate a random move within the radius
        Func<Vector3> get_position = () =>
        {
            Vector3 target_pos = player.transform.position;
            // note: this is not the correct way of uniformly generating an angle, but we'll just use it
            float rand_angle = UnityEngine.Random.value * Mathf.PI * 2;
            float rand_radius = UnityEngine.Random.value * 0.8f * radius + 0.2f * radius;
            float rand_x = Mathf.Cos(rand_angle) * rand_radius;
            float rand_y = Mathf.Sin(rand_angle) * rand_radius;
            target_pos.x = target_pos.x + rand_x;
            target_pos.z = Mathf.Min(target_pos.z + rand_y, 24.0f);

            return target_pos;
        };
        return new Sequence(GoToStatic(player, get_position), new LeafWait(1000));
    }

    protected Node RandomGoToRadiusSafe_Player(GameObject player, float radius)
    {
        // generate a random move within the radius
        Func<Vector3> get_position = () =>
        {
            Vector3 target_pos = player.transform.position;
            float rand_angle = UnityEngine.Random.value * Mathf.PI * 2;
            float rand_radius = UnityEngine.Random.value * 0.8f * radius + 0.2f * radius;
            float rand_x = Mathf.Cos(rand_angle) * rand_radius;
            float rand_y = Mathf.Sin(rand_angle) * rand_radius;

            target_pos.x = target_pos.x + rand_x;
            target_pos.z = Mathf.Min(Mathf.Max(target_pos.z + rand_y, startLine.transform.position.z), 24.0f);
            return target_pos;
        };
        return new Sequence(GoToStatic(player, get_position), new LeafWait(1000));
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

        Node check_hit_node = new IfElseNode(hit_func, 
            new Sequence(new LeafTrace("player " + player.name + " check hit true"), before_anim_node, hit_anim_node, reset_hit_node),
            new Sequence(new LeafTrace("player " + player.name + " check hit false, stop animation and take action"), stop_anim, action));
        return check_hit_node;
    }

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
            return RunStatus.Success;
        };

        Node action_node = new Sequence(new LeafInvoke(before_action_func), pick_up, new LeafWait(500), throw_it, new LeafWait(100), after_throw, new LeafWait(100), new LeafInvoke(after_throw_ball_func),
                                        new LeafInvoke(after_action_func));
        return action_node;
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
            player.GetComponent<PlayerController>().moving = false;
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

            return ((!p_trigger)&&(!space) && Mathf.Abs(horizontal) < 1e-8f && Mathf.Abs(vertical) < 1e-8f);
        };

        Func<bool> moving = () =>
        {
            float horizontal = player.GetComponent<PlayerController>().horizontal;
            float vertical = player.GetComponent<PlayerController>().vertical;
            bool space = player.GetComponent<PlayerController>().space_triggered;
            bool p_trigger = player.GetComponent<PlayerController>().p_triggered;
            return !((!p_trigger)&&(!space) && Mathf.Abs(horizontal) < 1e-8f && Mathf.Abs(vertical) < 1e-8f);
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



    protected Node PreGame_Action_Player(GameObject player)
    {
        /** 
         * NEWLY ADDED: first check if the player is hit or not, if hit, then perform the hit action
         * 
         * perform actions with random probabilities
         *  potential actions:
         *  1. move to random location within a radius
         *  2. move to random location within a radius, and do not cross the line
         *  3. dance
         *  4. discuss with other players
         *  5. have a fight with other players
         **/

        // if the player is controlled by keyboard, then return a different node
        if (player.GetComponent<PlayerController>().controllable)
        {
            return human_move(player);
        }
        
        int num_actions = 4;
        NodeWeight[] pregame_actions = new NodeWeight[num_actions];

        // action 1: random move
        pregame_actions[0] = new NodeWeight(0.1f, RandomGoToRadius_Player(player, 2.0f));
        // action 2: random move within startline
        pregame_actions[1] = new NodeWeight(0.3f, RandomGoToRadiusSafe_Player(player, 2.0f));

        // action 3: idle
        pregame_actions[2] = new NodeWeight(0.5f, new LeafWait(500));

        // action 4: hit others
        // randomly select one player
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

        pregame_actions[3] = new NodeWeight(0.1f, GoToAndHit(player, selected_player));


        //pregame_actions[2] = new NodeWeight(0.1f, player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("BREAKDANCE", 1000));

        Node action_node = new Sequence(new SelectorShuffle(pregame_actions));
        //return action_node;
        return check_hit(player, action_node);
    }
    #endregion
    protected Node PreGame_Loop(float timeout)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Node dollNode = PreGame_Doll(players);

        Node[] totalNodes = new Node[players.Length + 1];
        totalNodes[0] = dollNode;
        for (int i=0; i<players.Length; i++)
        {
            Node playerNode = PreGame_Player(players[i]);
            totalNodes[i + 1] = playerNode;
        }
        Node parallelNode = new SequenceParallel(totalNodes);
        //Node loopNode = new DecoratorLoop((int)(timeout / Time.deltaTime), parallelNode);
        Node loopNode = new LoopUntilTimeOut(parallelNode, Val.V(() => ((long)(timeout * 1000))));

        return new Sequence(loopNode);
        //return new Sequence(participant.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(1000));
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
        //Node root = new DecoratorLoop(new DecoratorForceStatus(RunStatus.Success, 
        //                              new Sequence(PreGame_Loop(10.0f))));
        Node root = PreGame_Loop(total_time);

        return root;
    }
}
