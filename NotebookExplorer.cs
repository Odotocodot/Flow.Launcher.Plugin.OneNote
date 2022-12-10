using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class NotebookExplorer
    {
        private PluginInitContext context;
        private OneNotePlugin oneNotePlugin;
        private ResultCreator rc;

        public NotebookExplorer(PluginInitContext context, OneNotePlugin oneNotePlugin, ResultCreator resultCreator)
        {
            this.context = context;
            this.oneNotePlugin = oneNotePlugin;
            rc = resultCreator;
        }

        public List<Result> Explore(Query query)
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
                        oneNotePlugin.lastSelectedNotebook = null;
                        return OneNoteProvider.NotebookItems.Select(nb => rc.CreateNotebookResult(nb)).ToList();
                    }

                    return OneNoteProvider.NotebookItems.Where(nb =>
                    {
                        if (oneNotePlugin.lastSelectedNotebook != null && nb.ID == oneNotePlugin.lastSelectedNotebook.ID)
                            return true;
                        return TreeQuery(nb.Name, searchString, out highlightData);
                    })
                    .Select(nb =>  rc.CreateNotebookResult(nb, highlightData))
                    .ToList();

                case 3://Full query for section not complete e.g nb\User Notebook\Happine
                    searchString = searchStrings[2];

                    if (!ValidateNotebook(searchStrings[1]))
                        return new List<Result>();

                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        oneNotePlugin.lastSelectedSection = null;
                        return oneNotePlugin.lastSelectedNotebook.Sections.Select(s => rc.CreateSectionResult(s,oneNotePlugin.lastSelectedNotebook)).ToList();
                    }
                    return oneNotePlugin.lastSelectedNotebook.Sections.Where(s =>
                    {
                        if (oneNotePlugin.lastSelectedSection != null && s.ID == oneNotePlugin.lastSelectedSection.ID)
                            return true;
                        return TreeQuery(s.Name, searchString, out highlightData);
                    })
                    .Select(s => rc.CreateSectionResult(s, oneNotePlugin.lastSelectedNotebook, highlightData))
                    .ToList();

                case 4://Searching pages in a section
                    searchString = searchStrings[3];

                    if (!ValidateNotebook(searchStrings[1]))
                        return new List<Result>();

                    if (!ValidateSection(searchStrings[2]))
                        return new List<Result>();

                    if (string.IsNullOrWhiteSpace(searchString))
                        return oneNotePlugin.lastSelectedSection.Pages.Select(pg => rc.CreatePageResult(pg,oneNotePlugin.lastSelectedSection, oneNotePlugin.lastSelectedNotebook)).ToList();

                    return oneNotePlugin.lastSelectedSection.Pages.Where(pg => TreeQuery(pg.Name, searchString, out highlightData))
                    .Select(pg => rc.CreatePageResult(pg,oneNotePlugin.lastSelectedSection, oneNotePlugin.lastSelectedNotebook, highlightData))
                    .ToList();

                default:
                    return new List<Result>();
            }
                
        }
    
        private bool ValidateNotebook(string notebookName)
        {
            if (oneNotePlugin.lastSelectedNotebook == null)
            {
                var notebook = OneNoteProvider.NotebookItems.FirstOrDefault(nb => nb.Name == notebookName);
                if (notebook == null)
                    return false;
                oneNotePlugin.lastSelectedNotebook = notebook;
                return true;
            }
            return true;
        }

        private bool ValidateSection(string sectionName)
        {
            if (oneNotePlugin.lastSelectedSection == null) //Check if section is valid
            {
                var section = oneNotePlugin.lastSelectedNotebook.Sections.FirstOrDefault(s => s.Name == sectionName);
                if (section == null)
                    return false;
                oneNotePlugin.lastSelectedSection = section;
                return true;
            }
            return true;
        }
        private bool TreeQuery(string itemName, string searchString, out List<int> highlightData)
        {
            var matchResult = context.API.FuzzySearch(searchString, itemName);
            highlightData = matchResult.MatchData;
            return matchResult.IsSearchPrecisionScoreMet();
        }
    }
}