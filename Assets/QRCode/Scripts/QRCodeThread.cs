using BarcodeScanner;
using BarcodeScanner.Scanner;
using BarcodeScanner.Webcam;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Wizcorp.Utils.Logger;
using ZXing;

public class QRCodeThread : MonoBehaviour
{
    private IScanner scanner;
    private WebCamTexture camtexture;
    public Color32[] pixels = null;
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

    }


    public void ThreadQR()
    {
        var cordReader = new BarcodeReader();
       
        while (true)
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
                }
                // pixels == null;
                Thread.Sleep(200);
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
            }
            Thread.Sleep(200);
        }

        #region Test
        /*ScanQR();
        Thread.Sleep(300);

        while (true)
        {

            // Wait
            // Thread.Sleep(Mathf.FloorToInt(250));
            try
            {
                // pixels = cam.GetPixels(pixels);
                // pixels = scanner.Camera.GetPixels(pixels);
                // pixels = QRPixels(camtexture);
            }
            catch
            {
                Debug.Log("pixels is null");
                continue;
            }

            // Process
            // Log.Debug(this + " SimpleScanner -> Scan ... " + cam.Width + " / " + cam.Height);
            try
            {
                // var result = parser.Decode(pixels, cam.Width, cam.Height);
                var result = barcode.Decode(pixels, Icam.Width, Icam.Height);

                if (result == null)
                    continue;

                // Sleep a little bit and set the signal to get the next frame
                Thread.Sleep(250);
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }*/
        #endregion
    }

    private void Update()
    {
        if (scanner != null)
            scanner.Update();

        // if (pixels == null)
        pixels = camtexture.GetPixels32();
        
        text.text = resultstr;

        Debug.Log($"1 :: {pixels.Length} , 2 :: {camtexture.width} , 3 :: {camtexture.height}");
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
