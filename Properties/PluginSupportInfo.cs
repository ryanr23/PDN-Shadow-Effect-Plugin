using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Seren.PaintDotNet.Effects
{
    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Shadow Effect")]
    public class PluginSupportInfo : IPluginSupportInfo
    {
            public string Author
            {
                get
                {
                    return "Ryan Reading";
                }
            }
            public string Copyright
            {
                get
                {
                    return "Copyright © 2015 Ryan Reading";
                }
            }

            public string DisplayName
            {
                get
                {
                    return "Shadow Effect";
                }
            }

            public Version Version
            {
                get
                {
                    return base.GetType().Assembly.GetName().Version;
                }
            }

            public Uri WebsiteUri
            {
                get
                {
                    return new Uri("https://github.com/ryanr23/PDN-Shadow-Effect-Plugin");
                }
            }
     }
}

#pragma warning restore 1591