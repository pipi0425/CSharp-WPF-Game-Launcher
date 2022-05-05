# CSharp-WPF-Game-Launcher
simple C# game launcher that downloads game files from server  
forked from https://github.com/tom-weiland/csharp-game-launcher and made some modifications

## Functionality
- check for update from server (compare version)
- download and extract
- display patch notes (from server)
- compatible with unity games and others

## Set Up
(tested in visual studio 2019 and remote http server)

1. replace necessary information in `App.config`:  
value of `downloadLink` to server link `http://xxx.com/folder/`  
value of `exeName` to name of executable of game
2. set up server side (tested with http, ftp should work similarly)  
three files `Version.txt`, `PatchNote.txt`, `Build.zip` in same folder that can be accessed with `downloadlink`

## TODO
- [ ] Download only updated parts as patches instead of full game
