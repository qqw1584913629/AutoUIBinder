using System.Collections;
using System.Collections.Generic;
using AutoUIBinder;
using UnityEngine;

public partial class LobbyPanel : AutoUIBinderBase
{
    //load panel prefab
    public GameObject LoadPanelPrefab;

    void Start()
    {
        Button_Button.onClick.AddListener(OnButton_ButtonOnClick);
    }
    private void OnButton_ButtonOnClick()
    {
        Instantiate(LoadPanelPrefab, transform.parent);
        gameObject.SetActive(false);
    }
}
