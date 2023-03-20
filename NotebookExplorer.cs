using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class NotebookExplorer
    {
        private PluginInitContext context;

        private IOneNoteExtNotebook currentNotebook;
        private IOneNoteExtSection currentSection;

        private ResultCreator rc;

        
        public NotebookExplorer(PluginInitContext context, ResultCreator resultCreator)
        {
            this.context = context;
            rc = resultCreator;
        }

        public List<Result> Explore(Query query)
        {
            string[] searchStrings = query.Search.Split('\\', StringSplitOptions.None);
            //Could replace switch case with for loop
            switch (searchStrings.Length)
            {
                case 2://Full query for notebook not complete e.g. nb\User Noteb
                       //Get matching notebooks and create results.
                    return GetNotebooks(searchStrings[1]);

                case 3://Full query for section not complete e.g nb\User Notebook\Happine
                    return GetSections(searchStrings[1],searchStrings[2]);

                case 4://Searching pages in a section
                    return GetPages(searchStrings[1],searchStrings[2],searchStrings[3]);

                default:
                    return new List<Result>();
            }
        }

        private List<Result> GetNotebooks(string query)
        {
            List<Result> results = new List<Result>();

            if (string.IsNullOrWhiteSpace(query)) // Do a normal notebook search
            {
                currentNotebook = null;
                results = OneNoteProvider.NotebookItems.Select(nb => GetResult(nb)).ToList();
                return results;
            }

            List<int> highlightData = null;


            results = OneNoteProvider.NotebookItems.Where(nb => TreeQuery(nb.Name, query, out highlightData))
                                                   .Select(nb => GetResult(nb, highlightData))
                                                   .ToList();

            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(rc.CreateNewNotebookResult(query));
            }
            return results;

            Result GetResult(IOneNoteExtNotebook nb, List<int> highlightData = null)
            {
                var result = rc.CreateNotebookResult(nb, highlightData);
                result.Action = c =>
                {
                    currentNotebook = nb;
                    context.API.ChangeQuery(result.AutoCompleteText);
                    return false;
                };
                return result;
            }
        }

        private List<Result> GetSections(string notebookName, string query)
        {
            List<Result> results = new List<Result>();

            if(!ValidateNotebook(notebookName))
                return results;

            if (string.IsNullOrWhiteSpace(query))
            {
                currentSection = null;
                results = currentNotebook.Sections.Where(s => !s.Encrypted)
                                           .Select(s => GetResult(s))
                                           .ToList();

                //if no sections show ability to create section
                if (!results.Any())
                {
                    results.Add(new Result
                    {
                        Title = "Create section: \"\"",
                        SubTitle = "No (unencrypted) sections found. Type a valid title to create one",
                        IcoPath = Icons.NewSection
                    });
                }
                return results;
            }

            List<int> highlightData = null;

            results = currentNotebook.Sections.Where(s =>
            {
                if (s.Encrypted)
                    return false;

                // if (LastSelectedSection != null && s.ID == LastSelectedSection.ID)
                //     return true;

                return TreeQuery(s.Name, query, out highlightData);
            })
            .Select(s => GetResult(s, highlightData))
            .ToList();

            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(rc.CreateNewSectionResult(currentNotebook, query));
            }
            return results;

            Result GetResult(IOneNoteExtSection s, List<int> highlightData = null)
            {
                var result = rc.CreateSectionResult(s, currentNotebook, highlightData);
                result.Action = c =>
                {
                    currentSection = s;
                    context.API.ChangeQuery(result.AutoCompleteText);
                    return false;
                };
                return result;
            }
        }
     
        private List<Result> GetPages(string notebookName, string sectionName, string query)
        {
            List<Result> results = new List<Result>();


            if (!ValidateNotebook(notebookName))
                return results;

            if (!ValidateSection(sectionName))
                return results;

            if (string.IsNullOrWhiteSpace(query))
            {
                results = currentSection.Pages.Select(pg => rc.CreatePageResult(pg, currentSection, currentNotebook)).ToList();
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

            results = currentSection.Pages.Where(pg => TreeQuery(pg.Name, query, out highlightData))
            .Select(pg => rc.CreatePageResult(pg, currentSection, currentNotebook, highlightData))
            .ToList();

            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(rc.CreateNewPageResult(currentSection, currentNotebook, query));
            }
            return results;
        }
        

        private bool ValidateNotebook(string notebookName)
        {
            if(currentNotebook == null)
            {
                currentNotebook = OneNoteProvider.NotebookItems.FirstOrDefault(nb => nb.Name == notebookName);
                return currentNotebook != null;
            }
            return currentNotebook.Name == notebookName;
            
        }


        

        private bool ValidateSection(string sectionName)
        {
            if(currentSection == null)
            {
                currentSection = currentNotebook.Sections.FirstOrDefault(s => s.Name == sectionName);
                return currentSection != null;
            }
            return currentSection.Name == sectionName;

        }
        private bool TreeQuery(string itemName, string searchString, out List<int> highlightData)
        {
            var matchResult = context.API.FuzzySearch(searchString, itemName);
            highlightData = matchResult.MatchData;
            return matchResult.IsSearchPrecisionScoreMet();
        }
    }
}