using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace E7_Optimizer_Updater
{
    class Program
    {
        static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        static void Main(string[] args)
        {
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.UserAgent, "E7 Optimizer Updater");
            Console.WriteLine("Checking for updates...");
            string json = "";
            string latestver = "0";
            int count = 0;
            try
            {
                json = client.DownloadString("https://api.github.com/repos/Zarroc2762/E7-Gear-Optimizer/releases/latest");
                latestver = JObject.Parse(json).Value<string>("tag_name").Replace("v", "");
                count = latestver.Count(x => x == '.');
                if (count == 1)
                {
                    latestver += ".0.0";
                }
                else if (count == 2)
                {
                    latestver += ".0";
                }
                latestver = latestver.Replace(".", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            string ver = "0";
            try
            {
                ver = FileVersionInfo.GetVersionInfo("E7 Gear Optimizer.exe").FileVersion.Replace(".", "");
            }
            catch
            {
                Console.WriteLine("Could not find \"E7 Gear Optimizer.exe\"...");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            if (int.Parse(latestver) > int.Parse(ver))
            {
                try
                {
                    Console.WriteLine("Downloading update...");
                    string url = JObject.Parse(json)["assets"][0].Value<String>("browser_download_url");
                    client.DownloadFile(url, "release.rar");
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
                Console.WriteLine("Extracting files...");
                using (var archive = RarArchive.Open("release.rar"))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        if (File.Exists(entry.Key))
                        {
                            if (!IsFileLocked(new FileInfo(entry.Key)))
                            {
                                entry.WriteToDirectory(".\\", new ExtractionOptions()
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                            }
                            else
                            {
                                Console.WriteLine("Skipped " + entry.Key + " because it's in use by another Process");
                            }
                        }
                        else
                        {
                            entry.WriteToDirectory(".\\", new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }
                Console.WriteLine("Done!");
            }
            else
            {
                Console.WriteLine("No update available.");
            }

            try
            {
                File.Delete("release.rar");
                Process.Start("E7 Gear Optimizer.exe", "-updated");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            //Console.ReadKey(true);
        }
    }
}
