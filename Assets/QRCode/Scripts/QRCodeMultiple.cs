using BarcodeScanner;
using BarcodeScanner.Scanner;
using BarcodeScanner.Webcam;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Wizcorp.Utils.Logger;
using ZXing;

public class QRCodeMultiple : MonoBehaviour
{
    private IScanner scanner;
    private WebCamTexture camtexture;
    private Color32[] pixels = null;
    private int width;
    private int height;
    private string resultstr;

    public Text text;
    public RawImage image;
    private Thread scanThread;

    void Start()
    {
        scanner = new Scanner();
        scanner.Camera.Play();

        // 배경 이미지
        scanner.OnReady += (sender, org) =>
        {
            image.texture = scanner.Camera.Texture;
            image.transform.localEulerAngles = scanner.Camera.GetEulerAngles();
            image.transform.localScale = scanner.Camera.GetScale();
            image.texture.filterMode = FilterMode.Trilinear;

            // 비율 조정
            var rect = image.GetComponent<RectTransform>();
            // var newHeight = rect.sizeDelta.x * scanner.Camera.Height / scanner.Camera.Width;
            // rect.sizeDelta = new Vector2(rect.sizeDelta.x, newHeight);
            rect.sizeDelta = new Vector2(Screen.height, Screen.width);

        };

        camtexture = (WebCamTexture)scanner.Camera.Texture;
        height = camtexture.height;
        width = camtexture.width;

        scanThread = new Thread(ThreadQR);
        scanThread.Start();

        Debug.Log($"wid :: {Screen.width} , hei :: {Screen.height}");

    }

    private ResultPoint[] point;
    private Result[] results;

    public void ThreadQR()
    {
        var cordReader = new BarcodeReader();

        #region multi
        while (true)
        {
            Debug.Log("Thread is Run");
            try
            {
                // decode the current frame
                results = cordReader.DecodeMultiple(pixels, width, height);

                if (results != null)
                {
                    Debug.Log($"leng :: {results.Length}");
                    foreach(var item in results)
                    {
                        point = item.ResultPoints;
                        Debug.Log($"qr :: {item.Text}\n" +
                            $"x :: {point[0].X} , y :: {point[0].Y}");

                        resultstr = $"qr :: {item.Text}\n" +
                            $"x :: {point[0].X} , y :: {point[0].Y}";
                    }
                }
                // pixels == null;
                Thread.Sleep(200);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            Thread.Sleep(200);
        }
        #endregion
        #region single
        /*while (true)
        {
            Debug.Log("Thread is Run");
            try
            {
                // decode the current frame
                var result = cordReader.Decode(pixels, width, height);
                if (result != null)
                {
                    Debug.Log(result.Text);
                    resultstr = result.Text;
                    point = result.ResultPoints;
                    Debug.Log($"x :: {point[0].X} , y :: {point[0].Y}");
                }
                // pixels == null;
                Thread.Sleep(200);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            Thread.Sleep(200);
        }*/
        #endregion


    }

    private void Update()
    {
        if (scanner != null)
            scanner.Update();

        // if (pixels == null)
        pixels = camtexture.GetPixels32();

        // text.text = $"x :: {point[0].X} , y :: {point[1].Y}";
        text.text = resultstr;

    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            scanThread.Abort();
            scanner.Destroy();
            scanner = null;
            image = null;
        }
    }
}
