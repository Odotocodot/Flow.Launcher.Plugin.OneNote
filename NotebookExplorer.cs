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
                    return GetNotebooks(searchStrings);

                case 3://Full query for section not complete e.g nb\User Notebook\Happine
                    return GetSections(searchStrings);

                case 4://Searching pages in a section
                    return GetPages(searchStrings);

                default:
                    return new List<Result>();
            }
        }

        private List<Result> GetNotebooks(string[] searchStrings)
        {
            List<Result> results = new List<Result>();
            string query = searchStrings[1];

            if (string.IsNullOrWhiteSpace(query)) // Do a normal notebook search
            {
                LastSelectedNotebook = null;
                results = OneNoteProvider.NotebookItems.Select(nb => rc.CreateNotebookResult(nb)).ToList();
                return results;
            }
            List<int> highlightData = null;

            results = OneNoteProvider.NotebookItems.Where(nb =>
            {
                if (LastSelectedNotebook != null && nb.ID == LastSelectedNotebook.ID)
                    return true;

                return TreeQuery(nb.Name, query, out highlightData);
            })
            .Select(nb => rc.CreateNotebookResult(nb, highlightData))
            .ToList();

            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(rc.CreateNewNotebookResult(query));
            }
            return results;
        }

        private List<Result> GetSections(string[] searchStrings)
        {
            List<Result> results = new List<Result>();

            string query = searchStrings[2];

            if (!ValidateNotebook(searchStrings[1]))
                return results;

            if (string.IsNullOrWhiteSpace(query))
            {
                LastSelectedSection = null;
                results = LastSelectedNotebook.Sections.Where(s => !s.Encrypted)
                    .Select(s => rc.CreateSectionResult(s, LastSelectedNotebook))
                    .ToList();

                //if no sections show ability to create section
                if (!results.Any())
                {
                    results.Add(new Result
                    {
                        Title = "Create section: \"\"",
                        SubTitle = "No (unecrypted) sections found. Type a valid title to create one",
                        IcoPath = Icons.NewSection
                    });
                }
                return results;
            }

            List<int> highlightData = null;

            results = LastSelectedNotebook.Sections.Where(s =>
            {
                if (s.Encrypted)
                    return false;

                if (LastSelectedSection != null && s.ID == LastSelectedSection.ID)
                    return true;

                return TreeQuery(s.Name, query, out highlightData);
            })
            .Select(s => rc.CreateSectionResult(s, LastSelectedNotebook, highlightData))
            .ToList();
            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(rc.CreateNewSectionResult(LastSelectedNotebook, query));
            }
            return results;
        }
        
        private List<Result> GetPages(string[] searchStrings)
        {
            List<Result> results = new List<Result>();

            string query = searchStrings[3];

            if (!ValidateNotebook(searchStrings[1]))
                return results;

            if (!ValidateSection(searchStrings[2]))
                return results;

            if (string.IsNullOrWhiteSpace(query))
            {
                results = LastSelectedSection.Pages.Select(pg => rc.CreatePageResult(pg, LastSelectedSection, LastSelectedNotebook)).ToList();
                //if no sections show ability to create section
                if (!results.Any())
                {
                    results.Add(new Result
                    {
                        Title = "Create page: \"\"",
                        SubTitle = "No pages found. Type a valid title to create one",
                        IcoPath = Icons.NewPage
                    });
                }
                return results;
            }

            List<int> highlightData = null;

            results =  LastSelectedSection.Pages.Where(pg => TreeQuery(pg.Name, query, out highlightData))
            .Select(pg => rc.CreatePageResult(pg, LastSelectedSection, LastSelectedNotebook, highlightData))
            .ToList();
            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(rc.CreateNewPageResult(LastSelectedSection, LastSelectedNotebook, query));
            }
            return results;
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