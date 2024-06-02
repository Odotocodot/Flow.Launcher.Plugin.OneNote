### Unreleased

- [x] Add Icon Themes ( change the smelly OneNoteItemIcons class)
  - This involves doing up the icons.GetIcon method.
  - Needs a setting for icons 
  - Colored Icons takes precedence over the light and dark icons.
  - will need to check powerytoys for getting the imageSource/Bitmap source thing.
- [x] Fix clearing cached icons button
- [x] Implement PluginTheme setting
- [ ] Maybe use AsParallel for getting recent items [link](https://devblogs.microsoft.com/pfxteam/plinq-ordering/)
- [ ] Make static icon class non static icon provider
- [ ] refactor settings
- [ ] Add stuff to new page with title and discription see iseue.
#### Added
- New and improved icons.
- New setting for plugin theme: system, light, dark and color.

#### Changed
- Refactored icon generation.

#### Removed



<!-- omit from toc -->
### 2.0.0 - 2023-10-05

<!-- omit from toc -->
#### Added

- **[Created custom OneNote parser/library](https://github.com/Odotocodot/Linq2OneNote)**, adding the ability for several new features.
- Support for section groups when using the notebook explorer.
- Support for displaying unread results.
- Support for showing locked sections in results (you still can't see inside them unless they are unlocked).
- The ability to search by only title.
- The ability to do a scoped search (e.g. search in one section only).
- **Settings!** You can change these options:
    - Show unread icons.
    - Show encrypted sections.
    - Show recycle bin items.
    - Created coloured icons for notebook and sections.
    - Default number of recent pages
    - **Customisable keywords!**

<!-- omit from toc -->
#### Changed

- Compressed images.
- Reduced the calls to create a OneNote COM object, this should lead to a overall smoother experience.
- Updated to .NET 7 (update Flow Launcher if an error persists).
- Refactored majority of code and project structure.

<!-- omit from toc -->
#### Removed

- [Scipbe.Common.Office.OneNote](https://github.com/scipbe/ScipBe-Common-Office) package reference.
