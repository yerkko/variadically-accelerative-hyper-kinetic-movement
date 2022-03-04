using System.Collections.Generic;
using UnityEngine;
public class Sensor : MonoBehaviour
{

    public float RefreshRate = 0.5f;
    public PlayerRB _playerRb;
    // Start is called before the first frame update
    public Pathfinder pf;
    private float _refreshingTimer;


    public struct TargetData
    {
        public float DistanceToTarget;
        public float TargetToGoalDistance { get; set; }
        public Vector2 VectorToGoalPost { get; set; }
        public Vector2 TargetVelocity { get; set; }
        public float TargetNormalVelocityComponent { get; set; }
        public float TargetToOpponentGoalDistance { get; internal set; }
    }
    public float MaximumDistanceToGoal => MapManager.Instance.MaxBallDistance;

    public Dictionary<Ball, TargetData> TargetDatas;

    private void Awake()
    {
        _playerRb = GetComponent<PlayerRB>();
        TargetDatas = new Dictionary<Ball, TargetData>();
    }

    private void Start()
    {
        pf = GetComponent<AIControlledEntity>().pathfinder;
        CollectData();
    }

    private void Update()
    {
        _refreshingTimer += Time.deltaTime;
        if (_refreshingTimer >= RefreshRate)
        {
            CollectData();
            _refreshingTimer = 0.0f;
        }



    }

    public void CollectData()
    {
        UpdateData();
    }

    private TargetData CollectSingleDataForTarget(GameObject target)
    {

        var data = new TargetData();

        if (target != null)
        {
            // var defending = distance(ball, own_goalpost) + ( vector_from_ball_to_own_goalpost * ball_velocity )
            GameObject goalPost = MapManager.Instance.GetGoalPostFromTeam(_playerRb.Team);
            GameObject opponent = MapManager.Instance.GetOpposingGoalPostFromTeam(_playerRb.Team);
            data.TargetToGoalDistance = Vector2.Distance(target.transform.position, goalPost.transform.position);
            data.TargetToOpponentGoalDistance = Vector2.Distance(target.transform.position, opponent.transform.position);
            data.DistanceToTarget = Vector2.Distance(target.transform.position, this.transform.position);
            data.VectorToGoalPost = goalPost.transform.position - transform.position;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            data.TargetVelocity = target.GetComponent<Rigidbody2D>().velocity;
            data.TargetNormalVelocityComponent = Vector2.Dot(data.VectorToGoalPost.normalized, data.TargetVelocity.normalized);

        }
        else
        {
            data.TargetToGoalDistance = float.NegativeInfinity;
            data.TargetVelocity = Vector2.negativeInfinity;
            data.TargetNormalVelocityComponent = float.NegativeInfinity;


        }
        return data;
    }

    public void UpdateData()
    {
        foreach (Ball ball in Ball.AllBalls)
        {
            if (!TargetDatas.ContainsKey(ball))
            {
                TargetDatas[ball] = new TargetData();
            }
            TargetDatas[ball] = CollectSingleDataForTarget(ball.gameObject);

        }
    }

}
