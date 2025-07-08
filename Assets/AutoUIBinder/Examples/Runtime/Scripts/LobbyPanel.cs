using System.Collections;
using System.Collections.Generic;
using AutoUIBinder;
using UnityEngine;

public partial class LobbyPanel : AutoUIBinderBase
{
    //load panel prefab
    public GameObject LoadPanelPrefab;
    [UIEvent("Button_Button", "onClick")]
    private void OnButton_ButtononClick()
    {
        Instantiate(LoadPanelPrefab, transform.parent);
        gameObject.SetActive(false);
    }
}
