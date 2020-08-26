using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic; 
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;


namespace buildDocs
{
    class FormTypes {
        private FormTypes(string value) { Value = value; }
        private readonly static string[] formTypes = { "FORMTEXT", "FORMCHECKBOX" };
        public string Value { get; set; }
        public bool Is(string type) {return Value == type.Trim();}
        public static FormTypes FormText     { get { return new FormTypes(formTypes[0]); } }
        public static FormTypes FormCheckBox { get { return new FormTypes(formTypes[1]); } }
        public static bool isFormType(string type) { return formTypes.Any(value => value == type.Trim()); }
    }

    class GenerateForm {
        public Dictionary<string, string> stringMap { get; set; }
        public Dictionary<string, bool> checkboxMap { get; set; }
        public GenerateForm() {
            stringMap = new Dictionary<string, string>();
            checkboxMap = new Dictionary<string, bool>();
        }
    }

    public class Pair {
        public int Count {get; set;}
        public string Value {get; set;}
        public Pair(string value, int count) {
            Count = count;
            Value = value;
        }
        public Pair() {
            Count = 0;
            Value = "";
        }
    }

    class GenerateFormNoDupe {
        public Dictionary<string, Pair> stringMap { get; set; }
        public Dictionary<string, bool> checkboxMap { get; set; }
        public GenerateFormNoDupe() {
            stringMap = new Dictionary<string, Pair>();
            checkboxMap = new Dictionary<string, bool>();
        }
    }

    class Program
    {
        static bool isVerbose {get; set;}
        public static void WriteJsonToFile<T>(string fileName, T genForm) {
            //string jsonString = JsonSerializer.Serialize<GenerateForm>(genForm);
            //File.WriteAllText(fileName, jsonString);
            using (FileStream fs = File.Create(fileName)) {
                Action fsAsync = async () => {
                    var options = new JsonSerializerOptions {
                        WriteIndented = true
                    };
                    await JsonSerializer.SerializeAsync<T>(fs, genForm, options);
                };
                fsAsync();
            }
        }

        public static GenerateFormNoDupe BuildNoDuplicateJson(GenerateForm genForm) {
            GenerateFormNoDupe noDupForm = new GenerateFormNoDupe();
            foreach(KeyValuePair<string, string> entry in genForm.stringMap) {
                string key = entry.Key.Split('_')[0];
                Pair valuePair;
                if (!noDupForm.stringMap.TryGetValue(key, out valuePair)) {
                    noDupForm.stringMap[key] = new Pair(entry.Value, 0);
                } else {
                    
                    noDupForm.stringMap[key].Count++;
                }
            }
            noDupForm.checkboxMap = genForm.checkboxMap;
            return noDupForm;
        }

        public static GenerateForm BuilDuplicateJson(GenerateFormNoDupe noDupForm) {
            if(noDupForm == null || noDupForm.stringMap == null ||
               noDupForm.checkboxMap == null) {
                return null;
            }
            GenerateForm genForm = new GenerateForm();
            foreach(KeyValuePair<string, Pair> entry in noDupForm.stringMap) {
                if(entry.Value.Count == 0) {
                    genForm.stringMap[entry.Key] = entry.Value.Value;
                } else {
                    for(int i = 0; i <= entry.Value.Count; i++) {
                        genForm.stringMap[entry.Key+"_"+i] = entry.Value.Value;
                    }
                }
            }
            genForm.checkboxMap = noDupForm.checkboxMap;
            return genForm;
        }

        public static T ReadJsonFromFile<T>(string fileName) {
            try {
                string jsonString = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<T>(jsonString);
            } catch(FileNotFoundException e) { 
                Console.WriteLine(e.Message);
            }
            catch(Exception e) {
                Console.WriteLine("Invalid json: " + e.Message);
            }
            return default(T);
        }
        public delegate void CheckBoxAction(GenerateForm genForm, string key, 
            DefaultCheckBoxFormFieldState checkboxChecked);
        public delegate void TextFieldAction (GenerateForm genForm, string key, Text bookmarkText);

        public static void GenerateJson(string filepath, GenerateForm genForm) {
            CheckBoxAction checkBoxGen = (GenerateForm genForm, string key, 
                    DefaultCheckBoxFormFieldState checkboxChecked) => {
                genForm.checkboxMap[key] = (bool) checkboxChecked.Val;
            };
            TextFieldAction textFieldGen =
                (GenerateForm genForm, string key, Text bookmarkText) => {
                genForm.stringMap[key] = bookmarkText.Text;
            };
            ParseJson(filepath, genForm, checkBoxGen, textFieldGen);
        }

        public static void fillJson(string filepath, GenerateForm genForm) {
            CheckBoxAction checkBoxFill = (GenerateForm genForm, string key, 
                    DefaultCheckBoxFormFieldState checkboxChecked) => {
                checkboxChecked.Val = genForm.checkboxMap[key];
            };
            TextFieldAction textFieldFill =
                (GenerateForm genForm, string key, Text bookmarkText) => {
                bookmarkText.Text = genForm.stringMap[key];
            };
            ParseJson(filepath, genForm, checkBoxFill, textFieldFill);
        }
 
        public static void ParseJson(string filepath, GenerateForm genForm, 
                CheckBoxAction checkBoxAction, TextFieldAction textFieldAction)
        {
            using (WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(filepath, true))
            {
                IDictionary<String, BookmarkStart> bookmarkMap = new Dictionary<String, BookmarkStart>();
                foreach (BookmarkStart bookmarkStart in wordprocessingDocument.MainDocumentPart.RootElement.Descendants<BookmarkStart>())
                {
                    //Console.WriteLine(bookmarkStart.Name);
                    bookmarkMap[bookmarkStart.Name] = bookmarkStart;
                }

                foreach (BookmarkStart bookmarkStart in bookmarkMap.Values)
                {
                    Run bookmarkFieldCode = bookmarkStart.NextSibling<Run>();
                    if (bookmarkFieldCode != null)
                    {
                        FieldCode fcode = bookmarkFieldCode.GetFirstChild<FieldCode>();
                        if(fcode != null && FormTypes.isFormType(fcode.Text)) {
                            if(FormTypes.FormCheckBox.Is(fcode.Text)) {
                                Run checkboxRun = bookmarkStart.PreviousSibling<Run>();
                                FieldChar fieldChar = checkboxRun?.GetFirstChild<FieldChar>();
                                FormFieldData formFieldData = fieldChar?.GetFirstChild<FormFieldData>();
                                CheckBox checkbox =  formFieldData?.GetFirstChild<CheckBox>();
                                //Note: docs say Checked should appear however type is DefaultCheckBoxFormFieldState
                                //Checked checkboxChecked =  checkbox?.GetFirstChild<Checked>();
                                DefaultCheckBoxFormFieldState checkboxChecked =  checkbox?.GetFirstChild<DefaultCheckBoxFormFieldState>();
                                //Console.WriteLine(checkboxChecked?.GetType());
                                if (checkboxChecked != null) {
                                    Console.WriteLine(""+(bool)checkboxChecked.Val);
                                    //genForm.checkboxMap[bookmarkStart.Name] = (bool) checkboxChecked.Val;
                                    checkBoxAction(genForm, bookmarkStart.Name, checkboxChecked);
                                } 
                            } else if(FormTypes.FormText.Is(fcode.Text)) {
                                while(bookmarkFieldCode.NextSibling<Run>() != null) {
                                    Text bookmarkText =  bookmarkFieldCode.GetFirstChild<Text>();
                                    if (bookmarkText != null) {
                                        Console.WriteLine(bookmarkText.Text);
                                        //genForm.stringMap[bookmarkStart.Name] = bookmarkText.Text;
                                        textFieldAction(genForm, bookmarkStart.Name, bookmarkText);
                                    }
                                    bookmarkFieldCode = bookmarkFieldCode.NextSibling<Run>();
                                }
                            }
                        }
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            var verboseOption = new Option<bool>("--verbose");
            verboseOption.AddAlias("-v");

            var templateOption = new Option<string>("--template");
            templateOption.AddAlias("t");
            templateOption.IsRequired = true;

            var jsonOutputOption = new Option<string>("--json",getDefaultValue: () => "output.json");
            jsonOutputOption.AddAlias("j");

            var jsonInputOption = new Option<string>("--json");
            jsonInputOption.AddAlias("j");
            jsonInputOption.IsRequired = true;

            var genCommand = new Command("generate");
            genCommand.AddAlias("gen");
            genCommand.Add(templateOption);
            genCommand.Add(jsonOutputOption);
            genCommand.Handler = CommandHandler.Create<string, string >((template, json) =>{
                GenerateForm genForm = new GenerateForm();
                //Console.WriteLine(template);
                //Console.WriteLine(json);
                GenerateJson(template, genForm);
                WriteJsonToFile(json, BuildNoDuplicateJson(genForm));
            });
            var fillCommand = new Command("fill");
            fillCommand.Add(templateOption);
            fillCommand.Add(jsonInputOption);
            fillCommand.Handler = CommandHandler.Create<string, string>((template, json) =>{
                //Console.WriteLine(template);
                //Console.WriteLine(json);
                if(String.IsNullOrEmpty(json)) {
                    Console.WriteLine("Invalid input json file name.");
                    return;
                }
                GenerateForm genForm = BuilDuplicateJson(ReadJsonFromFile<GenerateFormNoDupe>(json));
                if(genForm == null ||
                    (genForm.stringMap.Count == 0 && genForm.checkboxMap.Count == 0)) {
                    Console.WriteLine("exiting b\\c of invalid json.");
                    return;
                }
                fillJson(template,genForm);
            });

            var root = new RootCommand("");
            root.Handler = CommandHandler.Create<bool>((verbose) => {
                isVerbose = verbose;
            });
            root.Add(verboseOption);
            root.Add(genCommand);
            root.Add(fillCommand);
            root.InvokeAsync(args).Wait();
        }
    }
}
