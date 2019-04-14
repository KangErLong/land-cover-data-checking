using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Globe30Chk
{
    class OpenFileDocument
    {
        public string filename
        {
            get;
            set;
        }
        public void openFile()
        {
            string fln;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Tiff文件|*.tif|Erdas img文件|*.img|Bmp文件|*.bmp|jpeg文件|*.jpg|所有文件|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fln = ofd.FileName;
                this.filename = fln;
            }
            if (filename == "")
            {
                MessageBox.Show("影像不存在,打开失败");
                return;
            }
        }
    }
}
