namespace JavaBuild

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Text
open System.Management
open Newtonsoft.Json

exception FileNotFound
exception InvalidConfigurationKey
exception InvalidBuildHistory

/// <summary>Class for java build information file.</summary>  
type BuildInfo(files : Dictionary<string, string> [], mains : string [], runs: string[], CompileOptions : Dictionary<string, string>, Configurations : Dictionary<string, string>) =
    
    // private members ----------------------------------------------
    let _files = files
    let _mains = mains
    let _runs = runs
    let _CompileOptions = CompileOptions
    let _Configurations = Configurations
    let mutable _BuildHistory = new System.Collections.Generic.Dictionary<string, option<System.DateTime>>()

    // private methods ----------------------------------------------
    /// <summary>Check if given directory is where the build.json file resides.</summary>
    /// <param name="dir">Directory to be checked. It can be relative or absolute.</param>
    let directoryCheck (dir : string) =
        if dir = "" then
            true
        else if not (System.IO.Directory.Exists(dir)) then
            false
        else
            let buildJsonDir = System.IO.Directory.GetCurrentDirectory()
            let fullPath = System.IO.Path.GetFullPath(dir)
            if (fullPath <> buildJsonDir) then false else true

    /// <summary>Copies given srcFile to directory where the build.json file resides.</summary>
    /// <param name="srcFile">Source file with full path, relative or absolute, including file name.</param>
    /// <param name="fileName">Name of source file. Should include file name extension.</param>
    let copyClass (srcFile : string) (fileName : string) =
        if not (File.Exists(srcFile)) then
            raise FileNotFound
        else
            let buildJsonDir = System.IO.Directory.GetCurrentDirectory()
            File.Copy(srcFile, System.IO.Path.Combine(buildJsonDir, fileName), true)

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

    /// <summary>JavaBuild configurations</summary>
    member bi.Configurations = _Configurations

    /// <summary>Build history</summary>
    member bi.BuildHistory = _BuildHistory

    // public functions ---------------------------------------------

    /// <summary>Print source files to be compiled.</summary>
    member bi.printFiles() =
        for file in _files do
            printfn "Input File : %s (%s path : %s)" (file.Item("name")) (file.Item("locationType")) (file.Item("location"))

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

    /// <summary>Print JavaBuild configurations.</summary>
    member bi.printConfigurations() =
        for conf in _Configurations do
            if (conf.Value = "") then
                printfn "Configuration Name : %s, Value : (none)" conf.Key
            else
                printfn "Configuration Name : %s, Value : %s" conf.Key conf.Value

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

    /// <summary>Get configuration value for given configuration key.</summary>
    /// <param name="key">Key to find the value of configuration.</param>
    member bi.GetConfigurationValue(key : string) = 
        match key with
        | "AutomaticClassFileCopy" -> 
            if _Configurations.Item(key) = "" then "true" else _Configurations.Item(key)
        | _ -> raise InvalidConfigurationKey
    
    /// <summary>Reads build history for current folder.
    /// If build history file does not exist, get information for files and create one.
    /// Read or newly created build history will be stored into _BuildHistory.</summary>
    /// <returns>Returns true when there already exists build history file and successfully read from it,
    /// false when no build history file existed and should create one.</returns>
    member bi.GetBuildHistory() =
        if File.Exists("./.JavaBuildHistory") then
            printfn "Successfully found build history."
            let historyFile = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, option<System.DateTime>>>(File.ReadAllText("./.JavaBuildHistory"))
            // Check if .java file or .class file has been moved or deleted.
            for file in _files do
                let fileName = file.Item("name")
                let location = file.Item("location")
                let fullLocation = if location = "" then System.IO.Path.GetFullPath(".\\") else System.IO.Path.GetFullPath(location)
                let sourceFileLocation = System.IO.Path.Combine(fullLocation, fileName) + ".java"
                let classFileLocation = 
                    if bi.GetConfigurationValue("AutomaticClassFileCopy") = "false" then
                        System.IO.Path.Combine(fullLocation, fileName) + ".class"
                    else
                        System.IO.Path.Combine(System.IO.Path.GetFullPath(".\\"), fileName) + ".class"
                if not (File.Exists(sourceFileLocation)) then
                    raise FileNotFound
            _BuildHistory <- historyFile
            true
        else
            printfn "Build history does not exists. Creating..."
            let buildHistory = new Dictionary<string, option<System.DateTime>>()
            for file in _files do
                let fileName = file.Item("name")
                let location = file.Item("location")
                let fullLocation = if location = "" then System.IO.Path.GetFullPath(".\\") else System.IO.Path.GetFullPath(location)
                let sourceFileLocation = System.IO.Path.Combine(fullLocation, fileName) + ".java"
                let classFileLocation = System.IO.Path.Combine(fullLocation, fileName) + ".class"
                if File.Exists(sourceFileLocation) then
                    if File.Exists(classFileLocation) then 
                        buildHistory.Add(sourceFileLocation, Some(File.GetLastWriteTimeUtc(classFileLocation)))
                    else
                        buildHistory.Add(sourceFileLocation, None)
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
                    printfn "Build History - %s (source file) - Last Build Time : (none)" buildHistoryEnumJava.Current.Key
                else
                    printfn "Build History - %s (source file) - Last Build Time : %A" buildHistoryEnumJava.Current.Key buildHistoryEnumJava.Current.Value.Value

    /// <summary>Update build history.</summary>
    member bi.UpdateBuildHistory() =
        if not (File.Exists("./.JavaBuildHistory")) then
            failwith "Build History file missing."
        else
            let buildHistory = _BuildHistory
            for file in _files do
                let fileName = file.Item("name")
                let location = file.Item("location")
                let fullLocation = if location = "" then System.IO.Path.GetFullPath(".\\") else System.IO.Path.GetFullPath(location)
                let sourceFileLocation = System.IO.Path.Combine(fullLocation, fileName) + ".java"
                let classFileLocation = 
                    if bi.GetConfigurationValue("AutomaticClassFileCopy") = "true" then
                        System.IO.Path.Combine(System.IO.Path.GetFullPath(".\\"), fileName) + ".class"
                    else
                        System.IO.Path.Combine(fullLocation, fileName) + ".class"
                if File.Exists(sourceFileLocation) then
                    if File.Exists(classFileLocation) then
                        buildHistory.Remove(sourceFileLocation) |> ignore
                        buildHistory.Add(sourceFileLocation, Some(File.GetLastWriteTimeUtc(classFileLocation)))
                    else
                        buildHistory.Remove(sourceFileLocation) |> ignore
                        buildHistory.Add(sourceFileLocation, None)
                else
                    failwith ("Source file missing : " + sourceFileLocation)
            _BuildHistory <- buildHistory

    /// <summary>Check if source file is updated since last build.</summary>
    /// <param name="fileName">File name to be checked, without file name extension.</param>
    /// <returns>True when it is updated, false when it is not updated.</returns>
    member bi.isUpdated(fileName : string) =
        let javaFileName = fileName + ".java"
        let classFileName = fileName + ".class"
        let location = (Array.Find(_files, (fun x -> x.Item("name") = fileName))).Item("location")
        let fullLocation = if location = "" then System.IO.Path.GetFullPath(".\\") else System.IO.Path.GetFullPath(location)
        let sourceFileLocation = System.IO.Path.Combine(fullLocation, fileName) + ".java"
        let buildTime = ref(Some(System.DateTime.UtcNow))
        let mutable getBuildInfo = _BuildHistory.TryGetValue(sourceFileLocation, buildTime)
        if not getBuildInfo then
            if File.Exists(sourceFileLocation) then
                let mutable isPreviousSourceFileExists = false
                let previousData = ref(Some(System.DateTime.UtcNow))
                let mutable buildHistoryEnum = _BuildHistory.GetEnumerator()
                while (not isPreviousSourceFileExists) && buildHistoryEnum.MoveNext() do
                    if (buildHistoryEnum.Current.Key.Contains(fileName + ".java")) then
                        previousData.Value <- _BuildHistory.Item(buildHistoryEnum.Current.Key)
                        _BuildHistory.Remove(buildHistoryEnum.Current.Key) |> ignore
                        isPreviousSourceFileExists <- true
                if isPreviousSourceFileExists then
                    _BuildHistory.Add(sourceFileLocation, previousData.Value)
                    bi.WriteBuildHistory()
                    true
                else
                    raise InvalidBuildHistory
            else
                raise InvalidBuildHistory
        else if buildTime.Value.IsNone then
            true
        else
            let javaWriteTime = File.GetLastWriteTimeUtc(sourceFileLocation)
            if (javaWriteTime.CompareTo(buildTime.Value.Value) > 0) then
                true
            else
                false

    /// <summary>Check if class file with given name exists.</summary>
    /// <param name="fileName">Name of class file to be checked for existence.</param>
    /// <returns>True when class file exists, false when does not.</returns>
    member bi.isClassFileExists (fileName : string) =
        let location = (Array.Find(_files, (fun x -> x.Item("name") = fileName))).Item("location")
        if bi.GetConfigurationValue("AutomaticClassFileCopy") = "true" then
            File.Exists(System.IO.Path.Combine(System.IO.Path.GetFullPath(".\\"), fileName) + ".class")
        else
            File.Exists(System.IO.Path.Combine(System.IO.Path.GetFullPath(location), fileName) + ".class")

    /// <summary>Build current project. Things done are : Compilation, updating build history, printing build result and run-after-build.</summary>
    member bi.Build(buildFile : BuildInfo) =
        let compileSuccess = ref ([] : string list)
        let compileFail = ref ([] : string list)
        let compileUpToDate = ref ([] : string list)
        let getBuildHistoryResult = buildFile.GetBuildHistory()
        for file in buildFile.files do
            let fileName = file.Item("name")
            let fileLocationType = file.Item("locationType")
            let fileLocation = file.Item("location")
            let AutomaticClassFileCopyConfig = if buildFile.GetConfigurationValue("AutomaticClassFileCopy") = "true" then true else false
            let location = // Get location of file. Should not include ending backslash.
                if fileLocationType = "relative" then 
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), fileLocation) 
                else 
                    fileLocation 
            printfn "=================================================="
            printfn "Now compiling '%s'..." (fileName+".java")
            printfn "\n"
            if buildFile.isUpdated(fileName) || not (buildFile.isClassFileExists(fileName)) then
                let proc = new System.Diagnostics.Process()
                let procStartInfo = new System.Diagnostics.ProcessStartInfo()
                procStartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
                procStartInfo.FileName <- "javac "
                if AutomaticClassFileCopyConfig && (not (directoryCheck(file.Item("location")))) then 
                    printfn "\nAutomaticClassFileCopy ENABLED. Added class copy option.\n"
                    procStartInfo.Arguments <- "-d" + " " + System.IO.Directory.GetCurrentDirectory() + " "
                procStartInfo.Arguments <- procStartInfo.Arguments + "-classpath" + " " + location + " " + (System.IO.Path.Combine(location, (fileName + ".java"))) + " " + (buildFile.CompilerOptionsToString())
                printfn "Command to be executed : %s" (procStartInfo.FileName + procStartInfo.Arguments)
                procStartInfo.RedirectStandardOutput <- true
                procStartInfo.RedirectStandardError <- true
                procStartInfo.UseShellExecute <- false
                proc.StartInfo <- procStartInfo
                proc.Start() |> ignore
                proc.WaitForExit()
                let procResult = proc.StandardOutput.ReadToEnd()
                let procError = proc.StandardError.ReadToEnd()
                if (procError.Length = 0 && procResult.Length = 0) then
                    compileSuccess.Value <- List.append compileSuccess.Value [fileName]
                    printfn "Execution Result(output) : \nSuccess."
                    if AutomaticClassFileCopyConfig && (not (directoryCheck(file.Item("location")))) then 
                        printfn "\nAutomaticClassFileCopy ENABLED. Successfully copied class file to current directory.\n"
                    printfn "\nFinished compiling '%s'. Comilation result : SUCCESS" (fileName+".java")
                else if (procError.Length = 0 && procResult.Length > 0) then
                    compileSuccess.Value <- List.append compileSuccess.Value [fileName]
                    printfn "Execution Result(output) : \n%s" procResult
                    if AutomaticClassFileCopyConfig && (not (directoryCheck(file.Item("location")))) then 
                        printfn "\nAutomaticClassFileCopy ENABLED. Successfully copied class file to current directory.\n"
                    printfn "Finished compiling '%s'. Comilation result : SUCCESS" (fileName+".java")
                else
                    compileFail.Value <- List.append compileSuccess.Value [fileName]
                    printfn "Execution Result(error) : \n%s" procError
                    printfn "Finished compiling '%s'. Comilation result : FAIL" (fileName+".java")
                printfn "=================================================="
            else
                compileUpToDate.Value <- List.append compileUpToDate.Value [fileName]
                printfn "Class file is already up-to-date."
                printfn "Finished compiling '%s'. Comilation result : UP-TO-DATE" (fileName+".java")
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