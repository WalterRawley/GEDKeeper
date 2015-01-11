using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// 
/// </summary>

namespace ExtUtils
{
	public static class SysUtils
	{
		public static void Free(object self)
		{
			if (self != null && self is IDisposable)
			{
				(self as IDisposable).Dispose();
			}
		}

		public static long Trunc(double value)
		{
			return (long)Math.Truncate(value);
		}

		public static int ParseInt(string S, int Default)
		{
			int res;
			if (!int.TryParse(S, out res)) res = Default;
			return res;
		}

		public static double ParseFloat(string S, double Default)
		{
			if (string.IsNullOrEmpty(S)) return Default;

			NumberFormatInfo LFormat = Thread.CurrentThread.CurrentCulture.NumberFormat.Clone() as NumberFormatInfo;
			LFormat.NumberDecimalSeparator = ".";
			LFormat.NumberGroupSeparator = " ";

			double value;
			double result;
			if (double.TryParse(S, NumberStyles.Float, LFormat, out value)) {
				result = value;
			} else {
				result = Default;
			}
			return result;
		}

		public static string TrimLeft(string S)
		{
			int L = (S != null) ? S.Length : 0;
			int I = 1;
			while (I <= L && S[I - 1] <= ' ') I++;

			string result;
			if (I > L) {
				result = "";
			} else {
				result = ((I != 1) ? S.Substring(I - 1) : S);
			}
			return result;
		}

		public static string TrimRight(string S)
		{
			int L = (S != null) ? S.Length : 0;
			int I = L;
			while (I > 0 && S[I - 1] <= ' ') I--;

			string result = ((I != L) ? S.Substring(0, I) : S);
			return result;
		}

		private static string LogFilename;

		public static void LogInit(string aFileName)
		{
			LogFilename = aFileName;
		}

		public static void LogWrite(string msg)
		{
			using (StreamWriter Log = new StreamWriter(LogFilename, true, Encoding.GetEncoding(1251)))
			{
				Log.WriteLine("[" + DateTime.Now.ToString() + "] -> " + msg);
				Log.Flush();
				Log.Close();
			}
		}

		public static void LogSend()
		{
			if (File.Exists(LogFilename)) {
				MapiMailMessage message = new MapiMailMessage("GEDKeeper: error notification", "This automatic notification of error.");
				message.Recipients.Add("serg.zhdanovskih@gmail.com");
				message.Files.Add(LogFilename);
				message.ShowDialog();
			}
		}

		public static void LogView()
		{
			if (File.Exists(LogFilename)) {
                Win32Native.ShellExecute(IntPtr.Zero, "open", LogFilename, "", "", Win32Native.ShowCommands.SW_SHOW);
			}
		}


        public static string TrimChars(string s, params char[] trimChars)
        {
            if (string.IsNullOrEmpty(s)) {
                return string.Empty;
            }

            return s.TrimStart(trimChars);
        }


		private static readonly ScrollEventType[] fScrollEvents;

		static SysUtils()
		{
			fScrollEvents = new ScrollEventType[]
			{
				ScrollEventType.SmallDecrement, 
				ScrollEventType.SmallIncrement, 
				ScrollEventType.LargeDecrement, 
				ScrollEventType.LargeIncrement, 
				ScrollEventType.ThumbPosition, 
				ScrollEventType.ThumbTrack, 
				ScrollEventType.First, 
				ScrollEventType.Last, 
				ScrollEventType.EndScroll
			};
		}

		public static ScrollEventType GetScrollEventType(uint wParam)
		{
			ScrollEventType result = ((wParam <= 8u) ? fScrollEvents[(int)wParam] : ScrollEventType.EndScroll);
			return result;
		}

        public static ushort GetKeyLayout()
        {
            return unchecked((ushort)Win32Native.GetKeyboardLayout(0u));
        }

        public static void SetKeyLayout(ushort aLayout)
        {
            Win32Native.ActivateKeyboardLayout((uint)aLayout, 0u);
        }

        public static void LoadExtFile(string fileName)
        {
            Win32Native.ShellExecute(IntPtr.Zero, "open", fileName, "", "", Win32Native.ShowCommands.SW_SHOW);
        }

        public static bool IsConnectedToInternet()  
        {  
            int iDesc;
            return Win32Native.InternetGetConnectedState(out iDesc, 0);
        }

        public static int DoScroll(IntPtr handle, uint wParam, int nBar, int aOldPos, int aMin, int aMax, int sm_piece, int big_piece)
		{
			ScrollEventType scrType = SysUtils.GetScrollEventType(wParam & 65535u);
			
			int newPos = aOldPos;

			switch (scrType) {
				case ScrollEventType.SmallDecrement:
				{
					newPos -= sm_piece;
					break;
				}

				case ScrollEventType.SmallIncrement:
				{
					newPos += sm_piece;
					break;
				}

				case ScrollEventType.LargeDecrement:
				{
					newPos -= big_piece;
					break;
				}

				case ScrollEventType.LargeIncrement:
				{
					newPos += big_piece;
					break;
				}

				case ScrollEventType.ThumbPosition:
				case ScrollEventType.ThumbTrack:
				{
					Win32Native.TScrollInfo ScrollInfo = new Win32Native.TScrollInfo();
					ScrollInfo.cbSize = (uint)Marshal.SizeOf( ScrollInfo );
					ScrollInfo.fMask = 23u;
                    Win32Native.GetScrollInfo(handle, nBar, ref ScrollInfo);
					newPos = ScrollInfo.nTrackPos;
					break;
				}

				case ScrollEventType.First:
				{
					newPos = 0;
					break;
				}

				case ScrollEventType.Last:
				{
					newPos = aMax;
					break;
				}
			}

			if (newPos < aMin)
			{
				newPos = aMin;
			}
			if (newPos > aMax)
			{
				newPos = aMax;
			}

			return newPos;
		}

		public static int DaysBetween(DateTime ANow, DateTime AThen)
		{
			TimeSpan span = ((ANow < AThen) ? AThen - ANow : ANow - AThen);
			return span.Days;
		}

		private static readonly ushort[][] MonthDays = new ushort[][]
		{
			new ushort[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }, 
			new ushort[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }
		};

		public static ushort DaysInAMonth(ushort AYear, ushort AMonth)
		{
			return MonthDays[(AMonth == 2 && DateTime.IsLeapYear((int)AYear)) ? 1 : 0][(int)AMonth - 1];
		}

		public static string NumUpdate(int val, int up)
		{
			string result = val.ToString();
			if (result.Length < up)
			{
				StringBuilder sb = new StringBuilder(result);
				while (sb.Length < up) sb.Insert(0, '0');
				result = sb.ToString();
			}
			return result;
		}

		public static ExtRect GetFormRect(Form aForm)
		{
            if (aForm == null) return ExtRect.Empty();

		    int x = aForm.Left;
		    int y = aForm.Top;
		    int w = aForm.Width;
		    int h = aForm.Height;
		    Screen scr = Screen.PrimaryScreen;
		    int mw = scr.WorkingArea.Width;
		    int mh = scr.WorkingArea.Height;
		    if (x < 0) x = 0;
		    if (y < 0) y = 0;
		    if (w > mw) w = mw;
		    if (h > mh) h = mh;
		    return ExtRect.Create(x, y, x + w - 1, y + h - 1);
		}

        public static void SetFormRect(Form aForm, ExtRect rt, FormWindowState winState)
        {
            // check for new and empty struct
            if (aForm != null && !rt.IsEmpty())
            {
                aForm.Left = rt.Left;
                aForm.Top = rt.Top;
                aForm.Width = rt.GetWidth();
                aForm.Height = rt.GetHeight();

                aForm.WindowState = winState;
            }
        }

	}
}
