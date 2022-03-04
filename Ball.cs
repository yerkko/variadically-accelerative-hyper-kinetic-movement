using System;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Random = UnityEngine.Random;

public class Ball : MonoBehaviour
{
    // Start is called before the first frame update

    public Rigidbody2D rb;

    public float Shake;

    public float forceMagnitude;

    public AudioSource audioSource;

    public Team CurrentTeam;

    protected SpriteRenderer spriteRenderer;

    public bool FullInertia = false;

    protected float InertiaSpeed;

    public bool InSuperShot = false;

    public int MaxSuperShotBounces;

    protected int RemainingSuperShotBounces;

    internal void Goal()
    {
        AllBalls.Remove(this);
        Destroy(this.gameObject);

    }

    public static HashSet<Ball> AllBalls = new HashSet<Ball>();

    private TrailController _trailController;

    [SerializeField]
    private DeformationController? _deformationController;


    private BallSoundController _bsC;

    public bool SetInertiaMode(bool active, float speed)
    {

        if (active == FullInertia) return false;

        FullInertia = active;
        InertiaSpeed = speed;

        return true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        _trailController = GetComponentInChildren<TrailController>();
        _deformationController = GetComponentInChildren<DeformationController>();
        _bsC = GetComponentInChildren<BallSoundController>();
        AllBalls.Add(this);
    }

    protected void OnDisable()
    {
        AllBalls.Remove(this);
    }



    public void SuperShot()
    {
        this.InSuperShot = true;
        RemainingSuperShotBounces = MaxSuperShotBounces;
        _trailController?.ActivateTrail(spriteRenderer.color);
        if (_deformationController != null)
        {
            _deformationController.Accelerate();
        }

    }

    public void SuperShot(Vector2 v)
    {

        var rot = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90;
        transform.rotation = Quaternion.Euler(0f, 0f, rot);

        SuperShot();
    }

    public void ChangeTeam(Team newTeam)
    {
        CurrentTeam = newTeam;

        if (newTeam == Team.A)
            spriteRenderer.color = MapManager.Instance.ATeamColor;
        else if (newTeam == Team.B)
            spriteRenderer.color = MapManager.Instance.BTeamColor;

    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        _bsC.PlayBallHittingSound(rb.velocity.magnitude / this.CurrentMaxVelMag);

        var contact = col.GetContact(0);
        var vel = Vector2.Reflect(rb.velocity, contact.normal);
        // Debug.DrawRay(this.transform.position, 10 * vel, Color.blue, 4.0f);
        if (InSuperShot)
        {
            RemainingSuperShotBounces--;
        }

        if (RemainingSuperShotBounces <= 0)
        {
            InSuperShot = false;
            _trailController?.DisableTrail();
        }


        if (col.gameObject.CompareTag("Wall"))
        {
            WallJuice(col);
            ScreenShakeController.Instance.ShakeScreen(-1 * vel);
        }
    }

    private void WallJuice(Collision2D col)
    {
        var dir = (Vector3)col.contacts[0].normal;
        dir.z = Mathf.Atan2(dir.y, dir.x) * ((byte)Mathf.Rad2Deg) - 90;
        dir.x = 0;
        dir.y = 0;
        JuiceController.Instance.BallWallCollision(col.contacts[0].point - 2.0f * col.contacts[0].normal, dir, this.spriteRenderer.color);
    }

    void Update()
    {
        // Debug.DrawRay(this.transform.position, 10 * rb.velocity, Color.red);

        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var dir = mousePos - this.transform.position;


        if (Input.GetMouseButtonDown(0)) rb.AddForce(dir * forceMagnitude);


    }

    private float CurrentMaxVelMag => InSuperShot ? MapManager.Instance.TerminalBallSpeedSuperShot : MapManager.Instance.TerminalBallSpeed;


    void FixedUpdate()
    {
        if (rb.velocity.magnitude >= CurrentMaxVelMag)
        {
            rb.velocity = CurrentMaxVelMag * rb.velocity.normalized;
        }
        else if (FullInertia)
        {
            rb.velocity = rb.velocity.normalized * InertiaSpeed;
        }

    }
}
