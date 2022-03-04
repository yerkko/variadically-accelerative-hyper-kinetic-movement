using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public enum Team
{
    Z = -1, A = 0, B = 1
}


public class PlayerRB : MonoBehaviour
{
    // Start is called before the first frame update


    private float vertical, horizontal;

    public float MovementSpeed;

    private Vector3 movementVector = Vector3.zero;

    private Rigidbody2D rb;

    private SpriteRenderer spriteRenderer;

    public Team Team;

    public bool JumpButton;

    public float BoostBonus;

    public float Intertia;


    public bool PlayerControlled = false;


    private PlayerSoundController _playerSoundController;

    #region Jump

    [Space()]
    [Header("Dash parameters")]

    public float JumpBonus;
    public bool JumpActive;
    public float JumpRemainingTime;
    public float JumpCooldownTime;
    public float JumpTotalTime;

    [SerializeField]
    private bool JumpCooldownActive;
    public float JUMP_COLLISION_FACTOR;

    [SerializeField]
    private float CurrentMaxSpeed;

    [SerializeField]
    public float AccelerationTime;




    public float ChargingTime;

    [SerializeField]
    private float CurrentChargingTime;
    [SerializeField]
    private bool IsCharging;


    #endregion Jump



    #region Juice
    private ParticleSystem? ChargingParticleSystem;
    private DeformationController DeformationController;

    #endregion

    public float TargetTweenTime;

    public float OriginalTweenTime;

    private Tween acceleratingTween;
    private bool nonZeroInput;


    [Space()]
    [Header("Collision parameters")]

    public float PlayerCollisionFactorSlowShot;
    public float BallCollisionFactorSlowShot;

    public float BallCollisionFactor;
    public float PlayerCollisionFactor;
    public float MinimumVelocityThreshold;


    private Color originalColor;

    void ChangeToMaxSpeed()
    {
        CurrentMaxSpeed = MapManager.Instance.TerminalPlayerSpeedWithBoost;

    }
    void ChangeToNormalSpeed()
    {
        DOTween.To(() => this.CurrentMaxSpeed, (x) => { this.CurrentMaxSpeed = x; }, MapManager.Instance.TerminalPlayerSpeed, this.AccelerationTime).SetEase(Ease.OutQuad).SetId(0);

    }

    void Awake()
    {

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _playerSoundController = GetComponentInChildren<PlayerSoundController>();
        DeformationController = GetComponentInChildren<DeformationController>();

        if (PlayerControlled)
            MapManager.Instance.AddPlayerGO(this.gameObject);

        var tC = GetComponentInChildren<TeamColored>();
        if (tC != null)
        {
            tC.Team = this.Team;
        }

    }


    void Start()
    {

        JumpButton = false;
        ChangeToNormalSpeed();
        originalColor = spriteRenderer.color;

    }


    void OnCollisionEnter2D(Collision2D col)
    {

        var other = col.gameObject;

        if (other.gameObject.CompareTag("Ball"))
        {
            HitBall(col, other);
        }

        if (JumpActive)
        {

            if (!JumpCooldownActive) { StopJump(); }
        }

    }

    private void HitBall(Collision2D col, GameObject other)
    {
        var ball = other.GetComponent<Ball>();
        ball.ChangeTeam(Team);
        var vel = col.GetContact(0).normalImpulse * rb.velocity;
        var ballCollisionFactor = JumpActive ? (BallCollisionFactor + JUMP_COLLISION_FACTOR) : BallCollisionFactor;
        var ballVel = ball.rb.velocity;

        if (!nonZeroInput && !JumpActive)
        {
            ball.rb.AddForce(ballVel * BallCollisionFactorSlowShot, ForceMode2D.Impulse);
            rb.AddForce(-1 * ballVel * PlayerCollisionFactorSlowShot, ForceMode2D.Impulse);
        }
        else
        {
            ball.rb.AddForce(vel * ballCollisionFactor, ForceMode2D.Impulse);
            rb.AddForce(-1 * vel * PlayerCollisionFactor, ForceMode2D.Impulse);
        }

        if (JumpActive)
        {
            TimeManager.Instance.DoSlowMotion();
            ball.SuperShot(rb.velocity);
            JuiceController.Instance.SuperShot();
        }
    }

    public void UpdateInputs(float hor, float vert, bool boost)
    {

        horizontal = hor;
        vertical = vert;
        JumpButton = boost;

    }


    void Update()
    {

        // Debug.DrawRay(this.transform.position, 10 * this.transform.up, Color.red);
        rb.inertia = Intertia;

        if (PlayerControlled && !JumpActive)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            JumpButton = Input.GetButtonDown("Fire3");


        }

        movementVector.x = horizontal;
        movementVector.y = vertical;
        movementVector.Normalize();

        var dir = movementVector;
        var rot = Mathf.Atan2(movementVector.y, movementVector.x) * Mathf.Rad2Deg;
        dir.x = 0.0f;
        dir.y = 0.0f;
        dir.z = rot - 90;


        nonZeroInput = (Mathf.Abs(movementVector.x) >= 0.01 || Mathf.Abs(movementVector.y) >= 0.01);

        if (!JumpActive && nonZeroInput)
        {
            spriteRenderer.transform.rotation = Quaternion.Euler(dir);
            rb.SetRotation(dir.z);
        }

        else if (!JumpActive && !nonZeroInput)
        {
            spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, rb.rotation);

        }

        if (JumpButton)
        {

            if (!(JumpActive || JumpCooldownActive))
            {
                Boost();
            }
        }

        if (JumpActive)
        {
            _playerSoundController.StopPostChargingSound();

            JumpRemainingTime -= Time.deltaTime;
            if (JumpRemainingTime <= 0.0f)
            {
                StopJump();
            };

        }


    }

    private void StopChargingEffects()
    {
        _playerSoundController.StopChargingSound();
        _playerSoundController.PlayPostChargingSound();
        // acceleratingTween?.Kill(); // Charging tween
    }


    private void ChargeUpAnimation()
    {

        acceleratingTween = spriteRenderer.DOBlendableColor(Color.white, OriginalTweenTime).SetLoops(-1).SetId(-1).OnKill(() => spriteRenderer.color = originalColor);
        DOTween.To(() => acceleratingTween.timeScale, (x) => acceleratingTween.timeScale = x, TargetTweenTime, ChargingTime);
        // tw.timeScale = TweenTime;
    }

    private void BoostAnimation()
    {
        spriteRenderer.color = MapManager.Instance.GetUnsaturatedColor(this.Team);
        spriteRenderer.DOBlendableColor(MapManager.Instance.GetSaturatedColor(this.Team), JumpCooldownTime).SetEase(Ease.InExpo);

    }

    private void Boost()
    {
        JumpRemainingTime = JumpTotalTime;
        JumpActive = true;
        ChangeToMaxSpeed();
        _playerSoundController.PlayBoostingSound();
        DeformationController?.Accelerate();

    }

    private void StopJump()
    {
        JumpRemainingTime = 0.0f;
        JumpActive = false;
        // DOTween.Kill(0);
        DOTween.Complete(0, true);

        ChangeToNormalSpeed();
        if (!JumpCooldownActive) StartCoroutine(JumpCooldown());
    }

    IEnumerator JumpCooldown()
    {
        BoostAnimation();
        JumpCooldownActive = true;
        yield return new WaitForSeconds(JumpCooldownTime);
        JumpCooldownActive = false;

    }


    void FixedUpdate()
    {

        if (!IsCharging)
        {
            rb.AddForce(movementVector * Time.fixedDeltaTime * (MovementSpeed), ForceMode2D.Impulse);
            rb.AddForce(movementVector * BoostBonus, ForceMode2D.Impulse); // Don't touch lmao
        }

        if (JumpActive)
        {
            rb.AddForce(this.transform.up * JumpBonus, ForceMode2D.Impulse);

        }

        if (rb.velocity.magnitude >= CurrentMaxSpeed)
        {
            rb.velocity = CurrentMaxSpeed * rb.velocity.normalized;

        }



    }

}