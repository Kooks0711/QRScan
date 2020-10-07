using BarcodeScanner;
using BarcodeScanner.Scanner;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QRCodeScanner : MonoBehaviour
{
	private IScanner scanner;

	public Text text;
	public RawImage Image;
	private float RestartTime;

	void Awake()
	{
		/*Screen.autorotateToPortrait = false;
		Screen.autorotateToPortraitUpsideDown = false;*/

		text.text = $"{WebCamTexture.devices.Length}";
	}

	void Start()
	{
		// 스캐너 생성
		scanner = new Scanner();
		scanner.Camera.Play();

		scanner.OnReady += (sender, arg) =>
		{
			// 배경 이미지
			Image.texture = scanner.Camera.Texture;
			Image.transform.localEulerAngles = scanner.Camera.GetEulerAngles();
			Image.transform.localScale = scanner.Camera.GetScale();
			Image.texture.filterMode = FilterMode.Trilinear;
			
			// 비율 조정
			var rect = Image.GetComponent<RectTransform>();
			var newHeight = rect.sizeDelta.x * scanner.Camera.Height / scanner.Camera.Width;
			rect.sizeDelta = new Vector2(rect.sizeDelta.x, newHeight);

			RestartTime = Time.realtimeSinceStartup;
		};

		QRImageSetting();

	}

	private void QRImageSetting()
	{
		/*float ratio = (float)Screen.width / (float)Screen.height;
		fit.aspectRatio = ratio;*/

		/*float scaleY = cameraTexture.videoVerticallyMirrored ? -1f : 1f;
		Image.rectTransform.localScale = new Vector3(1f, scaleY * 1f, 1);*/

		/*int orient = -cameraTexture.videoRotationAngle;
		Image.rectTransform.localEulerAngles = new Vector3(0, 0, orient);*/

		var rect = Image.GetComponent<RectTransform>();
		rect.sizeDelta = new Vector2(Screen.height, Screen.width);
	}

	private void StartScanner()
	{
		scanner.Scan((type, value) =>
		{
			scanner.Stop();
			if (text.text.Length > 250)
			{
				text.text = "";
			}
			text.text += "Found: " + type + " / " + value + "\n";
			RestartTime += Time.realtimeSinceStartup + 1f;

		});
	}

	void Update()
	{
		if (scanner != null)
		{
			scanner.Update();
		}

		// 재시작 검사
		if (RestartTime != 0 && RestartTime < Time.realtimeSinceStartup)
		{
			StartScanner();
			RestartTime = 0;
		}
	}

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
			Image = null;
			scanner.Destroy();
			scanner = null;
		}
	}

}