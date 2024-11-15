using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using radar.Yolov8;

public class sentisInfer : MonoBehaviour
{
    public ModelAsset modelAsset;
    public RenderTexture inputTexture;
    Yolov8Inferencer yolov8Inferencer;
    Texture2D texture;
    void Start()
    {
        yolov8Inferencer = new Yolov8Inferencer(modelAsset);
        texture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        RenderTexture.active = inputTexture;
        texture.ReadPixels(new Rect(0, 0, inputTexture.width, inputTexture.height), 0, 0);
        texture.Apply();
        yolov8Inferencer.inference(texture, 0.1f, 0.3f, true);
    }
}


