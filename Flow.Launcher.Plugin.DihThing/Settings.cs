using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DihThing
{
	/// <summary>
	/// Represents the configurable settings for the DihThing plugin.
	/// </summary>
	public class Settings : BaseModel
	{
		private double _maxLevenshteinDistance = 0.2;

		/// <summary>
		/// Gets or sets the maximum Levenshtein distance ratio (0‑1) allowed for OCR text matching.
		/// </summary>
		public double MaxLevenshteinDistance
		{
			get => _maxLevenshteinDistance;
			set
			{
				if (_maxLevenshteinDistance != value)
				{
					_maxLevenshteinDistance = value;
					OnPropertyChanged();
				}
			}
		}

		private bool _useWinVind;

		/// <summary>
		/// Enables or disables win‑vind easyclick integration.
		/// When true, queries like "?", "!?" or "@?" will launch win‑vind with the appropriate command.
		/// </summary>
		public bool UseWinVind
		{
			get => _useWinVind;
			set
			{
				if (_useWinVind != value)
				{
					_useWinVind = value;
					OnPropertyChanged();
				}
			}
		}

		private string _commandSeparator = ",";

		/// <summary>
		/// Gets or sets the string used to separate multiple commands in a query.
		/// </summary>
		public string CommandSeparator
		{
			get => _commandSeparator;
			set
			{
				if (_commandSeparator != value)
				{
					_commandSeparator = value;
					OnPropertyChanged();
				}
			}
		}

		private int _commandDelay = 300;

		/// <summary>
		/// Gets or sets the delay, in milliseconds, between successive commands in a chain.
		/// </summary>
		public int CommandDelay
		{
			get => _commandDelay;
			set
			{
				if (_commandDelay != value)
				{
					_commandDelay = value;
					OnPropertyChanged();
				}
			}
		}

		private bool _enableAdaptiveThresholding = true;

		/// <summary>
		/// Enables or disables adaptive thresholding during OCR preprocessing.
		/// </summary>
		public bool EnableAdaptiveThresholding
		{
			get => _enableAdaptiveThresholding;
			set
			{
				if (_enableAdaptiveThresholding != value)
				{
					_enableAdaptiveThresholding = value;
					OnPropertyChanged();
				}
			}
		}

		private int _upscaleFactor = 2;

		/// <summary>
		/// Gets or sets the upscaling factor (1‑4) applied to the screenshot before OCR.
		/// </summary>
		public int UpscaleFactor
		{
			get => _upscaleFactor;
			set
			{
				if (_upscaleFactor != value)
				{
					_upscaleFactor = value;
					OnPropertyChanged();
				}
			}
		}
	}
}
