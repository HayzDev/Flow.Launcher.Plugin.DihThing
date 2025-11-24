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
            string searchText = query.Search;
            var commands = new List<Command>();
            var commandStrings =
                searchText.Split(new[] { _settings.CommandSeparator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var cmdStr in commandStrings)
            {
                var cmd = ParseCommand(cmdStr.Trim());
                if (cmd.HasValue)
                {
                    commands.Add(cmd.Value);
                }
            }

            if (commands.Count == 0)
            {
                return new List<Result>();
            }

            var result = new Result
            {
                Title = $"Execute Chain: {searchText}",
                SubTitle = $"Perform {commands.Count} actions",
                Action = c => ExecuteCommands(commands),
                IcoPath = "Images/app.png",
                Score = 100
            };

            return new List<Result> { result };
        }

        private struct Command
        {
            public string SearchText;
            public int? Quadrant;
            public string Direction;
            public Action<int, int> Action;
        }

        private Command? ParseCommand(string searchText)
        {
            int? quadrant = null;
            string direction = null;
            Action<int, int> action = MouseHelper.Click; // Default to Left Click

            if (string.IsNullOrWhiteSpace(searchText)) return null;

            int startIndex = 0;
            bool hasModifier = false;

            // Check for standalone modifiers at the start
            if (searchText.StartsWith("!"))
            {
                action = MouseHelper.RightClick;
                startIndex = 1;
                hasModifier = true;
            }
            else if (searchText.StartsWith("@"))
            {
                action = MouseHelper.Move;
                startIndex = 1;
                hasModifier = true;
            }

            // If we found a modifier, check if there's a space or if it's followed by quadrant/direction
            // But wait, the user wants "@ open".
            // If we have a modifier, we might still have a quadrant/direction after it?
            // "1L! test" vs "!1L test" vs "! test"
            // The previous logic handled "1L!" (suffix).
            // The user wants "@ open" (prefix).

            // Let's handle the prefix case first.
            if (hasModifier)
            {
                // If followed by space, it's just a modifier for the text
                if (startIndex < searchText.Length && searchText[startIndex] == ' ')
                {
                    searchText = searchText.Substring(startIndex).TrimStart();
                    return new Command
                    {
                        SearchText = searchText,
                        Quadrant = null,
                        Direction = null,
                        Action = action
                    };
                }

                // If not followed by space, it might be "!1L test"
                // Let's continue parsing from startIndex
            }

            // Reset startIndex for standard parsing if no prefix modifier or if we want to continue parsing
            // Actually, if we found a prefix modifier, we should probably stick with it.
            // But what if the user types "!1L test"?
            // If I strip the "!", I have "1L test".
            // I can recursively call ParseCommand? No, that might be complex.

            // Let's try to parse quadrant/direction from the current position

            string textToParse = searchText.Substring(startIndex);
            int localStartIndex = 0;
            bool hasQuadDir = false;

            if (textToParse.Length > 0)
            {
                // Check if starts with digit
                if (char.IsDigit(textToParse[0]))
                {
                    int firstDigit = int.Parse(textToParse[0].ToString());
                    if (firstDigit >= 1 && firstDigit <= 4)
                    {
                        // Check for direction letter after digit
                        if (textToParse.Length > 1 && char.IsLetter(textToParse[1]))
                        {
                            char dirChar = char.ToUpper(textToParse[1]);
                            if (dirChar == 'L' || dirChar == 'R' || dirChar == 'T' || dirChar == 'B')
                            {
                                quadrant = firstDigit;
                                direction = dirChar.ToString();
                                localStartIndex = 2;
                                hasQuadDir = true;
                            }
                        }
                        // Just quadrant, no direction
                        else
                        {
                            quadrant = firstDigit;
                            localStartIndex = 1;
                            hasQuadDir = true;
                        }
                    }
                }
                // Check if starts with direction letter
                else if (char.IsLetter(textToParse[0]))
                {
                    char dirChar = char.ToUpper(textToParse[0]);
                    if (dirChar == 'L' || dirChar == 'R' || dirChar == 'T' || dirChar == 'B')
                    {
                        // Check for quadrant after direction
                        if (textToParse.Length > 1 && char.IsDigit(textToParse[1]))
                        {
                            int digit = int.Parse(textToParse[1].ToString());
                            if (digit >= 1 && digit <= 4)
                            {
                                direction = dirChar.ToString();
                                quadrant = digit;
                                localStartIndex = 2;
                                hasQuadDir = true;
                            }
                        }
                        // Just direction, no quadrant
                        else
                        {
                            direction = dirChar.ToString();
                            localStartIndex = 1;
                            hasQuadDir = true;
                        }
                    }
                }
            }

            // Check for suffix modifiers if we haven't found a prefix one
            // Or if we found a prefix one, we shouldn't check for suffix?
            // Let's assume prefix overrides or is exclusive.

            if (!hasModifier)
            {
                // Check for action modifiers (! or @) immediately following the prefix (suffix style)
                int suffixCheckIndex = localStartIndex;
                if (hasQuadDir && suffixCheckIndex < textToParse.Length)
                {
                    char nextChar = textToParse[suffixCheckIndex];
                    if (nextChar == '!')
                    {
                        action = MouseHelper.RightClick;
                        localStartIndex++;
                    }
                    else if (nextChar == '@')
                    {
                        action = MouseHelper.Move;
                        localStartIndex++;
                    }
                }
            }

            if (hasQuadDir)
            {
                // Ensure we are at a boundary (space or end of string)
                if (localStartIndex < textToParse.Length && textToParse[localStartIndex] != ' ')
                {
                    // Invalid prefix, treat as part of search text
                    // If we had a prefix modifier, we keep it.
                    // If not, we reset.
                    if (!hasModifier)
                    {
                        quadrant = null;
                        direction = null;
                        action = MouseHelper.Click;
                        // textToParse is the full string
                    }
                    else
                    {
                        // We have a prefix modifier, but the quad/dir parsing failed validation.
                        // So we treat "1Lsomething" as just text.
                        // But we stripped the modifier.
                    }
                    // In both cases, the search text is the remainder.
                    // If hasModifier is true, searchText is "!..."
                    // We stripped "!" (startIndex=1).
                    // textToParse is "..."
                    // We failed to parse quad/dir.
                    // So search text is textToParse.
                }
                else
                {
                    // Valid quad/dir
                    textToParse = textToParse.Substring(localStartIndex).TrimStart();
                }
            }

            // Final search text
            // If we had a modifier at start, we stripped it.
            // If we had quad/dir, we stripped it from textToParse.
            // If we didn't have quad/dir, textToParse is the original substring.

            // Wait, this logic is getting complicated.
            // Let's simplify.
            // 1. Check prefix modifier.
            // 2. Check quad/dir on the remainder.
            // 3. If quad/dir found, check suffix modifier (only if no prefix modifier).
            // 4. Validate boundaries.

            // Re-implementation:

            string currentText = searchText;

            // 1. Prefix Modifier
            if (currentText.StartsWith("!"))
            {
                action = MouseHelper.RightClick;
                currentText = currentText.Substring(1);
                hasModifier = true;
            }
            else if (currentText.StartsWith("@"))
            {
                action = MouseHelper.Move;
                currentText = currentText.Substring(1);
                hasModifier = true;
            }

            // 2. Quadrant/Direction
            // We need to peek at currentText
            int parseIndex = 0;
            bool foundQuadDir = false;
            int? tempQuad = null;
            string tempDir = null;

            if (currentText.Length > 0)
            {
                if (char.IsDigit(currentText[0]))
                {
                    int d = int.Parse(currentText[0].ToString());
                    if (d >= 1 && d <= 4)
                    {
                        if (currentText.Length > 1 && char.IsLetter(currentText[1]))
                        {
                            char c = char.ToUpper(currentText[1]);
                            if (c == 'L' || c == 'R' || c == 'T' || c == 'B')
                            {
                                tempQuad = d;
                                tempDir = c.ToString();
                                parseIndex = 2;
                                foundQuadDir = true;
                            }
                        }

                        if (!foundQuadDir)
                        {
                            tempQuad = d;
                            parseIndex = 1;
                            foundQuadDir = true;
                        }
                    }
                }
                else if (char.IsLetter(currentText[0]))
                {
                    char c = char.ToUpper(currentText[0]);
                    if (c == 'L' || c == 'R' || c == 'T' || c == 'B')
                    {
                        if (currentText.Length > 1 && char.IsDigit(currentText[1]))
                        {
                            int d = int.Parse(currentText[1].ToString());
                            if (d >= 1 && d <= 4)
                            {
                                tempDir = c.ToString();
                                tempQuad = d;
                                parseIndex = 2;
                                foundQuadDir = true;
                            }
                        }

                        if (!foundQuadDir)
                        {
                            tempDir = c.ToString();
                            parseIndex = 1;
                            foundQuadDir = true;
                        }
                    }
                }
            }

            // 3. Suffix Modifier (only if no prefix modifier and we found a quad/dir)
            if (!hasModifier && foundQuadDir && parseIndex < currentText.Length)
            {
                if (currentText[parseIndex] == '!')
                {
                    action = MouseHelper.RightClick;
                    parseIndex++;
                }
                else if (currentText[parseIndex] == '@')
                {
                    action = MouseHelper.Move;
                    parseIndex++;
                }
            }

            // 4. Boundary Check
            if (foundQuadDir)
            {
                if (parseIndex < currentText.Length && currentText[parseIndex] != ' ')
                {
                    // Invalid boundary.
                    // If we had a prefix modifier, we keep the action but ignore the quad/dir.
                    // If we didn't, we reset everything.
                    if (!hasModifier)
                    {
                        // Reset to defaults
                        action = MouseHelper.Click;
                        // Search text is original
                        return new Command
                            { SearchText = searchText, Quadrant = null, Direction = null, Action = action };
                    }
                    else
                    {
                        // Keep prefix action, but treat quad/dir as text
                        // Search text is currentText (which has prefix stripped)
                        return new Command
                        {
                            SearchText = currentText.TrimStart(), Quadrant = null, Direction = null, Action = action
                        };
                    }
                }
                else
                {
                    // Valid
                    quadrant = tempQuad;
                    direction = tempDir;
                    currentText = currentText.Substring(parseIndex);
                }
            }

            return new Command
            {
                SearchText = currentText.TrimStart(),
                Quadrant = quadrant,
                Direction = direction,
                Action = action
            };
        }


        private bool ExecuteCommands(List<Command> commands)
        {
            // Hide Flow Launcher window before taking screenshot
            Context.API.HideMainWindow();

            // Small delay to ensure window is hidden
            System.Threading.Thread.Sleep(100);

            foreach (var cmd in commands)
            {
                // Get screen bounds and calculate quadrant if specified
                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                System.Drawing.Rectangle? searchArea = null;

                // Capture screen BEFORE showing any overlays to ensure clean OCR
                using var screenImage = Ocr.CaptureScreen();

                if (cmd.Quadrant.HasValue)
                {
                    int halfWidth = screenBounds.Width / 2;
                    int halfHeight = screenBounds.Height / 2;

                    searchArea = cmd.Quadrant.Value switch
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

                var queryWords = cmd.SearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var queryWordCount = queryWords.Length;

                if (queryWordCount > 0)
                {
                    // Collect all matches instead of clicking the first one
                    var matches = new List<(List<Ocr.TextRegion> regions, int centerX, int centerY)>();

                    for (int i = 0; i <= regions.Count - queryWordCount; i++)
                    {
                        var candidateRegions = regions.GetRange(i, queryWordCount);
                        var candidateText = string.Join(" ", candidateRegions.Select(r => r.Text)).ToLower();
                        var queryTextLower = cmd.SearchText.ToLower();

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
                        var selectedMatch = cmd.Direction switch
                        {
                            "L" => matches.OrderBy(m => m.centerX).First(), // Leftmost
                            "R" => matches.OrderByDescending(m => m.centerX).First(), // Rightmost
                            "T" => matches.OrderBy(m => m.centerY).First(), // Topmost
                            "B" => matches.OrderByDescending(m => m.centerY).First(), // Bottommost
                            _ => matches.First() // Default: first match
                        };

                        cmd.Action(selectedMatch.centerX, selectedMatch.centerY);
                    }
                }

                // Delay between commands
                if (commands.IndexOf(cmd) < commands.Count - 1)
                {
                    System.Threading.Thread.Sleep(_settings.CommandDelay);
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

            // Max Levenshtein Distance
            var labelLevenshtein = new System.Windows.Controls.Label { Content = "Max Levenshtein Distance:" };
            var textBoxLevenshtein = new System.Windows.Controls.TextBox
                { Text = _settings.MaxLevenshteinDistance.ToString() };

            textBoxLevenshtein.TextChanged += (_, _) =>
            {
                if (double.TryParse(textBoxLevenshtein.Text, out double result))
                {
                    _settings.MaxLevenshteinDistance = result;
                    Context.API.SaveSettingJsonStorage<Settings>();
                }
            };

            // Command Separator
            var labelSeparator = new System.Windows.Controls.Label { Content = "Command Separator:" };
            var textBoxSeparator = new System.Windows.Controls.TextBox
                { Text = _settings.CommandSeparator };

            textBoxSeparator.TextChanged += (_, _) =>
            {
                _settings.CommandSeparator = textBoxSeparator.Text;
                Context.API.SaveSettingJsonStorage<Settings>();
            };

            // Command Delay
            var labelDelay = new System.Windows.Controls.Label { Content = "Command Delay (ms):" };
            var textBoxDelay = new System.Windows.Controls.TextBox
                { Text = _settings.CommandDelay.ToString() };

            textBoxDelay.TextChanged += (_, _) =>
            {
                if (int.TryParse(textBoxDelay.Text, out int result))
                {
                    _settings.CommandDelay = result;
                    Context.API.SaveSettingJsonStorage<Settings>();
                }
            };

            panel.Children.Add(labelLevenshtein);
            panel.Children.Add(textBoxLevenshtein);
            panel.Children.Add(labelSeparator);
            panel.Children.Add(textBoxSeparator);
            panel.Children.Add(labelDelay);
            panel.Children.Add(textBoxDelay);

            return new System.Windows.Controls.UserControl { Content = panel };
        }
    }
}
