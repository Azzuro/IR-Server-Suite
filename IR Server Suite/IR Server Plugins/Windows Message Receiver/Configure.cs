#region Copyright (C) 2005-2009 Team MediaPortal

// Copyright (C) 2005-2009 Team MediaPortal
// http://www.team-mediaportal.com
// 
// This Program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2, or (at your option)
// any later version.
// 
// This Program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GNU Make; see the file COPYING.  If not, write to
// the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
// http://www.gnu.org/copyleft/gpl.html

#endregion

using System;
using System.Windows.Forms;

namespace IRServer.Plugin
{
  internal partial class Configure : Form
  {
    #region Properties

    public int MessageType
    {
      get { return Decimal.ToInt32(numericUpDownMessageType.Value); }
      set { numericUpDownMessageType.Value = new Decimal(value); }
    }

    public int WParam
    {
      get { return Decimal.ToInt32(numericUpDownWParam.Value); }
      set { numericUpDownWParam.Value = new Decimal(value); }
    }

    #endregion Properties

    #region Constructor

    public Configure()
    {
      InitializeComponent();
    }

    #endregion Constructor

    #region Buttons

    private void buttonOK_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.OK;
      Close();
    }

    private void buttonCancel_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }

    #endregion Buttons

    private void Configure_Load(object sender, EventArgs e)
    {
      textBoxWindowTitle.Text = String.Format(
        "To send windows messages to this receiver target the following window title:\r\n\r\n{0}",
        WindowsMessageReceiver.WindowTitle);
    }
  }
}