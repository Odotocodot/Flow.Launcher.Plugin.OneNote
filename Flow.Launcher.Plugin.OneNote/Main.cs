using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNote : IPlugin 
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly string logoIconPath = "Images/logo.png";
        private readonly string unavailableIconPath = "Images/unavailable.png";
        private readonly string syncIconPath = "Images/refresh.png";

        private IOneNoteExtNotebook lastSelectedNotebook;
        private IOneNoteExtSection lastSelectedSection;

        private ItemInfo notebookInfo;
        private ItemInfo sectionInfo;

        private class ItemInfo
        {
            public Dictionary<Color, string> icons;
            public DirectoryInfo iconDirectory;
            public readonly string baseIconPath;

            public ItemInfo(string folderName, string iconName, PluginInitContext context)
            {
                this.icons = new Dictionary<Color, string>();
                this.iconDirectory = Directory.CreateDirectory(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, folderName));
                this.baseIconPath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory,"Images/"+iconName);
                foreach (var fileInfo in iconDirectory.GetFiles())
                {
                    if(int.TryParse(fileInfo.Name, out int argb))
                        icons.Add(Color.FromArgb(argb), fileInfo.FullName);
                }
            }
        }

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

            notebookInfo = new ItemInfo("NotebookIcons","notebook.png",context);
            sectionInfo = new ItemInfo("SectionIcons","section.png",context);
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
                    SubTitle = "Type \"nb\\\" to search by notebook structure or select this option",
                    IcoPath = logoIconPath,
                    AutoCompleteText = "Type something",
                    Action = c =>
                    {
                        context.API.ChangeQuery($"on nb\\");
                        return false;
                    },
                    Score = int.MaxValue,
                });

                //results.AddRange(OneNoteProvider.NotebookItems.Select(nb => GetResultFromNotebook(nb)));
                //Unsure if this actually works.
                results.Add(new Result
                {
                    Title = "Sync Notebooks",
                    IcoPath = syncIconPath,
                    Score = int.MinValue,
                    Action = c =>
                    {
                        OneNoteProvider.NotebookItems.Sync();
                        return false;
                    }
                });
                return results;
            }

            //Search via notebook structure
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

                        if(!ValidateNotebook(searchStrings[1]))
                            return new List<Result>();

                        if(string.IsNullOrWhiteSpace(searchString))
                        {
                            lastSelectedSection = null;
                            return lastSelectedNotebook.Sections.Select(s => GetResultsFromSection(s,lastSelectedNotebook)).ToList();
                        }
                        return lastSelectedNotebook.Sections.Where(s => 
                        {
                            if(lastSelectedSection != null && s.ID == lastSelectedSection.ID)
                                return true;
                            return TreeQuery(s.Name,searchString,out highlightData);
                        })
                        .Select(s => GetResultsFromSection(s, lastSelectedNotebook, highlightData))
                        .ToList();

                    case 4://Searching pages in a section
                        searchString = searchStrings[3];

                        if(!ValidateNotebook(searchStrings[1]))
                            return new List<Result>();

                        if(!ValidateSection(searchStrings[2]))
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
        
            //Default search
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

        private bool ValidateNotebook(string notebookName)
        {
            if(lastSelectedNotebook == null)
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
            if(lastSelectedSection == null) //Check if section is valid
            {
                var section = lastSelectedNotebook.Sections.FirstOrDefault(s => s.Name == sectionName);
                if (section == null)
                    return false;
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
                TitleToolTip = "Created: " + page.DateTime + "\nLast Modified: " + page.LastModified,
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

        private Result GetResultsFromSection(IOneNoteExtSection section, IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index .. ^(section.Name.Length+5)].Replace("/", " > "); //The "+5" is to remove the ".one" and "/" from the path
            return new Result
            {
                Title = section.Name,
                SubTitle = path, // + " | " + section.Pages.Count().ToString(),
                TitleHighlightData = highlightData,
                IcoPath = GetIcon(section.Color.Value,sectionInfo),//GetSectionIcon(section.Color.Value),
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
                IcoPath = GetIcon(notebook.Color.Value, notebookInfo),
                TitleHighlightData = highlightData,
                Action = c =>
                {
                    lastSelectedNotebook = notebook;
                    context.API.ChangeQuery($"on nb\\{notebook.Name}\\");
                    return false;
                },
            };
        }
 
        private string GetIcon(Color color, ItemInfo item)
        {
            if (!item.icons.TryGetValue(color, out string path))
            {
                //Create Colored Image
                using (var bitmap = new Bitmap(item.baseIconPath))
                    {
                        BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

                        int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                        byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];
                        IntPtr pointer = bitmapData.Scan0;
                        Marshal.Copy(pointer, pixels, 0, pixels.Length);
                        int bytesWidth = bitmapData.Width * bytesPerPixel;

                        for (int j = 0; j < bitmapData.Height; j++)
                        {
                            int line = j * bitmapData.Stride;
                            for (int i = 0; i < bytesWidth; i = i + bytesPerPixel)
                            {
                                pixels[line + i] = color.B;
                                pixels[line + i + 1] = color.G;
                                pixels[line + i + 2] = color.R;
                            }
                        }

                        Marshal.Copy(pixels, 0, pointer, pixels.Length);
                        bitmap.UnlockBits(bitmapData);
                        path = Path.Combine(item.iconDirectory.FullName, color.ToArgb() + ".png");
                        bitmap.Save(path, ImageFormat.Png);
                    }
                item.icons.Add(color, path);
            }
            return path;
        }
    }
}