////////////////////////////////////////////////////////////////////////////////
// Bug command for TFS
////////////////////////////////////////////////////////////////////////////////
// Returns a list of TFS bug details (more functionality later).
//
// - Processes a list of numeric bug ID values
// - If no IDs are specified, bugs returned by default query are processed
// - Ouptut format is controlled by the application configuration file
//

namespace Microsoft.TeamFoundation.PowerTools.CommandLine
{
    using System.Linq;

    ////////////////////////////////////////////////////////////////////////////
    // TFBug Class:
    //     Main program logic

    internal class TFBug
    {
        #region Internal class member variables
        // object references
        private WorkItemTracking.Client.WorkItemStore privStore;
        private VersionControl.Client.VersionControlServer privServer;
        private CommandLineArgs privArgs;
        private System.Configuration.KeyValueConfigurationCollection privConfig;
        #endregion

        public void Run(string[] args)
        {
            try
            {
                this.privArgs = new CommandLineArgs(args);
            }
            catch (System.ArgumentException ex)
            {
                ReportError(ex);
                CommandLineArgs.ShowUsage();
                return;
            }

            var cfg = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                                                                System.Configuration.ConfigurationUserLevel.None);
            this.privConfig = ((System.Configuration.AppSettingsSection)cfg.GetSection("appSettings")).Settings;

            // Get a TFS instance and connect with the server
            //var tpc = new Client.TfsTeamProjectCollection(this.GetTFSWorkspaceFromPath(null).ServerUri);
            string default_server = this.privConfig[".DefaultServer"].Value;
            System.Diagnostics.Debug.WriteLine(default_server);
            var default_uri = new System.Uri(default_server);
            System.Diagnostics.Debug.WriteLine(default_uri);

            using (var tpc = new Client.TfsTeamProjectCollection(new System.Uri(this.privConfig[".DefaultServer"].Value)))
            {
                this.privStore = (WorkItemTracking.Client.WorkItemStore)tpc.GetService(
                                                                        typeof(WorkItemTracking.Client.WorkItemStore));
                this.privServer = tpc.GetService<VersionControl.Client.VersionControlServer>();
            }

            this.ProcessBugs();
        }

        private static void ReportError(System.Exception ex)
        {
            if (ex.Message.Length > 0)
            {
                WriteColor(ex.Message, System.ConsoleColor.Red);
            }
            else
            {
                System.Console.WriteLine();
            }
        }

        private static void WriteColor(string message, System.ConsoleColor color)
        {
            var cc = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = cc;
        }

        //private VersionControl.Client.WorkspaceInfo GetTFSWorkspaceFromPath(string strPath)
        //{
        //    // Query for workspace containing the path specified
        //    if (string.IsNullOrEmpty(strPath))
        //    {
        //        strPath = System.Environment.CurrentDirectory;
        //    }

        //    var wi = VersionControl.Client.Workstation.Current.GetLocalWorkspaceInfo(strPath);
        //    if (wi == null)
        //    {
        //        var wsi = VersionControl.Client.Workstation.Current.GetAllLocalWorkspaceInfo();
        //        if (wsi.Length == 0)
        //        {
        //            WriteColor(Resources.IDS_UPDATECACHEMSG, System.ConsoleColor.Yellow);
        //            foreach (string server in new[] { "http://vstfx14:8080/tfs/turn10" })
        //            {
        //                VersionControl.Client.Workstation.Current.EnsureUpdateWorkspaceInfoCache(
        //                    (new Client.TfsTeamProjectCollection(
        //                    new System.Uri(server))).GetService<VersionControl.Client.VersionControlServer>(),
        //                    string.Format(Resources.Culture, Resources.IDS_DOMAINUSERFORMAT, System.Environment.UserDomainName, System.Environment.UserName));
        //            }

        //            wsi = VersionControl.Client.Workstation.Current.GetAllLocalWorkspaceInfo();
        //        }

        //        // try this all again...
        //        wi = VersionControl.Client.Workstation.Current.GetLocalWorkspaceInfo(strPath);
        //        if (wi == null)
        //        {
        //            if (wsi.Length == 0)
        //            {
        //                throw new System.Exception(string.Format(Resources.Culture,
        //                                                         Resources.IDS_WARN_NOMAPPING,
        //                                                         strPath));
        //            }

        //            // Make sure we have something to return first
        //            wi = wsi[0];

        //            // Now check for a specified server in the config
        //            string strDefaultServer = this.privConfig[".DefaultServer"].Value;
        //            if (string.IsNullOrEmpty(strDefaultServer))
        //            {
        //                return wi;
        //            }

        //            foreach (var value in wsi.Where(value =>
        //                     string.CompareOrdinal(value.ServerUri.ToString(), strDefaultServer) == 0))
        //            {
        //                wi = value;
        //            }
        //        }
        //    }

        //    return wi;
        //}

        private void ProcessBugs()
        {
            WorkItemTracking.Client.WorkItemCollection bugs;

            if (this.privArgs.Bugs != null && this.privArgs.Bugs.Count > 0)
            {
                // We have a list of bugs, but need to build a query b/c TFS will hide values
                // (at least ChangedBy property) and this method ensures accuracy.
                var sb = new System.Text.StringBuilder("SELECT [System.Id] FROM WorkItems WHERE ");
                foreach (int id in this.privArgs.Bugs)
                {
                    sb.AppendFormat("[System.Id] = '{0}' OR ", id);
                }

                sb.Append("[System.Id] = '-1'");
                bugs = this.privStore.Query(sb.ToString());
            }
            else
            {
                // use the default query from the configuration file
                // TODO: consider other queries & selection via command line parameters
                bugs = this.privStore.Query(this.privConfig[".DefaultQuery"].Value);
            }

            foreach (WorkItemTracking.Client.WorkItem bug in bugs)
            {
                foreach (var kv in this.privConfig.Cast<System.Configuration.KeyValueConfigurationElement>()
                                                                                       .Where(kv => kv.Key[0] != '.'))
                {
                    System.Console.WriteLine(Resources.IDS_NAMEVALUEFORMAT, kv.Key, this.ParseValue(bug, kv.Value));
                }

                System.Console.WriteLine();
            }
        }

        private string ParseValue(WorkItemTracking.Client.WorkItem bug, string value)
        {
            if (bug == null)
            {
                throw new System.ArgumentException("Argument cannot be null", "bug");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new System.ArgumentException("Argument cannot be null", "value");
            }

            string retval = value;

            // use nested grouping so values needing multiple properties expanded are handled properly
            var colMatches = System.Text.RegularExpressions.Regex.Matches(value, "(\\[(.*?)\\])+");

            if (colMatches.Count <= 0)
            {
                return retval;
            }

            bool boolFirstMatch = true;
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
                                         where s.Length > 0 && s[0] != '['
                                         select s)
                    {
                        string v;

                        // this is an inner group, now we can do something...
                        if (s.StartsWith("Fields.", System.StringComparison.OrdinalIgnoreCase))
                        {
                            // ...too easy!
                            v = bug.Fields[s.Substring(7)].Value.ToString();
                        }
                        else
                        {
                            // ...hopefully this list won't change too often
                            switch (s)
                            {
                                case "AreaId":
                                    v = bug.AreaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "AreaPath":
                                    v = bug.AreaPath;
                                    break;
                                case "Attachments":
                                    var sb = new System.Text.StringBuilder();
                                    foreach (WorkItemTracking.Client.Attachment attach in bug.Attachments)
                                    {
                                        sb.Append(attach.Name);
                                        sb.Append(";");
                                    }

                                    v = sb.ToString();
                                    break;
                                case "ChangedBy":
                                    v = bug.ChangedBy;
                                    break;
                                case "ChangedDate":
                                    v = bug.ChangedDate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "CreatedBy":
                                    v = bug.CreatedBy;
                                    break;
                                case "CreatedDate":
                                    v = bug.CreatedDate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "Description":
                                    v = bug.Description;
                                    break;
                                case "Id":
                                    v = bug.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "IterationId":
                                    v = bug.IterationId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "IterationPath":
                                    v = bug.IterationPath;
                                    break;
                                case "NodeName":
                                    v = bug.NodeName;
                                    break;
                                case "Project":
                                    v = bug.Project.Name;
                                    break;
                                case "Reason":
                                    v = bug.Reason;
                                    break;
                                case "RevisedDate":
                                    v = bug.RevisedDate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "Revision":
                                    v = bug.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "State":
                                    v = bug.State;
                                    break;
                                case "Title":
                                    v = bug.Title;
                                    break;
                                case "Type":
                                    v = bug.Type.Name;
                                    break;
                                case "Uri":
                                    v = bug.Uri.ToString();
                                    break;
                                case "%shelf_contents%":
                                    // now we have to get fancy...
                                    v = this.ExpandShelf(bug.Fields["Changeset"].Value.ToString());
                                    break;
                                default:
                                    throw new System.ArgumentException("Unrecognized parameter in app config", s);
                            }
                        }

                        retval = retval.Replace(string.Format(Resources.Culture, "[{0}]", s), v);
                    }
                }
            }

            return retval;
        }

        private string ExpandShelf(string shelfname)
        {
            var retval = new System.Text.StringBuilder(shelfname);

            if (string.IsNullOrEmpty(shelfname))
            {
                return "(null)";
            }

            // query shelf for valid path & operation
            var rgps = this.privServer.QueryShelvedChanges(
                                                           shelfname.Split(new[] { ';' })[0],
                                                           shelfname.Split(new[] { ';' })[1]);
            foreach (var pc in rgps.SelectMany(ps => ps.PendingChanges))
            {
                retval.AppendFormat("\n            {0} ", pc.ChangeType);
                if (pc.IsMerge)
                {
                    // Call attention to the merge by putting the source item and change # before the item
                    retval.AppendFormat("{0};C{1} -> ", pc.SourceServerItem, pc.SourceVersionFrom);
                }

                retval.Append(pc.ServerItem);
            }

            return retval.ToString();
        }
    }
}
