# AndroidLayoutToCSharp

Tiny program that converts Android XML's into C# properties. It can save a few minutes of your life ^^

It runs on UWP.

![](https://raw.githubusercontent.com/Drutol/AndroidLayoutToCSharp/master/github/screenshot.png)

## Features

- [x] Convert Android XML layout to C# properties.
- [x] Recursive resolution of layouts in `<include>` elements.
- [x] Creating ViewHolders

## Additional stuff

* You can add `tools:managedTypeName` attribute to use custom Type for properties. Useful when you have override Type name in library binding project.
* Warns about duplicated IDs, helpful when layout in `<include>` contains the same ID.
