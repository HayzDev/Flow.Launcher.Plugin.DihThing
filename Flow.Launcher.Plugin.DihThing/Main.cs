using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DihThing
{
    public class DihThing : IPlugin
    {
        internal PluginInitContext Context;

        public List<Result> Query(Query query)
        {
            var result = new Result
            {
                Title = "Hello World from CSharp",
                SubTitle = $"Query: {query.Search}",
                Action = c =>
                {
                    Context.API.ShowMsg(Context.API.GetTranslation("plugin_helloworldcsharp_greet_title"),
                                            Context.API.GetTranslation("plugin_helloworldcsharp_greet_subtitle"));
                    return true;
                },
                IcoPath = "Images/app.png"
            };
            return new List<Result> { result };
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }
    }
}