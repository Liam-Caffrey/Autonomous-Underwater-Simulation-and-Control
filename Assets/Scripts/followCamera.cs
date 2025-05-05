
using UnityEngine;



public class followCamera : MonoBehaviour
{


    private float smoothSpeed = 0.9f;
    public Transform target;

    private Vector3 offset = new Vector3(0, 4, -4);

    private void Start()
    {
        transform.position = target.position + offset;
    }

    private void FixedUpdate()
    {
        Quaternion targetRotation = target.rotation;

        Vector3 newPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, newPos, smoothSpeed * Time.deltaTime);


        transform.LookAt(target);
    }


}
