using UnityEngine;

public class VehicleVisuals : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;


    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sprites.Length > 0)
        {
            sr.sprite = sprites[Random.Range(0, sprites.Length)];
        }
    }
}