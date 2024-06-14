### 2.1.0 - 2024-6-14
#### Added
- New and improved icons.
- New preview panel for creating pages (Closes [#20](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/20))
- New setting for icon theme: system, light, dark and color.

#### Changed
- Refactored icon generation.
- Refactored settings view.

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
