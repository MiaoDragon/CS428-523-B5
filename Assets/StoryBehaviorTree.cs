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
    public Transform doll_head;
    public GameObject startLine;
    public GameObject bulletPrefab;

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

    protected Node PreGame_Look_Shoot_Doll(GameObject player)
    {
        // doll looks at the player that should be set dead, and then shoot a bullet
        // after that, set the player to dead
        Val<Vector3> direction_target = Val.V(()=>new Vector3(player.transform.position.x, 9.0f, player.transform.position.z));

        Node lookNode = doll.GetComponent<BehaviorMecanim>().Node_HeadLook(direction_target);

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
        return action;
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
        Func<bool> dead = () => (!player.GetComponent<PlayerController>().alive);

        Node playDead = player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("DYING", 100);
        Node deadBehavior = new Sequence(new LeafAssert(dead), playDead);
        Node aliveBehavior = new Sequence(new LeafAssert(alive), alive_node);
        return new Selector(aliveBehavior, deadBehavior);
    }

    protected Node RandomGoToRadius_Player(GameObject player, float radius)
    {
        // generate a random move within the radius
        Val<Vector3> position = Val.V(() =>
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
        }        
        );
        return new Sequence(player.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(4000));
    }

    protected Node RandomGoToRadiusSafe_Player(GameObject player, float radius)
    {
        // generate a random move within the radius
        Val<Vector3> position = Val.V(() =>
        {
            Vector3 target_pos = player.transform.position;
            float rand_angle = UnityEngine.Random.value * Mathf.PI * 2;
            float rand_radius = UnityEngine.Random.value * 0.8f * radius + 0.2f*radius;
            float rand_x = Mathf.Cos(rand_angle) * rand_radius;
            float rand_y = Mathf.Sin(rand_angle) * rand_radius;

            target_pos.x = target_pos.x + rand_x;
            target_pos.z = Mathf.Min(Mathf.Max(target_pos.z + rand_y, startLine.transform.position.z), 24.0f);
            return target_pos;
        }
        );
        return new Sequence(player.GetComponent<BehaviorMecanim>().Node_GoTo(position), new LeafWait(4000));
    }


    protected Node PreGame_Action_Player(GameObject player)
    {
        /** perform actions with random probabilities
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
            Func<Vector3> get_target = () =>
            {
                float horizontal = player.GetComponent<PlayerController>().horizontal;
                float vertical = player.GetComponent<PlayerController>().vertical;
                Vector3 delta_transform = player.transform.forward * vertical +player.transform.right * horizontal;
                Vector3 target = player.transform.position + delta_transform;
                return target;
            };
            return player.GetComponent<BehaviorMecanim>().Node_GoTo(Val.V(get_target));
        }
        
        int num_actions = 3;
        NodeWeight[] pregame_actions = new NodeWeight[num_actions];

        // action 1: random move
        pregame_actions[0] = new NodeWeight(0.1f, RandomGoToRadius_Player(player, 8.0f));
        // action 2: random move within startline
        pregame_actions[1] = new NodeWeight(0.5f, RandomGoToRadiusSafe_Player(player, 8.0f));

        // action 3: dance
        pregame_actions[2] = new NodeWeight(0.2f, player.GetComponent<BehaviorMecanim>().ST_PlayBodyGesture("BREAKDANCE", 2000));

        Node action_node = new Sequence(new SelectorShuffle(pregame_actions));
        return action_node;
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
        Node loopNode = new DecoratorLoop((int)(timeout / Time.deltaTime), parallelNode);
        return new Sequence(loopNode);
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
                                      new Sequence(PreGame_Loop(10.0f))));
        Debug.Log("after building");

        return root;
    }
}
