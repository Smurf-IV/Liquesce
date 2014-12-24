#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Welcome.cs" company="Smurf-IV">
// 
//  Copyright (C) 2013 Simon Coghlan (Aka Smurf-IV)
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
using System.Reflection;
using System.Windows.Forms;

namespace Liquesce.Tabs
{
   public partial class Welcome : UserControl
   {
      public Welcome()
      {
         InitializeComponent();
         DoubleBuffered = true;

         // Converted from the Welcome.rtf text via notepad :-)
         richTextBox1.Rtf = @"{\rtf1\ansi\ansicpg1252\deff0\deflang2057{\fonttbl{\f0\fnil\fcharset0 Tahoma;}{\f1\fnil\fcharset2 Symbol;}}
{\colortbl ;\red0\green0\blue255;}
{\*\generator Msftedit 5.41.21.2510;}\viewkind4\uc1\pard\ul\b\f0\fs24 Welcome.\ulnone\b0\fs18\par
\fs20 This is the management application for the \i Liquesce Phase \fs22 ][\fs20  Suite\i0 . It allows you to take your hard disks (internal or external) and make them appear as single large drive.\par
\par
\ul\b Performance Tip:\ulnone\b0\par
Please ensure that for \i any \i0 mounted drive letter \i or \i0 folder that is created via Liquesce, is \i not \i0 protected by an Anti-Virus apllication.\par
This is because 'It' (the AV) will see all file actions (opens, closes, queries, etc.) on both the source \b AND \b0 the new mount point, and will seriously impact your performance.\par
\b\par
\ul Debug Tips:\b0\par
\pard{\pntext\f1\'B7\tab}{\*\pn\pnlvlblt\pnf1\pnindent0{\pntxtb\'B7}}\fi-360\li720\ulnone Stop the service\par
{\pntext\f1\'B7\tab}Delete the Svc logs\par
{\pntext\f1\'B7\tab}Adjust your logging to Trace,\par
{\pntext\f1\'B7\tab}Drop the threads down to 1, \par
{\pntext\f1\'B7\tab}Start the service\par
{\pntext\f1\'B7\tab}Perform the actions that cause a problem. e.g.\par
\pard{\pntext\f0 i.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pnlcrm{\pntxta.}}
\fi-360\li1080 Use wordpad to open a file on the mounted drive (Make new if you can)\par
{\pntext\f0 ii.\tab}Make some edits\par
\pard{\pntext\f1\'B7\tab}{\*\pn\pnlvlblt\pnf1\pnindent0{\pntxtb\'B7}}\fi-360\li720 Make a note of the time (UTC/GMT).\par
{\pntext\f1\'B7\tab}Stop the service and zip them up \fs18\par
\pard{\pntext\f0 i.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pnlcrm{\pntxta.}}
\fi-360\li1080\fs20 Use the 'Zip Logs' button in the Logging tab,\fs18\par
\fs20{\pntext\f0 ii.\tab}Create a new issue here: {\field{\*\fldinst{HYPERLINK 'https://liquesce.codeplex.com/workitem/list/basic'}}{\fldrslt{\ul\cf1 https://liquesce.codeplex.com/workitem/list/basic}}}\f0\fs20 . \lang9\fs22\par
\lang2057\fs20{\pntext\f0 iii.\tab}With the notes of how and when, fill in the details and attach the log(s)\lang9\fs22\par
\pard{\pntext\f1\'B7\tab}{\*\pn\pnlvlblt\pnf1\pnindent0{\pntxtb\'B7}}\fi-360\li360\lang2057\fs20 If the actions can be done via clicking things, then use PSR (Problem Steps Recorder) {\field{\*\fldinst{HYPERLINK 'http://www.msigeek.com/2410/problem-step-recorder-an-amazing-screen-capture-tool'}}{\fldrslt{\ul\cf1 http://www.msigeek.com/2410/problem-step-recorder-an-amazing-screen-capture-tool}}}\lang9\f0\fs22\par
}";
      }

      private void Welcome_Load(object sender, EventArgs e)
      {
         tsVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      }
   }
}
