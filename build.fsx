
#I "packages/FAKE/tools/"
#r "FakeLib.dll"


open Fake
open Fake.FscHelper
open System.IO
open System
open System.Linq

let outDir = "out"
let testDll = Path.Combine(outDir, "soundcloud-dl.dll")
let exe = Path.Combine(outDir, "soundcloud-dl.exe")

let GetDll(name: string) =
    DirectoryInfo("packages").GetFiles(name, SearchOption.AllDirectories) |> Seq.head

let Copy(info: FileInfo) =
    printf "|| copy -> %s" info.FullName
    let target = Path.Combine("out", info.Name)
    if not(File.Exists target) then File.Copy(info.FullName, target)

[
    "FSharp.Data.dll"
    "Argu.dll"
    "FSharp.Data.DesignTime.dll" ]
|> Seq.iter (GetDll >> Copy)

Target "buildExe" (fun _ ->
        ["src/Api.fsx"; "src/Program.fsx"]
        |> Fsc (fun p ->
            { p with Output = exe})
)

Target "buildTestDll" (fun _ ->
        ["src/Api.fsx"; "src/Tests.fsx"]
        |> Fsc (fun p -> { p with FscTarget = Library; Output = testDll })
    )

Target "test" (fun _ ->
        !!(testDll)
        |> NUnit (fun p -> { p  with ToolName = "nunit-console.exe"} )
)

Target "watch" (fun _ ->
        let watcher = !! "src/*.fsx" |> WatchChanges (fun changes ->
                try
                    tracefn "%A" changes
                    Run "buildTestDll"
                with ex ->
                    Console.WriteLine ex.Message
            )
        Console.ReadLine() |> ignore
        watcher.Dispose()
    )

"buildExe"
    ==> "buildTestDll"
    ==> "test"

RunTargetOrDefault "buildExe"
