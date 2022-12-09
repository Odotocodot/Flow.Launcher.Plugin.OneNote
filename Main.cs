using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNote : IPlugin, IContextMenu
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly string logoIconPath = "Images/logo.png";
        private readonly string unavailableIconPath = "Images/unavailable.png";
        private readonly string syncIconPath = "Images/refresh.png";
        private readonly string recentIconPath = "Images/recent.png";
        private readonly string recentPageIconPath = "Images/recent_page.png";

        private readonly string structureKeyword = "nb:\\";
        private readonly string recentKeyword = "rcntpgs:";
        private readonly int recentPagesCount = 5;

        private IOneNoteExtNotebook lastSelectedNotebook;
        private IOneNoteExtSection lastSelectedSection;

        private OneNoteItemInfo notebookInfo;
        private OneNoteItemInfo sectionInfo;

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
                return;
            }
            
            notebookInfo = new OneNoteItemInfo("NotebookIcons", "notebook.png", context);
            sectionInfo = new OneNoteItemInfo("SectionIcons", "section.png", context);
        }

        public List<Result> Query(Query query)
        {
            if (!hasOneNote)
            {
                return new List<Result>()
                {
                    new Result
                    {
                        Title = "OneNote is not installed.",
                        IcoPath = unavailableIconPath
                    }
                };
            }
            if (string.IsNullOrEmpty(query.Search))
            {
                var results = new List<Result>();
                results.Add(new Result
                {
                    Title = "Search OneNote pages",
                    SubTitle = $"Type \"{structureKeyword}\" to search by notebook structure or select this option",
                    AutoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {structureKeyword}",
                    IcoPath = logoIconPath,
                    Score = 2000,
                    Action = c =>
                    {
                        context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {structureKeyword}");
                        return false;
                    },
                });
                results.Add(new Result
                {
                    Title = "See recent pages",
                    SubTitle = $"Type \"{recentKeyword}\" to see last modified pages or select this option",
                    AutoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {recentKeyword}",
                    IcoPath = recentIconPath,
                    Score = -1000,
                    Action = c =>
                    {
                        context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {recentKeyword}");
                        return false;
                    },
                });
                results.Add(new Result
                {
                    Title = "Open and sync notebooks",
                    IcoPath = syncIconPath,
                    Score = int.MinValue,
                    Action = c =>
                    {
                        OneNoteProvider.PageItems.First().OpenInOneNote();
                        OneNoteProvider.NotebookItems.Sync();
                        return false;
                    }
                });
                return results;
            }
            if (query.FirstSearch.StartsWith(recentKeyword))
            {
                int count = recentPagesCount;
                if (query.FirstSearch.Length > recentKeyword.Length && int.TryParse(query.FirstSearch[recentKeyword.Length..], out int userChosenCount))
                    count = userChosenCount;
                   
                return OneNoteProvider.PageItems.OrderByDescending(pg => pg.LastModified)
                    .Take(count)
                    .Select(pg =>
                    {
                        Result result = CreatePageResult(pg, null);
                        result.SubTitle = $"{GetLastEdited(DateTime.Now - pg.LastModified)}\t{result.SubTitle}";
                        result.IcoPath = recentPageIconPath;
                        return result;
                    })
                    .ToList();
            }

            //Search via notebook structure
            //NOTE: There is no nested sections i.e. there is nothing for the Section Group in the structure 
            if (query.FirstSearch.StartsWith(structureKeyword))
            {
                string[] searchStrings = query.Search.Split('\\', StringSplitOptions.None);
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
                            return OneNoteProvider.NotebookItems.Select(nb => CreateNotebookResult(nb)).ToList();
                        }

                        return OneNoteProvider.NotebookItems.Where(nb =>
                        {
                            if (lastSelectedNotebook != null && nb.ID == lastSelectedNotebook.ID)
                                return true;
                            return TreeQuery(nb.Name, searchString, out highlightData);
                        })
                        .Select(nb => CreateNotebookResult(nb, highlightData))
                        .ToList();

                    case 3://Full query for section not complete e.g nb\User Notebook\Happine
                        searchString = searchStrings[2];

                        if (!ValidateNotebook(searchStrings[1]))
                            return new List<Result>();

                        if (string.IsNullOrWhiteSpace(searchString))
                        {
                            lastSelectedSection = null;
                            return lastSelectedNotebook.Sections.Select(s => CreateSectionResult(s, lastSelectedNotebook)).ToList();
                        }
                        return lastSelectedNotebook.Sections.Where(s =>
                        {
                            if (lastSelectedSection != null && s.ID == lastSelectedSection.ID)
                                return true;
                            return TreeQuery(s.Name, searchString, out highlightData);
                        })
                        .Select(s => CreateSectionResult(s, lastSelectedNotebook, highlightData))
                        .ToList();

                    case 4://Searching pages in a section
                        searchString = searchStrings[3];

                        if (!ValidateNotebook(searchStrings[1]))
                            return new List<Result>();

                        if (!ValidateSection(searchStrings[2]))
                            return new List<Result>();

                        if (string.IsNullOrWhiteSpace(searchString))
                            return lastSelectedSection.Pages.Select(pg => CreatePageResult(pg, lastSelectedSection, lastSelectedNotebook)).ToList();

                        return lastSelectedSection.Pages.Where(pg => TreeQuery(pg.Name, searchString, out highlightData))
                        .Select(pg => CreatePageResult(pg, lastSelectedSection, lastSelectedNotebook, highlightData))
                        .ToList();

                    default:
                        break;
                }
            }

            //Default search 
            return OneNoteProvider.FindPages(query.Search)
                .Select(page => CreatePageResult(page, context.API.FuzzySearch(query.Search, page.Name).MatchData))
                .ToList();

            bool TreeQuery(string itemName, string searchString, out List<int> highlightData)
            {
                var matchResult = context.API.FuzzySearch(searchString, itemName);
                highlightData = matchResult.MatchData;
                return matchResult.IsSearchPrecisionScoreMet();
            }
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            switch (selectedResult.ContextData)
            {
                case IOneNoteExtNotebook notebook:
                    return new List<Result>{new Result
                    {
                        Title = "Open and sync notebook",
                        SubTitle = notebook.Name,
                        IcoPath = notebookInfo.GetIcon(notebook.Color.Value),
                        Action = c =>
                        {
                            notebook.Sections.First().Pages
                                .OrderByDescending(pg => pg.LastModified)
                                .First()
                                .OpenInOneNote();
                            notebook.Sync();
                            return true;
                        }
                    }};
                case IOneNoteExtSection section:
                    return new List<Result>{
                    new Result
                    {
                        Title = "Open and sync section",
                        SubTitle = section.Name,
                        IcoPath = sectionInfo.GetIcon(section.Color.Value),
                        Action = c =>
                        {
                            section.Pages.OrderByDescending(pg => pg.LastModified)
                                .First()
                                .OpenInOneNote();
                            section.Sync();
                            return true;
                        }
                    },
                    new Result 
                    {
                        Title = "One and sync notebook",
                        SubTitle = lastSelectedNotebook.Name,
                        IcoPath = notebookInfo.GetIcon(lastSelectedNotebook.Color.Value),
                        Action = c =>
                        {
                            lastSelectedNotebook.Sections.First().Pages
                                .OrderByDescending(pg => pg.LastModified)
                                .First()
                                .OpenInOneNote();
                            lastSelectedNotebook.Sync();
                            return true;
                        }
                    }
                    };

                default:
                    return new List<Result>();
            }
        }

        private bool ValidateNotebook(string notebookName)
        {
            if (lastSelectedNotebook == null)
            {
                var notebook = OneNoteProvider.NotebookItems.FirstOrDefault(nb => nb.Name == notebookName);
                if (notebook == null)
                    return false;
                lastSelectedNotebook = notebook;
                return true;
            }
            return true;
        }

        private bool ValidateSection(string sectionName)
        {
            if (lastSelectedSection == null) //Check if section is valid
            {
                var section = lastSelectedNotebook.Sections.FirstOrDefault(s => s.Name == sectionName);
                if (section == null)
                    return false;
                lastSelectedSection = section;
                return true;
            }
            return true;
        }

        private Result CreatePageResult(IOneNoteExtPage page, List<int> highlightingData)
        {
            return CreatePageResult(page, page.Section, page.Notebook, highlightingData);
        }

        private Result CreatePageResult(IOneNotePage page, IOneNoteSection section, IOneNoteNotebook notebook, List<int> highlightingData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index..^4] //"+4" is to remove the ".one" from the path
                    .Replace("/", " > ")
                    .Replace("\\", " > "); 
            return new Result
            {
                Title = page.Name,
                SubTitle = path,
                TitleToolTip = $"Created: {page.DateTime}\nLast Modified: {page.LastModified}",
                SubTitleToolTip = path,
                IcoPath = logoIconPath,
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

        private Result CreateSectionResult(IOneNoteExtSection section, IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index..^(section.Name.Length + 5)] //The "+5" is to remove the ".one" and "/" from the path
                    .Replace("/", " > ")
                    .Replace("\\", " > "); 
            
            return new Result
            {
                Title = section.Name,
                SubTitle = path, // + " | " + section.Pages.Count().ToString(),
                TitleHighlightData = highlightData,
                ContextData = section,
                IcoPath = sectionInfo.GetIcon(section.Color.Value),
                Action = c =>
                {
                    lastSelectedSection = section;
                    context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {structureKeyword}{lastSelectedNotebook.Name}\\{section.Name}\\");
                    return false;
                },
            };
        }

        private Result CreateNotebookResult(IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            return new Result
            {
                Title = notebook.Name,
                TitleHighlightData = highlightData,
                ContextData = notebook,
                IcoPath = notebookInfo.GetIcon(notebook.Color.Value),
                Action = c =>
                {
                    lastSelectedNotebook = notebook;
                    context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {structureKeyword}{notebook.Name}\\");
                    return false;
                },
            };
        }
        private static string GetLastEdited(TimeSpan diff)
        {
            string lastEdited = "Last editied ";
            if (PluralCheck(diff.TotalDays, "day", ref lastEdited) 
            || PluralCheck(diff.TotalHours, "hour", ref lastEdited) 
            || PluralCheck(diff.TotalMinutes, "min", ref lastEdited) 
            || PluralCheck(diff.TotalSeconds, "sec", ref lastEdited))
                return lastEdited;
            else
                return lastEdited += "Now.";
            bool PluralCheck(double totalTime, string timeType, ref string lastEdited)
            {
                var roundedTime = (int)Math.Round(totalTime);
                if(roundedTime > 0)
                {
                    string plural = roundedTime == 1 ? "" : "s";
                    lastEdited += $"{roundedTime} {timeType}{plural} ago.";
                    return true;
                }
                else
                    return false;

            }
        }
    }
}