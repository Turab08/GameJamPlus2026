using UnityEngine;
using DG.Tweening;

public class DoorSlider : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.Find("Button Pushable").GetComponent<PushTrigger>().canDoorSlideOpen)
        {
            Vector3 targetPosition = new Vector3(-1f, 0f, 0f); // Target position to slide to
            float slideDuration = 1f; 
            transform.DOMove(targetPosition, slideDuration);

            
        }
        
    }
}
