## Quintrunner summary

Quintrunner is modding tool for Opus Magnum.  
It's intended purpose is both to be used by mod developers and for the development of [Quintessential](https://github.com/QuintessentialOM/Quintessential).  

Primary useful for copying compiled mod assets and dll-s to the required folder, launching  
the game and providing easy access to logs. Additionally it has the capability to merge the quintessential dll using [OpusMutatum](https://github.com/QuintessentialOM/OpusMutatum) before launch.

## Settings

The settings for the console application are specified by the paths.txt file.

### Basic settings:
* LightningDir:  
  The directory of the game (required)
* LightningArgs:  
  The arguments of the game
* RunGame:  
  To run the game after file copying and merging. (default : false)
* ReadLogs:  
  Display the logs given by Quintessential if the game is run. (default : true)
* AutoExit:  
  Terminate the application on finish instead of waiting for input. (default : false)

### Visual Studio settings:
* AttachVS:  
  If set to true Quintrunner will search for open Visual Studio projects and attempts to bind the launched game to the debugger. (default : true)
* BoundVSProjects:
  A list of Visual Studio solution (.sln) files to bind the launched game to

### For Mod devs:
* Mod:  
  The name of the mod being built.
* ModFiles:  
  A list of files and directories to be copied into the generated mod folder

### For Quintessential devs:
* CompiledQuintDir:  
  The directory of the newly compiled quintessential.dll to be copied into the game folder, and merged using Mutatum
* MutatumArgs:  
  The arguments to use for the merging process

### Format
Comments can be made by starting a line with #  
Lists of items can be assigned by starting with * in each line.  
<details> <summary> Example: </summary>

```plain
CompiledQuintDir:
  D:\C#_projects\Quintessential\bin\Debug\net452

LightningDir:
  D:\C#_projects\Quintessential\bin\Debug\net452\TestEnvironment

MutatumArgs:
devExe --mappings mappings.txt --mappings render_mappings.txt

\# LightningArgs:
\#   -devEnv -pseudoLang

RunGame:
  false

ReadLogs:
  true

AttachVS:
  true

AutoExit:
  true

BoundVSProjects:
* D:\C#_projects\Quintessential\Quintessential.sln
* D:\C#_projects\Quintrunner\Quintrunner.sln


# Mod:
#   UnstableElements

# ModFiles:
# * D:\C#_projects\UnstableElements\Content
# * D:\C#_projects\UnstableElements\quintessential.yaml
# * D:\C#_projects\UnstableElements\UnstableElements\bin\Debug\net452\UnstableElements.dll
# * D:\C#_projects\UnstableElements\UnstableElements\bin\Debug\net452\UnstableElements.pdb
```
</details>

## Visual Studio configurations:
To configure Quintrunner as a startup profile add the `.../MyProject/Properties/launchSettings.json` file to the given folder, and the paths.txt file to the build folder.

<img width="1694" height="433" alt="launchSettings" src="https://github.com/user-attachments/assets/11d8bca1-40d2-40a4-9dc8-ca43b4831411" />

```json
{
    "profiles": {
        "{MyProject}": {
            "commandName": "Project"
        },
        "Run With Quintrunner": {
            "commandName": "Executable",
            "executablePath": "{QuintrunnerPath}",
            "nativeDebugging": false
        }
    }
}
```
