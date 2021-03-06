﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using AngleSharp.Html.Parser;

namespace cia.factbook.parse
{
    public class FactbookParser
    {
        /// <summary>
        /// Reads print_profileguide.html in factbook and extracts the schema
        /// </summary>
        /// <param name="content">All text content of print_profileguide.html file</param>
        public static void GetProfileSchema(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            var bgelements = doc.QuerySelectorAll("div").Where(x =>
            x.HasAttribute("class")
            &&
            (x.GetAttribute("class").Equals("question category")
            || x.GetAttribute("class").Equals("field_label"))
            );
            ConsoleColor col = Console.ForegroundColor;
            Console.Clear();
            string currentcategory = string.Empty;
            foreach (var elem in bgelements)
            {
                string title = elem.TextContent.Trim(new char[] { ':', ' ' }).Trim();
                title = title.Replace(":",string.Empty).Trim();
                if (elem.GetAttribute("class").Equals("question category"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"+ {title}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    + {title}");
                    var nxt = elem.NextElementSibling;
                    do
                    {
                        if (nxt == null || nxt.HasAttribute("class") || nxt.LocalName.ToLower() != "div") break;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"        + {nxt.TextContent.Trim(new char[] { ':', ' ' }).Trim()}");
                        nxt = nxt.NextElementSibling;

                    } while (true);

                }
            }
        }
        /// <summary>
        /// Reads print_notesanddefs.html in factbook and saves as json 
        /// Json can be deserialized to a Dictionary<string,string>
        /// </summary>
        /// <param name="content">All text content of print_notesanddefs.html file</param>
        public static void GetDefinitionandNotes(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            var bgelements = doc.QuerySelectorAll("div").Where(x =>
            x.HasAttribute("class") && x.GetAttribute("class").Contains("appendix-entry reference-content")            
            );
            Dictionary<string, string> notesanddefs = new Dictionary<string, string>();

            TextWriter tw = new StreamWriter("notesanddefs.json", false);
            try
            {
                foreach (var item in bgelements)
                {
                    string key = item.Children.Where(x => x.HasAttribute("class")
                    && x.GetAttribute("class").Contains("appendix-entry-name")).First().TextContent.Trim();
                    string val = item.Children.Where(x => x.HasAttribute("class")
                    && x.GetAttribute("class").Contains("appendix-entry-text")).First().TextContent.Trim();
                    if (key != string.Empty && val != string.Empty)
                    {
                        Console.WriteLine(key);
                        notesanddefs.Add(key, val);
                    }
                }
                string json = JsonConvert.SerializeObject(notesanddefs, Formatting.Indented);
                tw.WriteLine(json);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { tw.Close(); }
        }
        /// <summary>
        /// Reads print_rankorderguide.html in factbook and saves as json 
        /// Json can be deserialized to a list of ComparableFieldType
        /// </summary>
        /// <param name="content">All text content of print_rankorderguide.html file</param>
        public static void GetComparableFields(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            var bgelements = doc.QuerySelectorAll("li").Where(x =>
            x.HasAttribute("id") && x.GetAttribute("id").EndsWith("-category-section-anchor")
            );
            List<ComparableField> comparablefields = new List<ComparableField>();

            TextWriter tw = new StreamWriter("comparablefields.json", false);
            try
            {
                foreach(var item in bgelements)
                {
                    string category = item.TextContent.Replace(":", "").Trim();
                    string fieldcontainerid = item.GetAttribute("id").Replace("-anchor",string.Empty).Trim();
                    var fields = doc.QuerySelectorAll("li").Where(x =>
                    x.HasAttribute("id") && x.GetAttribute("id") == fieldcontainerid
                    ).First().Children.Where(y => y.HasAttribute("class") &&
                    y.GetAttribute("class") == "field_label");
                    foreach (var field in fields)
                    {
                        string fieldname = field.TextContent.Replace(":", string.Empty).Trim();
                        bool isdescending = fieldname != "Unemployment rate" &&
                            !fieldname.StartsWith("Inflation rate");
                        comparablefields.Add(new ComparableField()
                        {
                            FieldName=fieldname,
                            Category=category,
                            IsDescending=isdescending
                        });
                        Console.WriteLine($"{category}: {fieldname} [{isdescending}]");
                    }
                }         
                string json = JsonConvert.SerializeObject(comparablefields, Formatting.Indented);
                tw.WriteLine(json);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { tw.Close(); }
        }
        /// <summary>
        /// Create a JSON file for countries with a valid GEC value
        /// Use appropriate path for the factbook files
        /// </summary>
        public static void CreateCountriesList()
        {
            TextWriter tw = new StreamWriter("countrieslist.json", false);
            try
            {               
                var flagFiles = new DirectoryInfo(@"C:\Users\sekha\Desktop\factbook\attachments\flags").EnumerateFiles("*.gif");
                var anthemFiles = new DirectoryInfo(@"C:\Users\sekha\Desktop\factbook\attachments\audios\original").EnumerateFiles("*.mp3");
                var dataFiles = new DirectoryInfo(@"C:\Users\sekha\Desktop\factbook\json").EnumerateFiles("*.json");

                List<Country> Countries = new List<Country>();
                string[] lines = File.ReadAllLines("./countries.csv");
                for(int i = 1; i < lines.Length; i++)
                {
                    string[] vals = lines[i].Split(new char[] { ',' });
                    if (vals.Length < 7) continue;
                    Country c = new Country()
                    {
                        Name = vals[0].Trim().Replace("$$",","),
                        GEC = vals[1].Trim(),
                        ISO_3166_1_Alpha2=vals[2].Trim(),
                        ISO_3166_1_Alpha3 = vals[3].Trim(),
                        ISO_3166_1_Numeric = vals[4].Trim(),
                        STANAG = vals[5].Trim(),
                        Internet = vals[6].Trim()
                    }; //Note that "," values in the countries.csv file are replaced using "$$"
                    if (vals.Length > 7)
                        c.Comment = vals[7].Trim();
                    if (c.GEC != "-")
                    {
                        Console.WriteLine(c.Name);
                        Countries.Add(c);
                    }
                    //Checking paths
                    if (flagFiles.Where(f => f.Name.StartsWith(c.GEC)).Count() == 1)
                        c.Flagfile = flagFiles.Where(f => f.Name.StartsWith(c.GEC)).First().Name;
                    if (anthemFiles.Where(f => f.Name.StartsWith(c.GEC)).Count() == 1)
                        c.AnthemFile = anthemFiles.Where(f => f.Name.StartsWith(c.GEC)).First().Name;
                    if (dataFiles.Where(f => f.Name.StartsWith(c.GEC.ToLower())).Count() == 1)
                        c.Datafile = dataFiles.Where(f => f.Name.StartsWith(c.GEC.ToLower())).First().Name;
                }
                Console.WriteLine($"{Countries.Count} Countries");
                string json = JsonConvert.SerializeObject(Countries, Formatting.Indented);
                tw.WriteLine(json);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            finally { tw.Close(); }
        }
        /// <summary>
        /// Collect data from factbook json folder into one json file
        /// </summary>
        public static void CreateCountryDataList()
        {
            TextWriter tw = new StreamWriter("countrydetailslist.json", false);
            try
            {
                List<CountryData> CountryDataList = new List<CountryData>();
                var dataFiles = new DirectoryInfo(@"C:\Users\sekha\Desktop\factbook\json").EnumerateFiles("*.json");
                int verified = 0;
                foreach (var file in dataFiles)
                {
                    CountryData data = JsonConvert.DeserializeObject<CountryData>(File.ReadAllText(file.FullName));
                    Console.WriteLine(data);
                    if (data.code != string.Empty && data.code.Length == 2 && data.name != string.Empty && data.html != string.Empty)
                    {
                        CountryDataList.Add(data); verified++;
                    }
                }
                Console.WriteLine($"Total files processed: {dataFiles.Count()}");
                Console.WriteLine($"Total files verified: {verified}");
                string json = JsonConvert.SerializeObject(CountryDataList, Formatting.Indented);
                tw.WriteLine(json);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            finally { tw.Close(); }
        }
        /// <summary>
        /// Parses the HTML content of the country profile data and converts them to a list of profile entities
        /// </summary>
        /// <param name="content">HTML content of the country profile data</param>
        /// <returns></returns>
        public static List<ProfileEntity> ParseProfileData(string content)
        {
            List<ProfileEntity> entities = new List<ProfileEntity>();
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            //Category Lists
            var cat_lis = doc.QuerySelectorAll("li").Where(x =>
            x.HasAttribute("id") && x.GetAttribute("id").EndsWith("-category-section-anchor"));
            foreach(var cat_li in cat_lis)
            {
                var catElem = cat_li.Children.Where(x => x.LocalName.ToLower() == "div" && x.HasAttribute("sectiontitle")).First();
                ProfileEntity Category = new ProfileEntity()
                {
                    EntityType =  ProfileEntityType.Category,
                    Key = catElem.GetAttribute("sectiontitle"),
                    Children= new List<ProfileEntity> ()                     
                };                                
                var field_divs = doc.QuerySelectorAll("div").Where(x =>
                x.LocalName.ToLower() == "div" && x.HasAttribute("id")
                && x.GetAttribute("id").StartsWith($"field-anchor-{Category.Key.Trim().Replace(" ", "-").ToLower()}-"));
                foreach(var field_div in field_divs)
                {
                    string fieldname = field_div.GetAttribute("id").Trim();
                    fieldname = fieldname.Replace($"-anchor-{Category.Key.Trim().Replace(" ", "-").ToLower()}", "");
                    ProfileEntity Field = new ProfileEntity()
                    {
                        EntityType = ProfileEntityType.Field,
                        Key = field_div.TextContent.Trim(new char[] { ':' }).Replace(":","").Trim(),
                        Children= new List<ProfileEntity> ()
                    };

                    //Get field content div
                    var field_content_div = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("id") && x.GetAttribute("id").Trim() == fieldname).First();                   
                    //Iterate through sub-fields
                    //--------------------------                    
                    //1.0 Check for Notes - directly for the field
                    var notes = field_content_div.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class").Trim() == "category_data note");
                    foreach(var note in notes)
                    {
                        Field.Note += note.TextContent.Replace("note:", "").Trim() + "\n";
                    }                    
                    //2.0 Go through subfields
                    foreach (var subfield in field_content_div.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class").Contains("subfield") &&
                    !x.GetAttribute("class").Contains("note")))
                    {
                        //2.1 Check if the sub-filed has a direct value
                        if (subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class").Contains("subfield")).Count() == 0)
                            Field.Value += subfield.TextContent +"\n";
                        else //3.2 Contains sub-fields
                        {
                            ProfileEntity SubField = new ProfileEntity() { EntityType = ProfileEntityType.SubField };
                            //3.2.1 Handle historic fields
                            if (subfield.GetAttribute("class").Contains("historic"))
                            {
                                if (subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class").Contains("subfield-name")).Count() != 0) //Sub Field is Numeric
                                {
                                    SubField.Key = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-name").First().TextContent.Replace(":", "").Trim();
                                }
                                else //Historic sub field with no key
                                {
                                    SubField.Key = "*";
                                }
                                SubField.IsHistoricEntity = true;
                                SubField.Value = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-number").FirstOrDefault()?.TextContent ?? "";
                                SubField.Note = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-note").FirstOrDefault()?.TextContent ?? "";
                                SubField.Date = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date").FirstOrDefault()?.TextContent ?? "";                                
                            }
                            //3.2.2 Handle numeric fields
                            else if (subfield.GetAttribute("class").Contains("numeric"))
                            {                                
                                if(subfield.Children.Where(x=>x.HasAttribute("class") && x.GetAttribute("class").Contains("subfield-name")).Count() != 0) //Sub Field is Numeric
                                {
                                    SubField.IsNumericEntity = true;
                                    SubField.Key = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-name").First().TextContent.Replace(":", "").Trim();
                                    SubField.Value = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-number").FirstOrDefault()?.TextContent??"";
                                    SubField.Note = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-note").FirstOrDefault()?.TextContent ?? "";
                                    SubField.Date = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date").FirstOrDefault()?.TextContent??"";
                                }
                                else //Parent field is numeric
                                {
                                    Field.IsNumericEntity = true;
                                    Field.Value = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-number").FirstOrDefault()?.TextContent ?? "";
                                    Field.Note = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-note").FirstOrDefault()?.TextContent ?? "";
                                    Field.Date = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date").FirstOrDefault()?.TextContent ?? "";
                                    continue;
                                }
                            }
                            //3.3.4 Handle grouped subfield case
                            else if (subfield.GetAttribute("class").Contains("grouped_subfield"))
                            {
                                if (subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class").Contains("subfield-name")).Count() != 0) //Sub Field is Numeric
                                {
                                    SubField.IsGroupedEntity = true;
                                    SubField.Key = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-name").First().TextContent.Replace(":", "").Trim();
                                    SubField.Value = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-number").First().TextContent;
                                    SubField.Note = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-note").FirstOrDefault()?.TextContent ?? "";
                                    SubField.Date = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date").FirstOrDefault()?.TextContent ?? "";
                                }
                                else //Parent field is grouped
                                {
                                    Field.IsGroupedEntity = true;
                                    Field.Value = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-number").First().TextContent;
                                    Field.Note = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-note").FirstOrDefault()?.TextContent ?? "";
                                    Field.Date = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date").FirstOrDefault()?.TextContent ?? "";
                                    continue;
                                }
                            }
                            //3.3.4 Handle text cases
                            else if (subfield.GetAttribute("class").Contains("text"))
                            {
                                if (subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class").Contains("subfield-name")).Count() != 0) //Is a sub filed
                                {
                                    var keyNode = subfield.Children.Where(x => x.GetAttribute("class") == "subfield-name").First();
                                    SubField.Key = keyNode.TextContent.Trim(new char[] { ':', ' ' });
                                    subfield.RemoveChild(keyNode);                                                 
                                    SubField.Value = subfield.TextContent.Trim();
                                }
                                else
                                {                                    
                                    string note = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-note").FirstOrDefault()?.TextContent.Trim() ?? "";
                                    string date = subfield.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date").FirstOrDefault()?.TextContent.Trim() ?? "";                                   
                                    string val = subfield.TextContent.Trim();
                                    if (note != string.Empty) val.Replace(note, "");
                                    if (date != string.Empty) val.Replace(date, "");
                                    Field.Value += val + "\n";
                                    Field.Note += note + "\n";
                                    Field.Date += date + "\n";
                                    continue;
                                }                                
                            }
                            SubField.Value = SubField.Value.Trim();
                            SubField.Note = SubField.Note.Trim();
                            SubField.Date = SubField.Date.Trim();
                            Field.Children.Add(SubField);
                        }
                    }
                    //Check for country comparison for the field
                    AngleSharp.Dom.IElement compareField = null;
                    if ((compareField = field_content_div.Children.Where(x=>x.LocalName.ToLower()=="div" && !x.HasAttribute("class") && x.TextContent.ToLower().Contains("country comparison to the world")).FirstOrDefault()) != null)
                    {
                        int rank = 0;
                        if(int.TryParse( compareField.Children.Where(x=>x.GetAttribute("class")== "category_data").FirstOrDefault()?.TextContent ?? "".Trim(), out rank))
                        {
                            Field.ComparisonRank = rank;
                        }                        
                    }
                    Field.Value = Field.Value.Trim();
                    Field.Note = Field.Note.Trim();
                    Field.Date = Field.Date.Trim();                    
                    Category.Children.Add(Field);                    
                }
                entities.Add(Category);
            }
            return entities;            
        }
        
                          
        #region Unused
        public static void ListAllSections(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            var bgelements = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("sectiontitle"));
            foreach (var elem in bgelements)
            {
                Console.WriteLine(elem.GetAttribute("sectiontitle"));
            }
        }
        public static void ListAllFields(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            var bgelements = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("id") 
            &&  x.GetAttribute("id").StartsWith("field-")
            &&  x.GetAttribute("id").Contains("anchor"));
            foreach(var elem in bgelements)
            {
                Console.WriteLine(elem.GetAttribute("id"));
            }
        }
        public static void ParseHTMLContent(string content)
        {
            var parser = new HtmlParser();          
            try
            {
                var doc = parser.ParseDocument(content);
              
                //Reading background info
                var bgelements = doc.QuerySelectorAll("div").Where(x => x.GetAttribute("id") == "field-background");
                if (bgelements.Count() != 0)
                {
                    //Console.WriteLine(bgelements.First().FirstElementChild.InnerHtml);                    
                    Console.WriteLine(bgelements.First().TextContent);
                }
                Console.WriteLine("----------------");
                bgelements = doc.QuerySelectorAll("div").Where(x => x.GetAttribute("id") == "field-capital");
                if (bgelements.Count() != 0)
                {
                    foreach (var elem in bgelements.First().Children.Where(x => x.HasAttribute("class")
                     && x.GetAttribute("class").Contains("category_data")
                     && (x.GetAttribute("class").Contains("text") || x.GetAttribute("class").Contains("note"))))
                    {
                        foreach (var child in elem.Children)
                        {
                            Console.Write(child.TextContent);

                        }
                        elem.RemoveChild(elem.Children.First());
                        Console.WriteLine(elem.TextContent);
                    }
                }
                Console.WriteLine("------------------");
                bgelements = doc.QuerySelectorAll("div").Where(x => x.GetAttribute("id") == "field-national-anthem");
                if (bgelements.Count() != 0)
                {
                    foreach (var elem in bgelements.First().Children.Where(x => x.HasAttribute("class")
                    && x.GetAttribute("class").Contains("category_data")
                    && (x.GetAttribute("class").Contains("text") || x.GetAttribute("class").Contains("note"))))
                    {
                        foreach (var child in elem.Children)
                        {
                            Console.Write(child.TextContent);
                        }
                        elem.RemoveChild(elem.Children.First());
                        Console.WriteLine(elem.TextContent);
                    }
                }
                Console.WriteLine("------------------");
                bgelements = doc.QuerySelectorAll("div").Where(x => x.GetAttribute("id") == "field-country-name");
                if (bgelements.Count() != 0)
                {
                    foreach (var elem in bgelements.First().Children.Where(x => x.HasAttribute("class")
                    && x.GetAttribute("class").Contains("category_data")
                    && (x.GetAttribute("class").Contains("text") || x.GetAttribute("class").Contains("note"))))
                    {
                        foreach (var child in elem.Children)
                        {
                            Console.Write(child.TextContent);
                        }
                        elem.RemoveChild(elem.Children.First());
                        Console.WriteLine(elem.TextContent);
                    }
                }
                Console.WriteLine("------------------");
                bgelements = doc.QuerySelectorAll("div").Where(x => x.GetAttribute("id") == "field-area");
                if (bgelements.Count() != 0)
                {
                    foreach (var elem in bgelements.First().Children.Where(x => x.HasAttribute("class")
                    && x.GetAttribute("class").Contains("category_data")
                    && (x.GetAttribute("class").Contains("text") || x.GetAttribute("class").Contains("note") || x.GetAttribute("class").Contains("numeric"))))
                    {
                        Console.Write(elem.Children.First().TextContent);
                        elem.RemoveChild(elem.Children.First());
                        Console.WriteLine(elem.TextContent);
                    }
                }
                Console.WriteLine("------------------");
                bgelements = doc.QuerySelectorAll("div").Where(x => x.GetAttribute("id") == "field-land-use");
                if (bgelements.Count() != 0)
                {
                    foreach (var elem in bgelements.First().Children.Where(x => x.HasAttribute("class")
                    && x.GetAttribute("class").Contains("category_data")
                    && (x.GetAttribute("class").Contains("text") || x.GetAttribute("class").Contains("note") || x.GetAttribute("class").Contains("numeric"))))
                    {
                        Console.Write(elem.Children.First().TextContent);
                        elem.RemoveChild(elem.Children.First());
                        Console.WriteLine(elem.TextContent);
                    }
                }




            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }
        public static void GetStructure(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            var bgelements = doc.QuerySelectorAll("div").Where(x =>
            x.HasAttribute("sectiontitle") ||
            (x.HasAttribute("id")
            && x.GetAttribute("id").StartsWith("field-")
            && x.GetAttribute("id").Contains("anchor")));

            ConsoleColor col = Console.ForegroundColor;
            Console.Clear();
            string currentcategory = string.Empty;
            foreach (var elem in bgelements)
            {
                string title = elem.TextContent.Trim(new char[] { ':' }).Trim();
                if (elem.HasAttribute("sectiontitle"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    currentcategory = elem.GetAttribute("sectiontitle").Trim();
                    currentcategory = currentcategory.Replace(" ", "-").ToLower();
                    Console.WriteLine($" + {elem.GetAttribute("sectiontitle").Trim()}");
                }
                else if (elem.GetAttribute("id").StartsWith($"field-anchor-{currentcategory}-"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    string fieldname = elem.GetAttribute("id");
                    fieldname = fieldname.Replace($"anchor-{currentcategory}-", string.Empty).Trim();
                    Console.WriteLine($" \t- {title}");
                    //Check for sub-fields
                    //var fieldContent = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("id")
                    //&& x.GetAttribute("id") == fieldname).First().Children
                    //.Where(y => y.HasAttribute("class")
                    //&& y.GetAttribute("class").Contains("category_data")
                    //&& (y.GetAttribute("class").Contains("text") || y.GetAttribute("class").Contains("note") 
                    //|| y.GetAttribute("class").Contains("numeric") || y.GetAttribute("class").Contains("historic")
                    //));
                    var fieldContent = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("id")
                    && x.GetAttribute("id") == fieldname).First().Children
                    .Where(y => y.HasAttribute("class") && y.GetAttribute("class").Contains("subfield"));

                    if (fieldContent.Count() != 0)
                    {
                        foreach (var item in fieldContent)
                        {
                            var subFields = item.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-name");
                            foreach (var subfield in subFields)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($" \t\t* {subfield.TextContent}");
                            }
                            if (item.GetAttribute("class").Contains("historic"))
                            {
                                subFields = item.Children.Where(x => x.HasAttribute("class") && x.GetAttribute("class") == "subfield-date");
                                foreach (var subfield in subFields)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($" \t\t* {subfield.TextContent} [historic]");
                                }
                            }
                        }
                    }
                    //Indentify additional notes
                    var noteContent = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("id")
                    && x.GetAttribute("id") == fieldname).First().Children
                    .Where(y => y.HasAttribute("class") && y.GetAttribute("class").Contains("note"));
                    foreach (var noteelement in noteContent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" \t\t* note:");
                    }
                    //Identify comparison
                    var compareContent = doc.QuerySelectorAll("div").Where(x => x.HasAttribute("id")
                    && x.GetAttribute("id") == fieldname).First().Children
                    .Where(y => y.TextContent.ToLower().Contains("country comparison to the world"));
                    if (compareContent.Count() == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" \t\t* country comparison to the world:");
                    }




                }
            }
            Console.ForegroundColor = col;
        }
        #endregion Unused

    }
}
