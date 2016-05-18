open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Text
open System.Management
open Newtonsoft.Json
open JavaBuild

// Read JavaBuild file from current directory.
let buildFile = JsonConvert.DeserializeObject<BuildInfo>(File.ReadAllText("./build.json"))
printfn "Generated buildFile. files : %d, mains : %d, CompileOptions: %d" buildFile.files.Length buildFile.mains.Length buildFile.CompileOptions.Count

// Read JavaBuild history file from current directory.
// If not exists, make one with current build configuration.
let getBuildHistoryResult = buildFile.GetBuildHistory()
if not getBuildHistoryResult then
    buildFile.WriteBuildHistory()
else
    buildFile.UpdateBuildHistory()
    buildFile.WriteBuildHistory()

// Print build file info.
buildFile.printFiles()
buildFile.printMains()
buildFile.printRuns()
buildFile.printCompileOptions()
buildFile.PrintBuildHistory()

// List of successful/failed files.
let compileSuccess = ref ([] : string list)
let compileFail = ref ([] : string list)
let compileUpToDate = ref ([] : string list)

// Build
buildFile.Build(buildFile)