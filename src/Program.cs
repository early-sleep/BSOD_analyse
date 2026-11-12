using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BsodAnalyzer
{
    internal static class Program
    {
        public const string Version = "1.2";

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
                return SelfTest(args);

            string tab = args.Length > 0 ? args[0].TrimStart('-', '/').ToLowerInvariant() : "";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(tab));
            return 0;
        }

        /// <summary>无界面自测：BsodAnalyzer.exe --selftest &lt;pkg&gt; &lt;endpoint&gt; &lt;key&gt; &lt;model&gt; [outfile]</summary>
        private static int SelfTest(string[] args)
        {
            string pkg = args.Length > 1 ? args[1] : "";
            string endpoint = args.Length > 2 ? args[2] : "https://api.deepseek.com/v1";
            string key = args.Length > 3 ? args[3] : "";
            string model = args.Length > 4 ? args[4] : "deepseek-chat";
            string outFile = args.Length > 5 ? args[5] : Path.Combine(Path.GetTempPath(), "bsodanalyzer-selftest.txt");

            StringBuilder log = new StringBuilder();
            try
            {
                List<FileItem> files = Logs.Tree(pkg);
                log.AppendLine("package: " + pkg);
                log.AppendLine("files  : " + files.Count);

                StringBuilder body = new StringBuilder();
                int picked = 0;
                foreach (FileItem f in files)
                {
                    if (picked >= 3) break;
                    if (!string.Equals(f.Name, "key_events.csv", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(f.Name, "Collection_Info.txt", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(f.Name, "power_settings.txt", StringComparison.OrdinalIgnoreCase)) continue;
                    string text = Logs.ReadText(Path.Combine(pkg, f.Rel), 512 * 1024);
                    if (text.Length > 6000) text = text.Substring(0, 6000);
                    body.AppendLine("=== FILE: " + f.Rel + " ===");
                    body.AppendLine(text);
                    picked++;
                }
                log.AppendLine("picked : " + picked + " file(s), " + body.Length + " chars");

                Settings s = new Settings();
                s.Endpoint = endpoint;
                s.Key = key;
                s.Model = model;

                string err;
                string answer = Ai.Ask(s,
                    "You are a Windows platform engineer. Answer in English with short bullets.",
                    "Analyse these logs.\r\n\r\n" + body + "\r\nQuestion: list the top 3 issues briefly.",
                    out err);

                if (answer == null) log.AppendLine("ERROR  : " + err);
                else log.AppendLine("ANSWER :\r\n" + answer);
            }
            catch (Exception ex)
            {
                log.AppendLine("EXCEPTION: " + ex);
            }

            File.WriteAllText(outFile, log.ToString(), new UTF8Encoding(false));
            return 0;
        }
    }
}
