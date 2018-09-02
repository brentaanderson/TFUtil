////////////////////////////////////////////////////////////////////////////////
// Bug command for TFS Application Wrapper
////////////////////////////////////////////////////////////////////////////////
// Exposes object functionality via command line
//
// <copyright company="Microsoft" file="Application.cs">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.TeamFoundation.PowerTools.CommandLine
{
    class Application
    {
        static void Main(string[] args)
        {
            var app = new TFBug();
            app.Run(args);
        }
    }
}
