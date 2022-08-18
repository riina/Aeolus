| Package                  | Release                                                                                                                       |
|--------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| `CrossLaunch`            | [![NuGet](https://img.shields.io/nuget/v/CrossLaunch.svg)](https://www.nuget.org/packages/CrossLaunch/)                       |
| `CrossLaunch.Ubiquitous` | [![NuGet](https://img.shields.io/nuget/v/CrossLaunch.Ubiquitous.svg)](https://www.nuget.org/packages/CrossLaunch.Ubiquitous/) |

# Aeolus

A project launcher for Windows (maybe macOS later).

A learning exercise of sorts using .NET MAUI.

## Features

- Lists compatible projects (searchable by path) from selected folders
  - By default, limited depth search
- Lists recent projects
- Facilitates launching applicable editor program
- Provides resolution options for missing software

## Pending Features

- Options configuration (configure search depth limit per folder, and defaults)
  - Prefer to use context menu action, waiting on first-party feature
- Software icons
  - Use icon for primary editor software or default file icon if possible
- More project types

## Supported Project Types

### Visual Studio

Support for opening Visual Studio solutions.

Allows Visual Studio and Visual Studio for Mac by default, as a fallback.

Precedence when all software is enabled is Rider > VSC > VS/VSMac.

No validation of program version - if it looks like you have any version of any enabled software, that's used.

Options:

- `visualstudio.vscode.enable`: enable Visual Studio Code

- `visualstudio.rider.enable`: enable JetBrains Rider

### Unity

Support for opening Unity projects with Unity Editor installations from Unity Hub.

Missing Editor versions are detected and prompt a Unity Hub link and a normal link to Unity Download Archive.

# cl

A terminal program for simple project launching.

Uses same project support providers as Aeolus.

## cl usage examples

**Add a project folder:**

`cl folder add /Users/me/wkspaces`

**Scan project folder:**

`cl folder scan /Users/me/wkspaces`

`cl s /Users/me/wkspaces`

**Scan all project folders:**

`cl folder scan`

`cl s`

**List projects:**

`cl project list`

`cl l`

**List recent projects:**

`cl recent list`

`cl r`

**Nickname project:**

`cl project nick /Users/me/wkspaces/Cybertek cybertek`

**Launch project by nickname:**

`cl project launch cybertek`

`cl x cybertek`

**Launch project by path:**

`cl project launch ~/wkspaces/Cybertek`

`cl x ~/wkspaces/Cybertek`

**Launch project by shorthand path:**

`cl project launch wkspaces:Cybertek`

`cl x wkspaces:Cybertek`

Based on the root registered folder a project comes from, the shorthand name of a project is
the shortest unique name for the root folder, a colon, and the relative path from that root folder.

e.g. if two folders `/Users/Ypsilon/projects/GitHub` and `/Users/Ypsilon/GitHub` are registered, their
shorthand prefixes would be `projects/GitHub` and `Ypsilon/GitHub`. A project `/Users/Ypsilon/GitHub/Neopals/Neopals.sln`
would have the shorthand `Ypsilon/GitHub:Neopals/Neopals.sln`. Yes, that's not very short. Yeah, maybe project relative
paths themselves could be shortened as well. Maybe later.

**[Get help:](https://www.youtube.com/watch?v=CpZakOJlRoY)**

`cl [command] --help`

# CrossLaunch

.NET 6.0 library for use with Aeolus and cl. Provides EF Core DbContext base class for underlying project database and some convenience utilities for writing project evaluators / loaders.
