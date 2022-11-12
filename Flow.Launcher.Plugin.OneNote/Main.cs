using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

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
    public class OneNote : IPlugin 
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly string logoPath = "Images/logo.png";
        private readonly string warningPath = "Images/warning.png";
        private readonly string syncPath = "Images/icons8-refresh-240.png";
        private readonly string notebookPath = "Images/notebook.png";
        private Image notebookImage;

        private IOneNoteExtNotebook lastSelectedNotebook;
        private IOneNoteExtSection lastSelectedSection;      

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
        }
    
        /// <inheritdoc/>

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            if (!hasOneNote)
            {
                results.Add(new Result
                {
                    Title = "OneNote is not installed.",
                    IcoPath = warningPath
                });
                return results;

            }
            //TODO: Cache all NotebookItems searchs
            if (string.IsNullOrEmpty(query.Search))
            {
                results.Add(new Result
                {
                    Title = "Search OneNote pages",
                    SubTitle = "Type \"nb\\\" to search by notebook structure",
                    IcoPath = logoPath,
                    AutoCompleteText = "Type something",
                    Score = int.MaxValue,
                });

                results.AddRange(OneNoteProvider.NotebookItems.Select(nb => GetResultFromNotebook(nb)));
                results.Add(new Result
                {
                    Title = "Sync Notebooks",
                    IcoPath = syncPath,
                    Score = int.MinValue,
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
                return results;
            }

            //NOTE: There is no nested sections i.e. there is nothing for the Section Group in the structure 
            if(query.FirstSearch.StartsWith("nb\\"))
            {
                string[] searchStrings = query.Search.Split('\\',StringSplitOptions.None);
                string searchString;
                List<int> highlightData = null;
                //Could replace switch case with for loop
                switch (searchStrings.Length)
                {
                    case 2://Full query for notebook not complete e.g. nb\User Noteb 
                        
                        //Get matching notebooks and create results.
                        searchString = searchStrings[1];
                        
                        if (string.IsNullOrWhiteSpace(searchString)) // Do a normall notebook search
                        {
                            lastSelectedNotebook = null;
                            return OneNoteProvider.NotebookItems.Select(nb => GetResultFromNotebook(nb)).ToList();
                        }

                        return OneNoteProvider.NotebookItems.Where(nb =>
                        {
                            if (lastSelectedNotebook != null && nb.ID == lastSelectedNotebook.ID)
                                return true;
                            return TreeQuery(nb.Name, searchString, out highlightData);
                        })
                        .Select(nb => GetResultFromNotebook(nb, highlightData))
                        .ToList();
                    case 3://Full query for section not complete e.g nb\User Notebook\Happine
                        searchString = searchStrings[2];

                        if(!NotebookValidCheck(searchStrings[1]))
                            return new List<Result>();

                        if(string.IsNullOrWhiteSpace(searchString))
                        {
                            lastSelectedSection = null;
                            return lastSelectedNotebook.Sections.Select(st => GetResultsFromSection(st,lastSelectedNotebook)).ToList();
                        }
                        return lastSelectedNotebook.Sections.Where(st => 
                        {
                            if(lastSelectedSection != null && st.ID == lastSelectedSection.ID)
                                return true;
                            return TreeQuery(st.Name,searchString,out highlightData);
                        })
                        .Select(st => GetResultsFromSection(st, lastSelectedNotebook, highlightData))
                        .ToList();

                    case 4://Searching pages in a section
                        searchString = searchStrings[3];

                        if(!NotebookValidCheck(searchStrings[1]))
                            return new List<Result>();

                        if(!SectionValidCheck(searchStrings[2]))
                            return new List<Result>();

                        if(string.IsNullOrWhiteSpace(searchString))
                            return lastSelectedSection.Pages.Select(pg => GetResultFromPage(pg, lastSelectedSection, lastSelectedNotebook)).ToList();

                        return lastSelectedSection.Pages.Where(pg => TreeQuery(pg.Name,searchString,out highlightData))
                        .Select(pg => GetResultFromPage(pg, lastSelectedSection, lastSelectedNotebook, highlightData))
                        .ToList();

                    default:
                        break;
                }
            }
            return OneNoteProvider.FindPages(query.Search)
                .Select(page => GetResultFromPage(page, context.API.FuzzySearch(query.Search, page.Name).MatchData))
                .ToList();

            bool TreeQuery(string itemName, string searchString, out List<int> highlightData)
            {
                var matchResult = context.API.FuzzySearch(searchString, itemName);
                highlightData = matchResult.MatchData;
                return matchResult.IsSearchPrecisionScoreMet();
            }
        }

        private bool NotebookValidCheck(string notebookName)
        {
            if(lastSelectedNotebook == null)
            {
                var notebook = OneNoteProvider.NotebookItems.FirstOrDefault(nb => nb.Name == notebookName);
                if (notebook == null)
                {
                    return false;
                }
                lastSelectedNotebook = notebook;
                return true;
            }
            return true;
        }

        private bool SectionValidCheck(string sectionName)
        {
            if(lastSelectedSection == null) //Check if section is valid
            {
                var section = lastSelectedNotebook.Sections.FirstOrDefault(st => st.Name == sectionName);
                if (section == null)
                {
                    return false;
                }
                lastSelectedSection = section;
                return true;
            }
            return true;
        }


        private Result GetResultFromPage(IOneNoteExtPage page, List<int> highlightingData)
        {
            return GetResultFromPage(page, page.Section, page.Notebook, highlightingData);
        }

        private Result GetResultFromPage(IOneNotePage page, IOneNoteSection section, IOneNoteNotebook notebook, List<int> highlightingData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index .. ^4].Replace("/", " > "); //"+4" is to remove the ".one" from the path
            return new Result
            {
                Title = page.Name,
                SubTitle = path,
                TitleToolTip = "Last Modified: " + page.LastModified,
                SubTitleToolTip = "Created: " + page.DateTime,
                IcoPath = logoPath,
                ContextData = page,
                TitleHighlightData = highlightingData,
                Action = c =>
                {
                    lastSelectedNotebook = null;
                    lastSelectedSection = null;
                    page.OpenInOneNote();
                    return true;
                },
            };
        }

        private Result GetResultsFromSection(IOneNoteExtSection section, IOneNoteExtNotebook notebook,List<int> highlightData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index .. ^(section.Name.Length+5)].Replace("/", " > "); //The "+5" is to remove the ".one" and "/" from the path
            return new Result
            {
                Title = section.Name,
                SubTitle = path, // + " | " + section.Pages.Count().ToString(),
                TitleHighlightData = highlightData,
                Action = c =>
                {
                    lastSelectedSection = section;
                    context.API.ChangeQuery($"on nb\\{lastSelectedNotebook.Name}\\{section.Name}\\");
                    return false;
                },
            };
        }

        private Result GetResultFromNotebook(IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            return new Result
            {
                Title = notebook.Name,
                //SubTitle = notebook.Sections.Count().ToString(),
                //SubTitle = notebook..?.ToString(),
                //IcoPath = GetNotebookImage(notebook.Color),
                TitleHighlightData = highlightData,
                Action = c =>
                {
                    lastSelectedNotebook = notebook;
                    context.API.ChangeQuery($"on nb\\{notebook.Name}\\");
                    return false;
                },
            };
        }
        
        // private List<int> GetHighlightData(Query query, string stringToCheck,int searchTermsStartIndex = 0, int stringToCheckIndex = 0)
        // {
        //     List<int> highlightData = new();
        //     for (int i = searchTermsStartIndex; i < query.SearchTerms.Length; i++)
        //     {
        //         string searchTerm = query.SearchTerms[i];
        //         var index = stringToCheck.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase);
        //         if (index != -1)
        //         {
        //             for (int j = 0 + stringToCheckIndex; j < searchTerm.Length + stringToCheckIndex; j++)
        //             {
        //                 highlightData.Add(index + j);
        //             }
        //         }
        //     }
        //     return highlightData;
        // }

        //TODO: Image of notebook and section then dune
        //Create Image
        //Double check how to colour of image in C# and wee gooooooooooooooooooooooooooooood to gos
        //Change Hue depending on colour and set Value to 100 for all pixels that arent transparent
        // private string CreateCacheImage(string name)
        // {
        //     using (var bitmap = new Bitmap(IMG_SIZE, IMG_SIZE))
        //     using (var graphics = Graphics.FromImage(bitmap))
        //     {
        //         var color = ColorTranslator.FromHtml(name);
        //         graphics.Clear(color);


        //         var path = Path.Combine(ColorsDirectory.FullName, name+".png");
        //         bitmap.Save(path, ImageFormat.Png);
        //         return path;
        //     }
        // }
    }
}