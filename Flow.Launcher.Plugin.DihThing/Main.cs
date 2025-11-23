using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DihThing
{
    /// <summary>
    /// A Flow Launcher plugin that provides custom functionality.
    /// </summary>
    public class DihThing : IPlugin, ISettingProvider
    {
        internal PluginInitContext Context;
        private Settings _settings;

        /// <summary>
        /// Queries the plugin with the user's search term.
        /// </summary>
        /// <param name="query">The query object containing the search term.</param>
        /// <returns>A list of results to display in Flow Launcher.</returns>
        public List<Result> Query(Query query)
        {
            var result = new Result
            {
                Title = "Hello World from CSharp",
                SubTitle = $"Query: {query.Search}",
                Action = c =>
                {
                    Console.WriteLine(c);
                    var regions = Ocr.GetScreenText();
                    var queryWords = query.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var queryWordCount = queryWords.Length;

                    var results = regions.Where(r =>
                    {
                        var regionWords = r.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (regionWords.Length < queryWordCount)
                        {
                            return false;
                        }

                        for (int i = 0; i <= regionWords.Length - queryWordCount; i++)
                        {
                            var currentChunk = string.Join(" ", regionWords.Skip(i).Take(queryWordCount));
                            if (StringExtensions.LevenshteinDistance(currentChunk, query.Search) <
                                _settings.MaxLevenshteinDistance)
                            {
                                return true;
                            }
                        }

                        return false;
                    }).ToList();
                    if (results.Any())
                    {
                        var firstResult = results.First();
                        var centerX = firstResult.Bounds.X + firstResult.Bounds.Width / 2;
                        var centerY = firstResult.Bounds.Y + firstResult.Bounds.Height / 2;
                        MouseHelper.Click(centerX, centerY);
                    }

                    return true;
                },
                IcoPath = "Images/app.png",
            };
            return new List<Result> { result };
        }

        /// <summary>
        /// Initializes the plugin with the given context.
        /// </summary>
        /// <param name="context">The context for the plugin initialization.</param>
        public void Init(PluginInitContext context)
        {
            Context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();
        }

        /// <summary>
        /// Creates the settings panel for the plugin.
        /// </summary>
        /// <returns>The settings panel control.</returns>
        public System.Windows.Controls.Control CreateSettingPanel()
        {
            var panel = new System.Windows.Controls.StackPanel();
            var label = new System.Windows.Controls.Label { Content = "Max Levenshtein Distance:" };
            var textBox = new System.Windows.Controls.TextBox { Text = _settings.MaxLevenshteinDistance.ToString() };

            textBox.TextChanged += (sender, e) =>
            {
                if (int.TryParse(textBox.Text, out int result))
                {
                    _settings.MaxLevenshteinDistance = result;
                    Context.API.SaveSettingJsonStorage<Settings>();
                }
            };

            panel.Children.Add(label);
            panel.Children.Add(textBox);
            return new System.Windows.Controls.UserControl { Content = panel };
        }
    }
}
