using System.Collections;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class GaiserScript : MonoBehaviour
{
    [SerializeField] private GameObject bubbleObject;
    [SerializeField] private Transform spawnPosition;

    [SerializeField] private float coolDownMax = 0.25f;
    private float coolDown = 0.25f;

    [SerializeField] private float xOffset = 0.2f;

    private void Start()
    {
        coolDown = coolDownMax;
    }

    private void Update()
    {
        float randXPos = Random.Range(spawnPosition.position.x - xOffset, spawnPosition.position.x + xOffset);
        coolDown -= Time.deltaTime;

        // AudioManager.Instance.PlaySFX(AudioManager.Instance.GaiserSound);

        if (coolDown < 0) {
            Spawn(randXPos);
        }
    }

    private void Spawn(float randXPos)
    {
        Instantiate(bubbleObject, new Vector2(randXPos, spawnPosition.position.y), Quaternion.identity);
        coolDown = coolDownMax; 
    }
}
