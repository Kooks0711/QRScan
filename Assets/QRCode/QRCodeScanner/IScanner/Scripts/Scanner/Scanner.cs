using BarcodeScanner.Parser;
using BarcodeScanner.Webcam;
using System;
using UnityEngine;
using Wizcorp.Utils.Logger;

#if !UNITY_WEBGL
using System.Threading;
#endif

namespace BarcodeScanner.Scanner
{
	public class Scanner : IScanner
	{
		//
		public event EventHandler OnReady;
		public event EventHandler StatusChanged;

		//
		public IWebcam Camera { get; private set; }
		public IParser Parser { get; private set; }
		public ScannerSettings Settings { get; private set; }

		//
		private ScannerStatus status;
		public ScannerStatus Status {
			get { return status; }
			private set
			{
				status = value;
				if (StatusChanged != null)
				{
					StatusChanged.Invoke(this, EventArgs.Empty);
				}
			}
		}

		// Store information about last image / results (use the update loop to access camera and callback)
		public Color32[] pixels = null;
		private Action<string, string> Callback;
		private ParserResult Result;

		//
		private bool parserPixelAvailable = false;
		private float mainThreadLastDecode = 0;
		private int webcamFrameDelayed = 0;
		private int webcamLastChecksum = -1;
		private bool decodeInterrupted = true;

		public Scanner() : this(null, null, null) { }
		public Scanner(ScannerSettings settings) : this(settings, null, null) {}
		public Scanner(IParser parser, IWebcam webcam) : this(null, parser, webcam) {}

		public Scanner(ScannerSettings settings, IParser parser, IWebcam webcam)
		{
			// Check Device Authorization
			if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
			{
				throw new Exception("This Webcam Library can't work without the webcam authorization");
			}

			Status = ScannerStatus.Initialize;

			// Default Properties
			Settings = (settings == null) ? new ScannerSettings() : settings;
			Parser = (parser == null) ? new ZXingParser(Settings) : parser;
			Camera = (webcam == null) ? new UnityWebcam(Settings) : webcam;
		}

		public void Scan(Action<string, string> callback)
		{
			if (Callback != null)
			{
				Log.Warning(this + " Already Scan");
				return;
			}
			Callback = callback;

			Log.Info(this + " SimpleScanner -> Start Scan");
			Status = ScannerStatus.Running;

			#if !UNITY_WEBGL
			if (Settings.ScannerBackgroundThread)
			{
				if (CodeScannerThread != null)
				{
					Stop(true);
				}

				decodeInterrupted = false;
				CodeScannerThread = new Thread(ThreadDecodeQR);
				CodeScannerThread.Start();
			}
			#endif
		}

		public void Stop()
		{
			Stop(false);
		}

		private void Stop(bool forced)
		{
			if (!forced && Callback == null)
			{
				Log.Warning(this + " No Scan running");
				return;
			}

			// Stop thread / Clean callback
			Log.Info(this + " SimpleScanner -> Stop Scan");
			#if !UNITY_WEBGL
			if (CodeScannerThread != null)
			{
				decodeInterrupted = true;
				CodeScannerThread.Join();
				CodeScannerThread = null;
			}
			#endif

			Callback = null;
			Status = ScannerStatus.Paused;
		}

		public void Destroy()
		{
			// clean events
			OnReady = null;
			StatusChanged = null;

			// Stop it
			Stop(true);

			// clean returns
			Callback = null;
			Result = null;
			pixels = null;
			parserPixelAvailable = false;

			// clean camera
			Camera.Destroy();
			Camera = null;
			Parser = null;
		}

		#region Unthread

		public void DecodeQR()
		{
			// Wait
			if (Status != ScannerStatus.Running || !parserPixelAvailable || Camera.Width == 0)
			{
				return;
			}

			// Process
			Log.Debug(this + " SimpleScanner -> Scan ... " + Camera.Width + " / " + Camera.Height);
			try
			{
				Result = Parser.Decode(pixels, Camera.Width, Camera.Height);
				parserPixelAvailable = false;
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

		#endregion

		#region Background Thread

		#if !UNITY_WEBGL
		private Thread CodeScannerThread;

		public void ThreadDecodeQR()
		{
			while (decodeInterrupted == false && Result == null)
			{
				// Wait
				if (Status != ScannerStatus.Running || !parserPixelAvailable || Camera.Width == 0)
				{
					Thread.Sleep(Mathf.FloorToInt(Settings.ScannerDecodeInterval * 1000));
					continue;
				}

				// Process
				Log.Debug(this + " SimpleScanner -> Scan ... " + Camera.Width + " / " + Camera.Height);
				try
				{
					Result = Parser.Decode(pixels, Camera.Width, Camera.Height);
					parserPixelAvailable = false;
					if (Result == null)
					{
						continue;
					}

					// Sleep a little bit and set the signal to get the next frame
					Thread.Sleep(Mathf.FloorToInt(Settings.ScannerDecodeInterval * 1000));
				}
				catch (ThreadAbortException) { }
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}
		#endif

		#endregion

		private bool WebcamInitialized()
		{
			// If webcam information still change, reset delayFrame
			if (webcamLastChecksum != Camera.GetChecksum())
			{
				webcamLastChecksum = Camera.GetChecksum();
				webcamFrameDelayed = 0;
				return false;
			}

			// Increment delayFrame
			if (webcamFrameDelayed < Settings.ScannerDelayFrameMin)
			{
				webcamFrameDelayed++;
				return false;
			}

			Camera.SetSize();
			webcamFrameDelayed = 0;
			return true;
		}

		public void Update()
		{
			// If not ready, wait
			if (!Camera.IsReady())
			{
				Log.Warning(this + " Camera Not Ready Yet ...");
				if (status != ScannerStatus.Initialize)
				{
					Status = ScannerStatus.Initialize;
				}
				return;
			}

			// If the app start for the first time (select size & onReady Event)
			if (Status == ScannerStatus.Initialize)
			{
				if (WebcamInitialized())
				{
					Log.Info(this + " Camera is Ready ", Camera);

					Status = ScannerStatus.Paused;

					if (OnReady != null)
					{
						OnReady.Invoke(this, EventArgs.Empty);
					}
				}
			}

			if (Status == ScannerStatus.Running)
			{
				// Call the callback if a result is there
				if (Result != null)
				{
					//
					Log.Info(Result);
					Callback(Result.Type, Result.Value);

					// clean and return
					Result = null;
					parserPixelAvailable = false;
					return;
				}

				// Get the image as an array of Color32
				pixels = Camera.GetPixels(pixels);
				parserPixelAvailable = true;

				// If background thread OFF, do the decode main thread with 500ms of pause for UI
				if (!Settings.ScannerBackgroundThread && mainThreadLastDecode < Time.realtimeSinceStartup - Settings.ScannerDecodeInterval)
				{
					DecodeQR();
					mainThreadLastDecode = Time.realtimeSinceStartup;
				}
			}
		}

		public override string ToString()
		{
			return "[UnityBarcodeScanner]";
		}
	}
}
