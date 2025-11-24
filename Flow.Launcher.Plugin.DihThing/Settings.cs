namespace Flow.Launcher.Plugin.DihThing
{
	public class Settings : BaseModel
	{
		private double _maxLevenshteinDistance = 0.2;

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

		private string _commandSeparator = ",";

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
	}
}
