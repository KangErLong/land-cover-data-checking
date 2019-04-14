extern alias toc;
using toc.ESRI.ArcGIS.ADF;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;



namespace Globe30Chk
{
    class DelLayer : BaseCommand
    {
        //定义指针   
        private IMapControl3 pMapControl;
        public DelLayer()
        {
            base.m_caption = "_RemoveLyr";
        }
        //重写BaseCommand基类的虚拟方法OnClick()   
        public override void OnClick()
        {
            ILayer pLayer = (ILayer)pMapControl.CustomProperty;
            pMapControl.Map.DeleteLayer(pLayer);
        }
        //重写BaseCommand基类的抽象方法OnCreate(object hook)   
        public override void OnCreate(object hook)
        {
            pMapControl = (IMapControl3)hook;
        }
    }   
}
