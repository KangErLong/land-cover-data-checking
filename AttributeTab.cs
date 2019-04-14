using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Globe30Chk
{
    public partial class AttributeTab : Form
    {
        public AttributeTab()
        {
            InitializeComponent();
        }
        public DataGridView dGrideView
        {
            get
            {
                return this.dataGridView1;
            }
        }

    }
}
