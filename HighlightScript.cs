using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HighlightScript : MonoBehaviour
{
    private float timeAlive;

    private float timeSinceAnimation;
    private Image _sR;
    private Color transparent, initialColor;
    private bool isTransparent;
    private bool _started = false;
    private void Start()
    {
        timeAlive =timeSinceAnimation= 0f;
        
        _sR = GetComponent<Image>();
        isTransparent = false;
        initialColor = _sR.color;
        transparent = new Color(initialColor.r,initialColor.g,initialColor.b,0);
        _started = true;

    }

    // Update is called once per frame
    void Update()
    {
        //using this instead of Time.time since I don't know
        timeAlive += Time.deltaTime;

        //if the time that's passed is at least .75 seconds
        if (timeAlive - timeSinceAnimation >= .75f)
        {
            _sR.color = isTransparent ? initialColor : transparent;
            isTransparent = !isTransparent;
            timeSinceAnimation = timeAlive;
        }
    }

    private void OnEnable()
    {
        if(_started)
            _sR.color = initialColor;
    }
}
