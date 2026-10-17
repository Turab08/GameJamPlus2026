using DG.Tweening;
using UnityEngine;

public class PatrolingAI : MonoBehaviour
{
    [SerializeField] private Transform startPosition;
    [SerializeField] private Transform endPosition;

    [SerializeField] private float speed = 5f;
    [SerializeField] private float moveDuration = 2;

    private void Start()
    {
        transform.DOMove(endPosition.position, moveDuration).SetLoops(-1, LoopType.Yoyo);
    }

}
