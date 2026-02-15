using System.Collections;
using UnityEngine;

namespace SiliconHeart.Rendering
{
    /// <summary>
    /// This sample shows how to change the size of the line and update the position of its points.
    /// </summary>
    public class Sample2Setup : MonoBehaviour
    {
        public LineRenderer2D LinePrefab;
        public Camera MainCamera;

        private LineRenderer2D m_lineInstance;

        private void Start()
        {
            m_lineInstance = Instantiate(LinePrefab);
            m_lineInstance.SetPointCount(10); // Extendes the buffer to store 10 positions
            m_lineInstance.CurrentCamera = MainCamera;

            StartCoroutine(LineGrowingCoroutine());
        }

        private void Update()
        {
            // Updates each point of the line
            for (int i = 0; i < m_lineInstance.Points.Count; ++i)
            {
                m_lineInstance.Points[i] = new Vector2(i * 0.1f, Mathf.Sin(Time.time * 5.0f + i * 0.25f)); // Points field can be used to change positions but not to extend or shrink the list
            }

            m_lineInstance.ApplyPointPositionChanges(); // The Auto Apply Position Changes option is disabled in the prefab so it updates manually
        }

        private IEnumerator LineGrowingCoroutine()
        {
            while(true)
            {
                yield return new WaitForSeconds(0.1f);

                m_lineInstance.AddPoint(); // Extends the buffer by 1
            }
        }
    }
}
