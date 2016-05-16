open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Text
open System.Management
open Newtonsoft.Json

/// <summary>Class for java build file.</summary>
type JavaBuild(files : string [], mains : string [], runs: string[], CompileOptions : Dictionary<string, string>) =
    
    // private members ----------------------------------------------
    let _files = files
    let _mains = mains
    let _runs = runs
    let _CompileOptions = CompileOptions
    let mutable _BuildHistory = new System.Collections.Generic.Dictionary<string, option<System.DateTime>>()

    // public members -----------------------------------------------
    member jb.files = _files
    member jb.mains = _mains
    member jb.runs = 
        if (runs <> null) && ((Array.except mains runs).Length > 0) then
            failwith ("There are " + (Array.except mains runs).Length.ToString() + " run files not included in main files.")
            null
        else
            _runs
    member jb.CompileOptions = _CompileOptions
    member jb.BuildHistory = _BuildHistory

    // public functions ---------------------------------------------
    member jb.printFiles() =
        for file in _files do
            printfn "Input File : %s" file

    member jb.printMains() =
        for main in _mains do
            printfn "Input File with main : %s" main

    member jb.printRuns() = 
        if (_runs <> null) then
            for run in _runs do
                printfn "Input file to run after build : %s" run

    member jb.printCompileOptions() =
        for co in _CompileOptions do
            if (co.Value = "") then
                printfn "Option Name : %s, Option Value : (none)" co.Key
            else
                printfn "Option Name : %s, Option Value : %s" co.Key co.Value

    member jb.GetFormalOption(compileOption : string) =
        match compileOption with
        | "ClassPath" -> "-classpath"
        | "CharacterEncoding" -> "-encoding"
        | "TerminateOnWarning" -> 
            let optionValue = ref ""
            _CompileOptions.TryGetValue(compileOption, optionValue) |> ignore
            if (optionValue.Value = "True") then
                "-Werror"
            else
                ""
        | _ -> ""

    member jb.GetFormalOptionValue(compilerOption : string, optionValue : string) =
        match compilerOption with
        | "ClassPath" -> optionValue
        | "CharacterEncoding" -> optionValue
        | "TerminateOnWarning" -> ""
        | _ -> ""

    member jb.CompilerOptionsToString() =
        let mutable res = ""
        for co in _CompileOptions do
            if (co.Value <> "") then
                res <- res + jb.GetFormalOption(co.Key) + " " + jb.GetFormalOptionValue(co.Key, co.Value) + " "
        res
    
    member jb.GetBuildHistory() =
        if File.Exists("./.JavaBuildHistory") then
            printfn "Successfully found build history."
            let historyFile = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, option<System.DateTime>>>(File.ReadAllText("./.JavaBuildHistory"))
            _BuildHistory <- historyFile
            true
        else
            printfn "Build history does not exists. Creating..."
            let buildHistory = new Dictionary<string, option<System.DateTime>>()
            for file in _files do
                if File.Exists("./"+file+".java") then
                    if File.Exists("./"+file+".class") then 
                        buildHistory.Add((file + ".java"), Some(File.GetLastWriteTimeUtc("./"+file+".java")))
                        buildHistory.Add((file + ".class"), Some(File.GetLastWriteTimeUtc("./"+file+".class")))
                    else
                        buildHistory.Add((file + ".java"), Some(File.GetLastWriteTimeUtc("./"+file+".java")))
                        buildHistory.Add((file + ".class"), None)
            _BuildHistory <- buildHistory
            printfn "Successfully created build history. Count : %d" _BuildHistory.Count
            false

    member jb.WriteBuildHistory() =
        let buildHistoryString = JsonConvert.SerializeObject(_BuildHistory)
        File.WriteAllText("./.JavaBuildHistory", buildHistoryString)
    
    member jb.PrintBuildHistory() =
        let mutable buildHistoryEnumJava = _BuildHistory.GetEnumerator()
        while (buildHistoryEnumJava.MoveNext()) do
            if (buildHistoryEnumJava.Current.Key.Contains(".java")) then
                if buildHistoryEnumJava.Current.Value.IsNone then
                    printfn "Build History - %s (source file) - Last Write Time : (none)" buildHistoryEnumJava.Current.Key
                else
                    printfn "Build History - %s (source file) - Last Write Time : %A" buildHistoryEnumJava.Current.Key buildHistoryEnumJava.Current.Value.Value

        let mutable buildHistoryEnumClass = _BuildHistory.GetEnumerator()
        while (buildHistoryEnumClass.MoveNext()) do
            if (buildHistoryEnumClass.Current.Key.Contains(".class")) then
                if buildHistoryEnumClass.Current.Value.IsNone then
                    printfn "Build History - %s (class file) - Last Write Time : (none)" buildHistoryEnumClass.Current.Key
                else
                    printfn "Build History - %s (class file) - Last Write Time : %A" buildHistoryEnumClass.Current.Key buildHistoryEnumClass.Current.Value.Value

    member jb.UpdateBuildHistory() =
        if not (File.Exists("./.JavaBuildHistory")) then
            failwith "Build History file missing."
        else
            let buildHistory = _BuildHistory
            for file in _files do
                if File.Exists("./"+file+".java") then
                    if File.Exists("./"+file+".class") then
                        buildHistory.Remove(file + ".java") |> ignore
                        buildHistory.Remove(file + ".class") |> ignore
                        buildHistory.Add((file + ".java"), Some(File.GetLastWriteTimeUtc("./"+file+".java")))
                        buildHistory.Add((file + ".class"), Some(File.GetLastWriteTimeUtc("./"+file+".class")))
                    else
                        buildHistory.Remove(file + ".java") |> ignore
                        buildHistory.Remove(file + ".class") |> ignore
                        buildHistory.Add((file + ".java"), Some(File.GetLastWriteTimeUtc("./"+file+".java")))
                        buildHistory.Add((file + ".class"), None)
                else
                    failwith ("Source file missing : " + file + ".java")
            _BuildHistory <- buildHistory

    member jb.isUpdated(fileName : string) =
        let javaFileName = fileName + ".java"
        let classFileName = fileName + ".class"
        let classBuildTime = ref(Some(System.DateTime.UtcNow))
        let mutable getBuildInfo = _BuildHistory.TryGetValue(classFileName, classBuildTime)
        if not getBuildInfo then
            true
        else if classBuildTime.Value.IsNone then
            true 
        else
            let javaWriteTime = File.GetLastWriteTimeUtc("./"+javaFileName)
            if (javaWriteTime.CompareTo(classBuildTime.Value.Value) > 0) then
                true
            else
                false

            
    
// Read JavaBuild file from current directory.
let buildFile = JsonConvert.DeserializeObject<JavaBuild>(File.ReadAllText("./build.json"))
printfn "Generated buildFile. files : %d, mains : %d, CompileOptions: %d" buildFile.files.Length buildFile.mains.Length buildFile.CompileOptions.Count

// Read JavaBuild history file from current directory.
// If not exists, make one with current build configuration.
let getBuildHistoryResult = buildFile.GetBuildHistory()
if not getBuildHistoryResult then
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

// Build.
for file in buildFile.files do
    printfn "=================================================="
    printfn "Now compiling '%s'..." (file+".java")
    printfn "\n"
    if buildFile.isUpdated(file) then
        let proc = new System.Diagnostics.Process()
        let procStartInfo = new System.Diagnostics.ProcessStartInfo()
        printfn "Command to be executed : %s" ("javac "+file+".java"+" "+(buildFile.CompilerOptionsToString()))
        procStartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
        procStartInfo.FileName <- "javac "
        procStartInfo.Arguments <- file + ".java" + " " + (buildFile.CompilerOptionsToString())
        procStartInfo.RedirectStandardOutput <- true
        procStartInfo.RedirectStandardError <- true
        procStartInfo.UseShellExecute <- false
        proc.StartInfo <- procStartInfo
        proc.Start() |> ignore
        proc.WaitForExit()
        let procResult = proc.StandardOutput.ReadToEnd()
        let procError = proc.StandardError.ReadToEnd()
        if (procError.Length = 0 && procResult.Length = 0) then
            compileSuccess.Value <- List.append compileSuccess.Value [file]
            printfn "Execution Result(output) : \nSuccess."
            printfn "\nFinished compiling '%s'. Comilation result : SUCCESS" (file+".java")
        else if (procError.Length = 0 && procResult.Length > 0) then
            compileSuccess.Value <- List.append compileSuccess.Value [file]
            printfn "Execution Result(output) : \n%s" procResult
            printfn "Finished compiling '%s'. Comilation result : SUCCESS" (file+".java")
        else
            compileFail.Value <- List.append compileSuccess.Value [file]
            printfn "Execution Result(error) : \n%s" procError
            printfn "Finished compiling '%s'. Comilation result : FAIL" (file+".java")
        printfn "=================================================="
    else
        compileUpToDate.Value <- List.append compileUpToDate.Value [file]
        printfn "Class file is already up-to-date."
        printfn "Finished compiling '%s'. Comilation result : UP-TO-DATE" (file+".java")
        printfn "=================================================="

// Update build history.
printfn "=================================================="
printfn "\nUpdating build history..."
buildFile.UpdateBuildHistory()
buildFile.PrintBuildHistory()
buildFile.WriteBuildHistory()
printfn "\nSuccessfully updated build history."
printfn "=================================================="

// Print build result.
printfn "=================================================="
printfn "Build Result :\n"
printfn "Number of requested compilations: %d\n" buildFile.files.Length
printfn "Compile SUCCESS: \n"
for file in compileSuccess.Value do
    printfn "     %s" file
printfn "\nCompile FAIL   : \n"
for file in compileFail.Value do
    printfn "     %s" file
printfn "\nAlready Up-To-Date: \n"
for file in compileUpToDate.Value do
    printfn "     %s" file
printfn "\nSUCCESS: %d files.\nFAIL: %d files.\nUP-TO-DATE: %d files\n" compileSuccess.Value.Length compileFail.Value.Length compileUpToDate.Value.Length
printfn "END of BUILD."
printfn "=================================================="

// If specified, do RUN-AFTER-BUILD
if (buildFile.runs <> null) then
    printfn "=================================================="
    printfn "Start of RUN-AFTER-BUILD"
    printfn "\n"
    for run in buildFile.runs do
        printfn "Now running %s..." run
        let proc = new System.Diagnostics.Process()
        let procStartInfo = new System.Diagnostics.ProcessStartInfo()
        procStartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Normal
        procStartInfo.FileName <- "java "
        procStartInfo.Arguments <- run
        procStartInfo.UseShellExecute <- false
        printfn "Command to be executed is : '%s'" (procStartInfo.FileName + procStartInfo.Arguments)
        proc.StartInfo <- procStartInfo
        proc.Start() |> ignore
        proc.WaitForExit()
        printfn "Execution of %s finished." run
        printfn "\n"
    printfn "End of RUN_AFTER_BUILD"
    printfn "=================================================="
printfn "Press any key to exit..."
Console.ReadKey() |> ignore