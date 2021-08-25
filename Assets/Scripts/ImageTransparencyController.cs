using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// General Image Transparency control. 
/// This class will change transparency of every child image of the gameobject.
/// </summary>
public class ImageTransparencyController : MonoBehaviour
{
    [SerializeField][Range(0F, 1F)] public float _transparency = 1F;
    [SerializeField] private ImageType _type = ImageType.Default;
    [SerializeField] private Object[] _possibleImages;


    private void Start()
    {
        switch (_type)
        {
            case ImageType.Default:
                {
                    _possibleImages = GetComponentsInChildren<Image>();
                    break;
                }
                
            case ImageType.Raw:
                {
                    _possibleImages = GetComponentsInChildren<RawImage>();
                    break;
                }                   
            default:
                {
                    break;
                }
        }
        
    }

    private void FixedUpdate()
    {
        TransparentDefault();
        TransparentRaw();
    }

    public void TransparentDefault()
    {
        if (_possibleImages.GetType() != typeof(Image[])) return;
        foreach (Image img in _possibleImages)
        {
            if (img.color.a != _transparency)
            {
                Debug.Log(img.color);
                Color color = new Color(img.color.r, img.color.g, img.color.b, _transparency);
                img.color = color;
            }
        }
    }

    public void TransparentRaw()
    {
        if (_possibleImages.GetType() != typeof(RawImage[])) return;
        foreach (RawImage img in _possibleImages)
        {
            if (img.color.a != _transparency)
            {
                Debug.Log(img.color);
                Color color = new Color(img.color.r, img.color.g, img.color.b, _transparency);
                img.color = color;
            }
        }
    }
}

public enum ImageType {Raw, Default}
