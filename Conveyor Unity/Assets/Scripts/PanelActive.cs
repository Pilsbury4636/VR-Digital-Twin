using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelActive : MonoBehaviour
{
    [Tooltip("Group of buttons toggled by the Robot Panel button (Go Ready, Start Conveyor, etc.)")]
    public GameObject controlButtons;

    [Tooltip("Group of buttons toggled by the Connection Panel button (PC Connect, Lab Connect, etc.)")]
    public GameObject connectButtons;

    [Tooltip("Whether each group starts visible.")]
    public bool controlButtonsVisibleOnStart = true;
    public bool connectButtonsVisibleOnStart = true;

    void Start()
    {
        if (controlButtons != null)
            controlButtons.SetActive(controlButtonsVisibleOnStart);

        if (connectButtons != null)
            connectButtons.SetActive(connectButtonsVisibleOnStart);
    }

    
    public void ToggleControlButtons()
    {
        if (controlButtons != null)
            controlButtons.SetActive(!controlButtons.activeSelf);
    }

   
    public void ToggleConnectButtons()
    {
        if (connectButtons != null)
            connectButtons.SetActive(!connectButtons.activeSelf);
    }
}
