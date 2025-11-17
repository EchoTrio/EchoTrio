using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class SpriteSwitcher : MonoBehaviour {
    [SerializeField] private Sprite[] sprites = new Sprite[0];
    [SerializeField] private int index = 0;

    public void CycleSprite() {
        index = (index + 1) % sprites.Length;
        GetComponent<UnityEngine.UI.Image>().sprite = sprites[index];
    }

    public void SetSprite(int index) {
        this.index = index;
        GetComponent<UnityEngine.UI.Image>().sprite = sprites[index];
    }

    private void OnValidate() {
        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        if (image != null && sprites[0] != null) {
            image.sprite = sprites[0];
        } else {
            image.sprite = null;
        }
    }
}