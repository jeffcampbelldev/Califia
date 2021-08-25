using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomDoors : MonoBehaviour
{
    public Animator animator;
    private bool opened;

    void OnTriggerEnter(Collider other)
	{
		animator.SetTrigger("open");
    }

    void OnTriggerExit(Collider other)
    {
		animator.SetTrigger("close");
    }
}
