/* CCreatorHelppage.cs
 * 
 * Copyright 2009 Alexander Curtis <alex@logicmill.com>
 * This file is part of GEDmill - A family history website creator
 * 
 * GEDmill is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * GEDmill is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with GEDmill.  If not, see <http://www.gnu.org/licenses/>.
 *
 *
 * History:  
 * 10Dec08 AlexC          Migrated from GEDmill 1.10
 *
 */

using System;
using System.IO;
using GKCommon.GEDCOM;

namespace GEDmill.HTML
{
    /// <summary>
    /// Summary description for CCreatorHelppage.
    /// </summary>
    public class CreatorHelpPage : Creator
    {
        public CreatorHelpPage(GEDCOMTree gedcom, IProgressCallback progress, string w3cfile) : base(gedcom, progress, w3cfile)
        {
        }

        // The main method that causes the help page to be created. 
        public void Create()
        {
            FileStream helpStream = null;
            StreamReader helpReader = null;
            try {
                helpStream = new FileStream(MainForm.Config.ApplicationPath + "\\helpsource.html", FileMode.Open);
                helpReader = new StreamReader(helpStream, System.Text.Encoding.UTF8);
            } catch (IOException e) {
                LogFile.Instance.WriteLine(LogFile.DT_HTML, LogFile.EDebugLevel.Error, String.Format("Caught io exception while loading help file source: {0}", e.ToString()));
                helpStream = null;
                helpReader = null;
            }

            if (helpStream != null && helpReader != null) {
                string pageDescription = "GEDmill GEDCOM to HTML family history website";
                string keywords = "family tree history " + MainForm.Config.OwnersName;
                string title = MainForm.Config.SiteTitle;

                HTMLFile f = null;
                try {
                    // Create a new file and put standard header html into it.
                    f = new HTMLFile(MainForm.Config.HelpPageURL, title, pageDescription, keywords);

                    OutputPageHeader(f.Writer, "", "", true, false);

                    f.Writer.WriteLine("  <div id=\"page\"> <!-- page -->");

                    // Copy in the help html source
                    string sHelpLine;
                    while (null != (sHelpLine = helpReader.ReadLine())) {
                        f.Writer.WriteLine(sHelpLine);
                    }

                    f.Writer.WriteLine("  </div> <!-- page -->");
                } catch (IOException e) {
                    LogFile.Instance.WriteLine(LogFile.DT_HTML, LogFile.EDebugLevel.Error, "Caught IO Exception : " + e.ToString());
                } catch (ArgumentException e) {
                    LogFile.Instance.WriteLine(LogFile.DT_HTML, LogFile.EDebugLevel.Error, "Caught Argument Exception : " + e.ToString());
                } finally {
                    if (f != null) {
                        // Add standard footer to the file
                        f.Close();
                    }
                }
            }
            if (helpReader != null) {
                helpReader.Close();
            }
            if (helpStream != null) {
                helpStream.Close();
            }
        }
    }
}
