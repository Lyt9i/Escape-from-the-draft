using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class BackgroundFit : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Tooltip("Минимальный размер видимой области в мировых единицах. Ноль оставляет масштаб камеры без изменений.")]
    [SerializeField] private Vector2 referenceView;
    private float lastAspect;

    private void Start()
    {
        if (Application.isPlaying || referenceView.y > 0f)
            Fit();
    }

    private void Update()
    {
        if (referenceView.y > 0f && targetCamera != null && targetCamera.aspect != lastAspect)
            Fit();
    }

    private void OnValidate() => Fit();

    private void Fit()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        if (sprite == null)
            return;

        lastAspect = targetCamera.aspect;
        if (lastAspect <= 0f)
            return;

        if (referenceView.x > 0f && referenceView.y > 0f)
            targetCamera.orthographicSize = Mathf.Max(referenceView.y, referenceView.x / lastAspect) * 0.5f;

        float height = targetCamera.orthographicSize * 2f;
        float width = height * lastAspect;
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
