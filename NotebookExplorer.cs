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

        private IOneNoteExtNotebook LastSelectedNotebook { get => oneNotePlugin.lastSelectedNotebook; set => oneNotePlugin.lastSelectedNotebook = value; }
        private IOneNoteExtSection LastSelectedSection { get => oneNotePlugin.lastSelectedSection; set => oneNotePlugin.lastSelectedSection = value; }

        private ResultCreator rc;

        private List<Result> NoResults => new List<Result>();
        
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
                        LastSelectedNotebook = null;
                        return OneNoteProvider.NotebookItems.Select(nb => rc.CreateNotebookResult(nb)).ToList();
                    }

                    return OneNoteProvider.NotebookItems.Where(nb =>
                    {
                        if (LastSelectedNotebook != null && nb.ID == LastSelectedNotebook.ID)
                            return true;

                        return TreeQuery(nb.Name, searchString, out highlightData);
                    })
                    .Select(nb =>  rc.CreateNotebookResult(nb, highlightData))
                    .ToList();

                case 3://Full query for section not complete e.g nb\User Notebook\Happine
                    searchString = searchStrings[2];

                    if (!ValidateNotebook(searchStrings[1]))
                        return NoResults;

                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        LastSelectedSection = null;
                        return LastSelectedNotebook.Sections.Where(s => !s.Encrypted)
                            .Select(s => rc.CreateSectionResult(s, LastSelectedNotebook))
                            .ToList();
                    }
                    return LastSelectedNotebook.Sections.Where(s =>
                    {
                        if(s.Encrypted)
                            return false;
                            
                        if (LastSelectedSection != null && s.ID == LastSelectedSection.ID)
                            return true;

                        return TreeQuery(s.Name, searchString, out highlightData);
                    })
                    .Select(s => rc.CreateSectionResult(s, LastSelectedNotebook, highlightData))
                    .ToList();

                case 4://Searching pages in a section
                    searchString = searchStrings[3];

                    if (!ValidateNotebook(searchStrings[1]))
                        return NoResults;

                    if (!ValidateSection(searchStrings[2]))
                        return NoResults;

                    if (string.IsNullOrWhiteSpace(searchString))
                        return LastSelectedSection.Pages.Select(pg => rc.CreatePageResult(pg,LastSelectedSection, LastSelectedNotebook)).ToList();

                    return LastSelectedSection.Pages.Where(pg => TreeQuery(pg.Name, searchString, out highlightData))
                    .Select(pg => rc.CreatePageResult(pg,LastSelectedSection, LastSelectedNotebook, highlightData))
                    .ToList();

                default:
                    return NoResults;
            }
                
        }
    
        private bool ValidateNotebook(string notebookName)
        {
            if (LastSelectedNotebook == null)
            {
                var notebook = OneNoteProvider.NotebookItems.FirstOrDefault(nb => nb.Name == notebookName);
                if (notebook == null)
                    return false;
                LastSelectedNotebook = notebook;
                return true;
            }
            return true;
        }

        private bool ValidateSection(string sectionName)
        {
            if (LastSelectedSection == null) //Check if section is valid
            {
                var section = LastSelectedNotebook.Sections.FirstOrDefault(s => s.Name == sectionName);
                if (section == null || section.Encrypted)
                    return false;
                LastSelectedSection = section;
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