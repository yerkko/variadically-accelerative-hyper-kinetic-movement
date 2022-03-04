using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public enum GuidanceType
{
    Pursuit,
    Lead
}


public class Pathfinder : MonoBehaviour
{

    private const float MINIMUM_GUIDE_SPEED = 1.0f;


    private float _activateTime;

    private Collider2D _collider;


    public float Acceleration = 0.0f;

    public float DropDelay = 0.0f;

    public Vector2 EjectVelocity = Vector2.zero;

    public GuidanceType GuidanceType = GuidanceType.Pursuit;

    public GameObject ObjectToFollow;

    private Vector3 GuidedRotation;

    public float InitialSpeed;

    private float launchTime;

    private Vector2 launchVelocity = Vector2.zero;

    private bool pathFinderActive;
    private float pathfinderSpeed;

    [Tooltip("How long the pathfinder will accelerate. After this, the pathfinder maintains a constant speed.")]
    public float AccelerationLifetime = 3.0f;

    private Rigidbody2D rb;

    public float seekerCone = 45.0f;

    public float seekerRange = 40.0f;


    public GameObject target;

    private Vector3 targetPosLastFrame;
    private bool targetTracking = true;

    public float ThresholdRadius = 250f;

    [Tooltip("After this time, the pathfinder will self-destruct. Timer starts on launch")]
    public float timeToLive = Mathf.Infinity;

    [Tooltip("How many degrees per second the pathfinder can turn.")]
    public float turnRate = 45.0f;

    public bool PathfinderLaunched { get; private set; }

    public bool MotorActive { get; private set; }


    public bool OutOfBounds { get; set; }

    public Vector2 Offset;

    public void ChangeOffset(Vector2 offset)
    {
        Offset = offset;
    }


    private void Awake()
    {

        rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        ObjectToFollow = transform.GetChild(0).gameObject;
    }


    private void Start()
    {
        if (!PathfinderLaunched)
            rb.isKinematic = true;
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.R)) Launch(target, Vector3.zero);
    }

    private void FixedUpdate()
    {
        if (pathFinderActive && target != null) PathFind();

        RunAgent();
    }

    private float TimeSince(float since) => Time.time - since;


    public void Launch(GameObject newTarget, Vector3 inheritedVelocity)
    {
        if (PathfinderLaunched) return;
        PathfinderLaunched = true;
        launchTime = Time.time;
        transform.parent = null;
        target = newTarget;
        launchVelocity = inheritedVelocity;
        rb.isKinematic = false;

        if (DropDelay > 0.0f)
        {
            rb.velocity = inheritedVelocity + transform.TransformDirection(EjectVelocity);
        }
        else
        {
            ActivatePathFinder();
        }
    }




    private void ActivatePathFinder()
    {
        rb.velocity = Vector2.zero;
        pathFinderActive = true;

        if (AccelerationLifetime <= 0.0f)
            MotorActive = true;

        _activateTime = Time.time;
        pathfinderSpeed = InitialSpeed;

        if (target != null)
            targetPosLastFrame = target.transform.position;
    }

    void OnTriggerExit2D(Collider2D other)
    {

        if (other.CompareTag("PlayArea"))
        {
            OutOfBounds = true;
        }

    }

    private void RunAgent()
    {
        if (!PathfinderLaunched) return;
        // Don't start moving under own power until drop delay has passed (if applicable).
        if (!pathFinderActive && DropDelay > 0.0f && TimeSince(launchTime) > DropDelay)
            ActivatePathFinder();
        if (pathFinderActive)
        {
            if (AccelerationLifetime > 0.0f && TimeSince(_activateTime) > AccelerationLifetime)
                MotorActive = false;
            else
                MotorActive = true;
            if (MotorActive)
                pathfinderSpeed += Acceleration * Time.fixedDeltaTime;

            if (targetTracking)
            {
                transform.right = Vector3.MoveTowards(transform.right, GuidedRotation, turnRate * Time.fixedDeltaTime);
            }

            rb.velocity = transform.right * pathfinderSpeed;
        }

        if (TimeSince(launchTime) > timeToLive) DestroyPathFinder();
    }


    private void DestroyPathFinder()
    {
        this.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere((target.transform.position - 100 * (Vector3)Offset), 10.0f);
    }

    private void PathFind()
    {
        // Get a vector to the target, use it to find angle to target for seeker cone check.
        Vector3 position = transform.position;
        Vector3 relPos = target.transform.position - position;
        Vector2 up = Vector2.up;

        float angleToTarget = Mathf.Abs(Vector2.Angle(transform.right.normalized, relPos.normalized));
        float dist = Vector2.Distance(target.transform.position, position);

        // When the target gets out of line of sight of the seeker's FOV or out of range, it can no longer track.
        // if (angleToTarget > seekerCone || dist > seekerRange)
        //     targetTracking = false;

        // Only turn the missile if the target is still within the seeker's limits.
        if (targetTracking)
        {
            // Pursuit guidance
            if (GuidanceType == GuidanceType.Pursuit)
            {
                relPos = (target.transform.position - 100 * (Vector3)Offset) - transform.position;

                //guidedRotation = Quaternion.LookRotation(relPos,transform.right);
                GuidedRotation = relPos.normalized;

            }

            // Lead guidance
            else
            {
                // Get where target will be in one second.
                Vector3 position1 = target.transform.position - 10 * (Vector3)Offset;
                Vector3 targetVelocity = position1 - targetPosLastFrame;
                targetVelocity /= Time.fixedDeltaTime;

                //=====================================================

                // Figure out time to impact based on distance.                
                //float dist = Mathf.Max(Vector3.Distance(target.position, transform.position), missileSpeed);
                float predictedSpeed = Mathf.Min(InitialSpeed + Acceleration * AccelerationLifetime,
                                                 pathfinderSpeed + Acceleration * TimeSince(_activateTime));
                float timeToImpact = dist / Mathf.Max(predictedSpeed, MINIMUM_GUIDE_SPEED);

                // Create lead position based on target velocity and time to impact.                
                Vector3 leadPos = position1 + targetVelocity * timeToImpact;
                Vector3 leadVec = leadPos - transform.position;

                //print(leadVec.magnitude.ToString());

                //=====================================================

                // It's very easy for the lead position to be outside of the seeker head. To prevent
                // this, only allow the target direction to be 90% of the seeker head's limit.
                relPos = Vector3.RotateTowards(relPos.normalized, leadVec.normalized,
                                               seekerCone * Mathf.Deg2Rad * 0.9f, 0.0f);
                //guidedRotation = Quaternion.LookRotation(relPos,transform.right);
                GuidedRotation = relPos.normalized;

                Debug.DrawRay(target.transform.position, targetVelocity * timeToImpact, Color.red);
                Debug.DrawRay(target.transform.position, targetVelocity * timeToImpact, Color.red);
                Debug.DrawRay(transform.position, leadVec, Color.red);

                targetPosLastFrame = position1;
            }
        }
    }





}