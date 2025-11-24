using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using System.Windows.Forms;
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
	/// <summary>
	/// OCR utility class for screen capture and text recognition.
	/// </summary>
	public class Ocr
	{
		/// <summary>
		/// Captures the primary screen and returns it as a Bitmap.
		/// </summary>
		/// <returns>A Bitmap object representing the screen capture.</returns>
		public static Bitmap CaptureScreen()
		{
			if (Screen.PrimaryScreen != null)
			{
				Rectangle bounds = Screen.PrimaryScreen.Bounds;
				Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(screenshot))
				{
					g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
				}

				return screenshot;
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
		/// Processes a Bitmap image and returns a list of recognized text regions.
		/// </summary>
		/// <param name="image">The Bitmap image to process.</param>
		/// <param name="scaleFactor">The factor to upscale the image by (1-4).</param>
		/// <param name="enableThresholding">Whether to enable adaptive thresholding (grayscale conversion).</param>
		/// <returns>A list of TextRegion objects.</returns>
		public static List<TextRegion> GetTextFromBitmap(Bitmap image, int scaleFactor, bool enableThresholding)
		{
			if (image == null) return new List<TextRegion>();

			// Preprocess image
			using var processedImage = PreprocessImage(image, scaleFactor, enableThresholding);

			// Convert to Pix
			using var ms = new System.IO.MemoryStream();
			processedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			using var pix = Pix.LoadFromMemory(ms.ToArray());

			string tessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";
			using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
			using var page = engine.Process(pix);

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
						// Scale coordinates back down
						int x = rect.X1 / scaleFactor;
						int y = rect.Y1 / scaleFactor;
						int w = rect.Width / scaleFactor;
						int h = rect.Height / scaleFactor;

						regions.Add(new TextRegion
						{
							Text = text,
							Bounds = new Rectangle(x, y, w, h)
						});
					}
				}
			} while (iter.Next(PageIteratorLevel.Word));

			return regions;
		}

		private static Bitmap PreprocessImage(Bitmap original, int scaleFactor, bool grayscale)
		{
			int width = original.Width * scaleFactor;
			int height = original.Height * scaleFactor;
			var resized = new Bitmap(width, height);

			using (var g = Graphics.FromImage(resized))
			{
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
				g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

				if (grayscale)
				{
					// Grayscale Color Matrix
					ColorMatrix colorMatrix = new ColorMatrix(
						new float[][]
						{
							new float[] { .3f, .3f, .3f, 0, 0 },
							new float[] { .59f, .59f, .59f, 0, 0 },
							new float[] { .11f, .11f, .11f, 0, 0 },
							new float[] { 0, 0, 0, 1, 0 },
							new float[] { 0, 0, 0, 0, 1 }
						});

					using (var attributes = new ImageAttributes())
					{
						attributes.SetColorMatrix(colorMatrix);
						g.DrawImage(original, new Rectangle(0, 0, width, height),
							0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
					}
				}
				else
				{
					g.DrawImage(original, 0, 0, width, height);
				}
			}

			return resized;
		}
	}
}