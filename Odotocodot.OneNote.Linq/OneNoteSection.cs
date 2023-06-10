using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSection : IOneNoteItem
    {
        internal OneNoteSection() { }
        internal OneNoteSection(XElement element, IOneNoteItem parent) : this(element, parent, true) { }
        internal OneNoteSection(XElement element, IOneNoteItem parent, bool addChildren)
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
                    case "isUnread":
                        IsUnread = (bool)attribute;
                        break;
                    case "color":
                        Color = attribute.Value != "none" ? ColorTranslator.FromHtml(attribute.Value) : null;
                        break;
                    case "lastModifiedTime":
                        LastModified = (DateTime)attribute;
                        break;
                    case "encrypted":
                        Encrypted = (bool)attribute;
                        break;
                    case "locked":
                        Locked = (bool)attribute;
                        break;
                    case "isInRecycleBin":
                        IsInRecycleBin = (bool)attribute;
                        break;
                    case "isDeletedPages":
                        IsDeletedPages = (bool)attribute;
                        break;
                }
            }
            if(addChildren)
            {
                Pages = element.Elements(OneNoteParser.GetXName(OneNoteItemType.Page))
                               .Select(e => new OneNotePage(e,this));
            }
        }

        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public DateTime LastModified { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.Section;
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Pages;
        public IOneNoteItem Parent { get; init; }
        /// <summary>
        /// Full path of the section.
        /// </summary>
        public string Path { get; init; }
        ///// <summary>
        ///// 
        ///// </summary>
        //public bool IsReadOnly { get; init; }
        /// <summary>
        /// Returns true if an encrypted section has been unlocked allowing access, otherwise false.
        /// </summary>
        public bool Locked { get; init; }
        /// <summary>
        /// Is the section encrypted.
        /// </summary>
        public bool Encrypted { get; init; }
        /// <summary>
        /// Is the section in the recycle bin.
        /// </summary>
        public bool IsInRecycleBin { get; init; }
        /// <summary>
        /// If true this is a special section that contains all the recently deleted pages in this sections notebook.
        /// </summary>
        public bool IsDeletedPages { get; init; }
        /// <summary>
        /// The color of the section.
        /// </summary>
        public Color? Color { get; init; }
        /// <summary>
        /// The collection of pages within this section, the same as <see cref="IOneNoteItem.Children"/> for a section.
        /// </summary>
        public IEnumerable<OneNotePage> Pages { get; init; }
    }
}