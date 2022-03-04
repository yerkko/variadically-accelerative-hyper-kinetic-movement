using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Start is called before the first frame update

    public static TimeManager Instance { get; private set; }

    public float SlowdownFactor = 0.05f;
    public float SlowdownLength = 2.0f;

    private bool Recovering;

    void Update()
    {

        if (Recovering)
        {
            Time.timeScale += (2.0f / SlowdownLength) * Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
            if (Time.timeScale == 1.0f)
            {
                Time.fixedDeltaTime = Time.deltaTime;
                Recovering = false;
            }
        }
    }


    void Awake()
    {

        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }


        }
    }

    public void RecoverNormalMotion()
    {
    }

    public void DoSlowMotion()
    {

        StartCoroutine(SlowTime());

    }

    private IEnumerator SlowTime()
    {
        Recovering = false;
        Time.timeScale = SlowdownFactor;
        yield return new WaitForSecondsRealtime(SlowdownLength);
        Recovering = true;
    }


}
