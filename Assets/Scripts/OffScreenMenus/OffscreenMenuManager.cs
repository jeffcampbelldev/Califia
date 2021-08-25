using System;
using UnityEngine;

public class OffscreenMenuManager : MonoBehaviour
{
    [SerializeField]
    private IVmenu iVmenu;
    [SerializeField]
    private CannulaMenu cannulaMenu;
    private static OffscreenMenuManager _instance;
    public static OffscreenMenuManager Instance{
        get{
            if(_instance == null){
                _instance = FindObjectOfType<OffscreenMenuManager>();
            }
            return _instance;
        }
    }
    
    public void OpenIVmenu(int module){
        iVmenu.OpenMenu(module);
    }

    public void OpenIVmenu(int module, Action<RoomConfig.IVBag> callback)
    {
        iVmenu.OpenMenuFromAvatar(module, callback);
    }
}
