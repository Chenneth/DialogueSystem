using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HistoryEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
   private bool _pointerIsDown;
   public DialogueManager history;
   public void OnPointerDown(PointerEventData eventData)
   {/*
      if(eventData.pointerPress)
      _pointerIsDown = true;*/
   }

   public void OnPointerUp(PointerEventData eventData)
   {
      if(!eventData.dragging)
         history.CloseHistory();
   }
   
   
}
