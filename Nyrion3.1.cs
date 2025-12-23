// NyrionShell - extended with 50 additional useful functions/commands
// Target: .NET Framework 4.0 (Console Application)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace NyrionShell
{
    class Program
    {
        static readonly string VERSION = "Nyrion v3.1 - Lion 0.4.0 (Created by Pingunix)";
        static readonly List<string> command_history = new List<string>();
        static readonly List<string> chat_log = new List<string>();

        // Track a running child process so Ctrl+C can stop it without killing the shell.
        static Process _childProcess = null;

        class CommandInfo
        {
            public string Usage;
            public string Description;
            public Action<List<string>> Handler;

            public CommandInfo(string usage, string description, Action<List<string>> handler)
            {
                Usage = usage;
                Description = description;
                Handler = handler;
            }
        }

        static Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        // ────── Basic Display ──────
        static void ShowLogo()
        {
            Console.WriteLine(@"
██        ██
████      ██    
██  ██    ██
██    ██  ██
██      ████
██        ██
██        ██
");
            Console.WriteLine("Welcome to " + VERSION);
            Console.WriteLine("Type 'lion help' to begin.\n");
        }

        // ────── Existing Command Handlers (translated) ──────
        static void ShowHelp()
        {
            Console.WriteLine("\nAvailable Commands:\n");

            // Print in a stable-ish order
            List<string> keys = new List<string>(_commands.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (string k in keys)
            {
                CommandInfo ci = _commands[k];
                Console.WriteLine(ci.Usage);
                Console.WriteLine("  - " + ci.Description);
            }

            Console.WriteLine();
        }

        static void ShowSpecs()
        {
            string os = Environment.OSVersion.Platform + " " + Environment.OSVersion.Version;
            string cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ??
                         Environment.GetEnvironmentVariable("HOSTTYPE") ??
                         "Unknown";
            string dotnet = Environment.Version.ToString();

            Console.WriteLine("OS: " + os);
            Console.WriteLine("CPU: " + cpu);
            Console.WriteLine(".NET: " + dotnet);
        }

        static void ShowVersion()
        {
            Console.WriteLine("Nyrion Shell - " + VERSION);
        }

        static void ShowBase()
        {
            Console.WriteLine("Platform: " + Environment.OSVersion.ToString());
        }

        static void OpenWeb()
        {
            try { Process.Start("https://www.google.com"); }
            catch (Exception e) { Console.WriteLine("Error opening browser: " + e.Message); }
        }

        static bool IsWindows()
        {
            PlatformID p = Environment.OSVersion.Platform;
            return p == PlatformID.Win32NT || p == PlatformID.Win32Windows || p == PlatformID.Win32S || p == PlatformID.WinCE;
        }

        static void LaunchEditor()
        {
            string editor = IsWindows() ? "notepad" : "nano";
            try { Process.Start(editor); }
            catch (Exception e) { Console.WriteLine("Error launching editor (" + editor + "): " + e.Message); }
        }

        static void FileExplorer()
        {
            Console.Write("Path (leave blank for current): ");
            string path = (Console.ReadLine() ?? "").Trim();
            if (path.Length == 0) path = ".";

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Invalid folder path.");
                return;
            }

            Console.WriteLine("\nFiles in " + Path.GetFullPath(path) + ":");
            try
            {
                foreach (string item in Directory.GetFileSystemEntries(path))
                    Console.WriteLine(" - " + Path.GetFileName(item));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error listing files: " + e.Message);
            }
        }

        static void CreateFileInteractive()
        {
            Console.Write("Enter new file name: ");
            string name = (Console.ReadLine() ?? "").Trim();
            if (name.Length == 0)
            {
                Console.WriteLine("File name cannot be empty.");
                return;
            }

            try
            {
                using (FileStream fs = new FileStream(name, FileMode.Create, FileAccess.Write)) { }
                Console.WriteLine("File '" + name + "' created.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error creating file: " + e.Message);
            }
        }

        static void CreateFolderInteractive()
        {
            Console.Write("Enter new folder name: ");
            string name = (Console.ReadLine() ?? "").Trim();
            if (name.Length == 0)
            {
                Console.WriteLine("Folder name cannot be empty.");
                return;
            }

            try
            {
                Directory.CreateDirectory(name);
                Console.WriteLine("Folder '" + name + "' created.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error creating folder: " + e.Message);
            }
        }

        static void ReadFile(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("File not found.");
                return;
            }

            try
            {
                Console.WriteLine("\n--- File Content ---");
                Console.WriteLine(File.ReadAllText(filename));
                Console.WriteLine("---------------------");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading file: " + e.Message);
            }
        }

        static void Chatbot()
        {
            Console.WriteLine("Chatbot: Type 'exit' to stop chatting.");
            while (true)
            {
                Console.Write("You: ");
                string userRaw = Console.ReadLine() ?? "";
                string user = userRaw.Trim().ToLowerInvariant();

                if (user == "exit")
                {
                    Console.WriteLine("Chatbot: Bye!");
                    break;
                }

                string reply;
                if (user.IndexOf("hello", StringComparison.OrdinalIgnoreCase) >= 0)
                    reply = "Hi there!";
                else if (user.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0)
                    reply = "The time is " + DateTime.Now.ToString("HH:mm:ss");
                else if (user.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0)
                    reply = "I'm NyrionBot!";
                else
                    reply = "I don't understand that yet.";

                Console.WriteLine("Chatbot: " + reply);
                chat_log.Add("You: " + user + "\nBot: " + reply + "\n");
            }
        }

        static void ViewChatlog()
        {
            if (chat_log.Count == 0)
            {
                Console.WriteLine("No chat history yet.");
                return;
            }

            try
            {
                File.WriteAllText("chatlog.txt", string.Join("\n", chat_log.ToArray()));
                Console.WriteLine("Chat log saved to 'chatlog.txt':");

                int start = Math.Max(0, chat_log.Count - 5);
                for (int i = start; i < chat_log.Count; i++)
                    Console.WriteLine(chat_log[i]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving chat log: " + e.Message);
            }
        }

        static void AsciiDraw()
        {
            char[,] grid = new char[10, 20];
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 20; x++)
                    grid[y, x] = ' ';

            Console.WriteLine("ASCII Draw Mode. Type 'save' to save, 'exit' to quit.");
            while (true)
            {
                for (int y = 0; y < 10; y++)
                {
                    char[] row = new char[20];
                    for (int x = 0; x < 20; x++)
                        row[x] = grid[y, x];
                    Console.WriteLine(new string(row));
                }

                Console.Write("Draw (x y char): ");
                string cmd = (Console.ReadLine() ?? "").Trim();

                if (cmd == "exit")
                {
                    break;
                }
                else if (cmd == "save")
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter("drawing.txt"))
                        {
                            for (int y = 0; y < 10; y++)
                            {
                                char[] row = new char[20];
                                for (int x = 0; x < 20; x++)
                                    row[x] = grid[y, x];
                                sw.WriteLine(new string(row));
                            }
                        }
                        Console.WriteLine("Drawing saved to 'drawing.txt'");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error saving drawing: " + e.Message);
                    }
                }
                else
                {
                    try
                    {
                        string[] parts = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3) throw new FormatException();

                        int x = int.Parse(parts[0]);
                        int y = int.Parse(parts[1]);
                        string chStr = parts[2];
                        char ch = (chStr.Length > 0) ? chStr[0] : ' ';

                        if (0 <= y && y < 10 && 0 <= x && x < 20)
                            grid[y, x] = ch;
                        else
                            Console.WriteLine("Out of bounds.");
                    }
                    catch
                    {
                        Console.WriteLine("Invalid format. Use: x y char");
                    }
                }
            }
        }

        static void ShowHistory()
        {
            Console.WriteLine("Last 5 commands:");
            int start = Math.Max(0, command_history.Count - 5);
            for (int i = start; i < command_history.Count; i++)
                Console.WriteLine(" - " + command_history[i]);
        }

        // ────── Helpers ──────
        static List<string> Tokenize(string input)
        {
            List<string> tokens = new List<string>();
            if (input == null) return tokens;

            StringBuilder cur = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (cur.Length > 0)
                    {
                        tokens.Add(cur.ToString());
                        cur.Length = 0;
                    }
                    continue;
                }

                cur.Append(c);
            }

            if (cur.Length > 0)
                tokens.Add(cur.ToString());

            return tokens;
        }

        static string HumanBytes(long bytes)
        {
            double b = bytes;
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
            int u = 0;
            while (b >= 1024 && u < units.Length - 1)
            {
                b /= 1024;
                u++;
            }
            return b.ToString("0.##") + " " + units[u];
        }

        static bool Confirm(string prompt)
        {
            Console.Write(prompt + " (y/N): ");
            string ans = (Console.ReadLine() ?? "").Trim();
            return ans.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                   ans.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        static void RunChildProcessAndStream(string fileName, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = fileName;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            Process p = new Process();
            p.StartInfo = psi;

            try
            {
                _childProcess = p;
                p.Start();

                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();

                p.WaitForExit();

                if (!string.IsNullOrEmpty(stdout))
                    Console.Write(stdout);
                if (!string.IsNullOrEmpty(stderr))
                    Console.Write(stderr);

                Console.WriteLine("\n[exit code: " + p.ExitCode + "]");
            }
            finally
            {
                try { if (_childProcess == p) _childProcess = null; }
                catch { }
                try { p.Close(); }
                catch { }
            }
        }

        static string CombineRemainder(List<string> args, int startIndex)
        {
            if (args == null || startIndex >= args.Count) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = startIndex; i < args.Count; i++)
            {
                if (i > startIndex) sb.Append(" ");
                sb.Append(args[i]);
            }
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────
        // 50 NEW USEFUL FUNCTIONS / COMMANDS
        // ─────────────────────────────────────────────────────────────

        // 1
        static void CmdPwd(List<string> args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
        }

        // 2
        static void CmdCd(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion cd <path>"); return; }
            string path = args[0];
            try
            {
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Directory not found.");
                    return;
                }
                Directory.SetCurrentDirectory(path);
                Console.WriteLine(Directory.GetCurrentDirectory());
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 3
        static void CmdLs(List<string> args)
        {
            string path = (args.Count >= 1) ? args[0] : ".";
            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                Console.WriteLine("Listing: " + di.FullName);

                foreach (DirectoryInfo d in di.GetDirectories())
                    Console.WriteLine("[D] " + d.Name);

                foreach (FileInfo f in di.GetFiles())
                    Console.WriteLine("[F] " + f.Name + "  (" + HumanBytes(f.Length) + ")");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 4
        static void CmdTree(List<string> args)
        {
            string path = (args.Count >= 1) ? args[0] : ".";
            int depth = 3;
            if (args.Count >= 2)
            {
                int d;
                if (int.TryParse(args[1], out d) && d >= 0) depth = d;
            }

            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            Console.WriteLine(Path.GetFullPath(path));
            PrintTree(path, "", depth);
        }

        static void PrintTree(string path, string indent, int depth)
        {
            if (depth == 0) return;

            try
            {
                string[] entries = Directory.GetFileSystemEntries(path);
                for (int i = 0; i < entries.Length; i++)
                {
                    string e = entries[i];
                    bool last = (i == entries.Length - 1);
                    string branch = last ? "└─ " : "├─ ";
                    Console.WriteLine(indent + branch + Path.GetFileName(e));

                    if (Directory.Exists(e))
                        PrintTree(e, indent + (last ? "   " : "│  "), depth - 1);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(indent + "└─ <access denied>");
            }
            catch (Exception ex)
            {
                Console.WriteLine(indent + "└─ <error: " + ex.Message + ">");
            }
        }

        // 5
        static void CmdStat(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion stat <path>"); return; }
            string path = args[0];

            try
            {
                if (File.Exists(path))
                {
                    FileInfo fi = new FileInfo(path);
                    Console.WriteLine("File: " + fi.FullName);
                    Console.WriteLine("Size: " + HumanBytes(fi.Length));
                    Console.WriteLine("Created: " + fi.CreationTime);
                    Console.WriteLine("Modified: " + fi.LastWriteTime);
                    Console.WriteLine("ReadOnly: " + fi.IsReadOnly);
                }
                else if (Directory.Exists(path))
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    Console.WriteLine("Directory: " + di.FullName);
                    Console.WriteLine("Created: " + di.CreationTime);
                    Console.WriteLine("Modified: " + di.LastWriteTime);
                }
                else
                {
                    Console.WriteLine("Not found.");
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 6
        static void CmdRm(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion rm <file>"); return; }
            string path = args[0];

            if (!File.Exists(path))
            {
                Console.WriteLine("File not found.");
                return;
            }

            if (!Confirm("Delete file '" + path + "'?")) return;

            try
            {
                File.Delete(path);
                Console.WriteLine("Deleted.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 7
        static void CmdRmdir(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion rmdir <folder>"); return; }
            string path = args[0];

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Folder not found.");
                return;
            }

            if (!Confirm("Delete folder '" + path + "' recursively?")) return;

            try
            {
                Directory.Delete(path, true);
                Console.WriteLine("Deleted.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 8
        static void CmdCopy(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion cp <src> <dst>"); return; }
            string src = args[0], dst = args[1];

            try
            {
                if (File.Exists(src))
                {
                    File.Copy(src, dst, true);
                    Console.WriteLine("Copied file.");
                    return;
                }

                if (Directory.Exists(src))
                {
                    CopyDirectoryRecursive(src, dst);
                    Console.WriteLine("Copied folder.");
                    return;
                }

                Console.WriteLine("Source not found.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        static void CopyDirectoryRecursive(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);

            foreach (string file in Directory.GetFiles(srcDir))
            {
                string name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(dstDir, name), true);
            }

            foreach (string dir in Directory.GetDirectories(srcDir))
            {
                string name = Path.GetFileName(dir);
                CopyDirectoryRecursive(dir, Path.Combine(dstDir, name));
            }
        }

        // 9
        static void CmdMove(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion mv <src> <dst>"); return; }
            string src = args[0], dst = args[1];

            try
            {
                if (File.Exists(src))
                {
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                    Console.WriteLine("Moved file.");
                }
                else if (Directory.Exists(src))
                {
                    if (Directory.Exists(dst)) Directory.Delete(dst, true);
                    Directory.Move(src, dst);
                    Console.WriteLine("Moved folder.");
                }
                else
                {
                    Console.WriteLine("Source not found.");
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 10
        static void CmdRename(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion ren <old> <new>"); return; }
            string oldPath = args[0], newPath = args[1];

            try
            {
                if (File.Exists(oldPath))
                {
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(oldPath, newPath);
                    Console.WriteLine("Renamed file.");
                }
                else if (Directory.Exists(oldPath))
                {
                    if (Directory.Exists(newPath)) Directory.Delete(newPath, true);
                    Directory.Move(oldPath, newPath);
                    Console.WriteLine("Renamed folder.");
                }
                else Console.WriteLine("Not found.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 11
        static void CmdTouch(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion touch <file>"); return; }
            string file = args[0];

            try
            {
                if (!File.Exists(file))
                    using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write)) { }

                File.SetLastWriteTime(file, DateTime.Now);
                Console.WriteLine("Touched: " + file);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 12
        static void CmdCat(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion cat <file>"); return; }
            ReadFile(args[0]);
        }

        // 13
        static void CmdHead(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion head <file> [n]"); return; }
            string file = args[0];
            int n = 10;
            if (args.Count >= 2) int.TryParse(args[1], out n);
            if (n < 0) n = 0;

            if (!File.Exists(file)) { Console.WriteLine("File not found."); return; }

            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    for (int i = 0; i < n; i++)
                    {
                        string line = sr.ReadLine();
                        if (line == null) break;
                        Console.WriteLine(line);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 14
        static void CmdTail(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion tail <file> [n]"); return; }
            string file = args[0];
            int n = 10;
            if (args.Count >= 2) int.TryParse(args[1], out n);
            if (n < 0) n = 0;

            if (!File.Exists(file)) { Console.WriteLine("File not found."); return; }

            try
            {
                // Simple implementation: read all lines then print last n
                string[] lines = File.ReadAllLines(file);
                int start = Math.Max(0, lines.Length - n);
                for (int i = start; i < lines.Length; i++)
                    Console.WriteLine(lines[i]);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 15
        static void CmdWrite(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion write <file> <text>"); return; }
            string file = args[0];
            string text = CombineRemainder(args, 1);

            try
            {
                File.WriteAllText(file, text + Environment.NewLine);
                Console.WriteLine("Wrote: " + file);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 16
        static void CmdAppend(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion append <file> <text>"); return; }
            string file = args[0];
            string text = CombineRemainder(args, 1);

            try
            {
                File.AppendAllText(file, text + Environment.NewLine);
                Console.WriteLine("Appended: " + file);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 17
        static void CmdLineCount(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion lines <file>"); return; }
            string file = args[0];
            if (!File.Exists(file)) { Console.WriteLine("File not found."); return; }

            try
            {
                long count = 0;
                using (StreamReader sr = new StreamReader(file))
                {
                    while (sr.ReadLine() != null) count++;
                }
                Console.WriteLine("Lines: " + count);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 18
        static void CmdGrep(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion grep <text> <file>"); return; }
            string needle = args[0];
            string file = args[1];

            if (!File.Exists(file)) { Console.WriteLine("File not found."); return; }

            try
            {
                int lineNo = 0;
                using (StreamReader sr = new StreamReader(file))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNo++;
                        if (line.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                            Console.WriteLine(lineNo.ToString() + ": " + line);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        static bool LooksBinary(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buf = new byte[1024];
                    int r = fs.Read(buf, 0, buf.Length);
                    for (int i = 0; i < r; i++)
                        if (buf[i] == 0) return true;
                }
            }
            catch { }
            return false;
        }

        // 19
        static void CmdGrepRecursive(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion grepr <text> [path]"); return; }
            string needle = args[0];
            string path = (args.Count >= 2) ? args[1] : ".";

            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            int hits = 0;
            try
            {
                foreach (string file in SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.Length > 5 * 1024 * 1024) continue; // skip huge files
                        if (LooksBinary(file)) continue;

                        int lineNo = 0;
                        using (StreamReader sr = new StreamReader(file))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                lineNo++;
                                if (line.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    Console.WriteLine(file + ":" + lineNo.ToString() + ": " + line);
                                    hits++;
                                }
                            }
                        }
                    }
                    catch { /* keep going */ }
                }
                Console.WriteLine("Matches: " + hits);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 20
        static void CmdFind(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion find <pattern> [path]"); return; }
            string pattern = args[0];
            string path = (args.Count >= 2) ? args[1] : ".";

            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            int count = 0;
            try
            {
                foreach (string f in SafeEnumerateFiles(path, pattern, SearchOption.AllDirectories))
                {
                    Console.WriteLine(f);
                    count++;
                }
                Console.WriteLine("Found: " + count);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        static IEnumerable<string> SafeEnumerateFiles(string path, string pattern, SearchOption opt)
        {
            // Avoid dying on UnauthorizedAccessException while recursing.
            Stack<string> dirs = new Stack<string>();
            dirs.Push(path);

            while (dirs.Count > 0)
            {
                string current = dirs.Pop();

                string[] files = null;
                try { files = Directory.GetFiles(current, pattern); }
                catch { files = null; }

                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                        yield return files[i];
                }

                if (opt == SearchOption.AllDirectories)
                {
                    string[] subdirs = null;
                    try { subdirs = Directory.GetDirectories(current); }
                    catch { subdirs = null; }

                    if (subdirs != null)
                    {
                        for (int i = 0; i < subdirs.Length; i++)
                            dirs.Push(subdirs[i]);
                    }
                }
            }
        }

        // 21
        static void CmdDirSize(List<string> args)
        {
            string path = (args.Count >= 1) ? args[0] : ".";
            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            try
            {
                long total = 0;
                foreach (string f in SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(f).Length; }
                    catch { }
                }
                Console.WriteLine("Directory size: " + HumanBytes(total) + " (" + total.ToString() + " bytes)");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 22
        static void CmdHash(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion hash <file> [md5|sha1|sha256]"); return; }
            string file = args[0];
            string algo = (args.Count >= 2) ? args[1].ToLowerInvariant() : "sha256";

            if (!File.Exists(file)) { Console.WriteLine("File not found."); return; }

            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (HashAlgorithm ha = CreateHashAlgorithm(algo))
                {
                    if (ha == null)
                    {
                        Console.WriteLine("Unknown algorithm. Use md5, sha1, sha256.");
                        return;
                    }

                    byte[] hash = ha.ComputeHash(fs);
                    Console.WriteLine(algo.ToUpperInvariant() + ": " + BytesToHex(hash));
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        static HashAlgorithm CreateHashAlgorithm(string algo)
        {
            if (algo == "md5") return MD5.Create();
            if (algo == "sha1") return SHA1.Create();
            if (algo == "sha256") return SHA256.Create();
            return null;
        }

        static string BytesToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        // 23
        static void CmdBase64Encode(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion base64enc <inputFile> <outputFile>"); return; }
            string input = args[0], output = args[1];

            if (!File.Exists(input)) { Console.WriteLine("Input file not found."); return; }

            try
            {
                byte[] data = File.ReadAllBytes(input);
                string b64 = Convert.ToBase64String(data);
                File.WriteAllText(output, b64);
                Console.WriteLine("Base64 written: " + output);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 24
        static void CmdBase64Decode(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion base64dec <inputFile> <outputFile>"); return; }
            string input = args[0], output = args[1];

            if (!File.Exists(input)) { Console.WriteLine("Input file not found."); return; }

            try
            {
                string b64 = File.ReadAllText(input);
                byte[] data = Convert.FromBase64String(b64);
                File.WriteAllBytes(output, data);
                Console.WriteLine("Decoded file written: " + output);
            }
            catch (FormatException)
            {
                Console.WriteLine("Input is not valid Base64.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 25
        static void CmdFindDuplicates(List<string> args)
        {
            string path = (args.Count >= 1) ? args[0] : ".";
            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            try
            {
                // Group by size first (fast), then hash only collisions.
                Dictionary<long, List<string>> bySize = new Dictionary<long, List<string>>();

                foreach (string f in SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        long len = new FileInfo(f).Length;
                        List<string> list;
                        if (!bySize.TryGetValue(len, out list))
                        {
                            list = new List<string>();
                            bySize[len] = list;
                        }
                        list.Add(f);
                    }
                    catch { }
                }

                int groups = 0;
                foreach (KeyValuePair<long, List<string>> kv in bySize)
                {
                    if (kv.Value.Count < 2) continue;

                    Dictionary<string, List<string>> byHash = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        string file = kv.Value[i];
                        try
                        {
                            string h = QuickMD5(file);
                            List<string> list;
                            if (!byHash.TryGetValue(h, out list))
                            {
                                list = new List<string>();
                                byHash[h] = list;
                            }
                            list.Add(file);
                        }
                        catch { }
                    }

                    foreach (KeyValuePair<string, List<string>> hk in byHash)
                    {
                        if (hk.Value.Count < 2) continue;
                        groups++;
                        Console.WriteLine("\nDuplicate group (size " + kv.Key.ToString() + " bytes, md5 " + hk.Key + "):");
                        foreach (string f in hk.Value)
                            Console.WriteLine("  " + f);
                    }
                }

                if (groups == 0) Console.WriteLine("No duplicates found (by size+md5).");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        static string QuickMD5(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (MD5 md5 = MD5.Create())
            {
                return BytesToHex(md5.ComputeHash(fs));
            }
        }

        // 26
        static void CmdRecentFiles(List<string> args)
        {
            string path = (args.Count >= 1) ? args[0] : ".";
            int n = 10;
            if (args.Count >= 2) int.TryParse(args[1], out n);
            if (n <= 0) n = 10;

            if (!Directory.Exists(path)) { Console.WriteLine("Invalid folder path."); return; }

            try
            {
                List<FileInfo> files = new List<FileInfo>();
                foreach (string f in SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { files.Add(new FileInfo(f)); } catch { }
                }

                files.Sort(delegate (FileInfo a, FileInfo b)
                {
                    return b.LastWriteTime.CompareTo(a.LastWriteTime);
                });

                int take = Math.Min(n, files.Count);
                for (int i = 0; i < take; i++)
                {
                    FileInfo fi = files[i];
                    Console.WriteLine(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + "  " + fi.FullName);
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 27
        static void CmdBackupFile(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion backup <srcFile> <dstFolder>"); return; }
            string src = args[0], dstFolder = args[1];

            if (!File.Exists(src)) { Console.WriteLine("Source file not found."); return; }
            if (!Directory.Exists(dstFolder)) { Console.WriteLine("Destination folder not found."); return; }

            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseName = Path.GetFileName(src);
                string dst = Path.Combine(dstFolder, baseName + "." + stamp + ".bak");
                File.Copy(src, dst, false);
                Console.WriteLine("Backup created: " + dst);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 28
        static void CmdTinyEditor(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion ted <file>"); return; }
            string file = args[0];

            List<string> lines = new List<string>();
            try
            {
                if (File.Exists(file))
                    lines.AddRange(File.ReadAllLines(file));
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not load file: " + e.Message);
                return;
            }

            Console.WriteLine("Tiny Editor: " + file);
            Console.WriteLine("Commands: :p, :p <from> <to>, :a <text>, :i <line> <text>, :d <line>, :w, :q, :wq, :h");

            while (true)
            {
                Console.Write("ted> ");
                string input = Console.ReadLine() ?? "";
                input = input.Trim();

                if (input == ":q") break;

                if (input == ":h")
                {
                    Console.WriteLine(":p                 print all");
                    Console.WriteLine(":p <from> <to>      print range (1-based)");
                    Console.WriteLine(":a <text>           append line");
                    Console.WriteLine(":i <line> <text>    insert before line (1-based)");
                    Console.WriteLine(":d <line>           delete line (1-based)");
                    Console.WriteLine(":w                 save");
                    Console.WriteLine(":wq                save + quit");
                    continue;
                }

                if (input == ":p")
                {
                    for (int i = 0; i < lines.Count; i++)
                        Console.WriteLine((i + 1).ToString() + ": " + lines[i]);
                    continue;
                }

                if (input.StartsWith(":p "))
                {
                    List<string> t = Tokenize(input);
                    if (t.Count >= 3)
                    {
                        int from, to;
                        if (int.TryParse(t[1], out from) && int.TryParse(t[2], out to))
                        {
                            from = Math.Max(1, from);
                            to = Math.Min(lines.Count, to);
                            for (int i = from; i <= to; i++)
                                Console.WriteLine(i.ToString() + ": " + lines[i - 1]);
                        }
                        else Console.WriteLine("Bad numbers.");
                    }
                    else Console.WriteLine("Usage: :p <from> <to>");
                    continue;
                }

                if (input.StartsWith(":a "))
                {
                    string text = input.Substring(3);
                    lines.Add(text);
                    continue;
                }

                if (input.StartsWith(":i "))
                {
                    List<string> t = Tokenize(input);
                    if (t.Count >= 3)
                    {
                        int line;
                        if (!int.TryParse(t[1], out line)) { Console.WriteLine("Bad line number."); continue; }
                        string text = input.Substring(input.IndexOf(t[2], StringComparison.Ordinal));
                        line = Math.Max(1, Math.Min(lines.Count + 1, line));
                        lines.Insert(line - 1, text);
                    }
                    else Console.WriteLine("Usage: :i <line> <text>");
                    continue;
                }

                if (input.StartsWith(":d "))
                {
                    List<string> t = Tokenize(input);
                    if (t.Count >= 2)
                    {
                        int line;
                        if (int.TryParse(t[1], out line))
                        {
                            if (line < 1 || line > lines.Count) { Console.WriteLine("Out of range."); continue; }
                            lines.RemoveAt(line - 1);
                        }
                        else Console.WriteLine("Bad line number.");
                    }
                    else Console.WriteLine("Usage: :d <line>");
                    continue;
                }

                if (input == ":w" || input == ":wq")
                {
                    try
                    {
                        File.WriteAllLines(file, lines.ToArray());
                        Console.WriteLine("Saved.");
                        if (input == ":wq") break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Save failed: " + e.Message);
                    }
                    continue;
                }

                Console.WriteLine("Unknown ted command. Use :h");
            }
        }

        // 29
        static void CmdDate(List<string> args)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // 30
        static void CmdUptime(List<string> args)
        {
            // Environment.TickCount is ms since system start (wraps ~24.9 days, but still useful).
            int ms = Environment.TickCount;
            if (ms < 0) ms = int.MaxValue + ms; // best-effort on wrap
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            Console.WriteLine("Uptime: " + ((int)t.TotalDays).ToString() + "d " + t.Hours.ToString() + "h " + t.Minutes.ToString() + "m " + t.Seconds.ToString() + "s");
        }

        // 31
        static void CmdWhoami(List<string> args)
        {
            Console.WriteLine(Environment.UserName);
        }

        // 32
        static void CmdHostname(List<string> args)
        {
            Console.WriteLine(Environment.MachineName);
        }

        // 33
        static void CmdEnv(List<string> args)
        {
            try
            {
                var vars = Environment.GetEnvironmentVariables();
                List<string> keys = new List<string>();
                foreach (var k in vars.Keys) keys.Add(k.ToString());
                keys.Sort(StringComparer.OrdinalIgnoreCase);

                foreach (string k in keys)
                    Console.WriteLine(k + "=" + (vars[k] ?? "").ToString());
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 34
        static void CmdGetEnv(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion getenv <key>"); return; }
            Console.WriteLine(Environment.GetEnvironmentVariable(args[0]) ?? "");
        }

        // 35
        static void CmdSetEnv(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion setenv <key> <value>"); return; }
            string key = args[0];
            string value = CombineRemainder(args, 1);

            try
            {
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
                Console.WriteLine("Set (process): " + key);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 36
        static void CmdUnsetEnv(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion unsetenv <key>"); return; }
            try
            {
                Environment.SetEnvironmentVariable(args[0], null, EnvironmentVariableTarget.Process);
                Console.WriteLine("Unset (process): " + args[0]);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 37
        static void CmdDrives(List<string> args)
        {
            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!d.IsReady)
                        {
                            Console.WriteLine(d.Name + " (not ready)");
                            continue;
                        }

                        Console.WriteLine(
                            d.Name + "  " +
                            d.DriveType + "  " +
                            d.DriveFormat + "  " +
                            "Free " + HumanBytes(d.AvailableFreeSpace) + "/" + HumanBytes(d.TotalSize));
                    }
                    catch
                    {
                        Console.WriteLine(d.Name + " (error reading drive)");
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 38
        static void CmdMemory(List<string> args)
        {
            try
            {
                Process p = Process.GetCurrentProcess();
                Console.WriteLine("WorkingSet: " + HumanBytes(p.WorkingSet64));
                Console.WriteLine("PrivateMemory: " + HumanBytes(p.PrivateMemorySize64));
                Console.WriteLine("PagedMemory: " + HumanBytes(p.PagedMemorySize64));
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 39
        static void CmdProcessList(List<string> args)
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                Array.Sort(procs, delegate (Process a, Process b) { return a.Id.CompareTo(b.Id); });

                foreach (Process p in procs)
                {
                    string name = "";
                    try { name = p.ProcessName; } catch { name = "<unknown>"; }
                    Console.WriteLine(p.Id.ToString().PadLeft(6) + "  " + name);
                }
                Console.WriteLine("Total: " + procs.Length);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 40
        static void CmdKill(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion kill <pid>"); return; }
            int pid;
            if (!int.TryParse(args[0], out pid)) { Console.WriteLine("Invalid pid."); return; }

            if (!Confirm("Kill process " + pid.ToString() + "?")) return;

            try
            {
                Process p = Process.GetProcessById(pid);
                p.Kill();
                Console.WriteLine("Killed.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 41
        static void CmdStart(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion start <exeOrUrlOrPath> [args...]"); return; }
            string target = args[0];
            string a = (args.Count > 1) ? CombineRemainder(args, 1) : "";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = target;
                psi.Arguments = a;
                psi.UseShellExecute = true; // allow shell handling
                Process.Start(psi);
                Console.WriteLine("Started.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 42
        static void CmdRun(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion run <commandline>"); return; }
            string cmdline = CombineRemainder(args, 0);

            try
            {
                if (IsWindows())
                {
                    RunChildProcessAndStream("cmd.exe", "/c " + cmdline);
                }
                else
                {
                    RunChildProcessAndStream("/bin/bash", "-c \"" + cmdline.Replace("\"", "\\\"") + "\"");
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 43
        static void CmdOpenPath(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion open <path>"); return; }
            string path = args[0];

            try
            {
                if (IsWindows())
                {
                    Process.Start(path);
                }
                else
                {
                    // best-effort for unix-like desktops
                    string opener = File.Exists("/usr/bin/xdg-open") ? "xdg-open" : "open";
                    Process.Start(opener, "\"" + path.Replace("\"", "\\\"") + "\"");
                }
                Console.WriteLine("Opened.");
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 44
        static void CmdPing(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion ping <host> [count]"); return; }
            string host = args[0];
            int count = 4;
            if (args.Count >= 2) int.TryParse(args[1], out count);
            if (count <= 0) count = 4;

            try
            {
                using (Ping p = new Ping())
                {
                    for (int i = 0; i < count; i++)
                    {
                        PingReply r = p.Send(host, 2000);
                        if (r.Status == IPStatus.Success)
                            Console.WriteLine("Reply from " + r.Address + ": time=" + r.RoundtripTime + "ms");
                        else
                            Console.WriteLine("Ping failed: " + r.Status);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 45
        static void CmdDns(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion dns <host>"); return; }
            string host = args[0];

            try
            {
                IPAddress[] addrs = Dns.GetHostAddresses(host);
                foreach (IPAddress a in addrs)
                    Console.WriteLine(a.ToString());
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 46
        static void CmdIpInfo(List<string> args)
        {
            try
            {
                NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < nis.Length; i++)
                {
                    NetworkInterface ni = nis[i];
                    Console.WriteLine("\n" + ni.Name + " (" + ni.NetworkInterfaceType + ") - " + ni.OperationalStatus);

                    IPInterfaceProperties props;
                    try { props = ni.GetIPProperties(); }
                    catch { continue; }

                    foreach (UnicastIPAddressInformation ua in props.UnicastAddresses)
                        Console.WriteLine("  IP: " + ua.Address);
                    foreach (GatewayIPAddressInformation gw in props.GatewayAddresses)
                        Console.WriteLine("  GW: " + gw.Address);
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 47
        static void CmdDownload(List<string> args)
        {
            if (args.Count < 2) { Console.WriteLine("Usage: lion download <url> <file>"); return; }
            string url = args[0], file = args[1];

            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(url, file);
                }
                Console.WriteLine("Downloaded to: " + file);
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 48
        static void CmdHttpGet(List<string> args)
        {
            if (args.Count < 1) { Console.WriteLine("Usage: lion httpget <url>"); return; }
            string url = args[0];

            try
            {
                using (WebClient wc = new WebClient())
                {
                    string s = wc.DownloadString(url);
                    Console.WriteLine("Length: " + s.Length);
                    int max = Math.Min(4000, s.Length);
                    Console.WriteLine(s.Substring(0, max));
                    if (s.Length > max) Console.WriteLine("\n--- truncated ---");
                }
            }
            catch (Exception e) { Console.WriteLine("Error: " + e.Message); }
        }

        // 49
        static void CmdSfcScan(List<string> args)
        {
            if (!IsWindows())
            {
                Console.WriteLine("sfc is Windows-only.");
                return;
            }
            Console.WriteLine("Running: sfc /scannow (may require admin)");
            RunChildProcessAndStream("sfc.exe", "/scannow");
        }

        // 50
        static void CmdDismRestoreHealth(List<string> args)
        {
            if (!IsWindows())
            {
                Console.WriteLine("dism is Windows-only.");
                return;
            }
            Console.WriteLine("Running: dism /Online /Cleanup-Image /RestoreHealth (may require admin)");
            RunChildProcessAndStream("dism.exe", "/Online /Cleanup-Image /RestoreHealth");
        }

        // ────── Command Registration ──────
        static void RegisterCommands()
        {
            // Original commands
            _commands["help"] = new CommandInfo("lion help", "Show this help list", delegate (List<string> a) { ShowHelp(); });
            _commands["specs"] = new CommandInfo("lion specs", "Show system specs", delegate (List<string> a) { ShowSpecs(); });
            _commands["version"] = new CommandInfo("lion version", "Show version info", delegate (List<string> a) { ShowVersion(); });
            _commands["base"] = new CommandInfo("lion base", "Show OS info", delegate (List<string> a) { ShowBase(); });
            _commands["web"] = new CommandInfo("lion web", "Open browser", delegate (List<string> a) { OpenWeb(); });
            _commands["edit"] = new CommandInfo("lion edit", "Launch external text editor (notepad/nano)", delegate (List<string> a) { LaunchEditor(); });
            _commands["explore"] = new CommandInfo("lion explore", "List files (interactive path prompt)", delegate (List<string> a) { FileExplorer(); });
            _commands["newfile"] = new CommandInfo("lion newfile", "Create a new file (interactive name prompt)", delegate (List<string> a) { CreateFileInteractive(); });
            _commands["newfolder"] = new CommandInfo("lion newfolder", "Create a new folder (interactive name prompt)", delegate (List<string> a) { CreateFolderInteractive(); });
            _commands["read"] = new CommandInfo("lion read <file>", "View a text file", delegate (List<string> a)
            {
                if (a.Count < 1) { Console.WriteLine("Usage: lion read <file>"); return; }
                ReadFile(a[0]);
            });
            _commands["chat"] = new CommandInfo("lion chat", "Simple chatbot", delegate (List<string> a) { Chatbot(); });
            _commands["chatlog"] = new CommandInfo("lion chatlog", "View/save chatbot log", delegate (List<string> a) { ViewChatlog(); });
            _commands["draw"] = new CommandInfo("lion draw", "ASCII drawing tool", delegate (List<string> a) { AsciiDraw(); });
            _commands["history"] = new CommandInfo("lion history", "Show recent commands", delegate (List<string> a) { ShowHistory(); });
            _commands["clear"] = new CommandInfo("lion clear", "Clear screen", delegate (List<string> a) { Console.Clear(); });
            _commands["exit"] = new CommandInfo("lion exit", "Quit shell", delegate (List<string> a) { throw new OperationCanceledException("exit"); });

            // 50 NEW commands
            _commands["pwd"] = new CommandInfo("lion pwd", "Print current directory", CmdPwd);
            _commands["cd"] = new CommandInfo("lion cd <path>", "Change current directory", CmdCd);
            _commands["ls"] = new CommandInfo("lion ls [path]", "List files and folders with sizes", CmdLs);
            _commands["tree"] = new CommandInfo("lion tree [path] [depth]", "Print directory tree (default depth 3)", CmdTree);
            _commands["stat"] = new CommandInfo("lion stat <path>", "Show file/folder details", CmdStat);
            _commands["rm"] = new CommandInfo("lion rm <file>", "Delete a file (asks confirmation)", CmdRm);
            _commands["rmdir"] = new CommandInfo("lion rmdir <folder>", "Delete a folder recursively (asks confirmation)", CmdRmdir);
            _commands["cp"] = new CommandInfo("lion cp <src> <dst>", "Copy file or folder", CmdCopy);
            _commands["mv"] = new CommandInfo("lion mv <src> <dst>", "Move file or folder", CmdMove);
            _commands["ren"] = new CommandInfo("lion ren <old> <new>", "Rename file or folder", CmdRename);
            _commands["touch"] = new CommandInfo("lion touch <file>", "Create file if missing, update modified time", CmdTouch);
            _commands["cat"] = new CommandInfo("lion cat <file>", "Print entire file", CmdCat);
            _commands["head"] = new CommandInfo("lion head <file> [n]", "Print first n lines (default 10)", CmdHead);
            _commands["tail"] = new CommandInfo("lion tail <file> [n]", "Print last n lines (default 10)", CmdTail);
            _commands["write"] = new CommandInfo("lion write <file> <text>", "Overwrite file with one line", CmdWrite);
            _commands["append"] = new CommandInfo("lion append <file> <text>", "Append one line to file", CmdAppend);
            _commands["lines"] = new CommandInfo("lion lines <file>", "Count lines in a file", CmdLineCount);
            _commands["grep"] = new CommandInfo("lion grep <text> <file>", "Search text in a file (prints matching lines)", CmdGrep);
            _commands["grepr"] = new CommandInfo("lion grepr <text> [path]", "Recursive text search (skips binary/huge files)", CmdGrepRecursive);
            _commands["find"] = new CommandInfo("lion find <pattern> [path]", "Find files by wildcard pattern recursively", CmdFind);
            _commands["dirsize"] = new CommandInfo("lion dirsize [path]", "Compute total size of a directory recursively", CmdDirSize);
            _commands["hash"] = new CommandInfo("lion hash <file> [md5|sha1|sha256]", "Compute file hash (default sha256)", CmdHash);
            _commands["base64enc"] = new CommandInfo("lion base64enc <inputFile> <outputFile>", "Base64-encode a file to text output", CmdBase64Encode);
            _commands["base64dec"] = new CommandInfo("lion base64dec <inputFile> <outputFile>", "Decode Base64 text file to binary output", CmdBase64Decode);
            _commands["dupe"] = new CommandInfo("lion dupe [path]", "Find duplicate files (by size + md5)", CmdFindDuplicates);
            _commands["recent"] = new CommandInfo("lion recent [path] [n]", "List most recently modified files recursively", CmdRecentFiles);
            _commands["backup"] = new CommandInfo("lion backup <srcFile> <dstFolder>", "Copy file to folder with timestamped .bak name", CmdBackupFile);
            _commands["ted"] = new CommandInfo("lion ted <file>", "Built-in tiny CLI text editor", CmdTinyEditor);
            _commands["date"] = new CommandInfo("lion date", "Show current date/time", CmdDate);
            _commands["uptime"] = new CommandInfo("lion uptime", "Show system uptime (best-effort)", CmdUptime);
            _commands["whoami"] = new CommandInfo("lion whoami", "Show current username", CmdWhoami);
            _commands["hostname"] = new CommandInfo("lion hostname", "Show machine name", CmdHostname);
            _commands["env"] = new CommandInfo("lion env", "List environment variables", CmdEnv);
            _commands["getenv"] = new CommandInfo("lion getenv <key>", "Get an environment variable", CmdGetEnv);
            _commands["setenv"] = new CommandInfo("lion setenv <key> <value>", "Set env var for this process", CmdSetEnv);
            _commands["unsetenv"] = new CommandInfo("lion unsetenv <key>", "Unset env var for this process", CmdUnsetEnv);
            _commands["drives"] = new CommandInfo("lion drives", "List drives and free space", CmdDrives);
            _commands["mem"] = new CommandInfo("lion mem", "Show current process memory usage", CmdMemory);
            _commands["proc"] = new CommandInfo("lion proc", "List running processes", CmdProcessList);
            _commands["kill"] = new CommandInfo("lion kill <pid>", "Kill a process by PID (asks confirmation)", CmdKill);
            _commands["start"] = new CommandInfo("lion start <target> [args...]", "Start/open something via shell", CmdStart);
            _commands["run"] = new CommandInfo("lion run <commandline>", "Run an OS command and print output", CmdRun);
            _commands["open"] = new CommandInfo("lion open <path>", "Open file/folder with default app", CmdOpenPath);
            _commands["ping"] = new CommandInfo("lion ping <host> [count]", "Ping a host (default 4)", CmdPing);
            _commands["dns"] = new CommandInfo("lion dns <host>", "Resolve DNS to IP addresses", CmdDns);
            _commands["ip"] = new CommandInfo("lion ip", "Show local IP addresses per interface", CmdIpInfo);
            _commands["download"] = new CommandInfo("lion download <url> <file>", "Download a URL to a file", CmdDownload);
            _commands["httpget"] = new CommandInfo("lion httpget <url>", "HTTP GET and print response (truncated)", CmdHttpGet);
            _commands["sfc"] = new CommandInfo("lion sfc", "Run Windows System File Checker (sfc /scannow)", CmdSfcScan);
            _commands["dism"] = new CommandInfo("lion dism", "Run Windows DISM restore health", CmdDismRestoreHealth);
        }

        // ────── Main Loop ──────
        static void Main(string[] args)
        {
            RegisterCommands();

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                // If a child process is running, stop it; otherwise keep shell alive.
                try
                {
                    if (_childProcess != null && !_childProcess.HasExited)
                    {
                        try { _childProcess.Kill(); }
                        catch { }
                        e.Cancel = true;
                        Console.WriteLine("\nStopped running command.");
                        return;
                    }
                }
                catch { }

                e.Cancel = true;
                Console.WriteLine("\nType 'lion exit' to quit.");
            };

            ShowLogo();

            while (true)
            {
                try
                {
                    Console.Write(">> ");
                    string line = (Console.ReadLine() ?? "").Trim();
                    if (line.Length == 0) continue;

                    command_history.Add(line);

                    List<string> tokens = Tokenize(line);
                    if (tokens.Count == 0) continue;

                    // Keep the same "lion ..." style
                    if (!tokens[0].Equals("lion", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Unknown command. Try 'lion help'.");
                        continue;
                    }

                    string sub = (tokens.Count >= 2) ? tokens[1] : "help";
                    List<string> cmdArgs = new List<string>();
                    for (int i = 2; i < tokens.Count; i++) cmdArgs.Add(tokens[i]);

                    CommandInfo ci;
                    if (_commands.TryGetValue(sub, out ci))
                    {
                        ci.Handler(cmdArgs);
                    }
                    else
                    {
                        Console.WriteLine("Unknown command. Try 'lion help'.");
                    }
                }
                catch (OperationCanceledException oce)
                {
                    if (oce.Message == "exit")
                    {
                        Console.WriteLine("Goodbye!");
                        break;
                    }
                    Console.WriteLine("Error: " + oce.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }
    }
}