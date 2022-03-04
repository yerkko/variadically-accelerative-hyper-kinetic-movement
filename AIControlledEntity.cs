using System;
using System.Collections;
using System.Collections.Generic;
using FSM;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(PlayerRB))]
public class AIControlledEntity : MonoBehaviour
{
    public GameObject Agent;

    public float MinDistance;

    public float MaxDistance;

    public float InitialAgentDistance;

    private float agentDistance;

    public PlayerRB playerRB;

    public Pathfinder pathfinder;
    public float BOOST_TURN_THRESH;

    [SerializeField]
    private FiniteStateMachine FiniteStateMachine;

    private Sensor GroundTruthProvider;


    [Header("STATE-TRANSITIONING PARAMS")]
    public float DefenseThreshold;
    public float OffenseThreshold;

    public float DistanceWeight;
    public float VelocityWeight;
    public float NormalVelocityComponentWeight;
    public float UrgencyBonus = 0.3f;
    public float DistanceToGoalWeight;

    protected float TotalWeight => DistanceWeight + VelocityWeight + NormalVelocityComponentWeight + DistanceToGoalWeight;


    // Defense
    private float currentBestDefensiveValue = Mathf.NegativeInfinity;
    private GameObject bestDefensiveObject = null;
    public int DefensiveVotes;
    // Offense
    private float currentBestOffensiveValue = Mathf.NegativeInfinity;
    private GameObject bestOffensiveObject;
    public int OffensiveVotes;



    public Type PreferredState;
    public GameObject Target { get; set; }

    private Vector3 previousDir;

    void Awake()
    {

        // rb = GetComponent<Rigidbody2D>();
        playerRB = GetComponent<PlayerRB>();
        pathfinder = Agent.GetComponent<Pathfinder>();
        FiniteStateMachine = GetComponent<FiniteStateMachine>();
        GroundTruthProvider = GetComponent<Sensor>();


    }

    public void Vote()
    {
        VoteAttacking();
        VoteDefending();
        MakeDecision();
    }

    public void MakeDecision()
    {
        // int defenseVotes = DefensiveVotes * ((currentBestDefensiveValue >= DefenseThreshold) ? 1 : 0);
        // int offenseVotes = OffensiveVotes * ((currentBestOffensiveValue >= OffenseThreshold) ? 1 : 0);
        float defenseVotes = DefensiveVotes * (currentBestDefensiveValue );
        float offenseVotes = OffensiveVotes * (currentBestOffensiveValue);
        float totalVotes = defenseVotes + offenseVotes;

        if (defenseVotes == offenseVotes && defenseVotes == 0)
        {
            // Debug.Log("SAME");
        }

        if(defenseVotes >= offenseVotes) {
            this.PreferredState = typeof(DefendingState);
            this.Target = bestDefensiveObject;

        }else{
            this.PreferredState = typeof(AttackingState);
            this.Target = bestOffensiveObject;

        }

    }

    public void VoteAttacking()
    {
        currentBestOffensiveValue = Mathf.NegativeInfinity;
        foreach (Ball ball in Ball.AllBalls)
        {
            if (GroundTruthProvider.TargetDatas.TryGetValue(ball, out Sensor.TargetData targetData))
            {

                float offValue;
                var maxDistance = GroundTruthProvider.MaximumDistanceToGoal;

                var offensiveAction = (DistanceWeight / TotalWeight) * (maxDistance / targetData.DistanceToTarget);
                var distToGoalPost = (DistanceToGoalWeight / TotalWeight) * (targetData.TargetToGoalDistance / maxDistance);

                offValue = offensiveAction + distToGoalPost;

                if (offensiveAction >= 0.6) offValue *= UrgencyBonus;

                if (offValue >= currentBestOffensiveValue)
                {
                    currentBestOffensiveValue = offValue;
                    bestOffensiveObject = ball.gameObject;
                }

            }
        }

    }

    public void VoteDefending()
    {
        currentBestDefensiveValue = Mathf.NegativeInfinity;
        foreach (Ball ball in Ball.AllBalls)
        {
            if (GroundTruthProvider.TargetDatas.TryGetValue(ball, out Sensor.TargetData targetData))
            {

                var maxDistance = GroundTruthProvider.MaximumDistanceToGoal;
                var defensiveAction = (DistanceToGoalWeight / TotalWeight) * (1 - (targetData.TargetToGoalDistance / maxDistance));
                var distance = targetData.VectorToGoalPost;
                var vel = targetData.TargetVelocity;
                var normalVel = (NormalVelocityComponentWeight / TotalWeight) * targetData.TargetNormalVelocityComponent;

                var defValue = defensiveAction;
                if (vel != Vector2.zero) defValue *= normalVel >= 0.5 ? normalVel : 1;

                if (defensiveAction >= 0.6) defValue += UrgencyBonus;
                // activationDictionary[ball.gameObject] = defensiveAction;
                // ActivationValue = defValue;

                if (defValue >= currentBestDefensiveValue)
                {
                    currentBestDefensiveValue = defValue;
                    bestDefensiveObject = ball.gameObject;
                }



            }
        }




    }


    void Start()
    {
        // agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        FiniteStateMachine.SetStates(
            new Dictionary<Type, BaseState>()
            {
                {
                    typeof(IdleState), new IdleState(this.gameObject, FiniteStateMachine)

                },
                {
                    typeof(DefendingState), new DefendingState(this.gameObject, FiniteStateMachine)

                },                {
                typeof(AttackingState), new AttackingState(this.gameObject, FiniteStateMachine)

            }

            });

        Vote();

    }


    void ResetAgent()
    {

        //check if this is correct with a rigidbody
        Agent.transform.position = this.transform.position + Vector3.up * InitialAgentDistance;
        pathfinder.OutOfBounds = false;
    }

    private Vector3 velocity = Vector3.zero;
    public float ChangingDirectionSmoothingTime;

    public Vector2 Offset => FiniteStateMachine.Offset;

    void AdjustEntity()
    {
        //this is useful with the rocket league control scheme, but not like this 
        // var dir = this.transform.InverseTransformDirection(offsetVector);
        Vector3 directionToTarget;
        var dir = this.transform.InverseTransformDirection((pathfinder.ObjectToFollow.transform.position) - this.transform.position);
        if (previousDir != null)
        {
            var newDirectionToTarget = (Agent.transform.position - this.transform.position).normalized;
            directionToTarget = Vector3.SmoothDamp(previousDir, newDirectionToTarget, ref velocity, ChangingDirectionSmoothingTime);
        }
        else
        {
            directionToTarget = (Agent.transform.position - this.transform.position).normalized;
        }
        playerRB.UpdateInputs(directionToTarget.x, directionToTarget.y, Mathf.Abs(dir.x) < BOOST_TURN_THRESH);
        previousDir = directionToTarget;
    }


    void RunAI()
    {

        // if agent is too far away, reset agent to pos + dist * up 
        agentDistance = Vector2.Distance(this.transform.position, pathfinder.ObjectToFollow.transform.position);

        if (Target == null || agentDistance > MaxDistance || pathfinder.OutOfBounds )
        {
            ResetAgent();
        }

        //if agent is between min dist and max dist, MOVE directly to it (with the same controls as the player)

        else if (MaxDistance >= agentDistance && agentDistance >= MinDistance)
        {

            // rb.velocity = (Agent.transform.position - this.transform.position) * Speed;

            AdjustEntity();

        }

        // if the agent is too close, stop moving (and hit the target, probably, still to be implemented)
        else if (agentDistance <= MinDistance)
        {
            // Debug.Log("TOO CLOSE");
        }
    }

    void OnTriggerEnter2D(Collider2D col){
        if(col.gameObject.CompareTag("DefendingZone")){
            this.Target = FiniteStateMachine.Target;
        }
    }


    public void ChangeTarget(GameObject target){
        if(target != this.Target){
            // Debug.Log("CHANGED TARGET TO " + target.name);
            ResetAgent();
            pathfinder.ChangeOffset(this.Offset);
        }
        this.Target = target;


    }

    // Update is called once per frame

    void FixedUpdate()
    {
        RunAI();
    }
    void Update()
    {
        // pathfinder.target = FiniteStateMachine.Target;
        pathfinder.target = this.Target;
        // agent.SetDestination(target.transform.position);
    }


#if UNITY_EDITOR
    // private void OnGUI()
    // {
    //     var s = $"DEFENSIVE VALUE : {this.currentBestDefensiveValue} \n";
    //     s += $"TARGET: {this.bestDefensiveObject} \n ------ \n";
    //     s += $"OFFENSIVE VALUE : {this.currentBestOffensiveValue} \n";
    //     s += $"TARGET: {this.bestOffensiveObject} \n ------ \n";
    //     s += $"STATE: {this.FiniteStateMachine.CurrentState.NAME} \n";
    //     s += $"OFFSET: {this.Offset.x}, {this.Offset.y}";
    //     GUI.Box(new Rect(10, 10, 500, 600), "BRAIN \n -------- \n" + s);

    // }
#endif



}
