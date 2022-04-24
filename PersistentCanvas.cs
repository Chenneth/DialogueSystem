using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentCanvas : MonoBehaviour
{
    private static PersistentCanvas _instance;

    public static PersistentCanvas Instance
    {
        get => _instance;
        set => _instance = value;
    }
    private Canvas _canvas;
         public Canvas Canvas => _canvas;

    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        } else {
            _instance = this;       
            _canvas = GetComponent<Canvas>();      
        }
    }

    
   
}
