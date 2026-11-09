using UnityEngine;

public class BackgroundFit : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Start() => Fit();
    private void OnValidate() => Fit();

    private void Fit()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        if (sprite == null)
            return;

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        Vector2 imageSize = sprite.bounds.size;

        transform.localScale = new Vector3(
            width / imageSize.x,
            height / imageSize.y,
            1f
        );

        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            transform.position.z
        );
    }
}