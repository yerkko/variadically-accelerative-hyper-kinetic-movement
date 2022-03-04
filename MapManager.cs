using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class MapManager : MonoBehaviour
{
    // Start is called before the first frame update

    public enum GameMode
    {
        Squash, Football, Billiards
    }

    public Dictionary<int, int> Scores;

    public int NumberOfTeams = 2;

    private Camera cam;

    public TMPro.TextMeshProUGUI ScoreText;
    public Color ATeamColor, BTeamColor, NeutralTeamColor;
    public Color ATeamColorUnsaturated, BTeamColorUnsaturated, NeutralTeamColorUnsaturated;

    public Color GetUnsaturatedColor(Team t)
    {
        switch (t)
        {
            case Team.A:
                return ATeamColorUnsaturated;
            case Team.B:
                return BTeamColorUnsaturated;
            default:
                return NeutralTeamColorUnsaturated;
        }

    }


    public Color GetSaturatedColor(Team t)
    {
        switch (t)
        {
            case Team.A:
                return ATeamColor;
            case Team.B:
                return BTeamColor;
            default:
                return NeutralTeamColor;
        }

    }

    public GameObject BallPrefab;
    public Transform BallSpawnPoint;

    public static MapManager Instance { get; private set; }

    public float TerminalPlayerSpeed;
    public float TerminalPlayerSpeedWithBoost;

    public float TerminalBallSpeed;
    public float TerminalBallSpeedSuperShot;


    public float MapBallDrag;

    public float MapBallAngularDrag;

    public float MaxShakeForce;

    public Dictionary<Team, ScoreMarker> ScoreMarkers;
    public Dictionary<Team, GoalPost> GoalPosts;

    public GameObject GetGoalPostFromTeam(Team t) => GoalPosts[t].gameObject;
    public GameObject GetOpposingGoalPostFromTeam(Team t) => GoalPosts[t == Team.A ? Team.B : t].gameObject;

    public GameObject FieldMidPoint;
    // public float MaxBallDistance => Vector2.Distance(ScoreMarkers.Values.First().transform.position, FieldMidPoint.transform.position);

    public float FieldSize;
    public float MaxBallDistance => FieldSize;

    public GameMode Mode;
    public GameObject CelebrationPrefab;
    public Transform CelebrationStartingPoint;
    public Transform CelebrationFinalPoint;
    public UnityEngine.Events.UnityEvent OnGoal;

    public AudioClip GoalClip;

    private GameObject PlayerGO;

    public void ChangeColors()
    {
        var teamColoreds = FindObjectsOfType<TeamColored>();
        foreach (var tc in teamColoreds)
        {
            Debug.Log("eeee" + tc.gameObject.name);
            tc.ChangeColor(tc.Team == Team.A ? ATeamColor : BTeamColor);
        }
    }

    public void AddPlayerGO(GameObject player)
    {
        if (PlayerGO != null)
            Debug.LogWarning("PLAYER ALREADY SET");


        PlayerGO = player;

    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        cam = Camera.main;

        Scores = new Dictionary<int, int>();
        ScoreMarkers = new Dictionary<Team, ScoreMarker>();

        GoalPosts = new Dictionary<Team, GoalPost>(2);
        InitializeGoalPosts();

        for (int i = 0; i < NumberOfTeams; i++)
        {
            Scores.Add(i, 0);
        }
        BallSpawnPoint = FindObjectOfType<FieldMidpoint>().transform;

    }

    private void InitializeGoalPosts()
    {
        var goals = FindObjectsOfType<GoalPost>();
        foreach (var goalPost in goals)
        {
            GoalPosts[goalPost.OwnTeam] = goalPost;
        }
    }

    public void Goal(Team team)
    {

        Scores[(int)team] += 1;
        UpdateScore();
        JumbotronController.Instance.Celebrate();
        OnGoal?.Invoke();
        Instantiate(BallPrefab, BallSpawnPoint.position, Quaternion.identity);
    }

    private void UpdateScore()
    {

        foreach (var gp in ScoreMarkers)
        {
            gp.Value.UpdateScore(Scores[(int)gp.Key]);
        }

    }

    public void Celebrate()
    {

        var cel = Instantiate(CelebrationPrefab, CelebrationStartingPoint.position, Quaternion.identity);
        cel.GetComponent<CelebrationController>().Celebrate(CelebrationFinalPoint.position);

    }

    public void AddGoalPost(Team team, ScoreMarker marker)
    {
        ScoreMarkers.Add(team, marker);
    }


    void Start()
    {
        ChangeColors();
        UpdateScore();

        // StartCoroutine(UpdateCamera());
    }

    private IEnumerator UpdateCamera()
    {
        // Debug.Log(cam.GetComponent<Cinemachine.CinemachineVirtualCamera>());
        // cam.GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = PlayerGO.transform;
        yield return new WaitForSeconds(2.0f);
        cam.GetComponent<Cinemachine.CinemachineVirtualCamera>().LookAt = PlayerGO.transform;
    }
}
