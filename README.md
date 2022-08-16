# Aeolus

TODO: A MAUI application for Windows and macOS meant for no-nonsense jumping into projects.

# cl

A terminal program for simple project launching.

## cl usage examples

Add a project folder:

`cl folder add /Users/me/wkspaces`

Scan project folder:

`cl folder scan /Users/me/wkspaces`

Scan all project folders:

`cl folder scan`

`cl s`

List projects:

`cl project list`

List recent projects:

`cl project recent`

`cl r`

Nickname project:

`cl project nick /Users/me/wkspaces/Cybertek cybertek`

Launch project by nickname:

`cl project launch cybertek`

`cl x cybertek`

Launch project by path:

`cl project launch ~/wkspaces/Cybertek`

`cl x ~/wkspaces/Cybertek`

# CrossLaunch

The core .NET 6.0 library for use with Aeolus and cl. Provides EF Core DbContext base class for underlying project database and some convenience utilities for writing project evaluators / loaders.
