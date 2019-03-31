/* CCreatorRecordIndividual.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GEDmill.MiniTree;
using GKCommon.GEDCOM;
using GKCore.Logging;
using GKCore.Types;

namespace GEDmill.HTML
{
    public class CreatorRecordIndividual : CreatorRecord
    {
        private static readonly ILogger fLogger = LogManager.GetLogger(CConfig.LOG_FILE, CConfig.LOG_LEVEL, typeof(CreatorRecordIndividual).Name);

        // The individual record that we are creating the page for.
        private GEDCOMIndividualRecord fIndiRec;

        // Indicates that this individual should have most of the record data excluded from the website for privacy.
        private bool fConcealed;

        // List of the events in the individual's life history.
        private List<Event> fEventList;

        // List of other facts known about the individual.
        private ArrayList fAttributeList;

        // List of sources referenced by this page.
        private List<GEDCOMSourceCitation> fReferenceList;

        // List of occupations known for the individual
        private ArrayList fOccupations;

        // An HTML link to the previous sibling in this fr.
        private string fPreviousChildLink;

        // An HTML link to the next sibling in this fr.
        private string fNextChildLink;

        // List of aliases and other names for the individual
        private ArrayList fOtherNames;

        // A date inferred for the individual's birthday, with associated quality so that it can be rejected in favour of more certain information.
        private QualifiedDate fInferredBirthday;

        // The individual's date of birth
        private GEDCOMDateValue fActualBirthday;

        // A date inferred for the individual's date of death, with associated quality so that it can be rejected in favour of more certain information.
        private QualifiedDate fInferredDeathday;

        // The individual's date of death
        private GEDCOMDateValue fActualDeathday;

        // Records first occurrence of one-off events, so that they may be marked as "preferred"
        private Hashtable fFirstFoundEvent;

        // The sources giving the individual's birth date
        private string fBirthdaySourceRefs;

        // The sources giving the individual's death date
        private string fDeathdaySourceRefs;

        // The individual's title (GEDCOM:TITL)
        private string fNameTitle;

        // Indicates that a name for this individual is not available
        private bool fUnknownName;

        // The individual's main name
        private string fName;

        // The suffix on the individual's name (e.g. "Snr")
        private string fNameSuffix;

        // The individual's first name
        private string fFirstName;

        // The individual's surname
        private string fSurname;

        // The individual's fully expanded name
        private string fFullName;

        // The individual's nickname
        private string fNickName;

        // The individual's commonly used name
        private string fUsedName;

        // The sources giving the individual's name
        private string fNameSources;

        // The individual's occupation, for display at the page head
        private string fOccupation;

        // All the frParents of the individual. May be more than one pair if individual was associated with more than one fr.
        private ArrayList fParents;

        // A reference to the index creator, so that individual pages can be added to the index as they are created.
        private CreatorIndexIndividuals fIndiIndexCreator;

        // The paintbox with which to draw the mini tree.
        private Paintbox fPaintbox;


        public CreatorRecordIndividual(GEDCOMTree gedcom, IProgressCallback progress, string w3cfile, GEDCOMIndividualRecord ir, CreatorIndexIndividuals indiIndexCreator, Paintbox paintbox) : base(gedcom, progress, w3cfile)
        {
            fIndiRec = ir;
            fIndiIndexCreator = indiIndexCreator;
            fPaintbox = paintbox;
            fFirstFoundEvent = new Hashtable();
            fBirthdaySourceRefs = "";
            fDeathdaySourceRefs = "";
            fNameTitle = "";
            fUnknownName = false;
            fName = fIndiRec.GetPrimaryFullName();
            fNameSuffix = ""/*fIndiRec.NameSuffix*/; // TODO
            fFirstName = "";
            fSurname = "";
            fOccupation = "";
            fConcealed = !fIndiRec.GetVisibility();
            fEventList = new List<Event>();
            fAttributeList = new ArrayList();
            fReferenceList = new List<GEDCOMSourceCitation>();
            fOccupations = new ArrayList();
            fPreviousChildLink = "";
            fNextChildLink = "";
            fOtherNames = new ArrayList();
            fInferredBirthday = null;
            fActualBirthday = null;
            fInferredDeathday = null;
            fActualDeathday = null;
            fParents = new ArrayList();
        }

        // The main method that causes the page to be created.
        public bool Create(Stats stats)
        {
            fLogger.WriteInfo("CCreatorRecordIndividual.Create()");

            if (fIndiRec == null) {
                return false;
            }

            if (!fIndiRec.GetVisibility()) {
                return false;
            }

            // Collect together multimedia links
            if (MainForm.Config.AllowMultimedia && !fConcealed) {
                AddMultimedia(fIndiRec.MultimediaLinks, String.Concat(fIndiRec.XRef, "mm"), String.Concat(fIndiRec.XRef, "mo"), MainForm.Config.MaxImageWidth, MainForm.Config.MaxImageHeight, stats);
            }

            AddEvents();

            RemoveLoneOccupation();

            var lifeDatesX = fIndiRec.GetLifeDates();
            fActualBirthday = (lifeDatesX.BirthEvent == null) ? null : lifeDatesX.BirthEvent.Date;
            fActualDeathday = (lifeDatesX.DeathEvent == null) ? null : lifeDatesX.DeathEvent.Date;

            ConstructName();

            GEDCOMDateValue age30;
            if (fInferredBirthday != null) {
                age30 = new GEDCOMDateValue(null);
                age30.Assign(fInferredBirthday.Date);
            } else {
                age30 = new GEDCOMDateValue(null);
                age30.SetDateTime(DateTime.Now);
            }
            try {
                ((GEDCOMDate)age30.Value).Year += (short)MainForm.Config.AgeForOccupation;
            } catch { }
            // FIXME

            // We should have birthday and deathday by now, so find longest occupation
            if (!fConcealed) {
                fOccupation = BestOccupation(fOccupations, age30, (fInferredBirthday != null) ? fInferredBirthday.Date : null, (fInferredDeathday != null) ? fInferredDeathday.Date : null); // Picks occupation with longest time span
            }

            // Go through all families this person was a irSubject to
            if (!fConcealed) {
                foreach (GEDCOMSpouseToFamilyLink sfl in fIndiRec.SpouseToFamilyLinks) {
                    GEDCOMFamilyRecord fr = (sfl.Value as GEDCOMFamilyRecord);
                    if (fr != null) {
                        // Find the irSubject's name
                        GEDCOMIndividualRecord spouse = null;
                        string spouseLink = "";
                        spouse = fr.GetSpouseBy(fIndiRec);
                        if (spouse != null && spouse.GetVisibility()) {
                            spouseLink = MakeLink(spouse);
                        }

                        // Add fr events as events connected to this individual
                        foreach (GEDCOMFamilyEvent fes in fr.Events) {
                            ProcessEvent(fes, spouseLink);
                        }

                        AddChildrensEvents(fr);

                        AddMarriage(spouse, spouseLink, fr);
                    }
                }
                AddParentsAndSiblings();
            } // end if !concealed

            string birthyear = "";
            string deathyear = "";
            if (!fConcealed) {
                if (fInferredBirthday != null && fInferredBirthday.Date != null) {
                    birthyear = fInferredBirthday.Date.GetDisplayStringExt(DateFormat.dfYYYY, false, false);
                }
                if (fInferredDeathday != null && fInferredDeathday.Date != null) {
                    deathyear = fInferredDeathday.Date.GetDisplayStringExt(DateFormat.dfYYYY, false, false);
                }
            }

            string title = fName; //"Fred Bloggs 1871-1921"
            string lifeDates = "";
            if (!fConcealed) {
                if (birthyear != "" || deathyear != "") {
                    lifeDates = String.Concat(birthyear, "-", deathyear);
                    title = String.Concat(fName, " ", lifeDates);
                }
            }

            AddIndividualIndexEntry(lifeDates);

            OutputHTML(title);

            return true;
        }

        // Adds the marriage associated with the fr record to the list of events. Also adds irSubject death if within this person's lifetime.
        private void AddMarriage(GEDCOMIndividualRecord spouse, string spouseLink, GEDCOMFamilyRecord fr)
        {
            // Find wedding date
            if (spouse != null) {
                string sourceRefs = AddSpouseDeath(spouse, spouseLink);

                GEDCOMDateValue marriageDate;
                string marriageNote;
                string marriagePlace;
                sourceRefs = AddMarriageEvent(fr, sourceRefs, out marriageDate, out marriageNote, out marriagePlace);

                marriageNote = BuildMaritalStatusNote(fr, marriageNote);

                // Add fr record notes to marriage event
                foreach (GEDCOMNotes ns in fr.Notes) {
                    if (marriageNote != "") {
                        marriageNote += "\n";
                    }
                    if (MainForm.Config.ObfuscateEmails) {
                        marriageNote += ObfuscateEmail(ns.Notes.Text);
                    } else {
                        marriageNote += ns.Notes.Text;
                    }
                }

                string marriedString = "married ";
                if (fr.Status == GKMarriageStatus.MarrNotRegistered) {
                    marriedString = "partner of ";
                }
                if (marriageDate != null) {
                    Event iEvent = new Event(marriageDate, "_MARRIAGE", String.Concat(marriedString, spouseLink, marriagePlace, ".", sourceRefs), "", marriageNote, true, MainForm.Config.CapitaliseEventDescriptions);
                    fEventList.Add(iEvent);
                }
                // else its an attribute.
                else {
                    Event iEvent = new Event(marriageDate, "_MARRIAGE", String.Concat(marriedString, spouseLink, marriagePlace, ".", sourceRefs), "", marriageNote, true, MainForm.Config.CapitaliseEventDescriptions);
                    // Marriages go at the front of the list so that they appear first in "Other facts"
                    fAttributeList.Insert(0, iEvent);
                }
            }
        }

        // Goes through all families this person was a irSibling in and finds their frParents and siblings.
        private void AddParentsAndSiblings()
        {
            // Set a limit for date comparisons
            DateTime dtNow = DateTime.Now;

            // Go through all families this person was a irSibling in
            foreach (GEDCOMChildToFamilyLink cfl in fIndiRec.ChildToFamilyLinks) {
                GEDCOMFamilyRecord fr = (cfl.Value as GEDCOMFamilyRecord);
                if (fr != null) {
                    GEDCOMIndividualRecord husband = fr.GetHusband();
                    GEDCOMIndividualRecord wife = fr.GetWife();

                    if (husband != null || wife != null) {
                        HusbandAndWife parents = new HusbandAndWife();
                        parents.Husband = husband;
                        parents.Wife = wife;
                        fParents.Add(parents);
                    }

                    // Get all the children in order, to find previous and next irSibling
                    GEDCOMDateValue testBirthday = (fInferredBirthday != null) ? fInferredBirthday.Date : null;

                    if (testBirthday == null) {
                        testBirthday = new GEDCOMDateValue(null);
                        testBirthday.SetDateTime(dtNow);
                    }

                    int previousDifference = -100 * 365; // 100 years should be enough
                    int nextDifference = 100 * 365;

                    foreach (var ch in fr.Children) {
                        if (ch.XRef == fIndiRec.XRef)
                            continue;

                        GEDCOMIndividualRecord child = ch.Value as GEDCOMIndividualRecord;
                        if (child != null) {
                            if (!child.GetVisibility())
                                continue;

                            GEDCOMCustomEvent childBirthday = child.FindEvent("BIRT");
                            if (childBirthday == null) {
                                childBirthday = child.FindEvent("CHR");
                            }
                            if (childBirthday == null) {
                                childBirthday = child.FindEvent("BAPM");
                            }

                            GEDCOMDateValue childBirthdate = null;
                            if (childBirthday != null)
                                childBirthdate = childBirthday.Date;
                            if (childBirthdate == null) {
                                childBirthdate = new GEDCOMDateValue(null);
                                childBirthdate.SetDateTime(dtNow);
                            }

                            int difference = Extensions.GetEventsYearsDiff(testBirthday, childBirthdate);
                            if (difference < 0) {
                                if (difference > previousDifference) {
                                    previousDifference = difference;
                                    fPreviousChildLink = MakeLink(child, "previous child");
                                }
                            } else if (difference > 0) {
                                if (difference < nextDifference) {
                                    nextDifference = difference;
                                    fNextChildLink = MakeLink(child, "next child");
                                }
                            } else {
                                fNextChildLink = MakeLink(child, "next child");
                            }
                        }
                    }
                }
            }
        }

        // Adds this individual page to the index of pages.
        private void AddIndividualIndexEntry(string lifeDates)
        {
            string relativeFilename = GetIndividualHTMLFilename(fIndiRec);
            // Create some strings to use in index entry
            string sUserRef = "";
            if (fIndiRec.UserReferences.Count > 0) {
                GEDCOMUserReference urn = fIndiRec.UserReferences[0];
                sUserRef = EscapeHTML(urn.StringValue, false);
                if (sUserRef.Length > 0) {
                    sUserRef = String.Concat(" [", sUserRef, "]");
                }
            }
            string alterEgo = "";
            if (MainForm.Config.IncludeNickNamesInIndex) {
                if (fNickName != "") {
                    alterEgo = String.Concat("(", fNickName, ") ");
                } else if (fUsedName != "") {
                    alterEgo = String.Concat("(", fUsedName, ") ");
                }
            }

            if (fIndiIndexCreator != null) {
                // Add index entry for this individuals main name (or hidden/unknown string)
                string sFirstName = fFirstName;
                if (fNameSuffix != null && fNameSuffix != "") {
                    sFirstName += ", " + fNameSuffix;
                }
                fIndiIndexCreator.AddIndividualToIndex(sFirstName, fSurname, fUnknownName, alterEgo, lifeDates, fConcealed, relativeFilename, sUserRef);

                // Add entries for this individual's other names
                if (!fConcealed && !fUnknownName) {
                    string other_name = "";
                    for (int i = 1; (other_name = fIndiRec.GetName(i)) != ""; i++) {
                        string other_firstName = "";
                        string other_surname = "";
                        other_name = MainForm.Config.CapitaliseName(other_name, ref other_firstName, ref other_surname); // Also splits name into first name and surname
                        fIndiIndexCreator.AddIndividualToIndex(other_firstName, other_surname, fUnknownName, alterEgo, lifeDates, fConcealed, relativeFilename, sUserRef);
                    }
                }
            }
        }

        // Extracts the data from the MARR event for the fr record.
        private string AddMarriageEvent(GEDCOMFamilyRecord fr, string sourceRefs, out GEDCOMDateValue marriageDate, out string marriageNote, out string marriagePlace)
        {
            // Find out when they were married
            marriageDate = null;
            marriagePlace = "";
            sourceRefs = "";
            marriageNote = "";
            foreach (GEDCOMFamilyEvent fes in fr.Events) {
                if (fes.Name == "MARR") {
                    {
                        marriageDate = fes.Date;

                        if (fes.Place != null) {
                            if (fes.Place.StringValue != "")
                                marriagePlace = String.Concat(" ", MainForm.Config.PlaceWord, " ", EscapeHTML(fes.Place.StringValue, false));
                        }

                        sourceRefs = AddSources(ref fReferenceList, fes.SourceCitations);

                        if (fes.Notes != null) {
                            foreach (GEDCOMNotes ns in fes.Notes) {
                                if (marriageNote != "") {
                                    marriageNote += "\n";
                                }

                                if (MainForm.Config.ObfuscateEmails) {
                                    marriageNote += ObfuscateEmail(ns.Notes.Text);
                                } else {
                                    marriageNote += ns.Notes.Text;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return sourceRefs;
        }

        // Extracts the data from the DEAT event for the given individual and adds it if it was an event in the current individual's lifetime.
        private string AddSpouseDeath(GEDCOMIndividualRecord spouse, string spouseLink)
        {
            string sourceRefs = "";
            string place = "";
            if (spouse.GetVisibility()) {
                // Record death of irSubject if within this person's lifetime
                GEDCOMDateValue spouseDeathDate = null;
                foreach (GEDCOMCustomEvent ies in spouse.Events) {
                    if (ies.Name == "DEAT") {
                        {
                            spouseDeathDate = ies.Date;
                            if (spouseDeathDate != null) {
                                if (fInferredDeathday == null || fInferredDeathday.Date == null || spouseDeathDate.CompareTo(fInferredDeathday.Date) <= 0) {
                                    if (ies.Place != null) {
                                        if (ies.Place.StringValue != "")
                                            place = String.Concat(" ", MainForm.Config.PlaceWord, " ", EscapeHTML(ies.Place.StringValue, false));
                                    }

                                    sourceRefs = AddSources(ref fReferenceList, ies.SourceCitations);

                                    if (spouseDeathDate != null) {
                                        Event iEvent = new Event(spouseDeathDate, "_SPOUSEDIED", String.Concat("death of ", spouseLink, place, ".", sourceRefs), "", null, false, MainForm.Config.CapitaliseEventDescriptions);
                                        fEventList.Add(iEvent);
                                    }
                                    // else its an attribute.
                                    else {
                                        Event iEvent = new Event(null, "_SPOUSEDIED", String.Concat("death of ", spouseLink, place, ".", sourceRefs), "", null, false, MainForm.Config.CapitaliseEventDescriptions);
                                        fAttributeList.Add(iEvent);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            return sourceRefs;
        }

        // Adds birth, baptism, death etc of the children in the given fr.
        private void AddChildrensEvents(GEDCOMFamilyRecord fr)
        {
            // Find out all the children.
            foreach (var ch in fr.Children) {
                GEDCOMIndividualRecord child = ch.Value as GEDCOMIndividualRecord;

                if (child != null) {
                    bool childConcealed = !child.GetVisibility();

                    string childSex = "child";
                    if (!childConcealed) {
                        if (child.Sex == GEDCOMSex.svMale) {
                            childSex = "son";
                        } else if (child.Sex == GEDCOMSex.svFemale) {
                            childSex = "daughter";
                        }
                    }

                    string childLink = MakeLink(child);
                    string sourceRefs = "";

                    if (!childConcealed) {
                        // Add death of children if happened in irSubject's lifetime.
                        // Note this is done before birth because the way the subsequent sort works it will put death after birth.
                        GEDCOMCustomEvent childDeathday = child.FindEvent("DEAT");
                        if (childDeathday == null) {
                            childDeathday = child.FindEvent("BURI");
                        }
                        if (childDeathday == null) {
                            childDeathday = child.FindEvent("CREM");
                        }

                        string deathPlace = "";
                        GEDCOMDateValue childDeathdate = null;

                        if (childDeathday != null) {
                            childDeathdate = childDeathday.Date;

                            if (childDeathday.Place != null) {
                                if (childDeathday.Place.StringValue != "") {
                                    deathPlace = String.Concat(" ", MainForm.Config.PlaceWord, " ", EscapeHTML(childDeathday.Place.StringValue, false));
                                }
                            }
                        }

                        if (childDeathdate != null && fInferredDeathday != null && fInferredDeathday.Date != null && (childDeathdate.CompareTo(fInferredDeathday.Date) <= 0)) {
                            sourceRefs = AddSources(ref fReferenceList, childDeathday.SourceCitations);
                            Event iEvent = new Event(childDeathdate, "_CHILDDIED", String.Concat("death of ", childSex, " ", childLink, deathPlace, ".", sourceRefs), "", null, false, MainForm.Config.CapitaliseEventDescriptions);
                            fEventList.Add(iEvent);
                        }
                    }

                    // Add birth of children.
                    // Note this is done after deaths because the way the subsequent sort works it will put death after birth.
                    GEDCOMCustomEvent childBirthday = child.FindEvent("BIRT");
                    if (childBirthday == null) {
                        childBirthday = child.FindEvent("CHR");
                    }
                    if (childBirthday == null) {
                        childBirthday = child.FindEvent("BAPM");
                    }

                    string birthPlace = "";
                    GEDCOMDateValue childBirthdate = null;
                    sourceRefs = "";

                    if (childBirthday != null && !childConcealed) {
                        childBirthdate = childBirthday.Date;

                        if (childBirthday.Place != null) {
                            if (childBirthday.Place.StringValue != "") {
                                birthPlace = String.Concat(" ", MainForm.Config.PlaceWord, " ", EscapeHTML(childBirthday.Place.StringValue, false));
                            }
                        }
                        sourceRefs = AddSources(ref fReferenceList, childBirthday.SourceCitations);
                    }

                    if (childBirthdate == null) {
                        Event iEvent = new Event(null, "_CHILDBORN", String.Concat("birth of ", childSex, " ", childLink, birthPlace, ".", sourceRefs), "", null, true, MainForm.Config.CapitaliseEventDescriptions);
                        fAttributeList.Add(iEvent);
                    } else {
                        Event iEvent = new Event(childBirthdate, "_CHILDBORN", String.Concat("birth of ", childSex, " ", childLink, birthPlace, ".", sourceRefs), "", null, true, MainForm.Config.CapitaliseEventDescriptions);
                        fEventList.Add(iEvent);
                    }
                }
            }
        }

        // Works through all the events records for this individual and extracts information from them.
        private void AddEvents()
        {
            if (fIndiRec.Events != null && !fConcealed) {
                foreach (GEDCOMCustomEvent ies in fIndiRec.Events) {
                    ProcessEvent(ies, null);
                    if (ies.Name == "TITL") {
                        if (fNameTitle.Length > 0) {
                            fNameTitle += " ";
                        }
                        fNameTitle += ies.StringValue;
                    }
                }
            }
        }

        // Extracts the name information from the individual record.
        private void ConstructName()
        {
            // Construct the guy's name
            if (fConcealed && !MainForm.Config.UseWithheldNames) {
                fFirstName = "";
                fSurname = fName = MainForm.Config.ConcealedName;
            } else {
                fName = MainForm.Config.CapitaliseName(fName, ref fFirstName, ref fSurname); // Also splits name into first name and surname
            }
            if (fName == "") {
                fFirstName = "";
                fSurname = fName = MainForm.Config.UnknownName;
                fUnknownName = true;
            }

            // Remember other name records
            if (!fConcealed && !fUnknownName) {
                NameAndSource nasOther;
                for (int i = 1; (nasOther = fIndiRec.GetNameAndSource(i)) != null; i++) {
                    string sFirstNameOther = "";
                    string sSurnameOther = "";
                    nasOther.Name = MainForm.Config.CapitaliseName(nasOther.Name, ref sFirstNameOther, ref sSurnameOther); // Also splits name into first name and surname
                    nasOther.SourceHtml = AddSources(ref fReferenceList, nasOther.Sources);
                    fOtherNames.Add(nasOther);
                }
            }

            if (fConcealed && !MainForm.Config.UseWithheldNames) {
                fFullName = MainForm.Config.ConcealedName;
            } else {
                fFullName = fIndiRec.GetPrimaryFullName();
                string sDummy = "";
                fFullName = MainForm.Config.CapitaliseName(fFullName, ref sDummy, ref sDummy); // Also splits name into first name and surname
            }
            if (fFullName == "") {
                fFullName = MainForm.Config.UnknownName;
            }

            if (fNameTitle.Length > 0) {
                fFullName = String.Concat(fNameTitle, " ", fFullName);
            }
            if (fConcealed) {
                fNickName = "";
                fUsedName = "";
            } else {
                fNickName = /*fIndiRec.NickName*/""; // TODO
                fUsedName = /*fIndiRec.UsedName*/""; // TODO
            }

            // Add general source references
            fNameSources = "";
            if (!fConcealed) {
                //fIndiRec.AddMainNameSources(ref alNameSourcesList);
                fNameSources = AddSources(ref fReferenceList, fIndiRec.SourceCitations);
            }
        }

        // Creates a file and writes into it the HTML for the individual's page.
        private void OutputHTML(string title)
        {
            HTMLFile f = null;
            string pageDescription = "GEDmill GEDCOM to HTML page for " + fName;
            string keywords = "family tree history " + fName;
            string relativeFilename = GetIndividualHTMLFilename(fIndiRec);
            string fullFilename = String.Concat(MainForm.Config.OutputFolder, "\\", relativeFilename);

            try {
                f = new HTMLFile(fullFilename, title, pageDescription, keywords); // Creates a new file, and puts standard header html into it.

                if (f != null) {
                    OutputPageHeader(f.Writer, fPreviousChildLink, fNextChildLink, true, true);

                    if (MainForm.Config.ShowMiniTrees) {
                        OutputMiniTree(f);
                    }
                    f.Writer.WriteLine("    <div class=\"hr\" />");
                    f.Writer.WriteLine("");
                    f.Writer.WriteLine("    <div id=\"page\"> <!-- page -->");

                    OutputMultimedia(f);

                    f.Writer.WriteLine("      <div id=\"main\">");

                    f.Writer.WriteLine("        <div id=\"summary\">");
                    OutputNames(f);
                    OutputIndividualSummary(f);
                    f.Writer.WriteLine("        </div> <!-- summary -->");

                    if (!MainForm.Config.ShowMiniTrees) {
                        OutputParentNames(f);
                    }

                    if (!fConcealed) {
                        fEventList.Sort();
                        OutputEvents(f);
                        OutputAttributes(f);
                        OutputNotes(f);
                        OutputSourceReferences(f);
                    }

                    f.Writer.WriteLine("      </div> <!-- main -->");

                    f.Writer.WriteLine("");

                    // Add footer (Record date, W3C sticker, GEDmill credit etc.)
                    OutputFooter(f, fIndiRec);

                    f.Writer.WriteLine("    </div> <!-- page -->");
                }
            } catch (IOException e) {
                fLogger.WriteError("Caught IO Exception(4) : ", e);
            } catch (ArgumentException e) {
                fLogger.WriteError("Caught Argument Exception(4) : ", e);
            } finally {
                if (f != null) {
                    // Close adds the standard footer to the file
                    f.Close();
                }
            }
        }

        // Outputs the HTML for the list of Sources referenced in the page.
        private void OutputSourceReferences(HTMLFile f)
        {
            if (fReferenceList.Count > 0) {
                f.Writer.WriteLine("        <div id=\"references\">");
                f.Writer.WriteLine("          <h1>Sources</h1>");
                f.Writer.WriteLine("          <ul>");

                for (uint i = 0; i < fReferenceList.Count; i++) {
                    GEDCOMSourceCitation sc = (GEDCOMSourceCitation)(fReferenceList[(int)i]);

                    string extraInfo = "";
                    GEDCOMSourceRecord sr = sc.Value as GEDCOMSourceRecord;

                    // Publication facts
                    if (sr != null && sr.Publication.Text != null && sr.Publication.Text != "") {
                        string pubFacts;
                        if (MainForm.Config.ObfuscateEmails) {
                            pubFacts = ObfuscateEmail(sr.Publication.Text);
                        } else {
                            pubFacts = sr.Publication.Text;
                        }

                        if (pubFacts.Length > 7 && pubFacts.ToUpper().Substring(0, 7) == "HTTP://") {
                            pubFacts = String.Concat("<a href=\"", pubFacts, "\">", EscapeHTML(pubFacts, false), "</a>");
                            extraInfo += String.Concat("\n                <li>", pubFacts, "</li>");
                        } else {
                            extraInfo += String.Concat("\n                <li>", EscapeHTML(pubFacts, false), "</li>");
                        }
                    }

                    // Where within source
                    // TODO
                    /*string whereWithinSource = sc.GetWhereWithinSource();
                    if (whereWithinSource != null && whereWithinSource.Length > 0) {
                        extraInfo += String.Concat("\n                <li>", EscapeHTML(whereWithinSource, false), "</li>");
                    }*/

                    // Certainty assessment
                    // TODO
                    /*string certaintyAssessment = sc.GetCertaintyAssessment();
                    if (certaintyAssessment != null && certaintyAssessment.Length > 0) {
                        extraInfo += String.Concat("\n                <li>", EscapeHTML(certaintyAssessment, false), "</li>");
                    }*/

                    // Surround any extra info in its own list
                    if (extraInfo.Length > 0) {
                        extraInfo = String.Concat("\n              <ul>", extraInfo, "\n              </ul>");
                    }

                    // Finally write source link and extra info
                    f.Writer.WriteLine(String.Concat("            <li>", sc.MakeLinkText(i + 1), extraInfo, "</li>"));
                }
                f.Writer.WriteLine("          </ul>");
                f.Writer.WriteLine("        </div> <!-- references -->");
            }
        }

        // Outputs the HTML for the Notes section of the page
        private void OutputNotes(HTMLFile f)
        {
            if (fIndiRec.Notes.Count > 0) {
                // Generate notes list into a local array before adding header title. This is to cope with the case where all notes are nothing but blanks.
                ArrayList note_strings = new ArrayList(fIndiRec.Notes.Count);

                foreach (GEDCOMNotes ns in fIndiRec.Notes) {
                    string noteText;
                    if (MainForm.Config.ObfuscateEmails) {
                        noteText = ObfuscateEmail(ns.Notes.Text);
                    } else {
                        noteText = ns.Notes.Text;
                    }

                    note_strings.Add(String.Concat("            <li>", EscapeHTML(noteText, false), "</li>"));
                }

                if (note_strings.Count > 0) {
                    f.Writer.WriteLine("        <div id=\"notes\">");
                    f.Writer.WriteLine("          <h1>Notes</h1>");
                    f.Writer.WriteLine("          <ul>");

                    foreach (string note_string in note_strings) {
                        f.Writer.WriteLine(note_string);
                    }

                    f.Writer.WriteLine("          </ul>");
                    f.Writer.WriteLine("        </div> <!-- notes -->");
                }
            }
        }

        // Outputs the HTML for the Other Facts section of the page.
        private void OutputAttributes(HTMLFile f)
        {
            if (fAttributeList.Count > 0) {
                f.Writer.WriteLine("        <div id=\"facts\">");
                f.Writer.WriteLine("          <h1>Other facts</h1>");
                f.Writer.WriteLine("          <table>");

                for (int i = 0; i < fAttributeList.Count; i++) {
                    Event iEvent = (Event)fAttributeList[i];

                    string importance;
                    if (iEvent.Important) {
                        importance = " class=\"important\"";
                    } else {
                        importance = "";
                    }

                    string attrNote = "";
                    string noteString = iEvent.Note;
                    if (noteString != null) {
                        attrNote = String.Concat("<p class=\"eventNote\">", EscapeHTML(noteString, false), "</p>");
                    }

                    f.Writer.WriteLine("            <tr>");
                    f.Writer.WriteLine("              <td class=\"date\"><p>&nbsp;</p></td>");
                    f.Writer.WriteLine("              <td class=\"event\"><p{0}>{1}</p>{2}</td>", importance, iEvent.ToString(), attrNote);
                    f.Writer.WriteLine("            </tr>");
                }
                f.Writer.WriteLine("          </table>");
                f.Writer.WriteLine("        </div> <!-- facts -->");
            }
        }

        // Outputs the HTML for the Life History section of the page.
        private void OutputEvents(HTMLFile f)
        {
            if (fEventList.Count > 0) {
                f.Writer.WriteLine("        <div id=\"events\">");
                f.Writer.WriteLine("          <h1>Life History</h1>");
                f.Writer.WriteLine("          <table>");

                for (int i = 0; i < fEventList.Count; i++) {
                    Event iEvent = fEventList[i];

                    string importance;
                    if (iEvent.Important) {
                        importance = " class=\"important\"";
                    } else {
                        importance = "";
                    }

                    string eventNote = "";
                    string overviewString = iEvent.Overview;
                    if (overviewString != null && overviewString != "") {
                        eventNote = String.Concat("<p class=\"eventNote\">", EscapeHTML(overviewString, false), "</p>");
                    }
                    string noteString = iEvent.Note;
                    if (noteString != null && noteString != "") {
                        eventNote += String.Concat("<p class=\"eventNote\">", EscapeHTML(noteString, false), "</p>");
                    }
                    string preference = "";
                    if (iEvent.Preference == EventPreference.First) {
                        preference = " (most likely)";
                    } else if (iEvent.Preference == EventPreference.Subsequent) {
                        preference = " (less likely)";
                    }
                    f.Writer.WriteLine("            <tr>");
                    f.Writer.WriteLine("              <td class=\"date\"><p{0}>{1}</p></td>", importance, EscapeHTML(iEvent.Date, false));
                    f.Writer.WriteLine("              <td class=\"event\"><p{0}>{1}</p>{2}{3}</td>", importance, iEvent.ToString(), preference, eventNote);
                    f.Writer.WriteLine("            </tr>");
                }
                f.Writer.WriteLine("          </table>");
                f.Writer.WriteLine("        </div> <!-- events -->");
            }
        }

        // Writes the "Parents" section of the page to the HTML file. 
        private void OutputParentNames(HTMLFile f)
        {
            if (fParents.Count > 0) {
                f.Writer.WriteLine("        <div id=\"parents\">");
                f.Writer.WriteLine("          <h1>Parents</h1>");

                string sChild = "Child";
                if (fIndiRec.Sex == GEDCOMSex.svMale) {
                    sChild = "Son";
                } else if (fIndiRec.Sex == GEDCOMSex.svFemale) {
                    sChild = "Daughter";
                }

                for (int i = 0; i < fParents.Count; i++) {
                    HusbandAndWife parents = (HusbandAndWife)fParents[i];
                    string sParents = "";
                    if (parents.Husband != null && parents.Husband.GetVisibility()) {
                        sParents = MakeLink(parents.Husband);
                    }
                    if (parents.Wife != null && parents.Wife.GetVisibility()) {
                        string wifeName = MakeLink(parents.Wife);
                        if (sParents == "") {
                            sParents = wifeName;
                        } else {
                            sParents += " & " + wifeName;
                        }
                    }
                    if (sParents != "") {
                        f.Writer.WriteLine(String.Concat("          <p>", sChild, " of ", sParents, ".</p>"));
                    }
                }
                f.Writer.WriteLine("        </div> <!-- parents -->");
            }
        }

        // Writes the individual's lifespan and occupation to the HTML file. 
        private void OutputIndividualSummary(HTMLFile f)
        {
            f.Writer.WriteLine("          <div id=\"individualSummary\">");

            string sBirthday;
            if (fActualBirthday != null) {
                sBirthday = fActualBirthday.GetDisplayStringExt(DateFormat.dfYYYY_MM_DD, true, false) + fBirthdaySourceRefs;
            } else {
                sBirthday = "";
            }

            string sDeathday;
            if (fActualDeathday != null) {
                sDeathday = fActualDeathday.GetDisplayStringExt(DateFormat.dfYYYY_MM_DD, true, false) + fDeathdaySourceRefs;
            } else {
                sDeathday = "";
            }

            if (fActualBirthday != null || fActualDeathday != null) {
                f.Writer.WriteLine(String.Concat("            <p>", sBirthday, " - ", sDeathday, "</p>"));
            }
            if (MainForm.Config.OccupationHeadline && fOccupation != null && fOccupation != "") {
                f.Writer.WriteLine(String.Concat("            <p>", fOccupation, "</p>"));
            }
            if (fConcealed) {
                f.Writer.WriteLine("            <p>Information about this individual has been withheld.</p>");
            }
            f.Writer.WriteLine("          </div> <!-- individualSummary -->");
        }

        // Writes the individual's names to the HTML file. 
        private void OutputNames(HTMLFile f)
        {
            f.Writer.WriteLine("          <div id=\"names\">");
            if (fFullName != fName) {
                f.Writer.WriteLine(String.Concat("            <h2>", EscapeHTML(fFullName, false), "</h2>"));
            }
            if (fUsedName != "" && fNickName != "") {
                fUsedName += ", ";
            }
            string nicknames = "";
            if (fUsedName != "" || fNickName != "") {
                nicknames = String.Concat(" <span class=\"nicknames\">(", EscapeHTML(fUsedName, false), EscapeHTML(fNickName, false), ")</span>");
            }
            f.Writer.WriteLine(String.Concat("            <h1>", EscapeHTML(fName, false), fNameSources, nicknames, "</h1>"));
            foreach (NameAndSource other_name in fOtherNames) {
                f.Writer.WriteLine(String.Concat("            <h2>also known as ", EscapeHTML(other_name.Name, false), other_name.SourceHtml, "</h2>"));
            }
            f.Writer.WriteLine("          </div> <!-- names -->");
        }

        // Writes the HTML for the multimedia files associated with this record. 
        private void OutputMultimedia(HTMLFile f)
        {
            if (fMultimediaList.Count > 0) {
                Multimedia iMultimedia = (Multimedia)fMultimediaList[0];
                f.Writer.WriteLine("    <div id=\"photos\">");
                f.Writer.WriteLine("      <div id=\"mainphoto\">");
                string non_pic_small_filename = "multimedia/" + MainForm.NonPicFilename(iMultimedia.Format, true, MainForm.Config.LinkOriginalPicture);
                string non_pic_main_filename = "multimedia/" + MainForm.NonPicFilename(iMultimedia.Format, false, MainForm.Config.LinkOriginalPicture);
                string image_title = "";
                string alt_name = fFullName;
                if (iMultimedia.Title != null) {
                    image_title = iMultimedia.Title;
                    alt_name = iMultimedia.Title;
                }
                if (MainForm.Config.LinkOriginalPicture) {
                    if (iMultimedia.Width != 0 && iMultimedia.Height != 0) {
                        // Must be a picture.
                        if (iMultimedia.LargeFileName.Length > 0) {
                            f.Writer.WriteLine(String.Concat("        <a href=\"", iMultimedia.LargeFileName, "\" id=\"mainphoto_link\"><img id=\"mainphoto_img\" src=\"", iMultimedia.FileName, "\" alt=\"", alt_name, "\" /></a>"));
                        } else {
                            f.Writer.WriteLine(String.Concat("        <img id=\"mainphoto_img\" src=\"", iMultimedia.FileName, "\" alt=\"I", alt_name, "\" />"));
                        }
                    } else {
                        // Must be a non-picture multimedia file.
                        if (iMultimedia.LargeFileName.Length > 0) {
                            f.Writer.WriteLine(String.Concat("        <a href=\"", iMultimedia.LargeFileName, "\" id=\"mainphoto_link\"><img id=\"mainphoto_img\" src=\"", non_pic_main_filename, "\" alt=\"", alt_name, "\" /></a>"));
                        } else {
                            f.Writer.WriteLine(String.Concat("        <img id=\"mainphoto_img\" src=\"", non_pic_main_filename, "\" alt=\"", alt_name, "\" />"));
                        }
                    }
                } else // Not linking to original picture.
                  {
                    if (iMultimedia.Width != 0 && iMultimedia.Height != 0) {
                        // Must be a picture.
                        f.Writer.WriteLine(String.Concat("        <img id=\"mainphoto_img\" src=\"", iMultimedia.FileName, "\" alt=\"", alt_name, "\" />"));
                    } else {
                        // Must be a non-picture multimedia file.
                        f.Writer.WriteLine(String.Concat("        <img id=\"mainphoto_img\" src=\"", non_pic_main_filename, "\" alt=\"", alt_name, "\" />"));
                    }
                }
                f.Writer.WriteLine(String.Concat("        <p id=\"mainphoto_title\">", image_title, "</p>"));
                f.Writer.WriteLine("      </div>");

                if (fMultimediaList.Count > 1 && MainForm.Config.AllowMultipleImages) {
                    f.Writer.WriteLine("      <div id=\"miniphotos\">");

                    for (int i = 0; i < fMultimediaList.Count; i++) {
                        iMultimedia = (Multimedia)fMultimediaList[i];

                        non_pic_small_filename = "multimedia/" + MainForm.NonPicFilename(iMultimedia.Format, true, MainForm.Config.LinkOriginalPicture);
                        non_pic_main_filename = "multimedia/" + MainForm.NonPicFilename(iMultimedia.Format, false, MainForm.Config.LinkOriginalPicture);

                        string largeFilenameArg;
                        if (iMultimedia.LargeFileName != null && iMultimedia.LargeFileName.Length > 0) {
                            largeFilenameArg = String.Concat("'", iMultimedia.LargeFileName, "'");
                        } else {
                            largeFilenameArg = "null";
                        }

                        f.Writer.WriteLine("         <div class=\"miniphoto\">");
                        if (iMultimedia.Width != 0 && iMultimedia.Height != 0) {
                            // Must be a picture.
                            // Scale mini pic down to thumbnail.
                            Rectangle newArea = new Rectangle(0, 0, iMultimedia.Width, iMultimedia.Height);
                            MainForm.ScaleAreaToFit(ref newArea, MainForm.Config.MaxThumbnailImageWidth, MainForm.Config.MaxThumbnailImageHeight);

                            f.Writer.WriteLine(String.Concat("          <img style=\"width:", newArea.Width, "px; height:", newArea.Height, "px; margin-bottom:", MainForm.Config.MaxThumbnailImageHeight - newArea.Height, "px;\" class=\"miniphoto_img\" src=\"", iMultimedia.FileName, "\" alt=\"Click to select\" onclick=\"updateMainPhoto('", iMultimedia.FileName, "','", EscapeJavascript(iMultimedia.Title), "',", largeFilenameArg, ")\" />"));
                        } else {
                            // Other multimedia.
                            f.Writer.WriteLine(String.Concat("          <img style=\"width:", MainForm.Config.MaxThumbnailImageWidth, "px; height:", MainForm.Config.MaxThumbnailImageHeight, "px;\" class=\"miniphoto_img\" src=\"", non_pic_small_filename, "\" alt=\"Click to select\" onclick=\"updateMainPhoto('", non_pic_main_filename, "','", EscapeJavascript(iMultimedia.Title), "',", largeFilenameArg, ")\" />"));
                        }
                        f.Writer.WriteLine("         </div>");
                    }

                    f.Writer.WriteLine("      </div>");
                }
                f.Writer.WriteLine("    </div> <!-- photos -->");
            }
        }

        // Writes the HTML for the mini tree diagram, including the image alMap data. 
        private void OutputMiniTree(HTMLFile f)
        {
            System.Drawing.Imaging.ImageFormat imageFormat;
            string miniTreeExtn;
            string imageFormatString = MainForm.Config.MiniTreeImageFormat;
            switch (imageFormatString) {
                case "png":
                    imageFormat = System.Drawing.Imaging.ImageFormat.Png;
                    miniTreeExtn = "png";
                    break;
                default:
                    imageFormat = System.Drawing.Imaging.ImageFormat.Gif;
                    miniTreeExtn = "gif";
                    break;
            }

            TreeDrawer treeDrawer = new TreeDrawer(fTree);
            string relativeTreeFilename = String.Concat("tree", fIndiRec.XRef, ".", miniTreeExtn);
            string fullTreeFilename = String.Concat(MainForm.Config.OutputFolder, "\\", relativeTreeFilename);
            ArrayList map = treeDrawer.CreateMiniTree(fPaintbox, fIndiRec, fullTreeFilename, MainForm.Config.TargetTreeWidth, imageFormat);
            if (map != null) {
                // Add space to height so that IE's horiz scroll bar has room and doesn't create a vertical scroll bar.
                f.Writer.WriteLine(String.Format("    <div id=\"minitree\" style=\"height:{0}px;\">", treeDrawer.Height + 20));
                f.Writer.WriteLine("      <map name=\"treeMap\" id=\"tree\">");
                foreach (MiniTreeMap mapItem in map) {
                    if (mapItem.Linkable) {
                        string href = GetIndividualHTMLFilename(mapItem.IndiRec);
                        f.Writer.WriteLine(String.Concat("        <area alt=\"", mapItem.Name, "\" coords=\"", mapItem.X1, ",", mapItem.Y1, ",", mapItem.X2, ",", mapItem.Y2, "\" href=\"", href, "\" shape=\"rect\" />"));
                    }
                }
                f.Writer.WriteLine("      </map>");
                f.Writer.WriteLine(String.Concat("      <img src=\"", relativeTreeFilename, "\"  usemap=\"#treeMap\" alt=\"Mini tree diagram\"/>"));
                f.Writer.WriteLine("    </div>");
            }
        }

        // If only one occupation for this individual, and it has no associated date, this method 
        // ensures that we show it only in the title, not in the other facts section as well.
        private void RemoveLoneOccupation()
        {
            bool bSanityCheck = false;
            if (MainForm.Config.OccupationHeadline) {
                if (fOccupations.Count == 1) {
                    if (((OccupationCounter)fOccupations[0]).Date == null) {
                        // Remove from attributeList
                        for (int i = 0; i < fAttributeList.Count; i++) {
                            Event iEvent = (Event)fAttributeList[i];
                            if (iEvent.Type == "OCCU") {
                                fAttributeList.RemoveAt(i);
                                bSanityCheck = true;
                                break;
                            }
                        }
                        if (!bSanityCheck) {
                            fLogger.WriteDebug("Expected to find occupation event");
                        }
                    }
                }
            }
        }

        // Extracts the data from the given event, and creates a CIEvent instance for it and adds it to the list of events.
        // Does specific processing if appropriate to the event.
        // linkToOtherParty is an href link to the other party concerned in the event. Typically this is for fr events such as engagement, marriage etc where
        // the other party would be the partner.
        private void ProcessEvent(GEDCOMCustomEvent es, string linkToOtherParty)
        {
            fLogger.WriteInfo(String.Format("ProcessEvent( {0}, {1} )", es.Name, es.StringValue));

            if (es.Name == null) {
                return;
            }
            string utype = es.Name.ToUpper();
            string subtype = es.StringValue;

            // Strip trailing _ that FTM seems sometimes to include
            while (subtype.Length > 0 && subtype.Substring(subtype.Length - 1, 1) == "_") {
                subtype = subtype.Substring(0, subtype.Length - 1);
            }

            // Useful holder vars
            GEDCOMDateValue date;
            string place;
            string escaped_description = "";
            string address = "";
            string url = "";
            string cause = "";

            bool important = false;
            date = null;
            place = "";
            string place_word = MainForm.Config.PlaceWord;
            string alternative_place_word = "and"; // For want of anything better...
            string alternative_place = "";
            if (es.Date != null) {
                date = es.Date;
                if (es.Place != null) {
                    place = es.Place.StringValue;
                }

                if (es.Address != null) {
                    address = es.Address.Address.Text;
                    if (es.Address.WebPages.Count > 0)
                        url = es.Address.WebPages[0].StringValue;
                }
                cause = es.Cause;
            }

            string sourceRefs = "";
            if (es.Name != "MARR" && es.Name != "TITL") // Marriage handled separately later.
            {
                sourceRefs = AddSources(ref fReferenceList, es.SourceCitations);
            }

            bool bNeedValue = false;
            bool bOnlyIncludeIfNotePresent = false;
            bool bIncludeOccupation = false;

            // First occurrence of an event in GEDCOM is the "preferred" one, where in real life there can be only one of the event (e.g. BIRT)
            bool bTypeIsAOneOff = false;

            // Fix for Family Tree Maker 2008 which exports occupation as generic EVEN events.
            // It also puts occupation in PLAC field, but this is already accommodated later.
            if (es.Name == "EVEN" && subtype.ToLower() == "occupation") {
                es.SetName("OCCU"); // FIXME!
            }

            switch (utype) {
                case "BIRT":
                    if (es is GEDCOMCustomEvent) {
                        bTypeIsAOneOff = true;
                        if (fInferredBirthday != null) {
                            // Throw away lesser qualified birthday inferences.
                            if (fInferredBirthday.Qualification > DateQualification.Birth) // ">" here means "further from the truth".
                            {
                                fInferredBirthday = null;
                            }
                        }
                        if (fInferredBirthday == null) // Take first BIRT we come across. In GEDCOM this means it is the preferred event.
                        {
                            fInferredBirthday = new QualifiedDate(date, DateQualification.Birth);
                        }
                        fBirthdaySourceRefs = sourceRefs;
                    }
                    escaped_description = "born";
                    important = true;
                    break;

                case "CHR":
                    if (es is GEDCOMCustomEvent) {
                        if (fInferredBirthday != null) {
                            // Throw away lesser qualified birthday inferences.
                            if (fInferredBirthday.Qualification > DateQualification.Christening) // ">" here means "further from the truth".
                            {
                                fInferredBirthday = null;
                            }
                        }
                        if (fInferredBirthday == null) // In the absence of a BIRT event this will have to do.
                        {
                            fInferredBirthday = new QualifiedDate(date, DateQualification.Christening);
                            fBirthdaySourceRefs = sourceRefs;
                        }
                    }
                    escaped_description = "christened";
                    break;

                case "BAPM":
                    if (es is GEDCOMCustomEvent) {
                        if (fInferredBirthday != null) {
                            // Throw away lesser qualified birthday inferences.
                            if (fInferredBirthday.Qualification > DateQualification.Baptism) // ">" here means "further from the truth".
                            {
                                fInferredBirthday = null;
                            }
                        }
                        if (fInferredBirthday == null) // In the absence of a BIRT event this will have to do.
                        {
                            fInferredBirthday = new QualifiedDate(date, DateQualification.Baptism);
                            fBirthdaySourceRefs = sourceRefs;
                        }
                    }
                    escaped_description = "baptised";
                    break;

                case "DEAT":
                    bTypeIsAOneOff = true;
                    if (es is GEDCOMCustomEvent) {
                        if (fInferredDeathday != null) {
                            // Throw away lesser qualified birthday inferences.
                            if (fInferredDeathday.Qualification > DateQualification.Death) // ">" here means "further from the truth".
                            {
                                fInferredDeathday = null;
                            }
                        }
                        if (fInferredDeathday == null) // Take first DEAT we come across. In GEDCOM this means it is the preferred event.
                        {
                            fInferredDeathday = new QualifiedDate(date, DateQualification.Death);
                        }
                        fDeathdaySourceRefs = sourceRefs;
                    }
                    escaped_description = "died";
                    important = true;
                    break;

                case "BURI":
                    bTypeIsAOneOff = true;
                    if (es is GEDCOMCustomEvent) {
                        if (fInferredDeathday != null) {
                            // Throw away lesser qualified birthday inferences.
                            if (fInferredDeathday.Qualification > DateQualification.Burial) // ">" here means "further from the truth".
                            {
                                fInferredDeathday = null;
                            }
                        }
                        if (fInferredDeathday == null) // In the absence of a DEAT event this will have to do.
                        {
                            fInferredDeathday = new QualifiedDate(date, DateQualification.Burial);
                            fDeathdaySourceRefs = sourceRefs;
                        }
                    }
                    escaped_description = "buried";
                    break;

                case "CREM":
                    bTypeIsAOneOff = true;
                    if (es is GEDCOMCustomEvent) {
                        if (fInferredDeathday != null) {
                            // Throw away lesser qualified birthday inferences.
                            if (fInferredDeathday.Qualification > DateQualification.Cremation) // ">" here means "further from the truth".
                            {
                                fInferredDeathday = null;
                            }
                        }
                        if (fInferredDeathday == null) // In the absence of a DEAT event this will have to do.
                        {
                            fInferredDeathday = new QualifiedDate(date, DateQualification.Cremation);
                            fDeathdaySourceRefs = sourceRefs;
                        }
                    }
                    escaped_description = "cremated";
                    break;

                case "ADOP":
                    escaped_description = "adopted";
                    break;

                case "BARM":
                    escaped_description = "bar mitzvah";
                    break;

                case "BASM":
                    escaped_description = "bat mitzvah";
                    break;

                case "BLES":
                    escaped_description = "blessing";
                    break;

                case "CHRA":
                    escaped_description = "christened (as adult)";
                    break;

                case "CONF":
                    escaped_description = "confirmed";
                    break;

                case "FCOM":
                    escaped_description = "first communion";
                    break;

                case "ORDN":
                    escaped_description = "ordained";
                    break;

                case "NATU":
                    escaped_description = "naturalized";
                    break;

                case "EMIG":
                    escaped_description = "emigrated";
                    place_word = "from";
                    alternative_place_word = "to";
                    break;

                case "IMMI":
                    escaped_description = "immigrated";
                    place_word = "to";
                    alternative_place_word = "from";
                    break;
                /*  handled as fr event below
                        case "CENS":
                          escaped_description = "recorded in census";
                          break;*/

                case "PROB":
                    escaped_description = "probate";
                    break;

                case "WILL":
                    escaped_description = "wrote will";
                    break;

                case "GRAD":
                    escaped_description = "graduated";
                    break;

                case "RETI":
                    escaped_description = "retired";
                    break;

                case "EVEN":
                    if (!string.IsNullOrEmpty(subtype)) {
                        escaped_description = EscapeHTML(subtype, false);
                    } else {
                        escaped_description = "other event";
                    }
                    if (!string.IsNullOrEmpty(es.StringValue)) {
                        escaped_description += ": " + es.StringValue;
                    }
                    break;

                case "CAST":
                    escaped_description = "caste";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "DSCR":
                    escaped_description = "physical description";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "EDUC":
                    escaped_description = "educated";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "IDNO":
                    escaped_description = "ID number";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "NATI":
                    escaped_description = "nationality";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "NCHI":
                    bTypeIsAOneOff = true;
                    escaped_description = "number of children";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "NMR":
                    bTypeIsAOneOff = true;
                    escaped_description = "number of marriages";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;

                    break;

                case "OCCU":
                    escaped_description = "occupation";
                    if (!string.IsNullOrEmpty(es.StringValue)) {
                        OccupationCounter oc = new OccupationCounter(EscapeHTML(es.StringValue, false) + sourceRefs, date);
                        fOccupations.Add(oc);
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                        bIncludeOccupation = true;
                    } else
                        bNeedValue = true;
                    break;

                case "PROP":
                    escaped_description = "property";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "RELI":
                    escaped_description = "religion";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "RESI":
                    escaped_description = "resident";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = false; // Special case, we need the "at" word left in for this.
                    break;

                case "SSN":
                    escaped_description = "Social Security number";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "TITL":
                    /* This is handled as a special case outside of event processing*/
                    place = ""; // Clear place to avoid creating spurious event entry
                    break;

                case "FACT":
                    escaped_description = "other fact";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;


                case "_NMR": // _NMR Brother's Keeper
                    bTypeIsAOneOff = true;
                    escaped_description = "never married";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                case "_AKA": // _AKA Brother's Keeper
                case "_AKAN": // _AKAN Brother's Keeper
                    escaped_description = "also known as";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;

                // Now the fr events:
                case "ANUL":
                    escaped_description = "annulment of marriage";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " to ", linkToOtherParty);
                    }
                    break;

                case "CENS":
                    escaped_description = "recorded in census";
                    break;

                case "DIV":
                    if (es.StringValue != null && (es.StringValue == "N" || es.StringValue == "n")) {
                        place = ""; // Clear place to prevent this event being shown
                    } else {
                        escaped_description = "divorced";
                        if (!string.IsNullOrEmpty(linkToOtherParty)) {
                            escaped_description = String.Concat(escaped_description, " from ", linkToOtherParty);
                        }
                    }
                    break;

                case "DIVF":
                    escaped_description = "filing of divorce";
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " from ", linkToOtherParty);
                    }
                    break;

                case "ENGA":
                    escaped_description = "engagement";
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " to ", linkToOtherParty);
                    }
                    break;

                case "MARB":
                    escaped_description = "publication of banns of marriage";
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " to ", linkToOtherParty);
                    }
                    break;

                case "MARC":
                    escaped_description = "contract of marriage";
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " to ", linkToOtherParty);
                    }
                    break;

                case "MARR":
                    /* This is handled as a special case outside of event processing*/
                    place = ""; // Clear place to avoid creating spurious event entry
                    break;

                case "MARL":
                    escaped_description = "licence obtained for marriage";
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " to ", linkToOtherParty);
                    }

                    break;

                case "MARS":
                    escaped_description = "settlement of marriage";
                    if (!string.IsNullOrEmpty(linkToOtherParty)) {
                        escaped_description = String.Concat(escaped_description, " to ", linkToOtherParty);
                    }
                    break;

                default:
                    escaped_description = "unknown event";
                    if (!string.IsNullOrEmpty(es.StringValue))
                        escaped_description = String.Concat(escaped_description, " ", EscapeHTML(es.StringValue, false));
                    else
                        bNeedValue = true;
                    break;
            }

            if (MainForm.Config.CapitaliseEventDescriptions) {
                Capitalise(ref escaped_description);
            }

            if (place != "") {
                // It seems some earlier GEDCOM has PLAC value filled with the event value, and the event value blank. Accomodate this:
                if ((string.IsNullOrEmpty(es.StringValue)) && bNeedValue) {
                    escaped_description += " " + EscapeHTML(place, false);
                    if (utype == "OCCU") {
                        OccupationCounter oc = new OccupationCounter(place, date);
                        fOccupations.Add(oc);
                    }
                    place = "";
                    bIncludeOccupation = true; // Needed to include occupation event, (without date or place), in page.
                } else {
                    escaped_description += String.Concat(" ", place_word, " ", EscapeHTML(place, false));
                    if (!string.IsNullOrEmpty(alternative_place)) {
                        escaped_description += String.Concat(" ", alternative_place_word, " ", EscapeHTML(alternative_place, false));
                    }
                }
            }

            if (address != "") {
                if (escaped_description.Length > 0) {
                    escaped_description += " (" + EscapeHTML(address, false) + ")";
                } else {
                    escaped_description = EscapeHTML(address, false);
                }
            }

            if (url != "") {
                if (escaped_description.Length > 0) {
                    escaped_description += " (<a href=\"" + (url) + "\">" + (url) + "</a>)";
                } else {
                    escaped_description = "<a href=\"" + (url) + "\">" + (url) + "</a>";
                }
            }

            string overview = "";
            if (!string.IsNullOrEmpty(es.Classification)) {
                overview = es.Classification;
            }

            if (escaped_description == "") {
                return; // In the case of MARR and TITL and DIV N we don't want to add anything here.
            }

            escaped_description += ".";
            escaped_description += sourceRefs;

            string eventNote = "";

            if (cause != "") {
                cause = EscapeHTML(cause, false);
                if (MainForm.Config.CapitaliseEventDescriptions) {
                    Capitalise(ref cause);
                }
                if (eventNote.Length > 0) {
                    eventNote += "\n";
                }
                if (MainForm.Config.ObfuscateEmails) {
                    eventNote += ObfuscateEmail(cause);
                } else {
                    eventNote += cause;
                }
            }

            foreach (GEDCOMNotes ns in es.Notes) {
                if (eventNote != "") {
                    eventNote += "\n";
                }
                if (MainForm.Config.ObfuscateEmails) {
                    eventNote += ObfuscateEmail(ns.Notes.Text);
                } else {
                    eventNote += ns.Notes.Text;
                }
            }

            Event iEvent = null;

            if (!bOnlyIncludeIfNotePresent || eventNote != "") {
                if (date != null) {
                    iEvent = new Event(date, utype, escaped_description, overview, eventNote, important, MainForm.Config.CapitaliseEventDescriptions);
                    fEventList.Add(iEvent);
                }
                // else its an attribute.
                else {
                    // Don't include plain "Died" and nothing else. Roots Magic seems to use this just to signify that person died. But it appears on every single page and looks silly.
                    // GSP Family Tree puts lots of blank tags (OCCU, CHR, SEX, NOTE, etc.etc). Don't display those without meaning
                    // Note CHR is contentious, as other s/w may use a CHR with no other info to mean that they were christened. GSP it appears puts a CHR for everyone?
                    if ((utype != "DEAT" && utype != "BIRT" && utype != "CHR" && utype != "OCCU") || place != "" || eventNote != "" || bIncludeOccupation) {
                        iEvent = new Event(null, utype, escaped_description, overview, eventNote, important, MainForm.Config.CapitaliseEventDescriptions);
                        fAttributeList.Add(iEvent);
                    }
                }
            }

            if (iEvent != null && bTypeIsAOneOff) {
                if (fFirstFoundEvent.ContainsKey(utype)) {
                    // We have multiple occurences of this event. Mark the one we saw first as 'preferred'.
                    Event firstEvent = (Event)fFirstFoundEvent[utype];
                    if (firstEvent != null) {
                        firstEvent.Preference = EventPreference.First;
                        iEvent.Preference = EventPreference.Subsequent;
                    }
                } else {
                    fFirstFoundEvent[utype] = iEvent;
                }
            }
        }

        // Adds the given source citations to the given list of referenced sources, and returns an HTML link string.
        private static string AddSources(ref List<GEDCOMSourceCitation> referenceList, GEDCOMList<GEDCOMSourceCitation> sourceCitations)
        {
            string sourceRefs = "";
            foreach (GEDCOMSourceCitation sc in sourceCitations) {
                int sourceNumber = -1;

                // Is source already in list?
                for (int i = 0; i < referenceList.Count; ++i) {
                    if (referenceList[i].Value == sc.Value) {
                        sourceNumber = i;
                        break;
                    }
                }

                bool bComma = false;
                if (sourceRefs != "") {
                    bComma = true;
                }

                if (sourceNumber == -1) {
                    sourceNumber = referenceList.Count;
                    referenceList.Add(sc);
                }

                sourceRefs += sc.MakeLinkNumber((uint)(sourceNumber + 1), bComma);
            }
            return sourceRefs;
        }

        // Picks the individual's occupation closest to the given date, within the given limits.
        private static string BestOccupation(ArrayList occupations, GEDCOMDateValue givenDate, GEDCOMDateValue lowerLimit, GEDCOMDateValue upperLimit)
        {
            int minDifference;
            if (lowerLimit == null || upperLimit == null) {
                minDifference = Int32.MaxValue;
            } else {
                minDifference = Math.Abs(Extensions.GetEventsYearsDiff(lowerLimit, upperLimit));
            }

            OccupationCounter bestOc = null;

            foreach (OccupationCounter oc in occupations) {
                if (oc.Date == null) {
                    // Dateless occupation assumed to be the generic answer
                    return oc.Name;
                } else {
                    int sdifference = Extensions.GetEventsYearsDiff(givenDate, oc.Date);
                    int difference = Math.Abs(sdifference);
                    if (Math.Sign(sdifference) == -1) {
                        // favours occupations before date rather than after it.
                        difference *= 3;
                        difference /= 2;
                    }
                    if (Math.Abs(difference) < minDifference) {
                        minDifference = difference;
                        bestOc = oc;
                    }
                }
            }

            if (bestOc == null)
                return "";

            return bestOc.Name;
        }

        // Creates a string describing the marital status of the given fr. Prepends the string provided in marriageNote.    
        private static string BuildMaritalStatusNote(GEDCOMFamilyRecord fr, string marriageNote)
        {
            if (marriageNote != "") {
                marriageNote += "\n";
            }

            // Nasty hack for Family Historian using strings to denote marital status
            if (fr.Status == GKMarriageStatus.Unknown) {
                marriageNote += "Marital status unknown";
            } else {
                marriageNote += fr.Status.ToString();
            }

            return marriageNote;
        }
    }
}
