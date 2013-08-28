#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="FileManager.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 fpDragon
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using NLog;

namespace LiquesceSvc
{
    class FileManager
    {

        private const char DOS_STAR = '<';
        private const char DOS_QM = '>';
        private const char DOS_DOT = '"';

        static private readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static string GetLocationFromFilePath(string path)
        {
            return path.Substring(0, path.LastIndexOf("\\"));
        }

        // check whether Name matches Expression
        // Expression can contain "?"(any one character) and "*" (any string)
        // when IgnoreCase is TRUE, do case insenstive matching
        public bool IsNameInExpression(string Expression, string Name, bool IgnoreCase)
        {
            int ei = 0;
            int ni = 0;

            while (ei < Expression.Length)
            {

                if (Expression[ei] == '*')
                {
                    ei++;
                    if (ei >= Expression.Length)
                        return true;

                    while (ni < Name.Length)
                    {
                        if (IsNameInExpression(Expression.Substring(ei), Name.Substring(ni), IgnoreCase))
                            return true;
                        ni++;
                    }

                }
                else if (Expression[ei] == DOS_STAR)
                {

                    int p = ni + 1;
                    int lastDot = 0;
                    ei++;

                    while (p < Name.Length)
                    {
                        if (Name[p] == '.')
                            lastDot = p;
                        p++;
                    }


                    while (true)
                    {
                        if (ni >= Name.Length || ni == lastDot)
                            break;

                        if (IsNameInExpression(Expression.Substring(ei), Name.Substring(ni), IgnoreCase))
                            return true;
                        ni++;
                    }

                }
                else if (Expression[ei] == DOS_QM)
                {

                    ei++;
                    if (Name[ni] != '.')
                    {
                        ni++;
                    }
                    else
                    {

                        int p = ni + 1;
                        while (p < Name.Length)
                        {
                            if (Name[p] == '.')
                                break;
                            p++;
                        }

                        if (Name[p] == '.')
                            ni++;
                    }

                }
                else if (Expression[ei] == DOS_DOT)
                {
                    ei++;

                    if (Name[ni] == '.')
                        ni++;

                }
                else
                {
                    if (Expression[ei] == '?')
                    {
                        ei++; ni++;
                    }
                    else if (IgnoreCase && CompareChars(Expression[ei], Name[ni], true))
                    {
                        ei++; ni++;
                    }
                    else if (!IgnoreCase && Expression[ei] == Name[ni])
                    {
                        ei++; ni++;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (ei == Expression.Length && ni == Name.Length)
                return true;

            return false;
        }


        private bool CompareChars(char char1, char char2, bool caseinsensitive)
        {
            if (caseinsensitive == false)
                return (char1 == char2);
            else
            {
                return char1.ToString().Equals(char2.ToString(), StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}
