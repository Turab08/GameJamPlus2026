using UnityEngine;

public class ArmTarget : MonoBehaviour
{
    private void Update()
    {
        transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
