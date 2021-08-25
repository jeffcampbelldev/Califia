using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Inventory/Item", order=1)]
public class Item : ScriptableObject
{
	public string _name;
	public Texture2D _icon;
}
