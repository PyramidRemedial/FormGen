# Form Field Generation

## Build Instructions
- `dotnet build`

## Run Instructions
- `dotnet run FormGen.exe <flags>`

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

## Supported Platform(s)
- OS X
- Linux
- Windows