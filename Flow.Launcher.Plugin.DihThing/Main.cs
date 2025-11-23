using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DihThing
{
    public class DihThing : IPlugin
    {
        internal PluginInitContext Context;

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
                    Context.API.ShowMsg(
                        Context.API.GetTranslation("flowlauncher_plugin_dihthing_plugin_name"),
                        Context.API.GetTranslation("flowlauncher_plugin_dihthing_plugin_description")
                    );
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
        }
    }
}
