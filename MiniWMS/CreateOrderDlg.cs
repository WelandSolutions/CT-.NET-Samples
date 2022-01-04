/* ========================================================================
* MIT License
*
* Copyright (c) 2021 Weland Solutions AB
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*
* ======================================================================*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Weland.Ct.Api.Sample.MiniWMS
{
    public partial class CreateOrderDlg : Form
    {
        public CreateOrderDlg()
        {
            InitializeComponent();

            comboBoxOpening.Items.Add(1);
            comboBoxOpening.Items.Add(2);
            comboBoxOpening.Items.Add(3);

            comboBoxOperation.SelectedIndex = 0;
            comboBoxOpening.SelectedIndex = 0;
        }

        public string Operation => comboBoxOperation.Text;

        public int ServiceOpening => (int)comboBoxOpening.SelectedItem;

        public float Quantity => float.Parse(textBoxQuantity.Text);

        private void button1_Click(object sender, EventArgs e)
        {

            DialogResult = DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void comboBoxOperation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ("INV" == (string)comboBoxOperation.SelectedItem)
            {
                textBoxQuantity.Text = "0";
                textBoxQuantity.Enabled = false;
            }
            else
                textBoxQuantity.Enabled = true;
        }
    }
}
