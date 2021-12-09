using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RootMotion.FinalIK;

using TreeSharpPlus;

public class TotalBehaviorTree : MonoBehaviour
{
    public Transform wander1;
    public Transform wander2;
    public Transform wander3;
    public GameObject doll;
    public Transform doll_head;
    public GameObject startLine;
    public GameObject bulletPrefab;

    private BehaviorAgent behaviorAgent;

    public StoryBehaviorTree pregameObj;
    public MidGameBehaviorTree midgameObj;
    public EndGameBehaviorTree endgameObj;

    public FullBodyBipedEffector leftHand;
    public FullBodyBipedEffector rightHand;
    public FullBodyBipedEffector rightFoot;
    public FullBodyBipedEffector leftFoot;
    public GameObject endLine;
    public GameObject screen;

    // Use this for initialization
    // Start is called before the first frame update
    void Start()
    {
        pregameObj = new StoryBehaviorTree();
        pregameObj.doll = doll;
        pregameObj.startLine = startLine;
        pregameObj.bulletPrefab = bulletPrefab;

        midgameObj = new MidGameBehaviorTree();
        midgameObj.doll = doll;
        midgameObj.bulletPrefab = bulletPrefab;
        midgameObj.leftHand = leftHand;
        midgameObj.rightHand = rightHand;
        midgameObj.leftFoot = leftFoot;
        midgameObj.rightFoot = rightFoot;
        midgameObj.endLine = endLine;
        midgameObj.screen = screen;

        endgameObj = new EndGameBehaviorTree();
        endgameObj.doll = doll;
        endgameObj.leftFoot = leftFoot;
        endgameObj.rightFoot = rightFoot;
        endgameObj.leftHand = leftHand;
        endgameObj.rightHand = rightHand;

        behaviorAgent = new BehaviorAgent(this.BuildTreeRoot());
        BehaviorManager.Instance.Register(behaviorAgent);
        behaviorAgent.StartBehavior();
    }

    // Update is called once per frame
    void Update()
    {
        
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

        // loop until duration has passed

        Node root_node = new Sequence(pregameObj.BuildTreeRoot(), midgameObj.BuildTreeRoot(), endgameObj.BuildTreeRoot());
        return root_node;
    }
}
