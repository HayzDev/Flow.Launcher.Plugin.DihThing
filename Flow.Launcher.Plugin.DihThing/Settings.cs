namespace Flow.Launcher.Plugin.DihThing
{
	public class Settings : BaseModel
	{
		private int _maxLevenshteinDistance = 2;

		public int MaxLevenshteinDistance
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
