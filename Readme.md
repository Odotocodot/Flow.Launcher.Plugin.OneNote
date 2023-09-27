 <p align="center">
	<a href="https://flowlauncher.com">
		<img src="doc/flow.png" width=12.5%>
	</a>
	<a href="https://www.microsoft.com/en-gb/microsoft-365/onenote/digital-note-taking-app">
		<img src= "doc/onenote.png" width=12.5%>
	</a>
</p>
<h1 align="center">OneNote for Flow Launcher</h1>

A [OneNote](https://www.microsoft.com/en-gb/microsoft-365/onenote/digital-note-taking-app) plugin for the [Flow launcher](https://github.com/Flow-Launcher/Flow.Launcher), allowing for the ability to quickly access and create notes.

<!-- omit from toc -->
## Contents 
- [Installation](#installation)
- [Features](#features)
	- [At a glance](#at-a-glance)
	- [Default Search](#default-search)
	- [Notebook Explorer](#notebook-explorer)
		- [Create New Items](#create-new-items)
	- [Recent Pages](#recent-pages)
	- [Scoped Search](#scoped-search)
	- [Title Search](#title-search)
- [Settings](#settings)
	- [Keywords](#keywords)
- [2.0 Changelog](#20-changelog)
- [Acknowledgements](#acknowledgements)

## Installation
Using Flow Launcher type:
```
pm install OneNote
```
> [!IMPORTANT]
> Requires at least Flow Launcher version 1.16.

## Features
### At a glance
| Keyword                      | Name                                    | Description                  |
| ---------------------------- | --------------------------------------- | ---------------------------- |
| `` on {your search query} `` | [Default Search](#default-search)       | Search OneNote pages         |
| `` on nb:\ ``                | [Notebook Explorer](#notebook-explorer) | Navigate notebooks hierarchy |
| `` on rcntpgs: ``            | [Recent Pages](#recent-pages)           | View recently modified pages |


### Default Search
```
on {your search query}
```
This is allows you to search OneNote pages. The OneNote API searches both the content in a page as well as the page title.

> [!NOTE]
> You can include bitwise operators like `AND` or `OR` (they must be uppercase) in your search. E.g. `on hello there AND general kenobi`.

![default search gif](doc/) 

### Notebook Explorer

```
on nb:\
```
Transverse your OneNote notebooks explorer style.

- Press <kbd>⏎ Enter</kbd> or <kbd>⇥ Tab</kbd> or left-click on a result to auto complete the query.
- Press <kbd>⇧ Shift</kbd> + <kbd>⏎ Enter</kbd> or right-click on a result to open it directly in OneNote.

see settings for options on recycle bin etc.

![notebook explorer gif](doc/)

> [!INFORMATION] 
> Supports all OneNote hierarchy items i.e. notebooks, section groups, sections and pages.

#### Create New Items

Whilst using the notebook explorer, if you search query does not match any names of the items in the results, the plugin will give you an option to create a new item.

![create new section gif](doc/)

> [!INFORMATION] 
> Supports all OneNote hierarchy items i.e. notebooks, section groups, sections and pages.
### Recent Pages

```
on rcntpgs:
```

Displays your recently modified OneNote pages.

Add a number after `` rcntpgs: `` to display that number of recent pages. E.g. the full query ``on rcntpgs:10`` will show the 10 most recently modified pages.

![recent pages gif](doc/)
### Scoped Search

### Title Search


## Settings

### Keywords

## 2.0 Changelog

## Acknowledgements

- Made with [Odotocodot.OneNote.Linq](https://github.com/Odotocodot.OneNote.Linq) a library for exposing the OneNote API also made by me :smiley:.
- Inspired by the OneNote plugin for [PowerToys](https://github.com/microsoft/PowerToys/tree/main/src/modules/launcher/Plugins/Microsoft.PowerToys.Run.Plugin.OneNote).
- Icons from [Icons8](https://icons8.com).