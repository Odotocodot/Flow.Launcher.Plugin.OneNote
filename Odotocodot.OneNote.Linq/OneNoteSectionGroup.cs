using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSectionGroup : IOneNoteItem
    {
        internal OneNoteSectionGroup() { }
        internal OneNoteSectionGroup(XElement element, IOneNoteItem parent) : this(element, parent, true) { }
        internal OneNoteSectionGroup(XElement element, IOneNoteItem parent, bool addChildren)
        {
            Parent = parent;
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "name":
                        Name = attribute.Value;
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
                    case "isUnread":
                        IsUnread = (bool)attribute;
                        break;
                    case "isRecycleBin":
                        IsRecycleBin = (bool)attribute;
                        break;
                }
            }

            if(addChildren)
            {
                Sections = element.Elements(OneNoteParser.GetXName<OneNoteSection>())
                                  .Select(e => new OneNoteSection(e, this));

                SectionGroups = element.Elements(OneNoteParser.GetXName<OneNoteSectionGroup>())
                                       .Select(e => new OneNoteSectionGroup(e, this)); 
            }
        }
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public DateTime LastModified { get; init; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => ((IEnumerable<IOneNoteItem>)Sections).Concat(SectionGroups);
        public IOneNoteItem Parent { get; init; }
        /// <summary>
        /// Full path of the section group.
        /// </summary>
        public string Path { get; init; }
        /// <summary>
        /// If true, this is a special section group which contains all the recently deleted sections as well as the "Deleted Pages" section, which contains all the recently deleted pages.
        /// </summary>
        public bool IsRecycleBin { get; init; }
        /// <summary>
        /// Contains the direct children of a section group, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<OneNoteSection> Sections { get; init; }
        public IEnumerable<OneNoteSectionGroup> SectionGroups { get; init; }
    }
}
