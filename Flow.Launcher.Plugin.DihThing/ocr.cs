using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using System.Windows.Forms;

namespace Flow.Launcher.Plugin.DihThing
{
	/// <summary>
	/// OCR utility class for screen capture and text recognition.
	/// </summary>
	public class Ocr
	{
		/// <summary>
		/// Captures the primary screen and returns it as a Tesseract Pix object.
		/// </summary>
		/// <returns>A Pix object representing the screen capture.</returns>
		public static Pix CaptureScreen()
		{
			if (Screen.PrimaryScreen != null)
			{
				Rectangle bounds = Screen.PrimaryScreen.Bounds;
				Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(screenshot))
				{
					g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
				}

				// Convert Bitmap to Pix using modern Tesseract API
				// Use fully qualified name to avoid ambiguity with Tesseract.ImageFormat
				using (var ms = new System.IO.MemoryStream())
				{
					screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
					return Pix.LoadFromMemory(ms.ToArray());
				}
			}

			return null;
		}

		/// <summary>
		/// Represents a region of text with its bounding box.
		/// </summary>
		public class TextRegion
		{
			/// <summary>
			/// The recognized text.
			/// </summary>
			public string Text { get; set; }

			/// <summary>
			/// The bounding box of the text.
			/// </summary>
			public Rectangle Bounds { get; set; }
		}

		/// <summary>
		/// Captures the screen and returns a list of recognized text regions with bounding boxes.
		/// </summary>
		/// <returns>A list of TextRegion objects.</returns>
		public static List<TextRegion> GetScreenText()
		{
			using var img = CaptureScreen();
			return GetTextFromPix(img);
		}

		/// <summary>
		/// Processes a Pix image and returns a list of recognized text regions.
		/// </summary>
		/// <param name="img">The Pix image to process.</param>
		/// <returns>A list of TextRegion objects.</returns>
		public static List<TextRegion> GetTextFromPix(Pix img)
		{
			if (img == null) return new List<TextRegion>();

			string tessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";
			using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
			using var page = engine.Process(img);

			var regions = new List<TextRegion>();
			using var iter = page.GetIterator();
			iter.Begin();
			do
			{
				if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
				{
					var text = iter.GetText(PageIteratorLevel.Word);
					if (!string.IsNullOrWhiteSpace(text))
					{
						regions.Add(new TextRegion
						{
							Text = text,
							Bounds = new Rectangle(rect.X1, rect.Y1, rect.Width, rect.Height)
						});
					}
				}
			} while (iter.Next(PageIteratorLevel.Word));

			return regions;
		}
	}
}