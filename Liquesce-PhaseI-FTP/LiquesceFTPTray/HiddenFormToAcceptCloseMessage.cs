﻿using System;
using System.Windows.Forms;

namespace LiquesceFTPTray
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