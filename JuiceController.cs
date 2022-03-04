using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using DG.Tweening;
public class JuiceController : MonoBehaviour
{


    public static JuiceController Instance { get; private set; }
    private PostProcessVolume _postProcessVolume;

    private float BasechromaticAberrationValue;

    [SerializeField]
    private float ChromaticAberrationBonus;
    public float SuperShotJuiceTime = 0.3f;

    [SerializeField]
    AudioSource SuperShotSound;

    [SerializeField]
    private ParticleSystem BallWallCollisionParticleSystem;
    
    [SerializeField]
    private ParticleSystem EntityChargingParticleSystem;

    [SerializeField]
    private SpriteRenderer WallCollisionSignifier;

    Vector3 defaultOffset = new Vector3(0,0,0);
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

        _postProcessVolume = FindObjectOfType<PostProcessVolume>();
        BasechromaticAberrationValue = _postProcessVolume.profile.GetSetting<ChromaticAberration>().intensity;

    }

    public void BallWallCollision(Vector3 position, Vector3 normal, Color c){
        // var ps = Instantiate(BallWallCollisionParticleSystem, position, Quaternion.Euler(normal)) as ParticleSystem;
        // ps.Play();
        // var main = ps.main;
        // main.startColor = c;
        var ws = Instantiate(WallCollisionSignifier, position, Quaternion.Euler(normal)) as SpriteRenderer;
        ws.color = c;


    }




    public ParticleSystem ChargeUp(GameObject chargingEntity, Color c){




        var ps = Instantiate(EntityChargingParticleSystem, chargingEntity.transform.position, Quaternion.identity, chargingEntity.transform) as ParticleSystem;
        ps.Play();
        var main = ps.main;
        main.startColor = c;

        return ps;

    }



    public void SuperShot()
    {
        ChromaticAberration chromatic = _postProcessVolume.profile.GetSetting<ChromaticAberration>();
        Vignette vig = _postProcessVolume.profile.GetSetting<Vignette>();
        SuperShotSound.Play();
        DOTween.To(

            () => chromatic.intensity,
            (float x) => { chromatic.intensity.Override(x); },
            BasechromaticAberrationValue + ChromaticAberrationBonus,
            SuperShotJuiceTime).SetEase(Ease.OutCubic).OnComplete(


                () => DOTween.To(

            () => chromatic.intensity,
            (float x) => { chromatic.intensity.Override(x); },
            BasechromaticAberrationValue,
            SuperShotJuiceTime).SetEase(Ease.InExpo)



            );







    }


}
