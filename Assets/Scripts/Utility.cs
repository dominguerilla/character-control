using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility : MonoBehaviour
{
    public void Quit() {
        if (Application.isEditor)
        {
            Debug.Log("Quit() has been called. Ignoring.");
        }
        Application.Quit();
    }
}
