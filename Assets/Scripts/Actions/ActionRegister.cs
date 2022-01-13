using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionRegister : MonoBehaviour
{
    PlayerControls controls;

    [SerializeField]
    List<PlayerAction> actions = new List<PlayerAction>();

    private void Awake()
    {
        controls = new PlayerControls();
    }
    private void OnEnable()
    {
        RegisterActions();
    }

    void RegisterActions()
    {
        Debug.Assert(controls != null, "controls is null!");
        foreach (PlayerAction action in actions)
        {
            action.Register(controls);
        }
    }

    private void OnDisable()
    {
        foreach(PlayerAction action in actions)
        {
            action.Unregister();
        }
    }
}
