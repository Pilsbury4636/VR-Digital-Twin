using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Belt : MonoBehaviour
{
    private static int _beltID = 0;

    public Belt beltInSequence;
    public BeltItem beltItem;
    public bool isSpaceTaken;

    private BeltManager _beltManager;

    public bool isOn = false;

    public Vector3 GetItemPosition()
    {
        var padding = 0.05f;

        var position = gameObject.transform.position;
        return new Vector3(position.x, position.y + padding, position.z);

    }

    private IEnumerator StartBeltMove()
    {
        isSpaceTaken = true;

        if(beltItem.item != null && beltInSequence != null && beltInSequence.isSpaceTaken == false)
        {
            Vector3 toPosition = beltInSequence.GetItemPosition();

            beltInSequence.isSpaceTaken = true;

            var step = _beltManager.speed * Time.deltaTime;

            while (beltItem.item.transform.position != toPosition)
            {
                beltItem.item.transform.position =
                    Vector3.MoveTowards(beltItem.transform.position, toPosition, step);
               Debug.Log("moving");

                yield return null;
            }

            isSpaceTaken = false;
            beltInSequence.beltItem = beltItem;
            beltItem = null;



        }
    }

    private Belt FindNextBelt()
    {
        
        Transform currentBeltTransform = gameObject.transform;
    
        RaycastHit hit;

        var forward = gameObject.transform.forward;

        Ray ray = new Ray(currentBeltTransform.position, forward);
        Debug.DrawRay(transform.position, transform.forward, Color.green);

        if (Physics.Raycast(ray, out hit, 10f))
        {
            Debug.Log("active");
            
            Belt belt = hit.collider.GetComponent<Belt>();

            if (belt != null)
                return belt;

        }

        return null;
    }
    // Start is called before the first frame update
    void Start()
    {
        _beltManager = FindObjectOfType<BeltManager>();
        beltInSequence = null;
        beltInSequence = FindNextBelt();
        gameObject.name = $"Belt: {_beltID++}";
        


    }

    // Update is called once per frame
    void Update()
    {
        if (beltInSequence == null)
            beltInSequence = FindNextBelt();

        if (beltItem != null && beltItem.item != null && isOn)
            StartCoroutine(StartBeltMove());
    }
}
