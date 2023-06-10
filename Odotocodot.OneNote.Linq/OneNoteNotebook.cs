using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteNotebook : IOneNoteItem
    {
        internal OneNoteNotebook() { }
        internal OneNoteNotebook(XElement element) : this(element,true) { }
        internal OneNoteNotebook(XElement element, bool addChildren)
        {
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "name":
                        Name = attribute.Value;
                        break;
                    case "nickname":
                        NickName = attribute.Value;
                        break;
                    case "ID":
                        ID = attribute.Value;
                        break;
                    case "path":
                        Path = attribute.Value;
                        break;
                    case "lastModifiedTime":
                        LastModified = (DateTime)attribute;
                        break;
                    case "color":
                        Color = attribute.Value != "none" ? ColorTranslator.FromHtml(attribute.Value) : null;
                        break;
                    case "isUnread":
                        IsUnread = (bool)attribute;
                        break;
                }
            }

            if(addChildren)
            {
                Sections = element.Elements(OneNoteParser.GetXName(OneNoteItemType.Section))
                                  .Select(e => new OneNoteSection(e, this));

                SectionGroups = element.Elements(OneNoteParser.GetXName(OneNoteItemType.SectionGroup))
                                       .Select(e => new OneNoteSectionGroup(e, this));
            }
        }
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public DateTime LastModified { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.Notebook;
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => ((IEnumerable<IOneNoteItem>)Sections).Concat(SectionGroups);
        IOneNoteItem IOneNoteItem.Parent => null;
        /// <summary>
        /// Nickname of the notebook.
        /// </summary>
        public string NickName { get; init; }
        /// <summary>
        /// Full path of the notebook.
        /// </summary>
        public string Path { get; init; }
        /// <summary>
        /// Color of the notebook.
        /// </summary>
        public Color? Color { get; init; }
        /// <summary>
        /// Contains the direct children of a notebook, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<OneNoteSection> Sections { get; init; }
        public IEnumerable<OneNoteSectionGroup> SectionGroups { get; init; }
    }
}