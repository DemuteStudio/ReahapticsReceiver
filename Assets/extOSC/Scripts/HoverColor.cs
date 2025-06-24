using UnityEngine;
using UnityEngine.UI;
public class HoverColor : MonoBehaviour
{
    public Button button;
    public Color wantedColor;
    private Color originalColor;
    private ColorBlock cb;
    void Start()
    {
        cb = button.colors;
        originalColor = cb.selectedColor;
    }

    public void OnHover()
    {
        cb.normalColor = wantedColor;
        button.colors = cb;
    }

    public void OnEndHover()
    {
        cb.normalColor = originalColor;
        button.colors = cb;
    }
}
