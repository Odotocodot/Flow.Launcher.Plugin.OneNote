using System;
using System.Windows.Input;
using Flow.Launcher.Plugin.OneNote.Icons;
using LinqToOneNote;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
	public class NewOneNotePageViewModel : Model
	{
		private string? pageTitle = string.Empty;
		private string pageContent = string.Empty;
		private readonly Section? section;
		private readonly PluginInitContext context;

		public NewOneNotePageViewModel(PluginInitContext context, Section? section, string? pageTitle)
		{
			this.context = context;
			this.section = section;
			PageTitle = pageTitle;
			CreateCommand = new RelayCommand(() => CreatePage(false));
			CreateAndOpenCommand = new RelayCommand(() => CreatePage(true));
		}

		private void CreatePage(bool openImmediately)
		{
			Page page;
			if (section == null)
			{
				OneNoteApp.CreateQuickNote(PageTitle, out page);
			}
			else
			{
				page = OneNoteApp.CreatePage(section, PageTitle);
			}
			var xmlWrap = $"""
						<one:Outline>
							<one:Position x="36.0" y="86.4000015258789" z="0"/>
							<one:Size width="72.0" height="13.42771339416504"/>
							<one:OEChildren>
								<one:OE alignment="left">
									<one:T>
										<![CDATA[{PageContent}]]>
									</one:T>
								</one:OE>
							</one:OEChildren>
						</one:Outline>
						""";
			var pageContentXml = page.GetPageContent();
			pageContentXml = pageContentXml.Insert(pageContentXml.IndexOf("</one:Page>", StringComparison.Ordinal), xmlWrap);
			OneNoteApp.UpdatePageContent(pageContentXml);
			context.API.ReQuery();
			if (openImmediately)
			{
				page.Open();
				context.API.HideMainWindow();
				WindowHelper.FocusOneNote();
			}
			else
			{
				context.API.ShowMsg("Page Created in OneNote", 
					$"Title: {PageTitle}", 
					$"{context.CurrentPluginMetadata.PluginDirectory}/{IconProvider.Logo}");
			}
		}
		
		public string? PageTitle
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