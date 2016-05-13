open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Text
open System.Management
open Newtonsoft.Json

type JavaBuild(files : string [], mains : string [], runs: string[], CompileOptions : Dictionary<string, string>) =
    let _files = files
    let _mains = mains
    let _runs = runs
    let _CompileOptions = CompileOptions

    member jb.files = _files
    member jb.mains = _mains
    member jb.runs = 
        if ((Array.except mains runs).Length > 0) then
            failwith ("There are " + (Array.except mains runs).Length.ToString() + " run files not included in main files.")
            null
        else
            _runs
    member jb.CompileOptions = _CompileOptions

    member jb.printFiles() =
        for file in _files do
            printfn "Input File : %s" file

    member jb.printMains() =
        for main in _mains do
            printfn "Input File with main : %s" main

    member jb.printRuns() = 
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
            
    

let buildFile = JsonConvert.DeserializeObject<JavaBuild>(File.ReadAllText("./build.json"))
printfn "Generated buildFile. files : %d, mains : %d, CompileOptions: %d" buildFile.files.Length buildFile.mains.Length buildFile.CompileOptions.Count
buildFile.printFiles()
buildFile.printMains()
buildFile.printRuns()
buildFile.printCompileOptions()

let compileSuccess = ref ([] : string list)
let compileFail = ref ([] : string list)

for file in buildFile.files do
    printfn "=================================================="
    printfn "Now compiling '%s'..." (file+".java")
    printfn "\n"
    let proc = new System.Diagnostics.Process()
    let procStartInfo = new System.Diagnostics.ProcessStartInfo()
    printfn "Command to be executed : %s" ("javac "+file+".java"+" "+(buildFile.CompilerOptionsToString()))
    procStartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
    procStartInfo.FileName <- "javac "
    procStartInfo.Arguments <- buildFile.files.[0] + ".java" + " " + (buildFile.CompilerOptionsToString())
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

printfn "=================================================="
printfn "Build Result :\n"
printfn "Number of requested compilations: %d\n" buildFile.files.Length
printfn "Compile SUCCESS: \n"
for file in compileSuccess.Value do
    printfn "     %s" file
printfn "\nCompile FAIL   : \n"
for file in compileFail.Value do
    printfn "     %s" file
printfn "\nSUCCESS: %d files.\nFAIL: %d files.\n" compileSuccess.Value.Length compileFail.Value.Length
printfn "END of BUILD."
printfn "=================================================="

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