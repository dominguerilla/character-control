using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerAction : MonoBehaviour
{
    protected PlayerControls controls;
    protected bool isRegistered = false;
    public virtual void Register(PlayerControls controls)
    {
        this.controls = controls;
        this.isRegistered = true;
    }

    public virtual void Unregister()
    {
        this.controls = null;
        this.isRegistered = false;
    }
}


