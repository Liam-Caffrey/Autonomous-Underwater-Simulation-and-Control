using UnityEngine;
using UnityEngine.UI;

public class SphereHoverHandler : MonoBehaviour
{
    private Texture2D image;
    private Image uiImageDisplay;

    public void Initialize(Texture2D assignedImage, Image displayImage)
    {
        image = assignedImage;
        uiImageDisplay = displayImage;
        uiImageDisplay.gameObject.SetActive(false);
    }

    void OnMouseEnter()
    {
        Debug.Log("ENTER");
        if (uiImageDisplay != null && image != null)
        {
            Sprite screenshotSprite = Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2(0.5f, 0.5f));
            uiImageDisplay.sprite = screenshotSprite;
            RectTransform rectTransform = uiImageDisplay.rectTransform;

            
            float baseWidth = 500f; 
            float aspectRatio = (float)image.width / image.height;
            float baseHeight = baseWidth / aspectRatio;
            rectTransform.sizeDelta = new Vector2(baseWidth * 2, baseHeight * 2);

            uiImageDisplay.gameObject.SetActive(true);
        }
    }


       void OnMouseExit()
    {
        if (uiImageDisplay != null)
        {
            uiImageDisplay.gameObject.SetActive(false);
        }
    }
}
