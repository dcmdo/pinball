﻿using UnityEngine;
using System.Collections;

namespace Cool.Dcm.Game.PinBall
{
	public class CameraController : MonoBehaviour
	{
        public Camera mainCamera;
        public Transform LaunchView;
        public Transform HitView;
        
        [SerializeField] private float moveSpeed = 5f;
        private Transform targetView;
        private bool isMoving = false;

        private static CameraController _instance;

    // 公共静态属性用于访问实例
        public static CameraController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CameraController>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<CameraController>();
                        singletonObject.name = typeof(CentralControl).ToString() + " (Singleton)";
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return _instance;
            }
        }

        void Start()
        {
            if(mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // 初始设置为LaunchView
            SetView(LaunchView);
        }

        void Update()
        {
            if (isMoving)
            {
                MoveToTarget();
            }
        }

        public void SwitchToLaunchView()
        {
            SetView(LaunchView);
        }

        public void SwitchToHitView()
        {
            SetView(HitView);
        }

        private void SetView(Transform newTarget)
        {
            targetView = newTarget;
            isMoving = true;
        }

        private void MoveToTarget()
        {
            // 平滑移动
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetView.position,
                moveSpeed * Time.deltaTime);

            mainCamera.transform.rotation = Quaternion.Lerp(
                mainCamera.transform.rotation,
                targetView.rotation,
                moveSpeed * Time.deltaTime);

            // 检查是否到达目标位置
            if (Vector3.Distance(mainCamera.transform.position, targetView.position) < 0.1f)
            {
                isMoving = false;
            }
        }
	}
}
