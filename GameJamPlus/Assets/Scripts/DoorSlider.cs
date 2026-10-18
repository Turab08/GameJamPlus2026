using UnityEngine;
using DG.Tweening;

public class DoorSlider : MonoBehaviour
{
    void Update()
    {
        if (GameObject.Find("Button Pushable").GetComponent<PushTrigger>().canDoorSlideOpen)
        {
            Vector3 targetPosition = new Vector3(-1f, 0f, 0f);
            float slideDuration = 1f; 
            transform.DOMove(targetPosition, slideDuration);
        }
        
    }
}
