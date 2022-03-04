using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SplashController : MonoBehaviour
{
    // Start is called before the first frame update

    private SpriteRenderer _sR;

    [SerializeField] private float FadeTime;
    [SerializeField] private float ExpandingAddition;
    void Awake()
    {
        _sR = GetComponent<SpriteRenderer>();
    }


    void OnEnable()
    {
        // lol i don't give a shit about object pooling this one... i don't have time
        _sR.DOFade(0.0f, FadeTime).SetEase(Ease.OutExpo);
        this.transform.DOScaleX(this.transform.localScale.x + ExpandingAddition, FadeTime).SetEase(Ease.OutExpo).OnComplete(() => Destroy(this.gameObject));

    }

    // Update is called once per frame
    void Update()
    {

    }
}
