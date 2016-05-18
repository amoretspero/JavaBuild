namespace JavaBuild

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Text
open System.Management
open Newtonsoft.Json

/// <summary>Class for java build information file.</summary>  
type BuildInfo(files : string [], mains : string [], runs: string[], CompileOptions : Dictionary<string, string>) =
    
    // private members ----------------------------------------------
    let _files = files
    let _mains = mains
    let _runs = runs
    let _CompileOptions = CompileOptions
    let mutable _BuildHistory = new System.Collections.Generic.Dictionary<string, option<System.DateTime>>()

    // public members -----------------------------------------------

    /// <summary>Files to be compiled.</summary>
    member bi.files = _files

    /// <summary>Files containing main method.</summary>
    member bi.mains = _mains

    /// <summary>Files to be run after build is successful.
    /// Set of run-after-build files should be subset of "mains".</summary>
    member bi.runs = 
        if (runs <> null) && ((Array.except mains runs).Length > 0) then
            failwith ("There are " + (Array.except mains runs).Length.ToString() + " run files not included in main files.")
            null
        else
            _runs

    /// <summary>Compile options</summary>
    member bi.CompileOptions = _CompileOptions

    /// <summary>Build history</summary>
    member bi.BuildHistory = _BuildHistory

    // public functions ---------------------------------------------

    /// <summary>Print source files to be compiled.</summary>
    member bi.printFiles() =
        for file in _files do
            printfn "Input File : %s" file

    /// <summary>Print files containing main method.</summary>
    member bi.printMains() =
        for main in _mains do
            printfn "Input File with main : %s" main

    /// <summary>Print run-after-build files.</summary>
    member bi.printRuns() = 
        if (_runs <> null) then
            for run in _runs do
                printfn "Input file to run after build : %s" run
    
    /// <summary>Print compile options.</summary>
    member bi.printCompileOptions() =
        for co in _CompileOptions do
            if (co.Value = "") then
                printfn "Option Name : %s, Option Value : (none)" co.Key
            else
                printfn "Option Name : %s, Option Value : %s" co.Key co.Value

    /// <summary>Get formal name for java compiler - javac.</summary>
    /// <param name="compileOption">Compile option in build file.</param>
    /// <returns>Formal compile option name.</returns>
    member bi.GetFormalOption(compileOption : string) =
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

    /// <summary>Get formal option value for "compilerOption" and given "optionValue".</summary>
    /// <param name="compilerOption">Name of compiler option.</param>
    /// <param name="optionValue">Value of option to be formalized.</param>
    /// <returns>Formalized option value for "compilerOption".</returns>
    member bi.GetFormalOptionValue(compilerOption : string, optionValue : string) =
        match compilerOption with
        | "ClassPath" -> optionValue
        | "CharacterEncoding" -> optionValue
        | "TerminateOnWarning" -> ""
        | _ -> ""

    /// <summary>Translates build file's compile options to javac compile option string.</summary>
    /// <returns>Generated javac compile option string.</summary>
    member bi.CompilerOptionsToString() =
        let mutable res = ""
        for co in _CompileOptions do
            if (co.Value <> "") then
                res <- res + bi.GetFormalOption(co.Key) + " " + bi.GetFormalOptionValue(co.Key, co.Value) + " "
        res
    
    /// <summary>Reads build history for current folder.
    /// If build history file does not exist, get information for files and create one.
    /// Read or newly created build history will be stored into _BuildHistory.</summary>
    /// <returns>Returns true when there already exists build history file and successfully read from it,
    /// false when no build history file existed and should create one.</returns>
    member bi.GetBuildHistory() =
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
    
    /// <summary>Write build history to build history file.</summary>
    member bi.WriteBuildHistory() =
        let buildHistoryString = JsonConvert.SerializeObject(_BuildHistory)
        File.WriteAllText("./.JavaBuildHistory", buildHistoryString)
    
    /// <summary>Print build history.</summary>
    member bi.PrintBuildHistory() =
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

    /// <summary>Update build history.</summary>
    member bi.UpdateBuildHistory() =
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

    /// <summary>Check if source file is updated since last build.</summary>
    /// <param name="fileName">File name to be checked, without file name extension.</param>
    /// <returns>True when it is updated, false when it is not updated.</returns>
    member bi.isUpdated(fileName : string) =
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

    /// <summary>Build current project. Things done are : Compilation, updating build history, printing build result and run-after-build.</summary>
    member bi.Build(buildFile : BuildInfo) =
        let compileSuccess = ref ([] : string list)
        let compileFail = ref ([] : string list)
        let compileUpToDate = ref ([] : string list)
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