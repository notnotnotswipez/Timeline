using System;
using BoneLib;
using HarmonyLib;
using Il2CppSLZ.Bonelab;
using MelonLoader;
using Timeline.Settings.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline.CameraRelated
{
    [RegisterTypeInIl2Cpp]
    public class CameraController : MonoBehaviour
    {
        public Camera camera;
        public Vector3 desiredPosition;
        public Quaternion desiredRotation;
        public float speed = 5;
        public float sensitivity = 10;
        public float lerpSpeed = 10;
        
        public float fov = 60;
        public float minFov = 10;
        public float maxFov = 120;
        
        public TimelineHolder holder;

        public float roll = 0;
        public bool cursorLocked = true;

        public bool controlling = true;

        public bool validated = false;
        public bool retainControl = false;
        
        public CameraController(IntPtr intPtr) : base(intPtr)
        {
        }

        public void ApplyCameraStateCapture(CameraStateCapture toApply)
        {
            transform.position = new Vector3(toApply.positionX, toApply.positionY, toApply.positionZ);
            transform.rotation = Quaternion.Euler(toApply.rotationX, toApply.rotationY, toApply.rotationZ);
            camera.fieldOfView = toApply.fov;
        }

        public void Validate() {
            if (validated) {
                return;
            }

            camera.gameObject.transform.localPosition = Vector3.zero;
            camera.gameObject.transform.localRotation = Quaternion.identity;
            camera.enabled = true;
            camera.gameObject.SetActive(true);

            GameObject.Destroy(gameObject.GetComponentInChildren<SmoothFollower>(true));

            // Init at player head rather than 0, 0, 0.
            gameObject.transform.position = Player.RigManager.physicsRig.m_head.transform.position;
            desiredPosition = gameObject.transform.position;

            validated = true;
        }
        
        public CameraStateCapture GetCameraStateCapture()
        {
            CameraStateCapture toReturn = new CameraStateCapture();
            toReturn.positionX = transform.position.x;
            toReturn.positionY = transform.position.y;
            toReturn.positionZ = transform.position.z;
            toReturn.rotationX = transform.rotation.eulerAngles.x;
            toReturn.rotationY = transform.rotation.eulerAngles.y;
            toReturn.rotationZ = transform.rotation.eulerAngles.z;
            toReturn.fov = camera.fieldOfView;
            return toReturn;
        }


        private void Awake()
        {
            
        }

        public void LockMouse(bool isLock)
        {
            if (isLock)
            {
                cursorLocked = true;
                holder._slider.interactable = false;
                Cursor.lockState = CursorLockMode.Locked;
                controlling = true;
            }
            else
            {
                cursorLocked = false;
                Cursor.lockState = CursorLockMode.None;
                if (!holder.playing)
                {
                    holder._slider.interactable = true;
                }

                controlling = false;
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                LockMouse(!cursorLocked);
            }

            if (!controlling || (holder.playing && !retainControl))
            {
                desiredPosition = transform.position;
                desiredRotation = transform.rotation;
                return;
            }

            if (Input.GetKey(KeyCode.W))
            {
                desiredPosition += transform.forward * (TimelineMainClass.lastDeltaTime * speed);
            }
            if (Input.GetKey(KeyCode.S))
            {
                desiredPosition -= transform.forward * (TimelineMainClass.lastDeltaTime * speed);
            }
            if (Input.GetKey(KeyCode.A))
            {
                desiredPosition -= transform.right * (TimelineMainClass.lastDeltaTime * speed);
            }
            if (Input.GetKey(KeyCode.D))
            {
                desiredPosition += transform.right * (TimelineMainClass.lastDeltaTime * speed);
            }
            if (Input.GetKey(KeyCode.Q))
            {
                desiredPosition -= transform.up * (TimelineMainClass.lastDeltaTime * speed);
            }
            if (Input.GetKey(KeyCode.E))
            {
                desiredPosition += transform.up * (TimelineMainClass.lastDeltaTime * speed);
            }

            Quaternion mouseRotation = Quaternion.Euler(-Input.GetAxis("Mouse Y") * sensitivity, Input.GetAxis("Mouse X") * sensitivity, 0);

            desiredRotation *= mouseRotation;

            float x = desiredRotation.eulerAngles.x;
            if (x > 180)
            {
                x -= 360;
            }
            x = Mathf.Clamp(x, -85, 85);
            desiredRotation.eulerAngles = new Vector3(x, desiredRotation.eulerAngles.y, roll);


            // Speed
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed += Input.mouseScrollDelta.y;
                if (speed < 0)
                {
                    speed = 0;
                }
            }


            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    fov = 60;
                }

                if (Input.GetKeyDown(KeyCode.T)) {
                    roll = 0;
                    holder.GetSettingPanelInstance<CameraPanel>().rollSetting.SetValue(0);
                }

                fov += Input.mouseScrollDelta.y;
                fov = Mathf.Clamp(fov, minFov, maxFov);
                camera.fieldOfView = fov;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, TimelineMainClass.lastDeltaTime * lerpSpeed);

            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, TimelineMainClass.lastDeltaTime * lerpSpeed);
        }
    }
}