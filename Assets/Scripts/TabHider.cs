using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabHider : MonoBehaviour
{
    [SerializeField] private RectTransform _TabParent = null;
    [SerializeField] private Animator[] _Tabs;

    private void Awake()
    {
        _Tabs = _TabParent.GetComponentsInChildren<Animator>();
       
        
    }

    /// <summary>
    /// Hides all tabs.
    /// </summary>
    public void HideAllTabs()
    {
        foreach (Animator tab in _Tabs) tab.SetTrigger("Hide");
    }

    /// <summary>
    /// Shows all tabs.
    /// </summary>
    public void ShowAllTabs()
    {
        foreach (Animator tab in _Tabs) tab.SetTrigger("Show");
    }
}
