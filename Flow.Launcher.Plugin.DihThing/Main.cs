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
            // Parse quadrant and direction from query
            int? quadrant = null;
            string direction = null; // L, R, T, B
            string searchText = query.Search;

            // Check for quadrant (1-4) and direction (L/R/T/B)
            // Must be followed by a space to avoid false positives
            if (searchText.Length > 0)
            {
                int startIndex = 0;
                bool hasModifier = false;

                // Check if starts with digit
                if (char.IsDigit(searchText[0]))
                {
                    int firstDigit = int.Parse(searchText[0].ToString());
                    if (firstDigit >= 1 && firstDigit <= 4)
                    {
                        // Check for direction letter after digit
                        if (searchText.Length > 1 && char.IsLetter(searchText[1]))
                        {
                            char dirChar = char.ToUpper(searchText[1]);
                            if (dirChar == 'L' || dirChar == 'R' || dirChar == 'T' || dirChar == 'B')
                            {
                                // Must be followed by space or end of string
                                if (searchText.Length == 2 || searchText[2] == ' ')
                                {
                                    quadrant = firstDigit;
                                    direction = dirChar.ToString();
                                    startIndex = 2;
                                    hasModifier = true;
                                }
                            }
                        }
                        // Just quadrant, no direction
                        else if (searchText.Length == 1 || searchText[1] == ' ')
                        {
                            quadrant = firstDigit;
                            startIndex = 1;
                            hasModifier = true;
                        }
                    }
                }
                // Check if starts with direction letter
                else if (char.IsLetter(searchText[0]))
                {
                    char dirChar = char.ToUpper(searchText[0]);
                    if (dirChar == 'L' || dirChar == 'R' || dirChar == 'T' || dirChar == 'B')
                    {
                        // Check for quadrant after direction
                        if (searchText.Length > 1 && char.IsDigit(searchText[1]))
                        {
                            int digit = int.Parse(searchText[1].ToString());
                            if (digit >= 1 && digit <= 4)
                            {
                                // Must be followed by space or end of string
                                if (searchText.Length == 2 || searchText[2] == ' ')
                                {
                                    direction = dirChar.ToString();
                                    quadrant = digit;
                                    startIndex = 2;
                                    hasModifier = true;
                                }
                            }
                        }
                        // Just direction, no quadrant
                        else if (searchText.Length == 1 || searchText[1] == ' ')
                        {
                            direction = dirChar.ToString();
                            startIndex = 1;
                            hasModifier = true;
                        }
                    }
                }

                if (hasModifier)
                {
                    searchText = searchText.Substring(startIndex).TrimStart();
                }
            }

            // Construct Title and SubTitle based on parsed info
            string title = $"Search for '{searchText}'";
            string subTitle = "Perform OCR and click text";

            if (quadrant.HasValue)
            {
                string quadText = quadrant.Value switch
                {
                    1 => "Top-Left",
                    2 => "Top-Right",
                    3 => "Bottom-Left",
                    4 => "Bottom-Right",
                    _ => "Unknown"
                };
                title += $" in {quadText}";
            }
            else
            {
                title += " on Screen";
            }

            if (!string.IsNullOrEmpty(direction))
            {
                string dirText = direction switch
                {
                    "L" => "Leftmost",
                    "R" => "Rightmost",
                    "T" => "Topmost",
                    "B" => "Bottommost",
                    _ => ""
                };
                title += $" ({dirText} match)";
            }

            var results = new List<Result>();

            results.Add(new Result
            {
                Title = title + " (Left Click)",
                SubTitle = subTitle,
                Action = c => ExecuteOcrAndAction(searchText, quadrant, direction, MouseHelper.Click),
                IcoPath = "Images/app.png",
                Score = 100
            });

            results.Add(new Result
            {
                Title = title + " (Right Click)",
                SubTitle = subTitle,
                Action = c => ExecuteOcrAndAction(searchText, quadrant, direction, MouseHelper.RightClick),
                IcoPath = "Images/app.png",
                Score = 90
            });

            results.Add(new Result
            {
                Title = title + " (Move Mouse)",
                SubTitle = subTitle,
                Action = c => ExecuteOcrAndAction(searchText, quadrant, direction, MouseHelper.Move),
                IcoPath = "Images/app.png",
                Score = 80
            });

            return results;
        }

        private bool ExecuteOcrAndAction(string searchText, int? quadrant, string direction,
            Action<int, int> mouseAction)
        {
            // Hide Flow Launcher window before taking screenshot
            Context.API.HideMainWindow();

            // Small delay to ensure window is hidden
            System.Threading.Thread.Sleep(100);

            // Get screen bounds and calculate quadrant if specified
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            System.Drawing.Rectangle? searchArea = null;

            // Capture screen BEFORE showing any overlays to ensure clean OCR
            using var screenImage = Ocr.CaptureScreen();

            if (quadrant.HasValue)
            {
                int halfWidth = screenBounds.Width / 2;
                int halfHeight = screenBounds.Height / 2;

                searchArea = quadrant.Value switch
                {
                    1 => new System.Drawing.Rectangle(0, 0, halfWidth, halfHeight), // Top-left
                    2 => new System.Drawing.Rectangle(halfWidth, 0, halfWidth, halfHeight), // Top-right
                    3 => new System.Drawing.Rectangle(0, halfHeight, halfWidth, halfHeight), // Bottom-left
                    4 => new System.Drawing.Rectangle(halfWidth, halfHeight, halfWidth,
                        halfHeight), // Bottom-right
                    _ => null
                };

                // Show overlay for visual feedback of the quadrant
                if (searchArea.HasValue)
                {
                    var overlay = new QuadrantOverlay(searchArea.Value);
                    overlay.ShowTemporarily(500);
                }
            }

            var regions = Ocr.GetTextFromPix(screenImage);

            // Filter regions by quadrant if specified
            if (searchArea.HasValue)
            {
                regions = regions.Where(r => searchArea.Value.IntersectsWith(r.Bounds)).ToList();
            }

            var queryWords = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var queryWordCount = queryWords.Length;

            if (queryWordCount > 0)
            {
                // Collect all matches instead of clicking the first one
                var matches = new List<(List<Ocr.TextRegion> regions, int centerX, int centerY)>();

                for (int i = 0; i <= regions.Count - queryWordCount; i++)
                {
                    var candidateRegions = regions.GetRange(i, queryWordCount);
                    var candidateText = string.Join(" ", candidateRegions.Select(r => r.Text)).ToLower();
                    var queryTextLower = searchText.ToLower();

                    var distance = StringExtensions.LevenshteinDistance(candidateText, queryTextLower);
                    var ratio = (double)distance / candidateText.Length;

                    if (ratio <= _settings.MaxLevenshteinDistance)
                    {
                        var minX = candidateRegions.Min(r => r.Bounds.X);
                        var minY = candidateRegions.Min(r => r.Bounds.Y);
                        var maxX = candidateRegions.Max(r => r.Bounds.X + r.Bounds.Width);
                        var maxY = candidateRegions.Max(r => r.Bounds.Y + r.Bounds.Height);

                        var centerX = (minX + maxX) / 2;
                        var centerY = (minY + maxY) / 2;

                        matches.Add((candidateRegions, centerX, centerY));
                    }
                }

                // Visualize all matches
                if (matches.Any())
                {
                    var allMatchRects = matches.SelectMany(m => m.regions.Select(r => r.Bounds)).ToList();
                    var matchOverlay = new QuadrantOverlay(allMatchRects);
                    matchOverlay.ShowTemporarily(500);

                    // Select match based on direction
                    var selectedMatch = direction switch
                    {
                        "L" => matches.OrderBy(m => m.centerX).First(), // Leftmost
                        "R" => matches.OrderByDescending(m => m.centerX).First(), // Rightmost
                        "T" => matches.OrderBy(m => m.centerY).First(), // Topmost
                        "B" => matches.OrderByDescending(m => m.centerY).First(), // Bottommost
                        _ => matches.First() // Default: first match
                    };

                    mouseAction(selectedMatch.centerX, selectedMatch.centerY);
                }
            }

            return true;
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
            var textBox = new System.Windows.Controls.TextBox
                { Text = _settings.MaxLevenshteinDistance.ToString() };

            textBox.TextChanged += (_, _) =>
            {
                if (double.TryParse(textBox.Text, out double result))
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
