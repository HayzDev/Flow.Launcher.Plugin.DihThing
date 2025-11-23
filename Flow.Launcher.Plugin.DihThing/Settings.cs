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
	}
}
