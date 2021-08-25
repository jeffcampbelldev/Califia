using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlDisabler : MonoBehaviour
{
    [SerializeField]private InputControl _control;
    private bool old;

    [SerializeField] private UnityEvent onEnabled;
    [SerializeField] private UnityEvent onDisabled;

    private void Start()
    {
        old = _control.isEnabled;
    }

    private void FixedUpdate()
    {
        if (old != _control.isEnabled)
        {
            old = _control.isEnabled;
            if (_control.isEnabled)
            {
                onEnabled?.Invoke();
            }
            else
            {
                onDisabled?.Invoke();
            }
        }
    }
}
