# Changelog

## 2.1.2 - 2025-6-11

- Added the ability to open items in a new OneNote window using the context menu.([#28](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/28))

## 2.1.1 - 2025-1-4

### Changes

- Indicated Windows/Microsoft Search is required for default and scoped search in readme. ([#24](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/24))
- Added some badges.
- Updated OneNote page result tooltips.
- Changed recent pages default keyword from `rcntpgs:` to `rp:`.

### Fixes

- Fixed spelling and grammar mistakes.

## 2.1.0 - 2024-6-24

### Added

- New and improved icons.
- New preview panel for creating pages ([#20](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/20))
- New setting for icon theme: FL Default (matches Flow Launcher's theme), light, dark and color.
- Opening a hierarchy item from the plugin now always brings OneNote to the front.
- New Hotkey (<kbd>Ctrl</kbd> + <kbd>⏎ Enter</kbd>) to create new items without opening them in OneNote.

### Changed

- Refactored icon generation.
- Refactored settings view.
- Changed Linq2OneNote library reference from submodule to NuGet package.
- Updated tooltips for notebooks, section groups, sections and pages.

### Fixes

- Fixed incorrect autocomplete text for creating new items.

## 2.0.1 - 2023-10-09

### Added

- OneNote is now opened asynchronously to prevent blocking UI ([#15](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/15))

## 2.0.0 - 2023-10-05

### **Breaking Changes**

- Now requires Flow Launcher version 1.16 or later.

### Added

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

### Changed

- Compressed images.
- Reduced the calls to create a OneNote COM object, this should lead to an overall smoother experience.
- Updated to .NET 7 (update Flow Launcher if an error persists).
- Refactored the majority of code and project structure.

### Removed

- [Scipbe.Common.Office.OneNote](https://github.com/scipbe/ScipBe-Common-Office) package reference.

## 1.1.1 - 2023-03-05

### Fixes

- Fixed crash on search ([#7](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/7))

## 1.1.0 - 2023-03-04

### Added

- Added the ability to create notebooks, sections and pages.

### Changes

- Improved the readme

### Fixes

- Fixed typos

## 1.0.2 - 2023-02-28

### Fixes

- Fixed crash due to encrypted sections ([#3](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/3), [#4](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/4))

## 1.0.1 - 2023-01-15

### Fixes

- Fixed crash on invalid search ([#1](https://github.com/Odotocodot/Flow.Launcher.Plugin.OneNote/issues/1))

## 1.0.0 - 2022-12-16

Initial Release
