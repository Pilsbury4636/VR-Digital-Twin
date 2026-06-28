using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    public Button startButton;
    public GameObject beltItem;
    public GameObject itemSpawn;
    public Belt beltRef;


    private void Start()
    {

        startButton.onClick.AddListener(SetPosition);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetPosition()
    {

        beltItem = GameObject.FindWithTag("BeltItem");
        itemSpawn = GameObject.Find("ItemSpawn");
        beltItem.transform.position = itemSpawn.transform.position;
        beltRef.isOn = true;
        gameObject.SetActive(false);

    }
}
