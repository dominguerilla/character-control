using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class MenuActions : PlayerAction
{

    public UnityEvent onPause = new UnityEvent(), onUnpause = new UnityEvent();

    [SerializeField]
    GameObject pauseUI;

    bool isPaused;
    bool canPause = true;

    public override void Register(PlayerControls controls)
    {
        if (!isRegistered)
        {
            base.Register(controls);
            controls.Gameplay.Menu1.performed += TogglePause;
            controls.UI.Menu1.performed += TogglePause;
        }
    }

    public override void Unregister()
    {
        if (isRegistered)
        {
            base.Unregister();

            // Sometimes, the controls object is destroyed before Unregister() is called.
            if(controls != null)
            {
                controls.Gameplay.Menu1.performed -= TogglePause;
                controls.UI.Menu1.performed -= TogglePause;
            }
        }
    }

    public void TogglePause(InputAction.CallbackContext ctx)
    {
        if (!isPaused && !canPause) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;

        pauseUI.SetActive(isPaused);

        if (isPaused)
        {
            controls.Gameplay.Disable();
            controls.UI.Enable();
            onPause.Invoke();
        }
        else
        {
            controls.UI.Disable();
            controls.Gameplay.Enable();
            onUnpause.Invoke();
        }

    }

    // Dumb hack I have to do in order to hook up Unity UI buttons to this function
    public void TogglePause()
    {
        TogglePause(new InputAction.CallbackContext());
    }

    /// <summary>
    /// Enables/disables pausing. 
    /// 
    /// </summary>
    /// <param name="value">true to allow pausing, false otherwise.</param>
    public void SetCanPause(bool value)
    {
        canPause = value;
    }
}
