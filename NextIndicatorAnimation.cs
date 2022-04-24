using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextIndicatorAnimation : MonoBehaviour
{
    private Image _sR;
    private bool _started = false;

    private Vector2 initialPosition, finalPosition;

    private float timeAnimationEnd;
    private float animationStartTime;
    private float timeOfAnimation;
    private bool animating = true;

    private bool directionDown;
    
    
    private void Start()
    {
        directionDown = true;
        _sR = GetComponent<Image>();
        _started = true;
        initialPosition = transform.position;
        finalPosition = new Vector2(initialPosition.x,initialPosition.y-10);
        
    }

    // Update is called once per frame
    void Update()
    {
        animationStartTime += Time.deltaTime;
        if (directionDown)
        {
            transform.position = Vector3.Lerp(transform.position,finalPosition, animationStartTime/5f);
            if (transform.position.y - finalPosition.y < .002f)
            {
                directionDown = false;
                animationStartTime = 0;
            }
        }
        else
        {
            animationStartTime += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, initialPosition, animationStartTime/5f);
            if (initialPosition.y -transform.position.y  < .002f)
            {
                directionDown = true;
                animationStartTime = 0;
            }
        }
        
        /*var curTime = Time.time;
        if(animating)
        {
            timeOfAnimation += Time.deltaTime;
            if (curTime - animationStartTime < .41f)
        {
            transform.position = Vector2.Lerp(transform.position, finalPosition, timeOfAnimation / 2.5f);
            Debug.Log($"Position at {transform.position.y}. Time of animation is {timeOfAnimation}");
        }
        else if (curTime - animationStartTime <= .82f)
        {
            transform.position = Vector2.Lerp(transform.position, initialPosition, (timeOfAnimation - .41f) / 2.5f);
            
        }
        else
        {
            /*animating = false;
            timeAnimationEnd = curTime;#1#
            animationStartTime = curTime;
            timeOfAnimation = 0f;

        }
            return;
        }
        
        if (!animating && curTime - timeAnimationEnd > .75f)
        {
            animating = true;
            animationStartTime = curTime;
            timeOfAnimation = 0f;
            return;
        }*/

       

    }

    //since we set things inactive and whatnot in our scripts, it may happen in the middle of an animation
    private void OnEnable()
    {
        if (_started)
        {
            transform.position = initialPosition;
        }
    }
}
