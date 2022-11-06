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
                foreach(var section in notebook.Sections)
                {
                    results.Add(new Result
                    {
                        Title = section.Name,
                        SubTitle = section.Color?.ToString(),
                        //Get Hue of color and Saturation?
                    });
                }
            }
            if(selectedResult.ContextData is IOneNoteExtSection section1)
            {
                
            }
            return results;
        }
    }
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
            if(!hasOneNote)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "OneNote is not installed.",
                        IcoPath = warningPath
                    }
                };
            }

            if(string.IsNullOrEmpty(query.Search))
            {
                var notebooks = OneNoteProvider.NotebookItems.Select(notebook => new Result 
                {
                    Title = notebook.Name,
                    IcoPath = logoPath,
                    ContextData = notebook,
                });

                return new List<Result>
                {
                    new Result
                    {
                        Title = "Search pages in OneNote",
                        IcoPath = logoPath,
                    },

                    new Result
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
                    }
                };
            }

            List<Result> results = OneNoteProvider
                .FindPages(query.Search)
                .Select(page => new Result
                {
                    Title = page.Name,
                    SubTitle = GetReadablePath(page),
                    TitleToolTip = "Last Modified: " + page.LastModified,
                    IcoPath = logoPath,
                    ContextData = page,
                    TitleHighlightData = GetHighlightData(query, page.Name),
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
            // var results = new List<Result> 
            // {
            //     // new Result 
            //     // {
            //     //     Title = "Open in OneNote for Windows 10",
            //     //     SubTitle = selectedResult.SubTitle + " > " +selectedResult.Title,
            //     //     IcoPath = logoPath,
            //     //     Action = _ => 
            //     //     {
            //     //         OpenInOneNoteWindows10((IOneNoteExtPage)selectedResult.ContextData);
            //     //         return true;
            //     //     },
            //     // },
            //     new Result 
            //     {
            //         Title = "Open in OneNote",
            //         SubTitle = selectedResult.SubTitle + " > " +selectedResult.Title,
            //         IcoPath = logoPath, 
            //         Action = _ => 
            //         {
            //             ((IOneNoteExtPage)selectedResult.ContextData).OpenInOneNote();
            //             return true;
            //         },
            //     },
            // };

            // return results;
        }

        // private static void OpenInOneNoteWindows10(IOneNoteExtPage page)
        // {
        //     string link = $"onenote:{page.Section.Path}#{page.Name}";
        //     link = link.Replace(" ", "%20");
        //     string sectionID = page.Section.ID[..(page.Section.ID.IndexOf('}') + 1)];
        //     string pageID = page.ID[..(page.ID.IndexOf('}') + 1)];
        //     link = $"{link}&section-id={sectionID}&page-id={pageID}&end";


        //     //string link = Utils.CallOneNoteSafely(onenote =>
        //     //{
        //     //     onenote.GetHyperlinkToObject(page.ID,"",out string link);
        //     //     return link;
        //     //});
        //     var psi = new ProcessStartInfo(link) { UseShellExecute = true };
        //     Process.Start(psi);
        // }

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
        private static List<int> GetHighlightData(Query query, string stringToCheck,int searchTermsStartIndex = 0, int stringToCheckIndex = 0)
        {
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

    }
}