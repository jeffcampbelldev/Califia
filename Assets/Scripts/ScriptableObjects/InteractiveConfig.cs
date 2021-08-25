using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Interactive Configuration", menuName ="Create/Configuration/Interactive")]
public class InteractiveConfig : ScriptableObject
{
    public float moveSensitivity;
    public float lookSensitivity;
	public float joystickSensitivity;
	public int lockedNavs;
}
