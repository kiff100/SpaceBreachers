using System.Collections;
using UnityEngine;

namespace SiliconHeart.Rendering
{
    /// <summary>
    /// This sample shows how to define a custom CameraResolver, which allows defining a method for lines to obtain the current camera.
    /// </summary>
    public class Sample3Setup : MonoBehaviour
    {
        public LineRenderer2D LinePrefab;
        public Camera CameraPrefab;
        
        /// <summary>
        /// This camera resolver stores 2 cameras provided by the application and allows switching between them at any moment.
        /// </summary>
        private class CustomCameraResolver : LineRenderer2D.CameraResolver
        {
            private bool m_cameraSwitch;
            private Camera m_camera1;
            private Camera m_camera2;

            public CustomCameraResolver(Camera camera1, Camera camera2)
            {
                m_camera1 = camera1;
                m_camera1.enabled = false;

                m_camera2 = camera2;
                m_camera2.enabled = true;
            }

            public void SwitchCamera()
            {
                m_cameraSwitch = !m_cameraSwitch;

                m_camera1.enabled = m_cameraSwitch;
                m_camera2.enabled = !m_cameraSwitch;

                // This must be called to let the lines that use the resolver know when to get the current camera
                NotifyCameraChanged();
            }

            public override Camera GetCamera()
            {
                // Lines will internally call this method and use the parameters of the camera to adjust their actual thickness
                return m_cameraSwitch ? m_camera1 : m_camera2;
            }
        }

        private void Start()
        {
            // In this sample the cameras are not defined in the scene, they are created in runtime
            Camera camera1 = Instantiate(CameraPrefab);
            camera1.name = "camera 1";
            Camera camera2 = Instantiate(CameraPrefab);
            camera2.name = "camera 2";
            camera2.orthographicSize *= 0.25f;

            CustomCameraResolver customCameraResolver = new CustomCameraResolver(camera1, camera2);

            // Storing the resolver as global will make all existing lines use it
            LineRenderer2D.GlobalCameraResolver = customCameraResolver;

            // Creates 2 lines that automatically use the global resolver
            Instantiate(LinePrefab, new Vector3(-1, 0, 0), Quaternion.identity);
            Instantiate(LinePrefab, new Vector3(1, 0, 0), Quaternion.identity);

            // Adds a resolver to a specific line which will ignore the global resolver
            Instantiate(LinePrefab, new Vector3(1, -2, 0), Quaternion.identity).SetCameraResolver(customCameraResolver);

            // Switches between the 2 cameras every N seconds
            StartCoroutine(SwitchCameraCoroutine(customCameraResolver));
        }

        private IEnumerator SwitchCameraCoroutine(CustomCameraResolver customCameraResolver)
        {
            while(true)
            {
                yield return new WaitForSeconds(1.5f);

                customCameraResolver.SwitchCamera();
            }
        }
    }
}
