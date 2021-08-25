using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Mouse motion reader for any given UI canvas element
/// </summary>
public class MotionReader : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [SerializeField] private float _countdownTime = 2F;
    private float _timer;

    [SerializeField] private UnityEvent onActive = null;
    [SerializeField] private UnityEvent onInactive = null;

    private Vector3 _position = new Vector3(0,0,0);
    private Vector3 _oldPosition = new Vector3(0, 0, 0);
    private bool _userInteracting = false;


    private void Update()
    {
         if (_position != null) _oldPosition = _position;
        _position = Input.mousePosition;
        DetectTouch();
    }

    /// <summary>
    /// Checks if mouse is currently moving.
    /// </summary>
    /// <returns>True if mouse moves and False if not.</returns>
    private bool IsMouseMoving()
    {
        Debug.Log("Is moving? " + (_position == _oldPosition), gameObject);
        return (_position != _oldPosition);
        
    }

    /// <summary>
    /// Waits given time in float and checks if mouse has moved in that time.
    /// </summary>
    /// <returns>An IEnumerator.</returns>
    IEnumerator WaitAndCheckRoutine()
    {
        yield return new WaitForSeconds(_countdownTime);

        if (!IsMouseMoving())
        {
            //Stop everything and invoke inactivity :)
            Debug.Log("Inactive closing tab", gameObject);
            onInactive?.Invoke();
            StopAllCoroutines();
        }
    }

    /// <summary>
    /// Reads mouse movement every x seconds to determine 
    /// if user has stopped moving mouse while interacting.
    /// </summary>
    /// <returns>An IEnumerator.</returns>
    IEnumerator MouseMovementCheckRoutine()
    {
        while (_userInteracting)
        {
            
            Debug.Log("Reading movement", gameObject);

            yield return new WaitUntil(() => !IsMouseMoving());
            StartCoroutine(WaitAndCheckRoutine());
        }
    }

    /// <summary>
    /// Event callback used to detect when user is interacting with tab.
    /// Sets interaction bool to true and starts tracking routine.
    /// </summary>
    /// <param name="eventData">The event data.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("Interacting", gameObject);
        _userInteracting = true;
        //StartCoroutine(MouseMovementCheckRoutine());
        onActive?.Invoke();
    }

    /// <summary>
    /// Event callback used to detect when user is not interacting with tab.
    /// Sets interaction bool to false and stops tracking routine.
    /// </summary>
    /// <param name="eventData">The event data.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        _userInteracting = false;
        //StopCoroutine(MouseMovementCheckRoutine());
        StopAllCoroutines();
        onInactive?.Invoke();
    }
    
    /// <summary>
    /// Detects users touch on canvas and invokes events accordingly every fixed frame.
    /// </summary>
    public void DetectTouch()
    {
        //Guard clause against no touch input.
        if (Input.touches.Length == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
           if (EventSystem.current.currentSelectedGameObject == gameObject)
            {
                onActive?.Invoke();
            }
        }

        if(touch.phase == TouchPhase.Ended)
        {
            onInactive?.Invoke();
        }
    }
}
