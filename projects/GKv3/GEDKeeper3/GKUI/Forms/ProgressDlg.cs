﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2017 by Sergey V. Zhdanovskih.
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
using System.ComponentModel;
using System.Threading;
using Eto.Forms;

using GKCommon;
using GKCore;
using GKCore.Interfaces;

namespace GKUI.Forms
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class ProgressDlg : Form
    {
        private DateTime fStartTime;
        private bool fRequiresClose;
        private int fVal;

        public ProgressDlg()
        {
            InitializeComponent();

            Title = LangMan.LS(LSID.LSID_Progress);
            lblTimePassed.Text = LangMan.LS(LSID.LSID_TimePassed);
            lblTimeRemain.Text = LangMan.LS(LSID.LSID_TimeRemain);
            lblTimeTotal.Text = LangMan.LS(LSID.LSID_TimeTotal);
        }

        #region Private methods

        private void DoInit(string title, int max)
        {
            lblTitle.Text = title;
            ProgressBar1.MaxValue = max;
            ProgressBar1.MinValue = 0;
            ProgressBar1.Value = 0;
            fStartTime = DateTime.Now;
            fVal = 0;
        }

        private void DoDone()
        {
            Close();
        }

        private void DoStep(int value)
        {
            if (fVal == value) return;

            fVal = value;
            ProgressBar1.Value = fVal;

            double max = ProgressBar1.MaxValue;
            double pos = fVal;
            if (pos == 0.0d) pos = 1;

            TimeSpan passTime = DateTime.Now - fStartTime;
            TimeSpan restTime = new TimeSpan((long)Math.Truncate((passTime.Ticks / pos) * (max - pos)));
            TimeSpan sumTime = passTime + restTime;

            lblPassedVal.Text = TimeSpanToString(passTime);
            lblRemainVal.Text = TimeSpanToString(restTime);
            lblTotalVal.Text = TimeSpanToString(sumTime);

            Invalidate();
        }

        private static string TimeSpanToString(TimeSpan ts)
        {
            return string.Format(null, "{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
        }

        #endregion

        #region Protected methods

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            fRequiresClose = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            fRequiresClose = false;
            base.OnClosing(e);
        }

        internal void ProgressInit(string title, int max)
        {
            try {
                //DoInit(title, max);
                Application.Instance.Invoke(delegate { DoInit(title, max); });
            } catch (Exception ex) {
                Logger.LogWrite("ProgressDlg.ProgressInit(): " + ex.Message);
            }
        }

        internal void ProgressDone()
        {
            try {
                if (fRequiresClose) {
                    //DoDone();
                    Application.Instance.Invoke(delegate { DoDone(); });
                }
            } catch (Exception ex) {
                Logger.LogWrite("ProgressDlg.ProgressDone(): " + ex.Message);
            }
        }

        internal void ProgressStep()
        {
            try {
                //DoStep(fVal + 1);
                Application.Instance.Invoke(delegate { DoStep(fVal + 1); });
            } catch { }
        }

        internal void ProgressStep(int value)
        {
            try {
                //DoStep(value);
                Application.Instance.Invoke(delegate { DoStep(value); });
            } catch { }
        }

        #endregion
    }

    public sealed class ProgressController : IProgressController
    {
        private ProgressProxy fProxy;
        private int fVal;

        public void ProgressInit(string title, int max)
        {
            if (fProxy != null) {
                fProxy.ProgressReset(title, max);
            } else {
                fProxy = new ProgressProxy(title, max);
            }

            fVal = 0;
        }

        public void ProgressDone()
        {
            if (fProxy != null) {
                fProxy.Close();
                fProxy = null;
            }
        }

        public void ProgressStep()
        {
            if (fProxy != null) {
                fProxy.UpdateProgress(fVal++);
            }
        }

        public void ProgressStep(int value)
        {
            if (fProxy != null) {
                fProxy.UpdateProgress(value);
            }
        }
    }

    internal sealed class ProgressProxy
    {
        private ProgressDlg fProgressForm;
        private readonly string fTitle;
        private readonly int fMax;
        //private ManualResetEvent fMRE = new ManualResetEvent(false);
        private bool fFormLoaded;
        //private readonly Thread fThread;
        private readonly IntPtr fParentHandle;

        public ProgressProxy(string title, int max)
        {
            fFormLoaded = false;
            fTitle = title;
            fMax = max;
            fParentHandle = AppHost.Instance.GetTopWindowHandle();

            ShowProgressForm();
            //fThread = new Thread(ShowProgressForm);
            //fThread.SetApartmentState(ApartmentState.STA);
            //fThread.Start();

            while (!fFormLoaded)
            {
                Thread.Sleep(100);
            }
            //fMRE.WaitOne();
        }

        private void ShowProgressForm()
        {
            fProgressForm = new ProgressDlg();
            fProgressForm.ProgressInit(fTitle, fMax);
            fProgressForm.Load += ProgressForm_Load;

            /*if (fParentHandle != IntPtr.Zero) {
                UIHelper.CenterFormByParent(fProgressForm, fParentHandle);
            }*/

            fProgressForm.Show();
            //fProgressForm.Close();
        }

        private void ProgressForm_Load(object sender, EventArgs e)
        {
            //fMRE.Set();
            fFormLoaded = true;
        }

        public void ProgressReset(string title, int max)
        {
            fProgressForm.ProgressInit(title, max);
        }

        public void UpdateProgress(int percent)
        {
            fProgressForm.ProgressStep(percent);
        }

        public void Close()
        {
            fProgressForm.ProgressDone();
        }
    }
}