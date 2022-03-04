using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


namespace FSM
{

    public enum Actions
    {
        Defense = 0, Offense = 1, Attack = 2

    }


    //TODO: Rework this whole thing, just use functions instead of objects for the brains 
    public class FiniteStateMachine : MonoBehaviour
    {

        private Dictionary<Type, BaseState> _availableStates;

        public BaseState CurrentState { get; private set; }
        public event Action<BaseState> OnStateChanged;


        [Tooltip("Amount of updates until it takes decision")]
        public int AccumulationPeriod;

        internal void NotifyChange(string type1)
        {
            // throw new NotImplementedException();

            switch (type1)
            {
                case "DEFENDING":
                    ett.ChangeTarget(MapManager.Instance.GetGoalPostFromTeam(ett.playerRB.Team));
                    break;
                case "ATTACKING":
                    ett.ChangeTarget(this.Target);
                    break;
                default:
                    ett.ChangeTarget(this.Target);
                    break;
            }


        }


        private int _currentAccumulation;

        private string State;
        private string TargetName;

        public Dictionary<Type, (GameObject, int)> Votes;

        private bool RestingFromDecision;
        public float PostDecisionTime;
        [SerializeField]
        public GameObject Target;
        public AIControlledEntity ett;

        public Vector2 Offset => CurrentState.Offset;

        public void SetStates(Dictionary<Type, BaseState> states)
        {
            _availableStates = states;

        }


        private void Awake()
        {
            // VotingBrains = GetComponentsInChildren<VotingBrain>().ToList();
            // GroundTruthProvider = GetComponent<Sensor>();
            ett = GetComponent<AIControlledEntity>();
        }

        private void Start()
        {
            if (Votes == null)
            {
                Votes = new Dictionary<Type, (GameObject, int)>();
            }

        }
        // TODO: in theory it shoudln't, but in practice one always dominates the other so probably gotta vote randomly
        public Type GetNextStateFromVotes()
        {
            // PollVotesFromBrains();
            Type max = Votes.Aggregate((l, r) => l.Value.Item2 > r.Value.Item2 ? l : r).Key;
            return max;

        }
        private void PollVotesFromBrains()
        {


            ett.Vote();
        }


        void Update()
        {

            //Act based on current State
            if (CurrentState == null)
                CurrentState = _availableStates.Values.First();
            CurrentState?.Tick();


            // Decide what to do next (or rest)
            if (RestingFromDecision) return;

            // Here it's not really necessary to do it every frame

            _currentAccumulation += 1;

            // Make decision to change state
            if (_currentAccumulation >= this.AccumulationPeriod)
            {


                // Debug.Log("MAKING DECISION");
                AccumulateInformation();
                MakeDecision();
                _currentAccumulation = 0;

            }

        }
        private void MakeDecision()
        {

            // Type nextState = GetNextStateFromVotes();
            var nextState = ett.PreferredState;
            if (nextState != null && nextState != CurrentState?.GetType())
            {
                SwitchToNextState(nextState);
                RestAndCommit();

            }


        }
        private void AccumulateInformation()
        {

            PollVotesFromBrains();
        }

        private void RestAndCommit()
        {
            StartCoroutine(StopUpdating());
        }


        private IEnumerator StopUpdating()
        {
            RestingFromDecision = true;
            yield return new WaitForSeconds(PostDecisionTime);
            RestingFromDecision = false;
        }

        // TODO: fix target not existing from time to time
        private void SwitchToNextState(Type nextState)
        {
            CurrentState = _availableStates[nextState];
            Target = ett.Target;
            TargetName = Target?.name;
            OnStateChanged?.Invoke(CurrentState);
            Votes.Keys.ToList().ForEach(x => Votes[x] = (null, 0)); // Reset Votes
            CurrentState.OnEnterState();
        }


    }
}