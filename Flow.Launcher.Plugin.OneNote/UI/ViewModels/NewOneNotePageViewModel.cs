using System;
using System.Windows.Input;
using Flow.Launcher.Plugin.OneNote.Icons;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
	public class NewOneNotePageViewModel : Model
	{
		private string pageTitle;
		private string pageContent;
		private readonly OneNoteSection section;
		private readonly PluginInitContext context;

		public NewOneNotePageViewModel(PluginInitContext context, OneNoteSection section, string pageTitle)
		{
			this.context = context;
			this.section = section;
			PageTitle = pageTitle;
			CreateCommand = new RelayCommand(_ => CreatePage(false));
			CreateAndOpenCommand = new RelayCommand(_ => CreatePage(true));
		}

		private void CreatePage(bool openImmediately)
		{
			var id = OneNoteApplication.CreatePage(section, PageTitle, false);
			var page = (OneNotePage)OneNoteItemExtensions.FindByID(id);
			var pageContentXml = page.GetPageContent();
			var xmlWrap = $"<one:Outline><one:Position x=\"36.0\" y=\"86.4000015258789\" z=\"0\"/><one:Size width=\"72.0\" height=\"13.42771339416504\"/><one:OEChildren><one:OE alignment=\"left\"><one:T><![CDATA[{PageContent}]]></one:T></one:OE></one:OEChildren></one:Outline>";
			pageContentXml = pageContentXml.Insert(pageContentXml.IndexOf("</one:Page>", StringComparison.Ordinal), xmlWrap);
			OneNoteApplication.UpdatePageContent(pageContentXml);
			Main.ForceReQuery();
			if (openImmediately)
			{
				page.OpenInOneNote();
				context.API.HideMainWindow();
			}
			else
			{
				context.API.ShowMsg("Page Created in OneNote", 
					$"Title: {PageTitle}", 
					$"{context.CurrentPluginMetadata.PluginDirectory}/{IconProvider.Logo}");
			}
		}
		public string PageTitle
		{
			get => pageTitle;
			set => SetProperty(ref pageTitle, value);
		}

		public string PageContent
		{
			get => pageContent;
			set => SetProperty(ref pageContent, value);
		}

		public ICommand CreateCommand { get; }
		public ICommand CreateAndOpenCommand { get; }
	}
}