using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNotePage : IOneNoteItem
    {
        internal OneNotePage() { }
        internal OneNotePage(XElement element, OneNoteSection parent)
        {
            Section = parent;
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "ID":
                        ID = attribute.Value;
                        break;
                    case "name":
                        Name = attribute.Value;
                        break;
                    case "dateTime":
                        Created = (DateTime)attribute;
                        break;
                    case "lastModifiedTime":
                        LastModified = (DateTime)attribute;
                        break;
                    case "pageLevel":
                        Level = (int)attribute;
                        break;
                    case "isUnread":
                        IsUnread = (bool)attribute;
                        break;
                    case "isInRecycleBin":
                        IsInRecycleBin = (bool)attribute;
                        break;
                }
            }
            RelativePath = $"{parent.RelativePath}{OneNoteParser.RelativePathSeparator}{Name}";
        }
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public DateTime LastModified { get; init; }
        public string RelativePath { get; init; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Enumerable.Empty<IOneNoteItem>();
        IOneNoteItem IOneNoteItem.Parent => Section;
        public OneNoteSection Section { get; init; }
        /// <summary>
        /// The page level.
        /// </summary>
        public int Level { get; init; }
        /// <summary>
        /// The time when the page was created.
        /// </summary>
        public DateTime Created { get; init; }
        /// <summary>
        /// Is the page in the recycle bin.
        /// </summary>
        public bool IsInRecycleBin { get; init; }
    }
}