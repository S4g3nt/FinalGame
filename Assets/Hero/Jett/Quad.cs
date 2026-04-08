using UnityEngine;

public class ParallaxQuad : MonoBehaviour
{
    public float parallaxSpeed = 0.05f;
    private Material mat;
    private Transform camTransform;

    void Start()
    {
        camTransform = Camera.main.transform;
        mat = GetComponent<Renderer>().material;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;
        transform.localScale = new Vector3(camWidth, camHeight, 1f);
    }

    void LateUpdate()
    {
        float offsetX = camTransform.position.x * parallaxSpeed;
        mat.mainTextureOffset = new Vector2(offsetX, 0f);
    }
}