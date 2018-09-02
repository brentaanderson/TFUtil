////////////////////////////////////////////////////////////////////////////
// CommandLineArgs Class:
//     Encapsulate command line processing and argument validation
//
// <copyright company="Microsoft" file="CommandLineArgs.cs">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
////////////////////////////////////////////////////////////////////////////

using System.Linq;

internal class CommandLineArgs
{
    private readonly System.Collections.Generic.List<int> privBugs;

    internal CommandLineArgs(System.Collections.Generic.IEnumerable<string> args)
    {
        this.privBugs = new System.Collections.Generic.List<int>();
        foreach (string arg in args)
        {
            this.Parse(arg);
        }
    }

    internal System.Collections.Generic.IList<int> Bugs
    {
        get
        {
            return this.privBugs.AsReadOnly();
        }
    }

    internal static void ShowUsage()
    {
        //var rm = new System.Resources.ResourceManager(System.Reflection.Assembly.GetExecutingAssembly());
        //rm.GetString("IDS_HELPMSG");

        System.Console.WriteLine(Resources.IDS_HELPMSG);
    }

    private void Parse(string arg)
    {
        bool boolFirstMatch = true;

        var regexInvalid = new System.Text.RegularExpressions.Regex("^[-/]([^0-9?hH\\s])+");
        var regexBugs    = new System.Text.RegularExpressions.Regex("([0-9]+)[,;\\s]*");
        var regexHelp    = new System.Text.RegularExpressions.Regex("^[-/][?hH]");

        var colMatches = regexInvalid.Matches(arg);

        if (colMatches.Count > 0)
        {
            var sb = new System.Text.StringBuilder();

            foreach (System.Text.RegularExpressions.Match m in colMatches)
            {
                foreach (System.Text.RegularExpressions.Group g in m.Groups)
                {
                    if (boolFirstMatch && m.Groups.Count > 1)
                    {
                        boolFirstMatch = false;
                        continue;
                    }

                    foreach (string s in from System.Text.RegularExpressions.Capture c in g.Captures
                             select c.Value.Trim()
                             into s
                             where s.Length > 0
                             select s)
                    {
                        if (s == "?")
                        {
                            // throw now w/o a description to just display usage
                            throw new System.ArgumentException(string.Empty);
                        }

                        sb.AppendFormat("Invalid argument \"{0}\"\n", s);
                    }
                }
            }

            throw new System.ArgumentException(sb.ToString());
        }

        colMatches = regexHelp.Matches(arg);
        if (colMatches.Count > 0)
        {
            throw new System.ArgumentException(string.Empty);
        }

        colMatches = regexBugs.Matches(arg);
        if (colMatches.Count <= 0)
        {
            return;
        }

        foreach (System.Text.RegularExpressions.Match m in colMatches)
        {
            boolFirstMatch = true;
            foreach (System.Text.RegularExpressions.Group g in m.Groups)
            {
                if (boolFirstMatch && m.Groups.Count > 1)
                {
                    boolFirstMatch = false;
                    continue;
                }

                foreach (System.Text.RegularExpressions.Capture c in g.Captures)
                {
                    int id;
                    if (!int.TryParse(c.Value, out id))
                    {
                        throw new System.ArgumentException(string.Format(Resources.Culture, Resources.IDS_INVALIDBUGIDFORMAT, id));
                    }

                    this.privBugs.Add(id);
                }
            }
        }
    }
}
