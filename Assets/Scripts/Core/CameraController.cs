using UnityEngine;

namespace ChaosCritters.Core
{
    public class CameraController : MonoBehaviour
    {
        public float panSpeed = 20f;
        public float scrollSpeed = 20f;
        public Vector2 panLimit = new Vector2(50, 50);

        void Update()
        {
            Vector3 pos = transform.position;

        void Update()
        {
            Vector3 pos = transform.position;

            // Standard 2D Panning (X and Y)
            if (Input.GetKey("w") || Input.GetKey(KeyCode.UpArrow))
            {
                pos.y += panSpeed * Time.deltaTime; 
            }
            if (Input.GetKey("s") || Input.GetKey(KeyCode.DownArrow))
            {
                pos.y -= panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("d") || Input.GetKey(KeyCode.RightArrow))
            {
                pos.x += panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("a") || Input.GetKey(KeyCode.LeftArrow))
            {
                pos.x -= panSpeed * Time.deltaTime;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Camera.main.orthographicSize -= scroll * scrollSpeed * 100f * Time.deltaTime;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 2f, 20f);

            pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);
            pos.y = Mathf.Clamp(pos.y, -panLimit.y, panLimit.y);

            transform.position = pos;
        }
    }
}
