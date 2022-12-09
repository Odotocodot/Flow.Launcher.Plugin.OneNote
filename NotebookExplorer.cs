using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public static class NotebookExplorer
    {
        public static List<Result> Explore(Query query)
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
                        OneNote.LastSelectedNotebook = null;
                        return OneNoteProvider.NotebookItems.Select(nb => nb.CreateResult()).ToList();
                    }

                    return OneNoteProvider.NotebookItems.Where(nb =>
                    {
                        if (OneNote.LastSelectedNotebook != null && nb.ID == OneNote.LastSelectedNotebook.ID)
                            return true;
                        return TreeQuery(nb.Name, searchString, out highlightData);
                    })
                    .Select(nb => nb.CreateResult(highlightData))
                    .ToList();

                case 3://Full query for section not complete e.g nb\User Notebook\Happine
                    searchString = searchStrings[2];

                    if (!ValidateNotebook(searchStrings[1]))
                        return new List<Result>();

                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        OneNote.LastSelectedSection = null;
                        return OneNote.LastSelectedNotebook.Sections.Select(s => s.CreateResult(OneNote.LastSelectedNotebook)).ToList();
                    }
                    return OneNote.LastSelectedNotebook.Sections.Where(s =>
                    {
                        if (OneNote.LastSelectedSection != null && s.ID == OneNote.LastSelectedSection.ID)
                            return true;
                        return TreeQuery(s.Name, searchString, out highlightData);
                    })
                    .Select(s => s.CreateResult(OneNote.LastSelectedNotebook, highlightData))
                    .ToList();

                case 4://Searching pages in a section
                    searchString = searchStrings[3];

                    if (!ValidateNotebook(searchStrings[1]))
                        return new List<Result>();

                    if (!ValidateSection(searchStrings[2]))
                        return new List<Result>();

                    if (string.IsNullOrWhiteSpace(searchString))
                        return OneNote.LastSelectedSection.Pages.Select(pg => pg.CreateResult(OneNote.LastSelectedSection, OneNote.LastSelectedNotebook)).ToList();

                    return OneNote.LastSelectedSection.Pages.Where(pg => TreeQuery(pg.Name, searchString, out highlightData))
                    .Select(pg => pg.CreateResult(OneNote.LastSelectedSection, OneNote.LastSelectedNotebook, highlightData))
                    .ToList();

                default:
                    return new List<Result>();
            }
                
        }
    
        private static bool ValidateNotebook(string notebookName)
        {
            if (OneNote.LastSelectedNotebook == null)
            {
                var notebook = OneNoteProvider.NotebookItems.FirstOrDefault(nb => nb.Name == notebookName);
                if (notebook == null)
                    return false;
                OneNote.LastSelectedNotebook = notebook;
                return true;
            }
            return true;
        }

        private static bool ValidateSection(string sectionName)
        {
            if (OneNote.LastSelectedSection == null) //Check if section is valid
            {
                var section = OneNote.LastSelectedNotebook.Sections.FirstOrDefault(s => s.Name == sectionName);
                if (section == null)
                    return false;
                OneNote.LastSelectedSection = section;
                return true;
            }
            return true;
        }
        private static bool TreeQuery(string itemName, string searchString, out List<int> highlightData)
        {
            var matchResult = OneNote.Context.API.FuzzySearch(searchString, itemName);
            highlightData = matchResult.MatchData;
            return matchResult.IsSearchPrecisionScoreMet();
        }
    }
}