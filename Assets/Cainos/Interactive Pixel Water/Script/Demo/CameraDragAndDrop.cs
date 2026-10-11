using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cainos.InteractivePixelWater
{
    public class CameraDragAndDrop : MonoBehaviour
    {
        public LayerMask draggableLayers;          //only colliders on these layers can be dragged.
        public float dampingRatio = 5.0f;          //spring settings used by the temporary drag joint.
        public float frequency = 10.0f;

        private Camera cam;
        private TargetJoint2D dragJoint;
        private Rigidbody2D draggingRb;             //rigidbody currently being dragged.
        private float originAugularDrag;            //angular drag before the object was picked up.

        private void Start()
        {
            cam = GetComponent<Camera>();
        }

        private void Update()
        {
            //begin, update, or finish a drag based on the left mouse button state.
            if (Input.GetMouseButtonDown(0))
            {
                HandleDragStart();
            }

            if (Input.GetMouseButton(0) && dragJoint != null)
            {
                HandleDragging();
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandleDragEnd();
            }
        }

        private void HandleDragStart()
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);

            //find if we clicked on a collider
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, draggableLayers);

            if (hit != null && hit.attachedRigidbody != null)
            {
                draggingRb = hit.attachedRigidbody;

                //create a temporary joint on the clicked object
                dragJoint = draggingRb.gameObject.AddComponent<TargetJoint2D>();
                dragJoint.dampingRatio = dampingRatio;
                dragJoint.frequency = frequency;

                //connect the joint to the point we clicked
                dragJoint.anchor = draggingRb.transform.InverseTransformPoint(mouseWorldPos);
                dragJoint.target = mouseWorldPos;

                //increase angular drag to keep the object from spinning while it is dragged.
                originAugularDrag = draggingRb.angularDamping;
                draggingRb.angularDamping = 100;
            }
        }

        private void HandleDragging()
        {
            //update the target position to follow the mouse
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            dragJoint.target = mouseWorldPos;
        }

        private void HandleDragEnd()
        {
            if (dragJoint != null)
            {
                Destroy(dragJoint);
                dragJoint = null;
            }

            //reset the object's angular drag after releasing it.
            if (draggingRb) draggingRb.angularDamping = originAugularDrag;
        }
    }
}
