using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using System.Windows.Forms;

namespace Globe30Chk
{
    class CreatVor
    {
        public void createVoronoiDiagram(string layername)
        {
            try
            {
                Geoprocessor gp = new Geoprocessor();
                gp.OverwriteOutput = false;
                IFeatureClass pInputFeatureClass = ChkMarkPoint.getFeatureLayer(layername).FeatureClass;
                MessageBox.Show("Choose the Save File of Voronoi Diagram");
                AutoChooseFile acf = new AutoChooseFile();
                string saveFilePath = acf.saveFullPathName();
                //CreateThiessenPolygons pCTP = new CreateThiessenPolygons(pInputFeatureClass, @"F:\Voronoi Land Cover\LC Voronoi.shp");
                CreateThiessenPolygons pCTP = new CreateThiessenPolygons(pInputFeatureClass, @saveFilePath);
                pCTP.fields_to_copy = "ALL";
                IGeoProcessorResult pGPR = gp.Execute(pCTP, null) as IGeoProcessorResult;
                for (int i = 0; i < gp.MessageCount; i++)
                {
                    ChkMarkPoint.changeText(gp.GetMessage(i));
                }
                //IFeatureClass pOutFeatureClass = gp.Open(pGPR.ReturnValue) as IFeatureClass;
                //IFeatureLayer pFeatureLayer = new FeatureLayerClass();
                //pFeatureLayer.Name = "Voronoi";
                //pFeatureLayer.FeatureClass = pOutFeatureClass;
                //ChkMarkPoint.Mapcontr.Map.AddLayer(pFeatureLayer as ILayer);
                //ChkMarkPoint.Mapcontr.Refresh();
            }
            catch (System.Exception e)
            {
                ChkMarkPoint.changeText(e.Message);
            }
        }
    }
}
