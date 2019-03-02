﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2019 by Sergey V. Zhdanovskih.
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

namespace GKCommon.GEDCOM
{
    public enum GEDCOMToken
    {
        Unknown,
        Whitespace,
        Symbol,
        Word,
        Number,
        EOL
    }

    /// <summary>
    /// GEDCOMParser tokenized string into tokens.
    /// </summary>
    public sealed class GEDCOMParser
    {
        private const char EOL = (char)0;

        private GEDCOMToken fCurrentToken;
        private char[] fData;
        private bool fIgnoreWhitespace;
        private int fLength;
        private int fPos;
        private int fSavePos;

        private int fIntValue;
        private string fStrValue;
        private bool fValueReset;


        public GEDCOMToken CurrentToken
        {
            get { return fCurrentToken; }
        }

        public int Position
        {
            get { return fPos; }
        }


        public GEDCOMParser(string data, bool ignoreWhitespace)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            fData = data.ToCharArray();
            fLength = data.Length;

            fIgnoreWhitespace = ignoreWhitespace;
            fCurrentToken = GEDCOMToken.Unknown;
            fPos = 0;
            fValueReset = false;
        }

        public GEDCOMToken Next()
        {
            while (true) {
                char ch = (fPos >= fLength) ? EOL : fData[fPos];
                char ltr = (char)(ch | ' ');

                if ((ltr >= 'a' && ltr <= 'z') || ch == '_') {
                    fSavePos = fPos;
                    fPos++;
                    while (true) {
                        ch = (fPos >= fLength) ? EOL : fData[fPos];
                        ltr = (char)(ch | ' ');
                        if ((ltr >= 'a' && ltr <= 'z') || (ch >= '0' && ch <= '9') || ch == '_') {
                            fPos++;
                        } else
                            break;
                    }

                    fValueReset = true;
                    fCurrentToken = GEDCOMToken.Word;
                    return fCurrentToken;
                }

                if (ch >= '0' && ch <= '9') {
                    fSavePos = fPos;
                    fPos++;
                    fIntValue = ((int)ch - 48);
                    while (true) {
                        ch = (fPos >= fLength) ? EOL : fData[fPos];
                        if (ch >= '0' && ch <= '9') {
                            fPos++;
                            fIntValue = (fIntValue * 10 + ((int)ch - 48));
                        } else
                            break;
                    }

                    fValueReset = true;
                    fCurrentToken = GEDCOMToken.Number;
                    return fCurrentToken;
                }

                if (ch == ' ' || ch == '\t') {
                    if (fIgnoreWhitespace) {
                        fPos++;
                        continue;
                    }

                    fSavePos = fPos;
                    fPos++;
                    while (true) {
                        ch = (fPos >= fLength) ? EOL : fData[fPos];
                        if (ch == ' ' || ch == '\t')
                            fPos++;
                        else
                            break;
                    }

                    fValueReset = true;
                    fCurrentToken = GEDCOMToken.Whitespace;
                    return fCurrentToken;
                }

                if (ch == EOL) {
                    fValueReset = true;
                    fCurrentToken = GEDCOMToken.EOL;
                    return fCurrentToken;
                } else {
                    fSavePos = fPos;
                    fPos++;

                    fValueReset = true;
                    fCurrentToken = GEDCOMToken.Symbol;
                    return fCurrentToken;
                }
            }
        }

        public void SkipWhitespaces()
        {
            if (fCurrentToken == GEDCOMToken.Unknown) {
                Next();
            }

            while (fCurrentToken == GEDCOMToken.Whitespace) {
                Next();
            }
        }

        public string GetWord()
        {
            if (fValueReset) {
                fStrValue = new string(fData, fSavePos, fPos - fSavePos);
                fValueReset = false;
            }
            return fStrValue;
        }

        public int GetNumber()
        {
            return fIntValue;
        }

        public char GetSymbol()
        {
            return fData[fSavePos];
        }

        public string GetRest()
        {
            return (fPos >= fLength) ? string.Empty : new string(fData, fPos, fLength - fPos);
        }

        public bool RequireToken(GEDCOMToken tokenKind)
        {
            return (fCurrentToken == tokenKind);
        }

        public void RequestSymbol(char symbol)
        {
            if (fCurrentToken != GEDCOMToken.Symbol || GetSymbol() != symbol) {
                throw new Exception("Required symbol not found");
            }
        }

        public void RequestNextSymbol(char symbol)
        {
            var token = Next();
            if (token != GEDCOMToken.Symbol || GetSymbol() != symbol) {
                throw new Exception("Required symbol not found");
            }
        }

        public int RequestInt()
        {
            if (fCurrentToken != GEDCOMToken.Number) {
                throw new Exception("Required integer not found");
            }
            return GetNumber();
        }

        public int RequestNextInt()
        {
            var token = Next();
            if (token != GEDCOMToken.Number) {
                throw new Exception("Required integer not found");
            }
            return GetNumber();
        }
    }
}