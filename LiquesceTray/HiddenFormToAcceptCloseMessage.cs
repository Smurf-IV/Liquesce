#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="HiddenFormToAcceptCloseMessage.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
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
using System.Windows.Forms;

namespace LiquesceTray
{
   public partial class HiddenFormToAcceptCloseMessage : Form
   {
      public HiddenFormToAcceptCloseMessage()
      {
         InitializeComponent();
      }

      private void HiddenFormToAcceptCloseMessage_Load(object sender, EventArgs e)
      {
         Visible = false;
      }

      private void HiddenFormToAcceptCloseMessage_FormClosing(object sender, FormClosingEventArgs e)
      {
         Application.Exit();
      }
   }
}
