/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    public class VersionControl
    {
        public static IApplicationSettings ApplicationSettings = new ApplicationSettings();

        private static void ConfigureStartInfo(ProcessStartInfo startInfo)
        {
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
        }

        public static string RunAndCapture(string filename, string args, string workingDir)
        {
            var p = new Process();

            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.Arguments = args;
            p.StartInfo.FileName = filename;

            ConfigureStartInfo(p.StartInfo);

            Util.WriteToLog(filename + " " + args);

            p.Start();
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            stdout += p.StandardOutput.ReadToEnd();

            var error = p.StandardError.ReadToEnd();

            if (error != "")
            {
                Util.WriteToLog("stderr:" + error);
                Util.WriteToLog("stdout:" + stdout);
            }

            if (error != ""
                && !error.Contains("RUNTIME_PREFIX")) // ignore the git "RUNTIME_PREFIX" error
            {
                var msg = "<div style='color:red; font-weight: bold; font-size: 10pt;'>";
                msg += "<br>Error executing git or hg command:";
                msg += "<br>Error: " + error;
                msg += "<br>Command: " + filename + " " + args;
                HttpContext.Current.Response.Write(msg);
                HttpContext.Current.Response.End();
                return msg;
            }

            Util.WriteToLog("stdout:" + stdout);
            return stdout;
        }

        public static string RunGit(string args, string workingDir)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            var filename = applicationSettings.GitPathToGit;
            return RunAndCapture(filename, args, workingDir);
        }

        public static string RunHg(string args, string workingDir)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            var filename = applicationSettings.MercurialPathToHg;
            return RunAndCapture(filename, args, workingDir);
        }

        public static string RunSvn(string argsWithoutPassword, string repo)
        {
            // run "svn.exe" and capture its output

            IApplicationSettings applicationSettings = new ApplicationSettings();

            var p = new Process();
            var filename = applicationSettings.SubversionPathToSvn;
            p.StartInfo.FileName = filename;

            ConfigureStartInfo(p.StartInfo);

            argsWithoutPassword += " --non-interactive";

            var moreArgs = applicationSettings.SubversionAdditionalArgs;
            if (moreArgs != "") argsWithoutPassword += " " + moreArgs;

            Util.WriteToLog(filename + " " + argsWithoutPassword);

            var argsWithPassword = argsWithoutPassword;

            // add a username and password to the args
            var usernameAndPasswordAndWebsvn = ApplicationSettings[repo];

            var parts = Util.RePipes.Split(usernameAndPasswordAndWebsvn);
            if (parts.Length > 1)
                if (parts[0] != "" && parts[1] != "")
                {
                    argsWithPassword += " --username ";
                    argsWithPassword += parts[0];
                    argsWithPassword += " --password ";
                    argsWithPassword += parts[1];
                }

            p.StartInfo.Arguments = argsWithPassword;
            p.Start();
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            stdout += p.StandardOutput.ReadToEnd();

            var error = p.StandardError.ReadToEnd();

            if (error != "")
            {
                Util.WriteToLog("stderr:" + error);
                Util.WriteToLog("stdout:" + stdout);
            }

            if (error != "")
            {
                var msg = "<div style='color:red; font-weight: bold; font-size: 10pt;'>";
                msg += "<br>Error executing svn command:";
                msg += "<br>Error: " + error;
                msg += "<br>Command: " + filename + " " + argsWithoutPassword;
                if (error.Contains("File not found"))
                {
                    msg += "<br><br>***** Has this file been deleted or renamed? See the following links:";
                    msg +=
                        "<br><a href=http://svn.apache.org/repos/asf/subversion/trunk/doc/user/svn-best-practices.html>";
                    msg += "http://svn.apache.org/repos/asf/subversion/trunk/doc/user/svn-best-practices.html</a>";
                    msg += "</div>";
                }

                HttpContext.Current.Response.Write(msg);
                HttpContext.Current.Response.End();
                return msg;
            }

            Util.WriteToLog("stdout:" + stdout);
            return stdout;
        }

        private static void WhichCharsChanged(string s1, string s2, ref string part1, ref string part2,
            ref string part3)
        {
            // Input is two strings
            // Output is the second string divided up into three parts based on which chars in string 2
            // are different than string 1.

            // Starting from the beginning of string 2, what is the first char that's different from string 1? That's one divide.
            // Starting from the end of string 2, going in reverse, what's the first char that's different from string 1?  That's another divider.

            var firstDiffChar = -1;
            var lastDiffChar = -1;

            if (s1.Length <= s2.Length)
            {
                var i = 0;
                while (firstDiffChar == -1
                       && i < s1.Length)
                {
                    if (s1[i] != s2[i])
                    {
                        firstDiffChar = i;
                        break;
                    }

                    i++;
                }

                // if chars were appended to s2
                if (firstDiffChar == -1) firstDiffChar = i;
            }
            else
            {
                var i = 0;
                while (firstDiffChar == -1
                       && i < s2.Length)
                {
                    if (s1[i] != s2[i])
                    {
                        firstDiffChar = i;
                        break;
                    }

                    i++;
                }

                // if chars were deleted off the end of s2, we won't mark anything as orange
            }

            if (firstDiffChar != -1)
            {
                var index1 = s1.Length - 1;
                var index2 = s2.Length - 1;

                if (s1.Length <= s2.Length)
                {
                    while (lastDiffChar == -1
                           && index1 > -1)
                    {
                        if (s1[index1] != s2[index2])
                        {
                            lastDiffChar = index2;
                            break;
                        }

                        index1--;
                        index2--;
                    }

                    // if chars were added onto the front of s2
                    if (lastDiffChar == -1) lastDiffChar = index2;
                }
                else
                {
                    while (lastDiffChar == -1
                           && index2 > -1)
                    {
                        if (s1[index1] != s2[index2])
                        {
                            lastDiffChar = index2;
                            break;
                        }

                        index1--;
                        index2--;
                    }

                    // if chars were deleted off the front of s2, we won't show anything in orange
                }
            }

            if (firstDiffChar == -1 || lastDiffChar == -1)
            {
                part1 = s2;
            }
            else
            {
                if (firstDiffChar > lastDiffChar)
                    // here's an example
                    // ab c
                    // ab b c
                    // From the left, the first diff char is the second "b", at index 3
                    // From the right, the first (meaning last) diff char is the space, at index 2
                    lastDiffChar = s2.Length - 1;

                part1 = s2.Substring(0, firstDiffChar);
                part2 = s2.Substring(firstDiffChar, lastDiffChar + 1 - firstDiffChar);
                part3 = s2.Substring(lastDiffChar + 1);
            }
        }

        private static void MarkChangeLines(int i, string[] lines, int plusCount)
        {
            // Modify the UDF to make it richer and easier to process.
            // The heuristic has identified some of the -/+ lines as really meaning a change.
            // Marking the before lines with a "P" and the after lines with an "M".

            // mark the "before" lines
            var prevLine = i - 1;
            for (var j = 0; j < plusCount; j++)
            {
                var m = prevLine - j;
                var sub = "";
                if (lines[m].Length > 0)
                    sub = lines[m].Substring(1);
                lines[m] = "P" + sub;
            }

            // mark the "after" lineas
            for (var j = plusCount; j < 2 * plusCount; j++)
            {
                var m = prevLine - j;
                var sub = "";
                if (lines[m].Length > 0)
                    sub = lines[m].Substring(1);
                lines[prevLine - j] = "M" + lines[prevLine - j].Substring(1);
            }
        }

        private static void MaybeMarkCangeLnesInUnifiedDiff(string[] lines)
        {
            // If there are N minuses followed by exactly N pluses, we'll call that a change

            var minusCount = 0;
            var plusCount = 0;
            var state = State.None;

            var len = lines.Length;

            var i = 0;
            for (i = 0; i < len; i++)
                if (state == State.None)
                {
                    if (lines[i].StartsWith("-"))
                    {
                        state = State.CountingMinuses;
                        minusCount++;
                    }
                }
                else if (state == State.CountingMinuses)
                {
                    if (lines[i].StartsWith("-"))
                    {
                        minusCount++;
                    }
                    else if (lines[i].StartsWith("+"))
                    {
                        state = State.CountingPluses;
                        plusCount++;
                    }
                    else
                    {
                        state = State.None;
                        minusCount = 0;
                        plusCount = 0;
                    }
                }
                else if (state == State.CountingPluses)
                {
                    if (lines[i].StartsWith("+"))
                    {
                        plusCount++;
                    }
                    else
                    {
                        if (plusCount > 0 && plusCount < 20 && plusCount == minusCount)
                            MarkChangeLines(i, lines, plusCount);

                        state = State.None;
                        plusCount = 0;
                        minusCount = 0;

                        if (lines[i].StartsWith("-"))
                        {
                            state = State.CountingMinuses;
                            minusCount++;
                        }
                    }
                }

            if (plusCount > 0 && plusCount < 20 && plusCount == minusCount) MarkChangeLines(i, lines, plusCount);
        }

        public static string VisualDiff(string unifiedDiffText, string leftIn, string rightIn, ref string leftOut,
            ref string rightOut)
        {
            var regex = new Regex("\n");
            var line = "";

            var diffText = unifiedDiffText;

            // get rid of lines we don't need
            var pos = unifiedDiffText.IndexOf("\n@@");
            if (pos > -1) diffText = unifiedDiffText.Substring(pos + 1);

            if (diffText == "") return "No differences.";

            // first, split everything into lines
            var diffLines = regex.Split(diffText.Replace("\r\n", "\n"));

            MaybeMarkCangeLnesInUnifiedDiff(diffLines);

            // html encode
            var leftText = HttpUtility.HtmlEncode(leftIn);
            var rightText = HttpUtility.HtmlEncode(rightIn);
            // split into lines
            var leftLines = regex.Split(leftText.Replace("\r\n", "\n"));
            var rightLines = regex.Split(rightText.Replace("\r\n", "\n"));

            // for formatting line numbers
            var maxLines = leftLines.Length;
            if (rightLines.Length > leftLines.Length)
                maxLines = rightLines.Length;

            // I just want to pad left a certain number of places
            // probably any 5th grader would know how to do this better than me
            var blank = "";
            var digitPlaces = Convert.ToString(maxLines).Length;

            var lx = 0;
            var rx = 0;
            var dx = 0;

            var sL = new StringBuilder();
            var sR = new StringBuilder();

            var changedLinesSavedForLaterCompare = new List<string>();

            // L E F T
            // L E F T
            // L E F T

            //sL.Append("<div class=difffile>" + "left"  + "</div>");
            //sR.Append("<div class=difffile>" + "right" + "</div>");

            while (dx < diffLines.Length)
            {
                line = diffLines[dx];
                if (line.StartsWith("@@ -") && line.Contains(" @@"))
                {
                    // See comment at the top of this file explaining Unified Diff Format
                    // Parse out the left start line.  For example, the "38" here:
                    // @@ -38,18 +39,12 @@
                    // Note that the range could also be specified as follows, with the number of lines assumed to be 1
                    // @@ -1 +1,2 @@

                    var pos1 = line.IndexOf("-");
                    var commaPos = line.IndexOf(",", pos1);
                    if (commaPos == -1) commaPos = 9999;
                    var pos2 = Math.Min(line.IndexOf(" ", pos1), commaPos);
                    var leftStartLineString = line.Substring(pos1 + 1, pos2 - (pos1 + 1));
                    var startLine = Convert.ToInt32(leftStartLineString);
                    startLine -= 1; // adjust for zero based index

                    // advance through left file until we hit the starting line of the range
                    while (lx < startLine)
                    {
                        sL.Append("<span class=diffnum>");
                        sL.Append(Convert.ToString(lx + 1).PadLeft(digitPlaces, '0'));
                        sL.Append(" </span>");
                        sL.Append(leftLines[lx++]);
                        sL.Append("\n");
                    }

                    // we are positioned in the left file at the start of the diff blockk
                    dx++;
                    line = diffLines[dx];
                    while (dx < diffLines.Length
                           && !(line.StartsWith("@@ -") && line.EndsWith(" @@")))
                    {
                        if (line.StartsWith("+"))
                        {
                            sL.Append("<span class=diffnum>");
                            sL.Append(blank.PadLeft(digitPlaces, ' '));
                            sL.Append(" </span>");
                            sL.Append("<span class=diffblank>&nbsp;&nbsp;&nbsp;&nbsp;</span>\n");
                        }
                        else if (line.StartsWith("-"))
                        {
                            sL.Append("<span class=diffnum>");
                            sL.Append(Convert.ToString(lx + 1).PadLeft(digitPlaces, '0'));
                            sL.Append(" </span>");

                            sL.Append("<span class=diffdel>");

                            sL.Append(leftLines[lx++]);
                            sL.Append("</span>\n");
                        }
                        else if (line.StartsWith("M"))
                        {
                            sL.Append("<span class=diffnum>");
                            sL.Append(Convert.ToString(lx + 1).PadLeft(digitPlaces, '0'));
                            sL.Append(" </span>");

                            sL.Append("<span class=diffchg>");

                            // Save the left lines for later, so that we can mark the changed chars in orange
                            changedLinesSavedForLaterCompare.Add(leftLines[lx]);

                            sL.Append(leftLines[lx++]);
                            sL.Append("</span>\n");
                        }
                        else if (line.StartsWith("\\") || line == "" || line.StartsWith("P"))
                        {
                        }
                        else
                        {
                            sL.Append("<span class=diffnum>");
                            sL.Append(Convert.ToString(lx + 1).PadLeft(digitPlaces, '0'));
                            sL.Append(" </span>");
                            sL.Append(leftLines[lx++]);
                            sL.Append("\n");
                        }

                        dx++;

                        if (dx < diffLines.Length) line = diffLines[dx];
                    } // end of range block
                }

                if (dx < diffLines.Length && line.StartsWith("@@ -") && line.Contains(" @@"))
                    continue;
                break;
            } // end of all blocks

            // advance through left file until we hit the starting line of the range

            while (lx < leftLines.Length)
            {
                sL.Append("<span class=diffnum>");
                sL.Append(Convert.ToString(lx + 1).PadLeft(digitPlaces, '0'));
                sL.Append(" </span>");
                sL.Append(leftLines[lx++]);
                sL.Append("\n");
            }

            // R I G H T
            // R I G H T
            // R I G H T
            dx = 0;
            var indexOfChangedLines = 0;

            while (dx < diffLines.Length)
            {
                line = diffLines[dx];
                if (line.StartsWith("@@ -") && line.Contains(" @@"))
                {
                    // See comment at the top of this file explaining Unified Diff Format

                    // parse out the right start line.  For example, the "39" here: @@ -38,18 +39,12 @@
                    var pos1 = line.IndexOf("+");

                    var pos2 = line.IndexOf(",", pos1);
                    if (pos2 == -1) pos2 = line.IndexOf(" ", pos1);

                    var rightStartLineString = line.Substring(pos1 + 1, pos2 - (pos1 + 1));
                    var startLine = Convert.ToInt32(rightStartLineString);
                    startLine -= 1; // adjust for zero based index

                    // advance through right file until we hit the starting line of the range
                    while (rx < startLine)
                    {
                        sR.Append("<span class=diffnum>");
                        sR.Append(Convert.ToString(rx + 1).PadLeft(digitPlaces, '0'));
                        sR.Append(" </span>");
                        sR.Append(rightLines[rx++]);
                        sR.Append("\n");
                    }

                    // we are positioned in the right file at the start of the diff block
                    dx++;
                    line = diffLines[dx];

                    while (dx < diffLines.Length && !(line.StartsWith("@@ -") && line.Contains(" @@")))
                    {
                        if (line.StartsWith("-"))
                        {
                            sR.Append("<span class=diffnum>");
                            sR.Append(blank.PadLeft(digitPlaces, ' '));
                            sR.Append(" </span>");
                            sR.Append("<span class=diffblank>&nbsp;&nbsp;&nbsp;&nbsp;</span>\n");
                        }
                        else if (line.StartsWith("+"))
                        {
                            sR.Append("<span class=diffnum>");
                            sR.Append(Convert.ToString(rx + 1).PadLeft(digitPlaces, '0'));
                            sR.Append(" </span>");
                            sR.Append("<span class=diffadd>");
                            sR.Append(rightLines[rx++]);
                            sR.Append("</span>\n");
                        }
                        else if (line.StartsWith("P"))
                        {
                            sR.Append("<span class=diffnum>");
                            sR.Append(Convert.ToString(rx + 1).PadLeft(digitPlaces, '0'));
                            sR.Append(" </span>");

                            var part1 = "";
                            var part2 = "";
                            var part3 = "";

                            WhichCharsChanged(changedLinesSavedForLaterCompare[
                                    indexOfChangedLines],
                                rightLines[rx++],
                                ref part1, ref part2, ref part3);

                            sR.Append("<span class=diffchg>");
                            sR.Append(part1);
                            sR.Append("</span>");

                            sR.Append("<span class=diffchg2>");
                            sR.Append(part2);
                            sR.Append("</span>");

                            sR.Append("<span class=diffchg>");
                            sR.Append(part3);
                            sR.Append("</span>");

                            indexOfChangedLines++;
                            sR.Append("</span>\n");
                        }
                        else if (line.StartsWith("\\") || line == "" || line.StartsWith("M"))
                        {
                        }
                        else
                        {
                            sR.Append("<span class=diffnum>");
                            sR.Append(Convert.ToString(rx + 1).PadLeft(digitPlaces, '0'));
                            sR.Append(" </span>");
                            sR.Append(rightLines[rx++]);
                            sR.Append("\n");
                        }

                        dx++;

                        if (dx < diffLines.Length) line = diffLines[dx];
                    } // end of range block
                }

                if (dx < diffLines.Length && line.StartsWith("@@ -") && line.EndsWith(" @@"))
                    continue;
                break;
            } // end of all blocks

            // advance through right file until we're done

            while (rx < rightLines.Length)
            {
                sR.Append("<span class=diffnum>");
                sR.Append(Convert.ToString(rx + 1).PadLeft(digitPlaces, '0'));
                sR.Append(" </span>");
                sR.Append(rightLines[rx++]);
                sR.Append("\n");
            }

            leftOut = sL.ToString();
            rightOut = sR.ToString();

            return "";
        }

        /*

        hg

        */

        public static string HgLog(string repo, string revision, string filePath)
        {
            var args = "log  --style btnet -r :";
            args += revision;
            args += " \"";
            args += filePath;
            args += "\"";

            var result = RunHg(args, repo);
            return result;
        }

        public static string HgBlame(string repo, string filePath, string revision)
        {
            var args = "blame -u -d -c -l -v -r ";
            args += revision;
            args += " \"";
            args += filePath;
            args += "\"";

            var result = RunHg(args, repo);
            return result;
        }

        public static string HgGetFileContents(string repo, string revision, string filePath)
        {
            var args = "cat -r ";
            args += revision;
            args += " \"";
            args += filePath;
            args += "\"";

            var result = RunHg(args, repo);
            return result;
        }

        public static string HgGetUnifiedDiffTwoRevisions(string repo, string revision0, string revision1,
            string filePath)
        {
            var args = "diff -r ";
            args += revision0;
            args += " -r ";
            args += revision1;

            args += " \"";
            args += filePath;
            args += "\"";

            var result = RunHg(args, repo);
            return result;
        }

        /*

        git

        */

        public static string GitLog(string repo, string commit, string filePath)
        {
            var args = "log --name-status --date=iso ";
            args += commit;
            args += " -- \"";
            args += filePath;
            args += "\"";

            var result = RunGit(args, repo);
            return result;
        }

        public static string GitBlame(string repo, string filePath, string commit)
        {
            var args = "blame ";
            args += " -- \"";
            args += filePath;
            args += "\" ";
            args += commit;

            var result = RunGit(args, repo);
            return result;
        }

        public static string GitGetFileContents(string repo, string commit, string filePath)
        {
            var args = "show --pretty=raw ";
            args += commit;
            args += ":\"";
            args += filePath;
            args += "\"";

            var result = RunGit(args, repo);
            return result;
        }

        public static string GitGetUnifiedDiffTwoCommits(string repo, string commit0, string commit1,
            string filePath)
        {
            var args = "diff ";
            args += commit0;
            args += " ";
            args += commit1;

            args += " -- \"";
            args += filePath;
            args += "\"";

            var result = RunGit(args, repo);
            return result;
        }

        public static string GitGetUnifiedDiffOneCommit(string repo, string commit, string filePath)
        {
            var args = "show  --pretty=format: ";
            args += commit;

            args += " -- \"";
            args += filePath;
            args += "\"";

            var result = RunGit(args, repo);
            return result;
        }

        /*

        svn

        */

        public static string SvnLog(string repo, string filePath, int rev)
        {
            var args = new StringBuilder();

            args.Append("log ");
            args.Append(repo);
            args.Append(filePath.Replace(" ", "%20"));
            args.Append("@" + Convert.ToString(rev)); // peg to revision rev in case file deleted
            args.Append(" -r ");
            args.Append(Convert.ToString(rev)); // view log from beginning to rev
            args.Append(":0 --xml -v");

            return RunSvn(args.ToString(), repo);
        }

        public static string SvnBlame(string repo, string filePath, int rev)
        {
            var args = new StringBuilder();

            args.Append("blame ");
            args.Append(repo);
            args.Append(filePath.Replace(" ", "%20"));
            args.Append("@");
            args.Append(Convert.ToString(rev));
            args.Append(" --xml");

            return RunSvn(args.ToString(), repo);
        }

        public static string SvnCat(string repo, string filePath, int rev)
        {
            var args = new StringBuilder();

            args.Append("cat ");
            args.Append(repo);
            args.Append(filePath.Replace(" ", "%20"));
            args.Append("@");
            args.Append(Convert.ToInt32(rev));

            return RunSvn(args.ToString(), repo);
        }

        public static string SvnDiff(string repo, string filePath, int revision, int oldRevision)
        {
            var args = new StringBuilder();

            if (oldRevision != 0)
            {
                args.Append("diff -r ");

                args.Append(Convert.ToString(oldRevision));
                args.Append(":");
                args.Append(Convert.ToString(revision));
                args.Append(" ");
                args.Append(repo);
                args.Append(filePath.Replace(" ", "%20"));
            }
            else
            {
                args.Append("diff -c ");
                args.Append(Convert.ToString(revision));
                args.Append(" ");
                args.Append(repo);
                args.Append(filePath.Replace(" ", "%20"));
            }

            var result = RunSvn(args.ToString(), repo);
            return result;
        }

        private enum State
        {
            None,
            CountingMinuses,
            CountingPluses
        }
    }
}