# MetFrag.NET
## Project information

This is a port to .NET of the [original MetFrag project](https://github.com/s-wolf/MetFrag/), and is licensed under the LGPL.

In addition, some changes have been made to suit our purposes and remove un-needed functionality.

## Changes from the Java version

This port incorporates the following changes:

- Port to .NET of all MetFrag code
- Removal of a lot of code that was un-required for the core MetFrag algorithm
- Improvement of scoring algorithm to facilitate comparison of scoring across searches
- A fair bit of refactoring
- Got rid of addition/removal of protons when matching peaks
- Added some unit tests
- Hard coding some of the various parameters to reduce the amount of config checks that are done

## How to modify this library

This library is used by [Progenesis QI](http://nonlinear.com/progenesis/qi/) to perform theoretical fragmentation searches.

If you wish to modify the operation of this library, you can simply modify this code and build the project.

You should then copy the files in:

`MetFragNET\bin\Release`

to the following location:

`C:\Program Files (x86)\Nonlinear Dynamics\Progenesis QI\Plugins\MetaScopeSearch`

And you should choose to overwrite the existing files.

Ensure that you make a copy of the appropriate directory for your product before copying your new files in.
This will allow you to restore to the officially supported version of the MetaScope plugin should your code not work as expected.
