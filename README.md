# Form Field Generation

## Build Instructions
- `dotnet build`

## Run Instructions
- `dotnet run buildDocs.exe <flags>`

## cli comands
- `generate`
  * ### flags
    * `template` (t) The only required flag. this is where you provide the docx file.
    *  `json` (j) The output file name for the json generated.
 
- `fill`
  * ### flags
    * `template` (t) A required flag. this is where you provide the docx file.
    *  `json` (j) A reuqired flag. The input file name for the json to fill your form.