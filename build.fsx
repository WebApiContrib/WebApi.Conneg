#I "./packages/FAKE.1.64.5/tools"
#r "FakeLib.dll"

open Fake 
open System.IO

// properties
let projectName = "AspNetWebApi.Conneg"
let version = "0.7." + System.DateTime.UtcNow.ToString("yyyyMMdd")
let projectSummary = "Explicit Conneg for Web API"
let projectDescription = "AspNetWebApi.Conneg makes the Content Negotiation mechanism of AspNetWebApi explicit."
let authors = ["Glenn Block";"Ryan Riley"]
let mail = "ryan.riley@panesofglass.org"
let homepage = "http://github.com/panesofglass/WebApi.Conneg"
let license = "http://github.com/panesofglass/WebApi.Conneg/raw/master/LICENSE.txt"
let nugetKey = if System.IO.File.Exists "./key.txt" then ReadFileAsString "./key.txt" else ""

// directories
let buildDir = "./build/"
let packagesDir = "./packages/"
let testDir = "./test/"
let deployDir = "./deploy/"
let nugetDir = "./nuget/"
let targetPlatformDir = getTargetPlatformDir "4.0.30319"
let nugetLibDir = nugetDir @@ "lib"
let webApiCoreVersion = GetPackageVersion packagesDir "AspNetWebApi.Core"

// params
let target = getBuildParamOrDefault "target" "All"

// tools
let fakePath = "./packages/FAKE.1.64.5/tools"
let nugetPath = "./.nuget/nuget.exe"
let nunitPath = "./packages/NUnit.2.5.10.11092/Tools"

// files
let appReferences =
    !+ "./src/**/*.csproj"
        |> Scan

let testReferences =
    !+ "./tests/**/*.csproj"
      |> Scan

let filesToZip =
    !+ (buildDir + "/**/*.*")
        -- "*.zip"
        |> Scan

// targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; deployDir]
)

Target "BuildApp" (fun _ ->
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    MSBuildDebug testDir "Build" testReferences
        |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    !+ (testDir + "/*.Tests.dll")
        |> Scan
        |> NUnit (fun p ->
            {p with
                ToolPath = nunitPath
                DisableShadowCopy = true
                OutputFile = testDir + "TestResults.xml" })
)

Target "CopyLicense" (fun _ ->
    [ "LICENSE.txt" ] |> CopyTo buildDir
)

Target "BuildNuGet" (fun _ ->
    CleanDirs [nugetDir; nugetLibDir]

    [ buildDir + "WebApi.Conneg.dll"
      buildDir + "WebApi.Conneg.pdb" ]
        |> CopyTo nugetLibDir

    NuGet (fun p -> 
        {p with               
            Authors = authors
            Project = projectName
            Description = projectDescription
            Version = version
            OutputPath = nugetDir
            Dependencies = ["AspNetWebApi.Core",RequireExactly webApiCoreVersion]
            AccessKey = nugetKey
            ToolPath = nugetPath
            Publish = nugetKey <> "" })
        "AspNetWebApi.Conneg.nuspec"

    [nugetDir + sprintf "AspNetWebApi.Conneg.%s.nupkg" version]
        |> CopyTo deployDir
)

Target "Deploy" (fun _ ->
    !+ (buildDir + "/**/*.*")
        -- "*.zip"
        |> Scan
        |> Zip buildDir (deployDir + sprintf "%s-%s.zip" projectName version)
)

Target "All" DoNothing

// Build order
"Clean"
  ==> "BuildApp" <=> "BuildTest" <=> "CopyLicense"
  ==> "Test"
  ==> "BuildNuGet"
  ==> "Deploy"

"All" <== ["Deploy"]

// Start build
Run target

