# Form Field Generation

## Build Status
[![Build Status](https://travis-ci.com/PyramidRemedial/FormGen.svg?branch=master)](https://travis-ci.com/PyramidRemedial/FormGen)

## Build Instructions
- `dotnet build`

## Run Instructions
- `dotnet run <flags>`
- ex. `dotnet run generate -t test.docx`
- ex. `dotnet run gen -t test.docx`
- ex. `dotnet run fill -t test.docx -j output.json`

## cli comands
- `generate`
  * ### flags
    * `template` (t) The only required flag. this is where you provide the docx file.
    *  `json` (j) The output file name for the json generated.
 
- `fill`
  * ### flags
    * `template` (t) A required flag. this is where you provide the docx file.
    *  `json` (j) A reuqired flag. The input file name for the json to fill your form.

## Docker Build & Run
- build: `docker build -t formgen:latest .`
- run: `docker run --name docs_vm -it formgen:latest`

## Build For Platforms
- macos 14: `dotnet publish -c Release -r osx.10.14-x64`
- macos:    `dotnet publish -c Release -r osx-x64`
- windows:  `dotnet publish -c Release -r win-x64`
- linux:    `dotnet publish -c Release -r linux-x64`
- see more RID configurations at: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
## Supported Platform(s)
- OS X
- Linux
- Windows