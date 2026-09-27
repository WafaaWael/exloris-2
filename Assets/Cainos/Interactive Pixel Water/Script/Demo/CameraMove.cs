using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cainos.InteractivePixelWater
{

    public class CameraMove : MonoBehaviour
    {
        public float moveSpeed = 1.0f;
        public float moveLerpSpeed = 4.0f;
        public float moveClampLerpSpeed = 4.0f;
        [Space]
        public float zoomSpeed = 1.0f;
        public float zoomLerpSpeed = 4.0f;
        public Vector2 zoomClamp = new Vector2(5.0f, 15.0f);
        [Space]
        public BoxCollider2D clampArea;

        private Camera cam;
        private Vector3 curPos;                                 //current smoothed camera position.
        private Vector3 targetPos;                              //position requested by pointer input.
        private Vector3 clampedPos;                             //target position constrained to the clamp area.
        private Vector3 lastPointerPos;
        private float curSize;
        private float targetSize;

        private void Start()
        {
            cam = GetComponent<Camera>();

            targetPos = cam.transform.position;
            curPos = targetPos;

            targetSize = cam.orthographicSize;
            curSize = targetSize;
        }

        private void Update()
        {
            //pan while the right mouse button is held
            if (Input.GetMouseButtonDown(1))
            {
                lastPointerPos = Input.mousePosition;
            }
            if (Input.GetMouseButton(1))
            {
                Vector3 delta = lastPointerPos - Input.mousePosition;
                lastPointerPos = Input.mousePosition;

                // convert the pointer delta to world units so panning is resolution-independent.
                float worldHeight = cam.orthographicSize * 2f;
                float worldWidth = worldHeight * cam.aspect;
                float moveX = (delta.x / Screen.width) * worldWidth;
                float moveY = (delta.y / Screen.height) * worldHeight;

                targetPos += new Vector3(moveX, moveY, 0) * moveSpeed;
            }

            //ease the requested position back inside the optional movement boundary
            clampedPos = targetPos;
            if (clampArea)
            {
                clampedPos.x = Mathf.Clamp(clampedPos.x, clampArea.bounds.min.x, clampArea.bounds.max.x);
                clampedPos.y = Mathf.Clamp(clampedPos.y, clampArea.bounds.min.y, clampArea.bounds.max.y);

                targetPos = Vector3.Lerp(targetPos, clampedPos, moveClampLerpSpeed * Time.deltaTime);
            }

            //smoothly move the camera toward the requested position
            curPos = Vector3.Lerp(curPos, targetPos, moveLerpSpeed * Time.deltaTime);
            transform.position = curPos;

            //adjust the orthographic size with the mouse wheel.
            float scrollInput = Input.mouseScrollDelta.y;
            if (scrollInput != 0)
            {
                targetSize -= scrollInput * zoomSpeed;
                targetSize = Mathf.Clamp(targetSize, zoomClamp.x, zoomClamp.y);
            }

            curSize = Mathf.Lerp(curSize, targetSize, Time.deltaTime * zoomLerpSpeed);
            cam.orthographicSize = curSize;
        }
    }
}
