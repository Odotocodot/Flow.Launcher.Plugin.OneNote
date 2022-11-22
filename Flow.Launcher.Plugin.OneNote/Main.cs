using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

//https://github.com/microsoft/PowerToys/blob/9b7a7f93b716afbbc476efaa2a57fb0f365b126b/src/modules/launcher/Plugins/Microsoft.PowerToys.Run.Plugin.OneNote/Main.cs
//Icon8
//Flaticon
//TODO: add open to use web only -> would need Microsoft.Graph async and Azure  account (for refereshing and keep an access token) nonsense
namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNote : IPlugin 
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly string logoIconPath = "Images/logo.png";
        private readonly string warningIconPath = "Images/warning.png";
        private readonly string syncIconPath = "Images/icons8-refresh-240.png";
        private readonly string notebookBaseIconPath = "Images/notebook.png";
        private readonly string sectionBaseIconPath = "Images/section.png";

        private IOneNoteExtNotebook lastSelectedNotebook;
        private IOneNoteExtSection lastSelectedSection;
        private DirectoryInfo notebookIconDirectory;
        private DirectoryInfo sectionIconDirectory;

        private Dictionary<Color,string> notebookIcons;
        private Dictionary<Color,string> sectionIcons;


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

            notebookIconDirectory = Directory.CreateDirectory(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "NotebookIcons"));
            sectionIconDirectory = Directory.CreateDirectory(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "SectionIcons"));
            notebookIcons = new Dictionary<Color, string>();
            sectionIcons = new Dictionary<Color, string>();

            foreach (var fileInfo in notebookIconDirectory.GetFiles())
            {
                if(int.TryParse(fileInfo.Name, out int argb))
                    notebookIcons.Add(Color.FromArgb(argb), fileInfo.FullName);
            }
            foreach (var fileInfo in sectionIconDirectory.GetFiles())
            {
                if(int.TryParse(fileInfo.Name, out int argb))
                    sectionIcons.Add(Color.FromArgb(argb), fileInfo.FullName);
            }

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
                        IcoPath = warningIconPath
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
                //This doesnt work probably remove it.
                results.Add(new Result
                {
                    Title = "Sync Notebooks",
                    IcoPath = syncIconPath,
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
                {
                    return false;
                }
                lastSelectedNotebook = notebook;
                return true;
            }
            return true;
        }

        private bool ValidateSection(string sectionName)
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
                IcoPath = GetSectionIcon(section.Color.Value),
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
                IcoPath = GetNotebookIcon(notebook.Color.Value),
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

        //https://stackoverflow.com/questions/24701703/c-sharp-faster-alternatives-to-setpixel-and-getpixel-for-bitmaps-for-windows-f
        private string GetNotebookIcon(Color color)
        {
            return GetColoredImaged(color,
                            Path.Combine(context.CurrentPluginMetadata.PluginDirectory, notebookBaseIconPath),
                            notebookIcons,
                            notebookIconDirectory);
            // if (!notebookIcons.TryGetValue(color, out string path))
            // {
            //     path = CreateColoredImage(color, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, notebookIconPath), notebookIconDirectory);
            //     notebookIcons.Add(color, path);
            // }
            // return path;
        }

        private string GetSectionIcon(Color color) => GetColoredImaged(color,
                                    Path.Combine(context.CurrentPluginMetadata.PluginDirectory, sectionBaseIconPath),
                                    sectionIcons,
                                    sectionIconDirectory);


        private string GetColoredImaged(Color color, string imageFileName, Dictionary<Color, string> iconsDictonary, DirectoryInfo directoryInfo)
        {
            if (!iconsDictonary.TryGetValue(color, out string path))
            {
                path = CreateColoredImage(color, imageFileName, directoryInfo);
                iconsDictonary.Add(color, path);
            }
            return path;
        }

        private string CreateColoredImage(Color color, string imageFileName, DirectoryInfo saveDirectory)
        {
            string path;
            using (var bitmap = new Bitmap(imageFileName))
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
                path = Path.Combine(saveDirectory.FullName, color.ToArgb() + ".png");
                bitmap.Save(path, ImageFormat.Png);
            }

            return path;
        }
    }
}