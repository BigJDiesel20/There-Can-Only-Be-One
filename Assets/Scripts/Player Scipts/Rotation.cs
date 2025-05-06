using System;
using Unity.VisualScripting;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    public float radius = 1f;
    public float distance;
    public Transform sphereTransform;

    public Transform center;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }

    //private void OnDrawGizmosSelected()
    //{
    //    if (sphereTransform != null)
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawSphere(sphereTransform.position, radius);
    //        Gizmos.DrawLine(new Vector3(0, transform.position.y, 0), transform.position);
    //        Gizmos.color = Color.blue;
    //        //Gizmos.DrawSphere(new Vector3(Mathf.Atan(transform.position.z)), transform.position.y, Mathf.Acos(transform.position.x / distance)), radius);
    //        Gizmos.DrawLine(new Vector3(0, transform.position.y, 0), new Vector3(10, transform.position.y, 0));
    //        distance = Vector3.Distance(transform.position, new Vector3(0, transform.position.y, 0));
    //    }

    //}

    
}
