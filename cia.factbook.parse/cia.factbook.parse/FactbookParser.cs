﻿using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;
using System.IO;
using Newtonsoft.Json;


namespace cia.factbook.parse
{
    public class FactbookParser
    {
        /// <summary>
        /// Create a JSON file for countries with a valid GEC value
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
    }
    public class CountryData
    {
        public string name { get; set; }="";
        public string code { get; set; } = "";
        public string publish_date { get; set; } = "";
        public string html { get; set; } = "";
    }
}
