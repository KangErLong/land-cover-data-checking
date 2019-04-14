using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Globe30Chk
{
    class AutoChooseFile
    {
        string filenamepostfix = string.Empty;
        string filename = string.Empty;
        string workspacepath = string.Empty;
        string savefilename = string.Empty;

        //完整的文件路径
        public string getFullPathName()
        {
            OpenFileDialog pOpenFileDialog = new OpenFileDialog();
            pOpenFileDialog.Filter = "所有文件|*.*";
            string pFullFilePathName = string.Empty;
            if (pOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                pFullFilePathName = pOpenFileDialog.FileName;
            }
            return pFullFilePathName;
        }

        //带后缀的文件名
        public string getFileNameandPostfix()
        {
            OpenFileDialog pOFD = new OpenFileDialog();
			pOFD.Filter = "所有文件|*.*";
			string pfilename = string.Empty;
            if (pOFD.ShowDialog() == DialogResult.OK)
            {
                pfilename = pOFD.FileName;
                int index = pfilename.LastIndexOf(@"\");
                filenamepostfix = pfilename.Substring(index + 1); 
            }
            return filenamepostfix;               
        }

        //不带后缀的文件名
        public string getFileNameWithoutPostfix()
        {
            OpenFileDialog pOFD = new OpenFileDialog();
            pOFD.Filter = "所有文件|*.*";
            string pfilename = string.Empty;
            if (pOFD.ShowDialog() == DialogResult.OK)
            {
                pfilename = pOFD.FileName;
                int index01 = pfilename.LastIndexOf(@"\");
                int index02 = pfilename.LastIndexOf(@".");
                filename = pfilename.Substring(index01 + 1,index02-1-index01);
            }
            return filename;
        }

        //获取文件夹的名字
        public string getWorkspaceFileName()
        {
            FolderBrowserDialog fBD = new FolderBrowserDialog();
            fBD.Description = "Choose the Workspace file";
            if (fBD.ShowDialog() == DialogResult.OK)
            {
                workspacepath = fBD.SelectedPath;
            }
            return workspacepath;
        }

        //设置保存文件的文件名称
        public string saveFileName()
        {
            SaveFileDialog SFD = new SaveFileDialog();
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                string localpath = SFD.FileName;
                savefilename = localpath.Substring(localpath.IndexOf(@"\") + 1);
            }
            return savefilename; 
        }

        //设置保存文件的完整路径名称
        public string saveFullPathName()
        {
            string fullPathName = string .Empty;
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                fullPathName = sfd.FileName;
            }
            return fullPathName;
        }
        public string OpenFullPathFileName()
        {
            string openfullpathfilename = string.Empty;
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                openfullpathfilename = ofd.FileName;
            }
            return openfullpathfilename;
        }
    }
}
