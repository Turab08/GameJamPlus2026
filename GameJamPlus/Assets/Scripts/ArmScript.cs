using System;
using DG.Tweening;
using UnityEngine;

public class ArmScript : MonoBehaviour
{
    [SerializeField] private Transform upperArm;
    [SerializeField] private Transform lowerArm;

    [SerializeField] private Transform target;

    [SerializeField] private float length1 = 2f;
    [SerializeField] private float length2 = 1.5f;

    private bool flipElbow = false;

    private void Update()
    {
        if (upperArm != null && lowerArm != null && target != null)
        {
            Vector2 toTarget = (target.position - transform.position);

            float distance = toTarget.magnitude;
            Vector2 direction = toTarget.normalized;

            float maxLength = (length1 + length2) * 0.999f;
            float minLength = Mathf.Abs(length1 - length2) * 1.001f;

            if (distance > maxLength)
            {
                toTarget = direction * maxLength;
                distance = maxLength;
            }
            else if (distance < minLength)
            {
                toTarget = direction * minLength;
                distance = minLength;
            }

            float cosElbow = (length1 * length1 + length2 * length2 - distance * distance) / (2f * length1 * length2);
            float angleElbowRad = Mathf.Acos(Mathf.Clamp(cosElbow, -1f, 1f));
            float angleElbowDeg = angleElbowRad * Mathf.Rad2Deg;


            float cosShoulder = (length1 * length1 + distance * distance - length2 * length2) / (2f * length1 * distance);
            float angleShoulderRad = Mathf.Acos(Mathf.Clamp(cosShoulder, -1f, 1f));
            float angleShoulderDeg = angleShoulderRad * Mathf.Rad2Deg;

            float baseAngleDeg = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;

            float flipModifier = flipElbow ? -1f : 1f;

            float finalShoulderAngle = baseAngleDeg - (angleShoulderDeg * flipModifier);
            float finalElbowAngle = 180f - angleElbowDeg;

            upperArm.rotation = Quaternion.Euler(0, 0, finalShoulderAngle);
            lowerArm.localRotation = Quaternion.Euler(0, 0, finalElbowAngle * flipModifier);
        }
    }
}
