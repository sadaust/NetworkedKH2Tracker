﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using System.Linq;
using System.Text;
using System.Text.Json;

//using System.Text.Json.Serialization;
//using YamlDotNet.Serialization;

namespace KhTracker
{
    public partial class MainWindow : Window
    {
        /// 
        /// Options
        ///

        private void SaveProgress(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                DefaultExt = ".txt",
                Filter = "txt files (*.txt)|*.txt",
                FileName = "kh2fm-tracker-save",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Save(saveFileDialog.FileName);
            }
        }

        public void Save(string filename)
        {
            string mode = "Mode: " + data.mode.ToString();

            // save settings
            string settings = "Settings: ";
            if (PromiseCharmOption.IsChecked)
                settings += "Promise Charm - ";
            if (ReportsOption.IsChecked)
                settings += "Secret Ansem Reports - ";
            if (AbilitiesOption.IsChecked)
                settings += "Second Chance & Once More - ";
            //if (TornPagesOption.IsChecked)
            //    settings += "Torn Pages - ";
            //if (CureOption.IsChecked)
            //    settings += "Cure - ";
            //if (FinalFormOption.IsChecked)
            //    settings += "Final Form - ";
            if (VisitLockOption.IsChecked)
                settings += "Visit Locks - ";
            if (ExtraChecksOption.IsChecked)
                settings += "Extra Checks - ";
            if (SoraHeartOption.IsChecked)
                settings += "Sora's Heart - ";
            if (SoraLevel01Option.IsChecked)
                settings += "Level01 - ";
            if (SoraLevel50Option.IsChecked)
                settings += "Level50 - ";
            if (SoraLevel99Option.IsChecked)
                settings += "Level99 - ";
            if (SimulatedOption.IsChecked)
                settings += "Simulated Twilight Town - ";
            if (HundredAcreWoodOption.IsChecked)
                settings += "100 Acre Wood - ";
            if (AtlanticaOption.IsChecked)
                settings += "Atlantica - ";
            if (CavernOption.IsChecked)
                settings += "Cavern of Remembrance - ";
            if (OCCupsOption.IsChecked)
                settings += "Olympus Cups - ";
            if (TerraOption.IsChecked)
                settings += "Lingering Will (Terra) - ";
            if (PuzzleOption.IsChecked)
                settings += "Puzzles - ";
            if (SynthOption.IsChecked)
                settings += "Synthesis - ";

            // save hint state (hint info, hints, track attempts)
            string attempts = "";
            string hintValues = "";
            if (data.mode == Mode.Hints || data.mode == Mode.OpenKHHints || data.mode == Mode.DAHints || data.mode == Mode.PathHints || data.mode == Mode.SpoilerHints)
            {
                attempts = "Attempts: ";
                if (data.hintsLoaded || data.mode == Mode.SpoilerHints)
                {
                    foreach (int num in data.reportAttempts)
                    {
                        attempts += " - " + num.ToString();
                    }
                }

                // store hint values
                hintValues = "HintValues: ";
                foreach (WorldData worldData in data.WorldsData.Values.ToList())
                {
                    if (worldData.hint == null)
                        continue;

                    int num = GetWorldNumber(worldData.hint);
                    if (worldData.containsGhost && GhostMathOption.IsChecked) //need to recaculate correct values if ghost items and automath are toggled
                    {
                        num += GetGhostPoints(worldData.worldGrid);
                    }
                    hintValues += num.ToString() + " ";
                }
            }

            FileStream file = File.Create(filename);
            StreamWriter writer = new StreamWriter(file);

            writer.WriteLine(mode);
            writer.WriteLine(settings);
            if (data.mode == Mode.Hints)
            {
                writer.WriteLine(attempts);
                writer.WriteLine(data.hintFileText[0]);
                writer.WriteLine(data.hintFileText[1]);
                writer.WriteLine(hintValues);
            }
            else if (data.mode == Mode.OpenKHHints)
            {
                writer.WriteLine(attempts);
                writer.WriteLine(data.openKHHintText);
                writer.WriteLine(hintValues);
            }
            else if (data.mode == Mode.AltHints)
            {
                Dictionary<string, List<string>> test = new Dictionary<string, List<string>>();
                foreach (string key in data.WorldsData.Keys.ToList())
                {
                    test.Add(key, data.WorldsData[key].checkCount);
                }
                string hintObject = JsonSerializer.Serialize(test);
                string hintText = Convert.ToBase64String(Encoding.UTF8.GetBytes(hintObject));
                writer.WriteLine(hintText);
            }
            else if (data.mode == Mode.OpenKHAltHints)
            {
                writer.WriteLine(data.openKHHintText);
            }
            else if (data.mode == Mode.PathHints)
            {
                writer.WriteLine(attempts);
                writer.WriteLine(data.openKHHintText);
                writer.WriteLine(hintValues);
            }
            else if (data.mode == Mode.DAHints)
            {
                writer.WriteLine(attempts);
                writer.WriteLine(data.openKHHintText);
                string worlditemlist = JsonSerializer.Serialize(Data.WorldItems);
                string worlditemlist64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(worlditemlist));
                writer.WriteLine(worlditemlist64);
                string reportlist = JsonSerializer.Serialize(data.TrackedReports);
                string reportlist64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(reportlist));
                writer.WriteLine(reportlist64);
                writer.WriteLine(hintValues);
            }
            else if (data.mode == Mode.SpoilerHints)
            {
                writer.WriteLine(attempts);
                writer.WriteLine(data.openKHHintText);
                writer.WriteLine(hintValues);
            }
            else if (data.mode == Mode.TimeHints)
            {
                //nothing yet
            }

            string ProgressString = "Progress:";
            foreach (string WorldName in data.WorldsData.Keys.ToList())
            {
                if (WorldName != "GoA" && WorldName != "SorasHeart" && WorldName != "DriveForms" && WorldName != "PuzzSynth")
                    ProgressString += " " + data.WorldsData[WorldName].progress.ToString();
            }
            writer.WriteLine(ProgressString);

            foreach (string WorldName in data.WorldsData.Keys.ToList())
            {
                string ItemString = WorldName + ":";
                foreach (Item item in data.WorldsData[WorldName].worldGrid.Children)
                {
                    ItemString += " " + item.Name;
                }
                writer.WriteLine(ItemString);
            }

            writer.Close();
        }

        private void LoadProgress(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "txt files (*.txt)|*.txt",
                FileName = "kh2fm-tracker-save",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (openFileDialog.ShowDialog() == true)
            {
                Load(openFileDialog.FileName);
            }
        }

        private void Load(string filename)
        {
            Stream file = File.Open(filename, FileMode.Open);
            StreamReader reader = new StreamReader(file);
            // reset tracker
            OnReset(null, null);

            string mode = reader.ReadLine().Substring(6);
            if (mode == "Hints")
                SetMode(Mode.Hints);
            else if (mode == "AltHints")
                SetMode(Mode.AltHints);
            else if (mode == "OpenKHHints")
                SetMode(Mode.OpenKHHints);
            else if (mode == "OpenKHAltHints")
                SetMode(Mode.OpenKHAltHints);
            else if (mode == "DAHints")
                SetMode(Mode.DAHints);
            else if (mode == "PathHints")
                SetMode(Mode.PathHints);
            else if (mode == "SpoilerHints")
                SetMode(Mode.SpoilerHints);

            // set settings
            string settings = reader.ReadLine();
            LoadSettings(settings.Substring(10));

            // set hint state
            if (mode == "Hints")
            {
                string attempts = reader.ReadLine();
                attempts = attempts.Substring(13);
                string[] attemptsArray = attempts.Split('-');
                for (int i = 0; i < attemptsArray.Length; ++i)
                {
                    data.reportAttempts[i] = int.Parse(attemptsArray[i]);
                }
                
                string line1 = reader.ReadLine();
                data.hintFileText[0] = line1;
                string[] reportvalues = line1.Split('.');

                string line2 = reader.ReadLine();
                data.hintFileText[1] = line2;
                line2 = line2.TrimEnd('.');
                string[] reportorder = line2.Split('.');

                for (int i = 0; i < reportorder.Length; ++i)
                {
                    data.reportLocations.Add(data.codes.FindCode(reportorder[i]));
                    string[] temp = reportvalues[i].Split(',');
                    data.reportInformation.Add(new Tuple<string, int>(data.codes.FindCode(temp[0]), int.Parse(temp[1]) - 32));
                }

                data.hintsLoaded = true;
                HintText.Content = "Hints Loaded";
            }
            else if (mode == "AltHints")
            {
                var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadLine()));
                var worlds = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(hintText);

                foreach (var world in worlds)
                {
                    if (world.Key == "GoA")
                    {
                        continue;
                    }
                    foreach (var item in world.Value)
                    {
                        data.WorldsData[world.Key].checkCount.Add(item);
                    }

                }
                foreach (var key in data.WorldsData.Keys.ToList())
                {
                    if (key == "GoA")
                        continue;

                    data.WorldsData[key].worldGrid.WorldComplete();
                    SetReportValue(data.WorldsData[key].hint, 0);
                }
            }
            else if (mode == "OpenKHHints")
            {
                string attempts = reader.ReadLine();
                attempts = attempts.Substring(13);
                string[] attemptsArray = attempts.Split('-');
                for (int i = 0; i < attemptsArray.Length; ++i)
                {
                    data.reportAttempts[i] = int.Parse(attemptsArray[i]);
                }
                data.openKHHintText = reader.ReadLine();
                var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
                var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
                JsmarteeHints(hintObject);
                //var reports = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(hintObject["Reports"].ToString());
                //
                //List<int> reportKeys = reports.Keys.Select(int.Parse).ToList();
                //reportKeys.Sort();
                //
                //foreach (var report in reportKeys)
                //{
                //    var world = convertOpenKH[reports[report.ToString()]["World"].ToString()];
                //    var count = reports[report.ToString()]["Count"].ToString();
                //    var location = convertOpenKH[reports[report.ToString()]["Location"].ToString()];
                //    data.reportInformation.Add(new Tuple<string, int>(world, int.Parse(count)));
                //    data.reportLocations.Add(location);
                //}
                //
                //data.hintsLoaded = true;
                //HintText.Content = "Hints Loaded";
            }
            else if (mode == "OpenKHAltHints")
            {
                data.openKHHintText = reader.ReadLine();
                var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
                var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
                ShanHints(hintObject);
                //var worlds = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(hintObject["world"].ToString());
                //
                //foreach (var world in worlds)
                //{
                //    if (world.Key == "Critical Bonuses" || world.Key == "Garden of Assemblage")
                //    {
                //        continue;
                //    }
                //    foreach (var item in world.Value)
                //    {
                //        data.WorldsData[convertOpenKH[world.Key]].checkCount.Add(convertOpenKH[item]);
                //    }
                //
                //}
                //foreach (var key in data.WorldsData.Keys.ToList())
                //{
                //    if (key == "GoA")
                //        continue;
                //
                //    data.WorldsData[key].worldGrid.WorldComplete();
                //    SetReportValue(data.WorldsData[key].hint, 0);
                //}
            }
            else if (mode == "DAHints")
            {
                string attempts = reader.ReadLine();
                attempts = attempts.Substring(13);
                string[] attemptsArray = attempts.Split('-');
                for (int i = 0; i < attemptsArray.Length; ++i)
                {
                    data.reportAttempts[i] = int.Parse(attemptsArray[i]);
                }
                data.openKHHintText = reader.ReadLine();
                var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
                var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
                PointsHints(hintObject);
                //var worldsP = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(hintObject["world"].ToString());
                //var reportsP = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(hintObject["Reports"].ToString());
                //var points = JsonSerializer.Deserialize<Dictionary<string, int>>(hintObject["checkValue"].ToString());
                //
                //List<int> reportKeysP = reportsP.Keys.Select(int.Parse).ToList();
                //reportKeysP.Sort();
                //
                //foreach (var point in points)
                //{
                //    if (data.PointsDatanew.Keys.Contains(point.Key))
                //    {
                //        data.PointsDatanew[point.Key] = point.Value;
                //    }
                //    else
                //    {
                //        Console.WriteLine($"Something went wrong in setting point values. error: {point.Key}");
                //    }
                //}
                //
                ////Fallback values for older seeds
                //if (!points.Keys.Contains("report"))
                //    data.PointsDatanew["report"] = data.PointsDatanew["page"];
                //if (!points.Keys.Contains("bonus"))
                //    data.PointsDatanew["bonus"] = 10;
                //if (!points.Keys.Contains("complete"))
                //    data.PointsDatanew["complete"] = 10;
                //if (!points.Keys.Contains("formlv"))
                //    data.PointsDatanew["formlv"] = 3;
                //if (!points.Keys.Contains("visit"))
                //    data.PointsDatanew["visit"] = 1;
                //
                ////get point totals for each world
                //foreach (var world in worldsP)
                //{
                //    if (world.Key == "Critical Bonuses" || world.Key == "Garden of Assemblage")
                //    {
                //        continue;
                //    }
                //    foreach (var item in world.Value)
                //    {
                //        if (item.Contains("Ghost_") && !GhostItemOption.IsChecked)
                //            continue;
                //
                //        data.WorldsData[convertOpenKH[world.Key]].checkCount.Add(convertOpenKH[item]);
                //
                //        string itemType = CheckItemType(item);
                //        if (data.PointsDatanew.Keys.Contains(itemType))
                //        {
                //            WorldPoints[convertOpenKH[world.Key]] += data.PointsDatanew[itemType];
                //        }
                //        else
                //        {
                //            Console.WriteLine($"Something went wrong in getting world points. error: {itemType}");
                //        }
                //    }
                //}
                //
                ////set points for each world
                //foreach (var key in data.WorldsData.Keys.ToList())
                //{
                //    if (key == "GoA")
                //        continue;
                //
                //    data.WorldsData[key].worldGrid.WorldPointsComplete();
                //
                //    if (WorldPoints.Keys.Contains(key))
                //    {
                //        SetReportValue(data.WorldsData[key].hint, WorldPoints[key]);
                //    }
                //    else
                //    {
                //        Console.WriteLine($"Something went wrong in setting world point numbers. error: {key}");
                //    }
                //}
                //
                //foreach (var reportP in reportKeysP)
                //{
                //    var worldP = convertOpenKH[reportsP[reportP.ToString()]["World"].ToString()];
                //    var checkP = reportsP[reportP.ToString()]["check"].ToString();
                //    var locationP = convertOpenKH[reportsP[reportP.ToString()]["Location"].ToString()];
                //
                //    data.pointreportInformation.Add(new Tuple<string, string>(worldP, checkP));
                //    data.reportLocations.Add(locationP);
                //}
                //
                //ReportsToggle(true);
                //data.hintsLoaded = true;
                //WorldPoints_c = WorldPoints;

                var witemlist64 = Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadLine()));
                var witemlist = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(witemlist64);
                Data.WorldItems = witemlist;

                var reportlist64 = Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadLine()));
                var reportlist = JsonSerializer.Deserialize<List<string>>(reportlist64);
                data.TrackedReports = reportlist;
            }
            else if (mode == "PathHints")
            {
                string attempts = reader.ReadLine();
                attempts = attempts.Substring(13);
                string[] attemptsArray = attempts.Split('-');
                for (int i = 0; i < attemptsArray.Length; ++i)
                {
                    data.reportAttempts[i] = int.Parse(attemptsArray[i]);
                }
                data.openKHHintText = reader.ReadLine();
                var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
                var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
                PathHints(hintObject);
                //var reports = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(hintObject["Reports"].ToString());
                //
                //List<int> reportKeys = reports.Keys.Select(int.Parse).ToList();
                //reportKeys.Sort();
                //
                //foreach (var report in reportKeys)
                //{
                //    var hinttext = reports[report.ToString()]["Text"].ToString();
                //    var location = convertOpenKH[reports[report.ToString()]["Location"].ToString()];
                //
                //    data.reportInformation.Add(new Tuple<string, int>(hinttext, 0));
                //    data.reportLocations.Add(location);
                //}
                //
                //data.hintsLoaded = true;
                //HintText.Content = "Hints Loaded";
            }
            else if (mode == "SpoilerHints")
            {
                string attempts = reader.ReadLine();
                attempts = attempts.Substring(13);
                string[] attemptsArray = attempts.Split('-');
                for (int i = 0; i < attemptsArray.Length; ++i)
                {
                    data.reportAttempts[i] = int.Parse(attemptsArray[i]);
                }
                data.openKHHintText = reader.ReadLine();
                var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
                var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
                SpoilerHints(hintObject);
            }

            // set hint values (DUMB)
            if (data.hintsLoaded)
            {
                string[] hintValues = reader.ReadLine().Substring(12).Split(' ');
                SetReportValue(data.WorldsData["SorasHeart"].hint, int.Parse(hintValues[0]));
                SetReportValue(data.WorldsData["DriveForms"].hint, int.Parse(hintValues[1]));
                SetReportValue(data.WorldsData["SimulatedTwilightTown"].hint, int.Parse(hintValues[2]));
                SetReportValue(data.WorldsData["TwilightTown"].hint, int.Parse(hintValues[3]));
                SetReportValue(data.WorldsData["HollowBastion"].hint, int.Parse(hintValues[4]));
                SetReportValue(data.WorldsData["BeastsCastle"].hint, int.Parse(hintValues[5]));
                SetReportValue(data.WorldsData["OlympusColiseum"].hint, int.Parse(hintValues[6]));
                SetReportValue(data.WorldsData["Agrabah"].hint, int.Parse(hintValues[7]));
                SetReportValue(data.WorldsData["LandofDragons"].hint, int.Parse(hintValues[8]));
                SetReportValue(data.WorldsData["HundredAcreWood"].hint, int.Parse(hintValues[9]));
                SetReportValue(data.WorldsData["PrideLands"].hint, int.Parse(hintValues[10]));
                SetReportValue(data.WorldsData["DisneyCastle"].hint, int.Parse(hintValues[11]));
                SetReportValue(data.WorldsData["HalloweenTown"].hint, int.Parse(hintValues[12]));
                SetReportValue(data.WorldsData["PortRoyal"].hint, int.Parse(hintValues[13]));
                SetReportValue(data.WorldsData["SpaceParanoids"].hint, int.Parse(hintValues[14]));
                SetReportValue(data.WorldsData["TWTNW"].hint, int.Parse(hintValues[15]));
                SetReportValue(data.WorldsData["Atlantica"].hint, int.Parse(hintValues[16]));
                SetReportValue(data.WorldsData["PuzzSynth"].hint, int.Parse(hintValues[17]));
            }
            else if (mode == "SpoilerHints") //we need to do this for spoiler hints because of the optional report mode
                reader.ReadLine();

            string[] progress = reader.ReadLine().Substring(10).Split(' ');
            data.WorldsData["SimulatedTwilightTown"].progress = int.Parse(progress[0]);
            data.WorldsData["TwilightTown"].progress = int.Parse(progress[1]);
            data.WorldsData["HollowBastion"].progress = int.Parse(progress[2]);
            data.WorldsData["BeastsCastle"].progress = int.Parse(progress[3]);
            data.WorldsData["OlympusColiseum"].progress = int.Parse(progress[4]);
            data.WorldsData["Agrabah"].progress = int.Parse(progress[5]);
            data.WorldsData["LandofDragons"].progress = int.Parse(progress[6]);
            data.WorldsData["HundredAcreWood"].progress = int.Parse(progress[7]);
            data.WorldsData["PrideLands"].progress = int.Parse(progress[8]);
            data.WorldsData["DisneyCastle"].progress = int.Parse(progress[9]);
            data.WorldsData["HalloweenTown"].progress = int.Parse(progress[10]);
            data.WorldsData["PortRoyal"].progress = int.Parse(progress[11]);
            data.WorldsData["SpaceParanoids"].progress = int.Parse(progress[12]);
            data.WorldsData["TWTNW"].progress = int.Parse(progress[13]);
            data.WorldsData["Atlantica"].progress = int.Parse(progress[14]);

            SetProgressIcons();

            // add items to worlds
            while (reader.EndOfStream == false)
            {
                string world = reader.ReadLine();
                string worldName = world.Substring(0, world.IndexOf(':'));
                string items = world.Substring(world.IndexOf(':') + 1).Trim();

                if (items != string.Empty)
                {
                    if (data.mode == Mode.DAHints)
                    {
                        foreach (string item in items.Split(' '))
                        {
                            WorldGrid grid = FindName(worldName + "Grid") as WorldGrid;
                            Item importantCheck = FindName(item) as Item;

                            if (grid.Handle_PointReport(importantCheck, this, data))
                            {
                                if (item.StartsWith("Ghost_"))
                                    grid.Add_Ghost(importantCheck, this);
                                else
                                    grid.Add_Item(importantCheck, this);
                            }
                        }
                    }
                    else if (data.mode == Mode.PathHints)
                    {
                        foreach (string item in items.Split(' '))
                        {
                            WorldGrid grid = FindName(worldName + "Grid") as WorldGrid;
                            Item importantCheck = FindName(item) as Item;

                            if (grid.Handle_PathReport(importantCheck, this, data))
                                grid.Add_Item(importantCheck, this);
                        }
                    }
                    else if (data.mode == Mode.SpoilerHints)
                    {
                        foreach (string item in items.Split(' '))
                        {
                            WorldGrid grid = FindName(worldName + "Grid") as WorldGrid;
                            Item importantCheck = FindName(item) as Item;

                            if (grid.Handle_SpoilerReport(importantCheck, this, data))
                            {
                                if (item.StartsWith("Ghost_"))
                                {
                                    //grid.Add_Ghost(importantCheck, this);
                                }

                                else
                                    grid.Add_Item(importantCheck, this);
                            }
                        }
                    }
                    else
                    {
                        foreach (string item in items.Split(' '))
                        {
                            //TODO: FIX MULTIPLAYER USING GHOSTS
                            if (item.StartsWith("Ghost_"))
                                continue;
                            WorldGrid grid = FindName(worldName + "Grid") as WorldGrid;
                            Item importantCheck = FindName(item) as Item;

                            if (grid.Handle_Report(importantCheck, this, data))
                                grid.Add_Item(importantCheck, this);
                        }
                    }
                }
            }

            reader.Close();
        }

        private void SetProgressIcons()
        {
            bool OldToggled = Properties.Settings.Default.OldProg;
            bool CustomToggled = Properties.Settings.Default.CustomIcons;
            string Prog = "Min-"; //Default
            if (OldToggled)
                Prog = "Old-";
            if (CustomProgFound && CustomToggled)
                Prog = "Cus-";

            string STTkey = Prog + data.ProgressKeys["SimulatedTwilightTown"][data.WorldsData["SimulatedTwilightTown"].progress];
            data.WorldsData["SimulatedTwilightTown"].progression.SetResourceReference(ContentProperty, STTkey);
            broadcast.SimulatedTwilightTownProgression.SetResourceReference(ContentProperty, STTkey);

            string TTkey = Prog + data.ProgressKeys["TwilightTown"][data.WorldsData["TwilightTown"].progress];
            data.WorldsData["TwilightTown"].progression.SetResourceReference(ContentProperty, TTkey);
            broadcast.TwilightTownProgression.SetResourceReference(ContentProperty, TTkey);

            string HBkey = Prog + data.ProgressKeys["HollowBastion"][data.WorldsData["HollowBastion"].progress];
            data.WorldsData["HollowBastion"].progression.SetResourceReference(ContentProperty, HBkey);
            broadcast.HollowBastionProgression.SetResourceReference(ContentProperty, HBkey);

            string BCkey = Prog + data.ProgressKeys["BeastsCastle"][data.WorldsData["BeastsCastle"].progress];
            data.WorldsData["BeastsCastle"].progression.SetResourceReference(ContentProperty, BCkey);
            broadcast.BeastsCastleProgression.SetResourceReference(ContentProperty, BCkey);

            string OCkey = Prog + data.ProgressKeys["OlympusColiseum"][data.WorldsData["OlympusColiseum"].progress];
            data.WorldsData["OlympusColiseum"].progression.SetResourceReference(ContentProperty, OCkey);
            broadcast.OlympusColiseumProgression.SetResourceReference(ContentProperty, OCkey);

            string AGkey = Prog + data.ProgressKeys["Agrabah"][data.WorldsData["Agrabah"].progress];
            data.WorldsData["Agrabah"].progression.SetResourceReference(ContentProperty, AGkey);
            broadcast.AgrabahProgression.SetResourceReference(ContentProperty, AGkey);

            string LoDkey = Prog + data.ProgressKeys["LandofDragons"][data.WorldsData["LandofDragons"].progress];
            data.WorldsData["LandofDragons"].progression.SetResourceReference(ContentProperty, LoDkey);
            broadcast.LandofDragonsProgression.SetResourceReference(ContentProperty, LoDkey);

            string HAWkey = Prog + data.ProgressKeys["HundredAcreWood"][data.WorldsData["HundredAcreWood"].progress];
            data.WorldsData["HundredAcreWood"].progression.SetResourceReference(ContentProperty, HAWkey);
            broadcast.HundredAcreWoodProgression.SetResourceReference(ContentProperty, LoDkey);

            string PLkey = Prog + data.ProgressKeys["PrideLands"][data.WorldsData["PrideLands"].progress];
            data.WorldsData["PrideLands"].progression.SetResourceReference(ContentProperty, PLkey);
            broadcast.PrideLandsProgression.SetResourceReference(ContentProperty, PLkey);

            string DCkey = Prog + data.ProgressKeys["DisneyCastle"][data.WorldsData["DisneyCastle"].progress];
            data.WorldsData["DisneyCastle"].progression.SetResourceReference(ContentProperty, DCkey);
            broadcast.DisneyCastleProgression.SetResourceReference(ContentProperty, DCkey);

            string HTkey = Prog + data.ProgressKeys["HalloweenTown"][data.WorldsData["HalloweenTown"].progress];
            data.WorldsData["HalloweenTown"].progression.SetResourceReference(ContentProperty, HTkey);
            broadcast.HalloweenTownProgression.SetResourceReference(ContentProperty, HTkey);

            string PRkey = Prog + data.ProgressKeys["PortRoyal"][data.WorldsData["PortRoyal"].progress];
            data.WorldsData["PortRoyal"].progression.SetResourceReference(ContentProperty, PRkey);
            broadcast.PortRoyalProgression.SetResourceReference(ContentProperty, PRkey);

            string SPkey = Prog + data.ProgressKeys["SpaceParanoids"][data.WorldsData["SpaceParanoids"].progress];
            data.WorldsData["SpaceParanoids"].progression.SetResourceReference(ContentProperty, SPkey);
            broadcast.SpaceParanoidsProgression.SetResourceReference(ContentProperty, SPkey);

            string TWTNWkey = Prog + data.ProgressKeys["TWTNW"][data.WorldsData["TWTNW"].progress];
            data.WorldsData["TWTNW"].progression.SetResourceReference(ContentProperty, TWTNWkey);
            broadcast.TWTNWProgression.SetResourceReference(ContentProperty, TWTNWkey);

            string ATkey = Prog + data.ProgressKeys["Atlantica"][data.WorldsData["Atlantica"].progress];
            data.WorldsData["Atlantica"].progression.SetResourceReference(ContentProperty, ATkey);
            broadcast.AtlanticaProgression.SetResourceReference(ContentProperty, ATkey);
        }

        private void DropFile(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (Path.GetExtension(files[0]).ToUpper() == ".HINT")
                    LoadHints(files[0]);
                else if (Path.GetExtension(files[0]).ToUpper() == ".PNACH")
                    ParseSeed(files[0]);
                else if (Path.GetExtension(files[0]).ToUpper() == ".ZIP")
                    OpenKHSeed(files[0]);
            }
        }

        private void LoadHints(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".hint",
                Filter = "hint files (*.hint)|*.hint",
                Title = "Select Hints File"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                LoadHints(openFileDialog.FileName);
            }
        }

        public void LoadHints(string filename)
        {
            SetMode(Mode.Hints);
            ResetHints();

            StreamReader streamReader = new StreamReader(filename);

            if (streamReader.EndOfStream)
            {
                HintText.Content = "Error loading hints";
                streamReader.Close();
                return;
            }

            string line1 = streamReader.ReadLine();
            data.hintFileText[0] = line1;
            string[] reportvalues = line1.Split('.');

            if (streamReader.EndOfStream)
            {
                HintText.Content = "Error loading hints";
                streamReader.Close();
                return;
            }

            string line2 = streamReader.ReadLine();
            data.hintFileText[1] = line2;
            line2 = line2.TrimEnd('.');
            string[] reportorder = line2.Split('.');

            LoadSettings(streamReader.ReadLine().Substring(24));

            streamReader.Close();

            for (int i = 0; i < reportorder.Length; ++i)
            {
                string location = data.codes.FindCode(reportorder[i]);
                if (location == "")
                    location = data.codes.GetDefault(i);

                data.reportLocations.Add(location);
                string[] temp = reportvalues[i].Split(',');
                data.reportInformation.Add(new Tuple<string, int>(data.codes.FindCode(temp[0]), int.Parse(temp[1]) - 32));
            }

            data.hintsLoaded = true;
            HintText.Content = "Hints Loaded";
        }

        private void ResetHints()
        {
            data.hintsLoaded = false;
            data.reportLocations.Clear();
            data.reportInformation.Clear();
            data.pointreportInformation.Clear();
            data.pathreportInformation.Clear();
            data.reportAttempts = new List<int>() { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 };

            foreach (var key in data.WorldsData.Keys.ToList())
            {
                data.WorldsData[key].hinted = false;
                data.WorldsData[key].hintedHint = false;
                data.WorldsData[key].containsGhost = false;
            }
            data.WorldsData["GoA"].hinted = true;

            foreach (ContentControl report in data.ReportAttemptVisual)
            {
                report.SetResourceReference(ContentProperty, "Fail0");
            }
            
            foreach (WorldData worldData in data.WorldsData.Values.ToList())
            {
                if (worldData.hint != null)
                    SetWorldNumber(worldData.hint, -1, "Y");
            }

            for (int i = 0; i < data.Reports.Count; ++i)
            {
                data.Reports[i].HandleItemReturn();
            }

            broadcast.OnResetHints();
        }

        private void LoadSettings(string settings)
        {
            string[] settinglist = settings.Split('-');

            foreach (string setting in settinglist)
            {
                string trimmed = setting.Trim();
                switch (trimmed)
                {
                    case "Promise Charm":
                        PromiseCharmToggle(true);
                        break;
                    case "Secret Ansem Reports":
                        ReportsToggle(true);
                        break;
                    case "Second Chance & Once More":
                        AbilitiesToggle(true);
                        break;
                    //case "Torn Pages":
                    //    TornPagesToggle(true);
                    //    break;
                    //case "Cure":
                    //    CureToggle(true);
                    //    break;
                    //case "Final Form":
                    //    FinalFormToggle(true);
                    //    break;
                    case "Visit Locks":
                        VisitLockToggle(true);
                        break;
                    case "Sora's Heart":
                        SoraHeartToggle(true);
                        break;
                    case "Level01":
                        SoraLevel01Toggle(true);
                        break;
                    case "Level50":
                        SoraLevel50Toggle(true);
                        break;
                    case "Level99":
                        SoraLevel99Toggle(true);
                        break;
                    case "Simulated Twilight Town":
                        SimulatedToggle(true);
                        break;
                    case "100 Acre Wood":
                        HundredAcreWoodToggle(true);
                        break;
                    case "Atlantica":
                        AtlanticaToggle(true);
                        break;
                    case "Cavern of Remembrance":
                        CavernToggle(true);
                        break;
                    case "Olympus Cups":
                        OCCupsToggle(true);
                        break;
                    case "Lingering Will (Terra)":
                        TerraToggle(true);
                        break;
                    case "Extra Checks":
                        ExtraChecksToggle(true);
                        break;
                    case "Puzzles":
                        PuzzleToggle(true);
                        break;
                    case "Synthesis":
                        SynthToggle(true);
                        break;
                }
            }
        }

        private void OnReset(object sender, RoutedEventArgs e)
        {
            if (aTimer != null)
                aTimer.Stop();

            SetWorking(false);

            collectedChecks.Clear();
            newChecks.Clear();

            ModeDisplay.Header = "";
            HintText.Content = "";
            data.mode = Mode.None;
            collected = 0;
            PointTotal = 0;

            data.SpoilerRevealTypes.Clear();
            SpoilerReportMode = false;
            SpoilerWorldCompletion = false;

            bool CustomMode = Properties.Settings.Default.CustomIcons;
            BitmapImage BarW = data.VerticalBarW;

            List<BitmapImage> CollectedNum = UpdateNumber(0, "Y");
            Collected_01.Source = CollectedNum[0];
            Collected_10.Source = null;

            if (CustomMode && CustomVBarWFound)
                BarW = data.CustomVerticalBarW;

            if (data.selected != null)
            {
                data.WorldsData[data.selected.Name].selectedBar.Source = BarW;
            }
            data.selected = null;

            foreach (WorldData worldData in data.WorldsData.Values.ToList())
            {
                for (int j = worldData.worldGrid.Children.Count - 1; j >= 0; --j)
                {
                    Item item = worldData.worldGrid.Children[j] as Item;
                    Grid pool = VisualTreeHelper.GetChild(ItemPool, GetItemPool[item.Name]) as Grid;

                    worldData.worldGrid.Children.Remove(worldData.worldGrid.Children[j]);
                    pool.Children.Add(item);

                    item.MouseDown -= item.Item_Return;
                    item.MouseEnter -= item.Report_Hover;
                    if (data.dragDrop)
                    {
                        item.MouseDoubleClick -= item.Item_Click;
                        item.MouseDoubleClick += item.Item_Click;
                        item.MouseMove -= item.Item_MouseMove;
                        item.MouseMove += item.Item_MouseMove;
                    }
                    else
                    {
                        item.MouseDown -= item.Item_MouseDown;
                        item.MouseDown += item.Item_MouseDown;
                        item.MouseUp -= item.Item_MouseUp;
                        item.MouseUp += item.Item_MouseUp;
                    }
                }
            }

            // Reset 1st column row heights
            RowDefinitionCollection rows1 = ((data.WorldsData["SorasHeart"].worldGrid.Parent as Grid).Parent as Grid).RowDefinitions;
            foreach (RowDefinition row in rows1)
            {
                // don't reset turned off worlds
                if (row.Height.Value != 0)
                    row.Height = new GridLength(1, GridUnitType.Star);
            }

            // Reset 2nd column row heights
            RowDefinitionCollection rows2 = ((data.WorldsData["DriveForms"].worldGrid.Parent as Grid).Parent as Grid).RowDefinitions;
            foreach (RowDefinition row in rows2)
            {
                // don't reset turned off worlds
                if (row.Height.Value != 0)
                    row.Height = new GridLength(1, GridUnitType.Star);
            }

            foreach (var key in data.WorldsData.Keys.ToList())
            {
                data.WorldsData[key].complete = false;
                data.WorldsData[key].checkCount.Clear();
                data.WorldsData[key].progress = 0;

                //world cross reset
                string crossname = key + "Cross";

                if (data.WorldsData[key].top.FindName(crossname) is Image Cross)
                {
                    Cross.Visibility = Visibility.Collapsed;
                }
                if (broadcast.FindName(crossname) is Image CrossB)
                {
                    CrossB.Visibility = Visibility.Collapsed;
                }
            }

            broadcast.TwilightTownProgression.SetResourceReference(ContentProperty, "");
            broadcast.HollowBastionProgression.SetResourceReference(ContentProperty, "");
            broadcast.LandofDragonsProgression.SetResourceReference(ContentProperty, "");
            broadcast.BeastsCastleProgression.SetResourceReference(ContentProperty, "");
            broadcast.OlympusColiseumProgression.SetResourceReference(ContentProperty, "");
            broadcast.SpaceParanoidsProgression.SetResourceReference(ContentProperty, "");
            broadcast.HalloweenTownProgression.SetResourceReference(ContentProperty, "");
            broadcast.PortRoyalProgression.SetResourceReference(ContentProperty, "");
            broadcast.AgrabahProgression.SetResourceReference(ContentProperty, "");
            broadcast.PrideLandsProgression.SetResourceReference(ContentProperty, "");
            broadcast.DisneyCastleProgression.SetResourceReference(ContentProperty, "");
            broadcast.HundredAcreWoodProgression.SetResourceReference(ContentProperty, "");
            broadcast.SimulatedTwilightTownProgression.SetResourceReference(ContentProperty, "");
            broadcast.TWTNWProgression.SetResourceReference(ContentProperty, "");
            broadcast.AtlanticaProgression.SetResourceReference(ContentProperty, "");

            TwilightTownProgression.SetResourceReference(ContentProperty, "");
            HollowBastionProgression.SetResourceReference(ContentProperty, "");
            LandofDragonsProgression.SetResourceReference(ContentProperty, "");
            BeastsCastleProgression.SetResourceReference(ContentProperty, "");
            OlympusColiseumProgression.SetResourceReference(ContentProperty, "");
            SpaceParanoidsProgression.SetResourceReference(ContentProperty, "");
            HalloweenTownProgression.SetResourceReference(ContentProperty, "");
            PortRoyalProgression.SetResourceReference(ContentProperty, "");
            AgrabahProgression.SetResourceReference(ContentProperty, "");
            PrideLandsProgression.SetResourceReference(ContentProperty, "");
            DisneyCastleProgression.SetResourceReference(ContentProperty, "");
            HundredAcreWoodProgression.SetResourceReference(ContentProperty, "");
            SimulatedTwilightTownProgression.SetResourceReference(ContentProperty, "");
            TWTNWProgression.SetResourceReference(ContentProperty, "");
            AtlanticaProgression.SetResourceReference(ContentProperty, "");

            LevelIcon.Visibility = Visibility.Hidden;
            Level.Visibility = Visibility.Hidden;
            StrengthIcon.Visibility = Visibility.Hidden;
            Strength.Visibility = Visibility.Hidden;
            MagicIcon.Visibility = Visibility.Hidden;
            Magic.Visibility = Visibility.Hidden;
            DefenseIcon.Visibility = Visibility.Hidden;
            Defense.Visibility = Visibility.Hidden;
            Weapon.Visibility = Visibility.Hidden;
            Connect.Visibility = AutoDetectOption.IsChecked ? Visibility.Visible : Visibility.Hidden;
            SimulatedTwilightTownPlus.Visibility = Visibility.Hidden;

            broadcast.LevelIcon.Visibility = Visibility.Hidden;
            broadcast.Level.Visibility = Visibility.Hidden;
            broadcast.StrengthIcon.Visibility = Visibility.Hidden;
            broadcast.Strength.Visibility = Visibility.Hidden;
            broadcast.MagicIcon.Visibility = Visibility.Hidden;
            broadcast.Magic.Visibility = Visibility.Hidden;
            broadcast.DefenseIcon.Visibility = Visibility.Hidden;
            broadcast.Defense.Visibility = Visibility.Hidden;
            broadcast.Weapon.Visibility = Visibility.Hidden;
            broadcast.SimulatedTwilightTownPlus.Visibility = Visibility.Hidden;

            FormRow.Height = new GridLength(0, GridUnitType.Star);
            broadcast.GrowthAbilityRow.Height = new GridLength(0, GridUnitType.Star);
            broadcast.StatsRow.Height = new GridLength(0, GridUnitType.Star);

            ValorM.Opacity = .45;
            WisdomM.Opacity = .45;
            LimitM.Opacity = .45;
            MasterM.Opacity = .45;
            FinalM.Opacity = .45;
            HighJump.Opacity = .45;
            QuickRun.Opacity = .45;
            DodgeRoll.Opacity = .45;
            AerialDodge.Opacity = .45;
            Glide.Opacity = .45;

            ValorLevel.Source = null;
            WisdomLevel.Source = null;
            LimitLevel.Source = null;
            MasterLevel.Source = null;
            FinalLevel.Source = null;
            HighJumpLevel.Source = null;
            QuickRunLevel.Source = null;
            DodgeRollLevel.Source = null;
            AerialDodgeLevel.Source = null;
            GlideLevel.Source = null;

            broadcast.ValorLevel.Source = null;
            broadcast.WisdomLevel.Source = null;
            broadcast.LimitLevel.Source = null;
            broadcast.MasterLevel.Source = null;
            broadcast.FinalLevel.Source = null;
            broadcast.HighJumpLevel.Source = null;
            broadcast.QuickRunLevel.Source = null;
            broadcast.DodgeRollLevel.Source = null;
            broadcast.AerialDodgeLevel.Source = null;
            broadcast.GlideLevel.Source = null;

            fireLevel = 0;
            blizzardLevel = 0;
            thunderLevel = 0;
            cureLevel = 0;
            reflectLevel = 0;
            magnetLevel = 0;
            tornPageCount = 0;

            if (fire != null)
                fire.Level = 0;
            if (blizzard != null)
                blizzard.Level = 0;
            if (thunder != null)
                thunder.Level = 0;
            if (cure != null)
                cure.Level = 0;
            if (reflect != null)
                reflect.Level = 0;
            if (magnet != null)
                magnet.Level = 0;
            if (pages != null)
                pages.Quantity = 0;

            if (highJump != null)
                highJump.Level = 0;
            if (quickRun != null)
                quickRun.Level = 0;
            if (dodgeRoll != null)
                dodgeRoll.Level = 0;
            if (aerialDodge != null)
                aerialDodge.Level = 0;
            if (glide != null)
                glide.Level = 0;

            //hide & reset seed hash
            if (ShouldResetHash)
            {
                HashRow.Height = new GridLength(0, GridUnitType.Star);
                SeedHashLoaded = false;
                SeedHashVisible = false;
            }

            foreach (string value in data.PointsDatanew.Keys.ToList())
            {
                data.PointsDatanew[value] = 0;
            }

            foreach (string world in WorldPoints.Keys.ToList())
            {
                WorldPoints[world] = 0;
                WorldPoints_c[world] = 0;
            }

            WorldGrid.Ghost_Fire = 0;
            WorldGrid.Ghost_Blizzard = 0;
            WorldGrid.Ghost_Thunder = 0;
            WorldGrid.Ghost_Cure = 0;
            WorldGrid.Ghost_Reflect = 0;
            WorldGrid.Ghost_Magnet = 0;
            WorldGrid.Ghost_Pages = 0;
            WorldGrid.Ghost_Fire_obtained = 0;
            WorldGrid.Ghost_Blizzard_obtained = 0;
            WorldGrid.Ghost_Thunder_obtained = 0;
            WorldGrid.Ghost_Cure_obtained = 0;
            WorldGrid.Ghost_Reflect_obtained = 0;
            WorldGrid.Ghost_Magnet_obtained = 0;
            WorldGrid.Ghost_Pages_obtained = 0;
            Data.WorldItems.Clear();
            data.TrackedReports.Clear();

            Collected.Visibility = Visibility.Visible;
            CollectedBar.Visibility = Visibility.Visible;
            CheckTotal.Visibility = Visibility.Visible;
            Score1000.Visibility = Visibility.Hidden;
            Score100.Visibility = Visibility.Hidden;
            Score10.Visibility = Visibility.Hidden;
            Score1.Visibility = Visibility.Hidden;

            broadcast.Collected.Visibility = Visibility.Visible;
            broadcast.CollectedBar.Visibility = Visibility.Visible;
            broadcast.CheckTotal.Visibility = Visibility.Visible;
            broadcast.Score1000.Visibility = Visibility.Hidden;
            broadcast.Score100.Visibility = Visibility.Hidden;
            broadcast.Score10.Visibility = Visibility.Hidden;
            broadcast.Score1.Visibility = Visibility.Hidden;

            score1000col.Width = new GridLength(0.0, GridUnitType.Star);
            ScoreSpacer.Width = new GridLength(15.0, GridUnitType.Star);
            broadcast.score1000col.Width = new GridLength(0.0, GridUnitType.Star);
            broadcast.scorespacer.Width = new GridLength(1.6, GridUnitType.Star);
            broadcast.ChestIconCol.Width = new GridLength(0.3, GridUnitType.Star);
            broadcast.BarCol.Width = new GridLength(0.3, GridUnitType.Star);

            //reset pathhints edits
            foreach (string key in data.WorldsData.Keys.ToList())
            {
                data.WorldsData[key].top.ColumnDefinitions[0].Width = new GridLength(1.5, GridUnitType.Star);
                Grid grid = data.WorldsData[key].world.Parent as Grid;
                grid.ColumnDefinitions[3].Width = new GridLength(0.1, GridUnitType.Star);

                Grid pathgrid = data.WorldsData[key].top.FindName(key + "Path") as Grid;
                pathgrid.Visibility = Visibility.Hidden;
                foreach (Image child in pathgrid.Children)
                {
                    if (child.Name.Contains(key + "Path_Non") && child.Source.ToString().Contains("cross.png")) //reset non icon to default image
                        child.Source = new BitmapImage(new Uri("Images/Checks/Simple/proof_of_nonexistence.png", UriKind.Relative));
                    child.Visibility = Visibility.Hidden;
                }
            }

            UpdatePointScore(0);
            ReportsToggle(true);
            ResetHints();
            VisitLockToggle(VisitLockOption.IsChecked);

            broadcast.OnReset();
            broadcast.UpdateNumbers();

            DeathCounter = 0;
            List<BitmapImage> DeathNum = UpdateNumber(0, "Y");
            Death_01.Source = DeathNum[0];
            broadcast.Death_01.Source = DeathNum[0];
            Death_10.Source = null;
            broadcast.Death_10.Source = null;
            DeathCounterGrid.Visibility = Visibility.Collapsed;

            HintTextParent.SetValue(Grid.ColumnProperty, 2);
            HintTextParent.SetValue(Grid.ColumnSpanProperty, 21);
            broadcast.DeathCounter.Width = new GridLength(0, GridUnitType.Star);

            foreach (Grid itempool in ItemPool.Children)
            {
                foreach (ContentControl item in itempool.Children)
                    if (!item.Name.Contains("Ghost"))
                        item.Opacity = 1.0;
            }

            SetAutoDetectTimer();
            NextLevelDisplay();
        }

        private void BroadcastWindow_Open(object sender, RoutedEventArgs e)
        {
            //ExtraItemToggleCheck();
            broadcast.Show();
        }

        private void ParseSeed(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".pnach",
                Filter = "pnach files (*.pnach)|*.pnach",
                Title = "Select Seed File"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ParseSeed(openFileDialog.FileName);
            }
        }

        private void JoinServer(object sender, RoutedEventArgs e)
        {
            Join();
        }

        private void StartServer(object sender, RoutedEventArgs e)
        {
            Host(Properties.Settings.Default.HostPort);
        }

        public void ParseSeed(string filename)
        {
            bool autotrackeron = false;
            bool ps2tracking = false;
            //check for autotracking on and which version
            {
                if (aTimer != null)
                    autotrackeron = true;

                if (pcsx2tracking)
                    ps2tracking = true;
            }

            //FixDictionary();
            SetMode(Mode.AltHints);

            foreach (string world in data.WorldsData.Keys.ToList())
            {
                data.WorldsData[world].checkCount.Clear();
            }

            StreamReader streamReader = new StreamReader(filename);
            bool check1 = false;
            bool check2 = false;

            while (streamReader.EndOfStream == false)
            {
                string line = streamReader.ReadLine();

                // ignore comment lines
                if (line.Length >= 2 && line[0] == '/' && line[1] == '/')
                    continue;

                string[] codes = line.Split(',');
                if (codes.Length == 5)
                {
                    string world = data.codes.FindCode(codes[2]);

                    //stupid fix
                    string[] idCode = codes[4].Split('/', ' ');

                    int id = Convert.ToInt32(idCode[0], 16);
                    if (world == "" || world == "GoA" || data.codes.itemCodes.ContainsKey(id) == false || (id >= 226 && id <= 238))
                        continue;

                    string item = data.codes.itemCodes[Convert.ToInt32(codes[4], 16)];
                    data.WorldsData[world].checkCount.Add(item);
                }
                else if (codes.Length == 1)
                {
                    if (codes[0] == "//Remove High Jump LVl" || codes[0] == "//Remove Quick Run LVl")
                    {
                        check1 = true;
                    }
                    else if (codes[0] == "//Remove Dodge Roll LVl")
                    {
                        check2 = true;
                    }
                }
            }
            streamReader.Close();

            if (check1 == true && check2 == false)
            {
                foreach (string world in data.WorldsData.Keys.ToList())
                {
                    data.WorldsData[world].checkCount.Clear();
                }
            }

            foreach (var key in data.WorldsData.Keys.ToList())
            {
                if (key == "GoA")
                    continue;

                data.WorldsData[key].worldGrid.WorldComplete();
                SetReportValue(data.WorldsData[key].hint, 0);
            }

            if (autotrackeron)
            {
                InitAutoTracker(ps2tracking);
            }
        }

        private void SetMode(Mode mode)
        {
            if ((data.mode != mode && data.mode != Mode.None) || mode == Mode.AltHints || mode == Mode.OpenKHAltHints || mode == Mode.DAHints || mode == Mode.PathHints || mode == Mode.SpoilerHints)
            {
                OnReset(null, null);
            }

            if (mode == Mode.AltHints || mode == Mode.OpenKHAltHints)
            {
                ModeDisplay.Header = "Alt Hints Mode";
                data.mode = mode;
                ReportsToggle(false);
            }
            else if (mode == Mode.Hints || mode == Mode.OpenKHHints)
            {
                ModeDisplay.Header = "Hints Mode";
                data.mode = mode;
                ReportsToggle(true);
            }
            else if (mode == Mode.DAHints)
            {
                ModeDisplay.Header = "Points Mode";
                data.mode = mode;
                ReportsToggle(true);

                ShowCheckCountToggle(null, null);

                broadcast.ChestIconCol.Width = new GridLength(0.5, GridUnitType.Star);
                broadcast.BarCol.Width = new GridLength(1, GridUnitType.Star);

                UpdatePointScore(0);
            }
            else if (mode == Mode.PathHints)
            {
                ModeDisplay.Header = "Path Hints";
                data.mode = mode;
                ReportsToggle(true);
            }
            else if (mode == Mode.SpoilerHints)
            {
                ModeDisplay.Header = "Spoiler Hints";
                data.mode = mode;
            }
            else if (mode == Mode.TimeHints)
            {
                ModeDisplay.Header = "Timed Hints";
                data.mode = mode;
                ReportRow.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void OpenKHSeed(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".zip",
                Filter = "OpenKH Seeds (*.zip)|*.zip",
                Title = "Select Seed File"
            };
            if (openFileDialog.ShowDialog() == true)
                OpenKHSeed(openFileDialog.FileName);
        }

        private void OpenKHSeed(string filename)
        {
            HintText.Content = "";

            foreach (string world in data.WorldsData.Keys.ToList())
            {
                data.WorldsData[world].checkCount.Clear();
            }

            using (ZipArchive archive = ZipFile.OpenRead(filename))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".Hints"))
                    {
                        using (StreamReader reader = new StreamReader(entry.Open()))
                        {
                            data.openKHHintText = reader.ReadToEnd();
                            var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
                            var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
                            var settings = new List<string>();
                            bool betterSTTon = false;

                            ShouldResetHash = false;

                            if (hintObject.ContainsKey("settings"))
                            {
                                settings = JsonSerializer.Deserialize<List<string>>(hintObject["settings"].ToString());

                                //set all settings to false
                                {
                                    PromiseCharmToggle(false);
                                    SimulatedToggle(false);
                                    HundredAcreWoodToggle(false);
                                    AtlanticaToggle(false);
                                    CavernToggle(false);
                                    OCCupsToggle(false);
                                    SoraHeartToggle(true);
                                    SoraLevel01Toggle(true);
                                    VisitLockToggle(false);
                                    TerraToggle(false);
                                    PuzzleToggle(false);
                                    SynthToggle(false);

                                    AbilitiesToggle(true);
                                    //TornPagesToggle(true);
                                    //CureToggle(true);
                                    //FinalFormToggle(true);

                                    ExtraChecksToggle(false);
                                    AntiFormToggle(false);

                                    SimulatedTwilightTownPlus.Visibility = Visibility.Hidden;
                                    broadcast.SimulatedTwilightTownPlus.Visibility = Visibility.Hidden;
                                }

                                //load settings from hints
                                foreach (string setting in settings)
                                {
                                    Console.WriteLine("setting found = " + setting);

                                    switch (setting)
                                    {
                                        case "PromiseCharm":
                                            PromiseCharmToggle(true);
                                            break;
                                        case "Level":
                                            {
                                                SoraHeartToggle(false);
                                            }
                                            break;
                                        case "ExcludeFrom50":
                                            {
                                                SoraHeartToggle(true);
                                                SoraLevel50Toggle(true);
                                            }
                                            break;
                                        case "ExcludeFrom99":
                                            {
                                                SoraHeartToggle(true);
                                                SoraLevel99Toggle(true);
                                            }
                                            break;
                                        case "Simulated Twilight Town":
                                            SimulatedToggle(true);
                                            break;
                                        case "Hundred Acre Wood":
                                            HundredAcreWoodToggle(true);
                                            break;
                                        case "Atlantica":
                                            AtlanticaToggle(true);
                                            break;
                                        case "Cavern of Remembrance":
                                            CavernToggle(true);
                                            break;
                                        case "Olympus Cups":
                                            OCCupsToggle(true);
                                            break;
                                        case "visit_locking":
                                            VisitLockToggle(true);
                                            break;
                                        case "Lingering Will (Terra)":
                                            TerraToggle(true);
                                            break;
                                        case "Puzzle":
                                            PuzzleToggle(true);
                                            break;
                                        case "Synthesis":
                                            SynthToggle(true);
                                            break;
                                        case "better_stt":
                                            betterSTTon = true;
                                            break;
                                        case "extra_ics":
                                            ExtraChecksToggle(true);
                                            break;
                                            //DEBUG! UPDATE LATER
                                            //case "Anti-Form":
                                            //    AntiFormToggle(true);
                                            //    break;
                                    }
                                    //if (setting.Key == "Second Chance & Once More ")
                                    //    AbilitiesToggle(true);
                                    //if (setting.Key == "Torn Pages")
                                    //    TornPagesToggle(true);
                                    //if (setting.Key == "Cure")
                                    //    CureToggle(true);
                                    //if (setting.Key == "Final Form")
                                    //    FinalFormToggle(true);
                                }
                            }

                            switch (hintObject["hintsType"].ToString())
                            {
                                case "Shananas":
                                    {
                                        SetMode(Mode.OpenKHAltHints);
                                        ShanHints(hintObject);
                                    }
                                    break;
                                case "JSmartee":
                                    {
                                        SetMode(Mode.OpenKHHints);
                                        JsmarteeHints(hintObject);
                                    }
                                    break;
                                case "Points":
                                    {
                                        SetMode(Mode.DAHints);
                                        PointsHints(hintObject);
                                    }
                                    break;
                                case "Path":
                                    {
                                        SetMode(Mode.PathHints);
                                        PathHints(hintObject);
                                    }
                                    break;
                                case "Spoiler":
                                    {
                                        SetMode(Mode.SpoilerHints);
                                        SpoilerHints(hintObject);
                                    }
                                    break;
                                case "Timed":
                                    {
                                        //incomplete
                                        //SetMode(Mode.TimeHints);
                                        //TimeHints(hintObject);
                                    }
                                    break;
                                default:
                                    break;
                            }

                            //better stt icon workaround
                            if (betterSTTon)
                            {
                                SimulatedTwilightTownPlus.Visibility = Visibility.Visible;
                                broadcast.SimulatedTwilightTownPlus.Visibility = Visibility.Visible;
                            }
                        }
                    }

                    if (entry.FullName.Equals("sys.yml"))
                    {
                        using (var reader2 = new StreamReader(entry.Open()))
                        {
                            string[] separatingStrings = { "- en: ", " ", "'", "{", "}", ":", "icon" };
                            string text1 = reader2.ReadLine();
                            string text2 = reader2.ReadLine();
                            string text = text1 + text2;
                            string[] hash = text.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                            //Set Icons
                            HashIcon1.SetResourceReference(ContentProperty, hash[0]);
                            HashIcon2.SetResourceReference(ContentProperty, hash[1]);
                            HashIcon3.SetResourceReference(ContentProperty, hash[2]);
                            HashIcon4.SetResourceReference(ContentProperty, hash[3]);
                            HashIcon5.SetResourceReference(ContentProperty, hash[4]);
                            HashIcon6.SetResourceReference(ContentProperty, hash[5]);
                            HashIcon7.SetResourceReference(ContentProperty, hash[6]);
                            SeedHashLoaded = true;

                            //make visible
                            if (SeedHashOption.IsChecked)
                            {
                                HashRow.Height = new GridLength(1.0, GridUnitType.Star);
                                SeedHashVisible = true;
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, int> GetItemPool = new Dictionary<string, int>()
        {
            {"Report1", 0},
            {"Report2", 0},
            {"Report3", 0},
            {"Report4", 0},
            {"Report5", 0},
            {"Report6", 0},
            {"Report7", 0},
            {"Report8", 0},
            {"Report9", 0},
            {"Report10", 0},
            {"Report11", 0},
            {"Report12", 0},
            {"Report13", 0},
            {"Fire1", 1},
            {"Fire2", 1},
            {"Fire3", 1},
            {"Blizzard1", 1},
            {"Blizzard2", 1},
            {"Blizzard3", 1},
            {"Thunder1", 1},
            {"Thunder2", 1},
            {"Thunder3", 1},
            {"Cure1", 1},
            {"Cure2", 1},
            {"Cure3", 1},
            {"HadesCup", 1},
            {"OlympusStone", 1},
            {"Reflect1", 2},
            {"Reflect2", 2},
            {"Reflect3", 2},
            {"Magnet1", 2},
            {"Magnet2", 2},
            {"Magnet3", 2},
            {"Valor", 2},
            {"Wisdom", 2},
            {"Limit", 2},
            {"Master", 2},
            {"Final", 2},
            {"Anti", 2},
            {"OnceMore", 2},
            {"SecondChance", 2},
            {"UnknownDisk", 3},
            {"TornPage1", 3},
            {"TornPage2", 3},
            {"TornPage3", 3},
            {"TornPage4", 3},
            {"TornPage5", 3},
            {"Baseball", 3},
            {"Lamp", 3},
            {"Ukulele", 3},
            {"Feather", 3},
            {"Connection", 3},
            {"Nonexistence", 3},
            {"Peace", 3},
            {"PromiseCharm", 3},
            {"BeastWep", 4},
            {"JackWep", 4},
            {"SimbaWep", 4},
            {"AuronWep", 4},
            {"MulanWep", 4},
            {"SparrowWep", 4},
            {"AladdinWep", 4},
            {"TronWep", 4},
            {"MembershipCard", 4},
            {"Picture", 4},
            {"IceCream", 4},
            {"Ghost_Report1", 5},
            {"Ghost_Report2", 5},
            {"Ghost_Report3", 5},
            {"Ghost_Report4", 5},
            {"Ghost_Report5", 5},
            {"Ghost_Report6", 5},
            {"Ghost_Report7", 5},
            {"Ghost_Report8", 5},
            {"Ghost_Report9", 5},
            {"Ghost_Report10", 5},
            {"Ghost_Report11", 5},
            {"Ghost_Report12", 5},
            {"Ghost_Report13", 5},
            {"Ghost_Fire1", 6},
            {"Ghost_Fire2", 6},
            {"Ghost_Fire3", 6},
            {"Ghost_Blizzard1", 6},
            {"Ghost_Blizzard2", 6},
            {"Ghost_Blizzard3", 6},
            {"Ghost_Thunder1", 6},
            {"Ghost_Thunder2", 6},
            {"Ghost_Thunder3", 6},
            {"Ghost_Cure1", 6},
            {"Ghost_Cure2", 6},
            {"Ghost_Cure3", 6},
            {"Ghost_HadesCup", 6},
            {"Ghost_OlympusStone", 6},
            {"Ghost_Reflect1", 7},
            {"Ghost_Reflect2", 7},
            {"Ghost_Reflect3", 7},
            {"Ghost_Magnet1", 7},
            {"Ghost_Magnet2", 7},
            {"Ghost_Magnet3", 7},
            {"Ghost_Valor", 7},
            {"Ghost_Wisdom", 7},
            {"Ghost_Limit", 7},
            {"Ghost_Master", 7},
            {"Ghost_Final", 7},
            {"Ghost_Anti", 7},
            {"Ghost_OnceMore", 7},
            {"Ghost_SecondChance", 7},
            {"Ghost_UnknownDisk", 8},
            {"Ghost_TornPage1", 8},
            {"Ghost_TornPage2", 8},
            {"Ghost_TornPage3", 8},
            {"Ghost_TornPage4", 8},
            {"Ghost_TornPage5", 8},
            {"Ghost_Baseball", 8},
            {"Ghost_Lamp", 8},
            {"Ghost_Ukulele", 8},
            {"Ghost_Feather", 8},
            {"Ghost_Connection", 8},
            {"Ghost_Nonexistence", 8},
            {"Ghost_Peace", 8},
            {"Ghost_PromiseCharm", 8},
            {"Ghost_BeastWep", 9},
            {"Ghost_JackWep", 9},
            {"Ghost_SimbaWep", 9},
            {"Ghost_AuronWep", 9},
            {"Ghost_MulanWep", 9},
            {"Ghost_SparrowWep", 9},
            {"Ghost_AladdinWep", 9},
            {"Ghost_TronWep", 9},
            {"Ghost_MembershipCard", 9},
            {"Ghost_Picture", 9},
            {"Ghost_IceCream", 9}
        };
    }
}
