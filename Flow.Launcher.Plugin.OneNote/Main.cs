using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;
using System.Diagnostics;

//https://github.com/microsoft/PowerToys/blob/9b7a7f93b716afbbc476efaa2a57fb0f365b126b/src/modules/launcher/Plugins/Microsoft.PowerToys.Run.Plugin.OneNote/Main.cs
//Icon8
//Flaticon
//TODO: add settings to change default open oneNote.
//TODO: add open to use web only -> would need Microsoft.Graph async and Azure  account (for refereshing and keep an access token) nonsense
namespace Flow.Launcher.Plugin.OneNote
{
    /// <summary>
    /// 
    /// </summary>
    public class OneNote : IPlugin, IContextMenu 
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly string logoPath = "Images/logo.png";
        private readonly string warningPath = "Images/warning.png";
        private readonly string syncPath = "Images/icons8-refresh-240.png";

        private ContextMenu contextMenu;
        /// <inheritdoc/>
        public void Init(PluginInitContext context)
        {
            this.context = context;
            try
            {
                _ = OneNoteProvider.PageItems.Any();
                hasOneNote = true;
            }
            catch (Exception)
            {
                hasOneNote = false;
            }
            contextMenu = new ContextMenu(context);
        }
        /// <inheritdoc/>

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            if(!hasOneNote)
            {
                results.Add(new Result
                {
                    Title = "OneNote is not installed.",
                    IcoPath = warningPath
                });
                return results;
                // return new List<Result>
                // {
                //     new Result
                //     {
                //         Title = "OneNote is not installed.",
                //         IcoPath = warningPath
                //     }
                // };
            }

            if(string.IsNullOrEmpty(query.Search))
            {
                results.Add(new Result
                {
                    Title = "Search pages in OneNote",
                    IcoPath = logoPath,
                });
                results.AddRange(OneNoteProvider.NotebookItems.Select(notebook => new Result 
                {
                    Title = notebook.Name,
                    SubTitle = notebook.Color?.ToString(),
                    IcoPath = logoPath,
                    ContextData = notebook,
                }));
                results.Add(new Result
                {
                    Title = "Sync Notebooks",
                    IcoPath = syncPath,
                    Action = c =>
                    {
                        foreach (var item in OneNoteProvider.NotebookItems)
                        {
                            Utils.CallOneNoteSafely<object>(oneNote =>
                            {
                                oneNote.SyncHierarchy(item.ID);
                                return default;
                            }
                            );
                        }
                        return false;
                    }
                });
                // return new List<Result>
                // {
                //     new Result
                //     {
                //         Title = "Search pages in OneNote",
                //         IcoPath = logoPath,
                //     },

                //     new Result
                //     {
                //         Title = "Sync Notebooks",
                //         IcoPath = syncPath,
                //         Action = c =>
                //         {
                //             foreach (var item in OneNoteProvider.NotebookItems)
                //             {
                //                 Utils.CallOneNoteSafely<object>(oneNote =>
                //                 {
                //                     oneNote.SyncHierarchy(item.ID);
                //                     return default;
                //                 }
                //                 );
                //             }
                //             return false;
                //         }
                //     }
                // };
            }

            results = OneNoteProvider
                .FindPages(query.Search)
                .Select(page => new Result
                {
                    Title = page.Name,
                    SubTitle = GetReadablePath(page),
                    TitleToolTip = "Last Modified: " + page.LastModified,
                    IcoPath = logoPath,
                    ContextData = page,
                    TitleHighlightData = GetMatchData(query.Search, page.Name),//GetHighlightData(query, page.Name),
                    Action = c =>
                    {
                        //OpenInOneNoteWindows10(page);
                        page.OpenInOneNote();
                        return true;
                    },
                })
                .ToList();
            return results;
        }

        /// <inheritdoc/>

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return contextMenu.LoadContextMenus(selectedResult);
        }
        
        private static string GetReadablePath(IOneNoteExtPage page)
        {
            var sectionPath = page.Section.Path;
            var index = sectionPath.IndexOf("OneNote/");
            if(index != -1)
            {
                return sectionPath[(index + 8)..^4].Replace("/" , " > ");
            }
            else
            {
                return page.Notebook.Name + " > " + page.Section.Name;
            }
        }
        private List<int> GetHighlightData(Query query, string stringToCheck,int searchTermsStartIndex = 0, int stringToCheckIndex = 0)
        {
            //return context.API.FuzzySearch(query.Search,stringToCheck).MatchData;
            List<int> highlightData = new();
            for (int i = searchTermsStartIndex; i < query.SearchTerms.Length; i++)
            {
                string searchTerm = query.SearchTerms[i];
                var index = stringToCheck.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    for (int j = 0 + stringToCheckIndex; j < searchTerm.Length + stringToCheckIndex; j++)
                    {
                        highlightData.Add(index + j);
                    }
                }
            }
            return highlightData;
        }

        private List<int> GetMatchData(string query, string stringToCompare)
        {
            return context.API.FuzzySearch(query, stringToCompare).MatchData;
        }

        public class ContextMenu : IContextMenu
        {
            private PluginInitContext context;

            public ContextMenu(PluginInitContext context)
            {
                this.context = context;
            }

            public List<Result> LoadContextMenus(Result selectedResult)
            {
                var results = new List<Result>();
                if(selectedResult.ContextData is IOneNoteExtNotebook notebook)
                {
                    results.AddRange(notebook.Sections.Select(notebookSection => new Result
                    {
                        Title = notebookSection.Name,
                        SubTitle = notebookSection.Color?.ToString(),
                        ContextData = notebookSection,
                        //TitleHighlightData = GetMatchData()
                        //Get Hue of color and Saturation?
                    }));
                }
                if(selectedResult.ContextData is IOneNoteExtSection section)
                {
                    results.AddRange(section.Pages.Cast<IOneNoteExtPage>().Select(sectionPage => new Result
                    {
                        Title = sectionPage.Name,
                        SubTitle = GetReadablePath(sectionPage),
                        ContextData = sectionPage,
                    }));
                }
                //if(selectedResult.ContextData is IOneNoteExtPage )
                return results;
            }
        }
    }
}