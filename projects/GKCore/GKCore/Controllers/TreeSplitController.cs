﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2018 by Sergey V. Zhdanovskih.
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using GKCommon.GEDCOM;
using GKCore.Options;
using GKCore.Tools;
using GKCore.Types;
using GKCore.UIContracts;

namespace GKCore.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class TreeSplitController : DialogController<ITreeSplitDlg>
    {
        private readonly List<GEDCOMRecord> fSplitList;

        public TreeSplitController(ITreeSplitDlg view) : base(view)
        {
            fSplitList = new List<GEDCOMRecord>();
        }

        public override void UpdateView()
        {
        }

        public void UpdateSplitLists()
        {
            fView.SelectedList.BeginUpdate();
            fView.SelectedList.ClearItems();
            fView.SkippedList.BeginUpdate();
            fView.SkippedList.ClearItems();
            try {
                var tree = fBase.Context.Tree;
                int cnt = 0;
                int num = tree.RecordsCount;
                for (int i = 0; i < num; i++) {
                    GEDCOMRecord rec = tree[i];
                    if (rec is GEDCOMIndividualRecord) {
                        cnt++;
                        GEDCOMIndividualRecord iRec = rec as GEDCOMIndividualRecord;
                        string st = iRec.XRef + " / " + GKUtils.GetNameString(iRec, true, false);

                        if (fSplitList.IndexOf(iRec) < 0) {
                            fView.SkippedList.AddItem(null, st);
                        } else {
                            fView.SelectedList.AddItem(null, st);
                        }
                    }
                }
                fView.Caption = fSplitList.Count.ToString() + @" / " + cnt.ToString();
            } finally {
                fView.SelectedList.EndUpdate();
                fView.SkippedList.EndUpdate();
            }
        }

        public void Select(GEDCOMIndividualRecord startPerson, TreeTools.TreeWalkMode walkMode)
        {
            fSplitList.Clear();

            if (startPerson == null) {
                AppHost.StdDialogs.ShowError(LangMan.LS(LSID.LSID_NotSelectedPerson));
            } else {
                TreeTools.WalkTree(startPerson, walkMode, fSplitList);
            }

            UpdateSplitLists();
        }

        public void Delete()
        {
            int num = fSplitList.Count;
            if (num == 0) return;

            for (int i = 0; i < num; i++) {
                object obj = fSplitList[i];

                if (obj is GEDCOMIndividualRecord) {
                    BaseController.DeleteRecord(fBase, obj as GEDCOMIndividualRecord, false);
                }
            }

            fSplitList.Clear();
            UpdateSplitLists();
            fBase.RefreshLists(false);

            AppHost.StdDialogs.ShowMessage(LangMan.LS(LSID.LSID_RecsDeleted));
        }

        public void Save()
        {
            string fileName = AppHost.StdDialogs.GetSaveFile("", "", LangMan.LS(LSID.LSID_GEDCOMFilter), 1, GKData.GEDCOM_EXT, "");
            if (string.IsNullOrEmpty(fileName)) return;

            TreeTools.CheckRelations(fSplitList);

            var tree = fBase.Context.Tree;
            GKUtils.PrepareHeader(tree, fileName, GlobalOptions.Instance.DefCharacterSet, true);

            using (StreamWriter fs = new StreamWriter(fileName, false, GEDCOMUtils.GetEncodingByCharacterSet(tree.Header.CharacterSet))) {
                var gedcomProvider = new GEDCOMProvider(tree);
                gedcomProvider.SaveToStream(fs, fSplitList);

                tree.Header.CharacterSet = GEDCOMCharacterSet.csASCII;
            }
        }
    }
}
