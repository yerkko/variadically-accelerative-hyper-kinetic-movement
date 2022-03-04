using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingGoalPostWarningSign : SpriteScroller
{


    public float DistanceThreshold;

    public float DistanceBonus;

    public AnimationCurve DistanceCurve;



    //TODO: Can be refactored to be on trigger enter !

    public float MinDistance = 18.0f;

    public float OnGoalBonus;



    override protected void Update()
    {



        float minDist = Mathf.Infinity;

        foreach (var ball in Ball.AllBalls)
        {
            var offsetVector = ball.transform.position - this.transform.position;
            var dir = transform.InverseTransformDirection(offsetVector);
            var dist = Mathf.Abs(dir.x);
            if (dist <= minDist) minDist = dist;
        }


        float bonus = 0;
        if (minDist <= DistanceThreshold && minDist >= 0)
        {
            var t = 1 - ((minDist - MinDistance) / DistanceThreshold);
            bonus = DistanceBonus * DistanceCurve.Evaluate(t);
            bonus += minDist <= MinDistance ? OnGoalBonus : 0.0f;

        }

        offsetVector.x += (ScrollX) * Time.deltaTime;
        offsetVector.y += (bonus + ScrollY) * Time.deltaTime;
        _renderer.material.mainTextureOffset = offsetVector;

    }


}
