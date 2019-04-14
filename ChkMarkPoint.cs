using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.SpatialAnalyst;
using System.Windows.Forms;
using System.Collections;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using System.IO;

namespace Globe30Chk
{
    class ChkMarkPoint
    {
        public static AxMapControl Mapcontr
        {
            get;
            set;
        }
        public static string info
        {
            get;
            set;
        }
        public static void changeText(string pText)
        {
            //ChkMarkPoint.info = string.Empty;
            info = info + pText + "\r\n";
        }

        //通过图层名字cglc_chkmarkt获得错误记录点图层
        public static IFeatureLayer getFeatureLayer(string Layername)
        {
            IFeatureLayer pFeaturelayer = null;
            for (int i = 0; i < ChkMarkPoint.Mapcontr.Map.LayerCount; i++)
            {
                if (ChkMarkPoint.Mapcontr.Map.get_Layer(i).Name == Layername)
                {
                    pFeaturelayer = ChkMarkPoint.Mapcontr.Map.get_Layer(i) as IFeatureLayer;
                    break;
                }
            }
            return pFeaturelayer;
        }

        //根据文件名字获取TOCControl中的栅格图层
        public static IRasterLayer getRasterLayer(string Layername)
        {
            IRasterLayer pRasterLayer = null;
            for (int i = 0; i < ChkMarkPoint.Mapcontr.Map.LayerCount; i++)
            {
                if (ChkMarkPoint.Mapcontr.Map.get_Layer(i).Name == Layername)
                {
                    pRasterLayer = ChkMarkPoint.Mapcontr.Map.get_Layer(i) as IRasterLayer;
                    break;
                }
            }
            return pRasterLayer;
        }

        //栅格图层的地图代数运算，返回运算结果
        public static IGeoDataset rasterMapAlgebra(IRasterLayer pRL_Update, IRasterLayer pRL_Base)
        {
            IRaster pRS_Update = pRL_Update.Raster;
            IRaster pRS_Base = pRL_Base.Raster;

            IGeoDataset pGDT_Update = pRS_Update as IGeoDataset;
            IGeoDataset pGDT_Base = pRS_Base as IGeoDataset;
            //地图代数接口
            IMapAlgebraOp pMapAlgebraOp = new RasterMapAlgebraOpClass();
            pMapAlgebraOp.BindRaster(pGDT_Update, "raster_update");
            pMapAlgebraOp.BindRaster(pGDT_Base, "raster_base");
            //地图代数运算
            IGeoDataset pOutDifGeoDT = pMapAlgebraOp.Execute("[raster_update] - [raster_base]");
            return pOutDifGeoDT;
        }

        //添加一个点—将检测的错误点添加到该图层，添加相应字段errdsp的错误文本描述
        public static void insertChkPoint(string Layername,IGeometry pGeometry,string errdsp)
        {
            IFeatureLayer pFeatureLayer = getFeatureLayer(Layername);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IDataset pDataset = (IDataset)pFeatureClass;
            IWorkspace pWorkspace = pDataset.Workspace;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pWorkspace;
            //开始编辑cglc_chkmarkt图层
            pWorkspaceEdit.StartEditing(false);
            pWorkspaceEdit.StartEditOperation();

            IFeature pFeature = pFeatureClass.CreateFeature();
            IPoint pPoint = pGeometry as IPoint;
            pFeature.Shape = pPoint;
            pFeature.set_Value(3, errdsp);
            pFeature.Store();

            //结束图层编辑并保存
            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
        }

        //设置实验结果栅格数据保存路径
        public static IRasterWorkspace setRasterWorkspace(string path)
        {
            IWorkspaceFactory pWorkspaceFactory = new RasterWorkspaceFactoryClass();
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(path, 0);
            IRasterWorkspace pRasterWorkspace = pWorkspace as IRasterWorkspace;
            return pRasterWorkspace;
        }
        //设置实验结果矢量数据保存路径
        public static IFeatureWorkspace setFeatureWorkspace(string path)
        {
            IWorkspaceFactory pWorkspaceFactory = new RasterWorkspaceFactoryClass();
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(path, 0);
            IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
            return pFeatureWorkspace;
        }

        //遍历Geodataset查询特定邻近关系Plus后的像素点若不满足关系（值），标注代表性对象点到chkmarker.shp
        public static bool adjCorrectRelation(IGeoDataset pGeodataset,int adjVal)
        {
            IRaster pRaster = pGeodataset as IRaster;
            IRaster2 pRaster2 = pRaster as IRaster2;
            IRasterProps pRasterProps = pRaster as IRasterProps;
            //获取图层的行列值
            int height = pRasterProps.Height;
            int width = pRasterProps.Width;
            //定义并初始化数组，用于存储栅格内的所有像元值
            double[,] PixelVal = new double[height, width];
            //这是像素块大小
            IPnt blocksize = new PntClass();
            blocksize.SetCoords(256, 256);
            IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(blocksize);
            //存储像素块的行列值、像素值
            System.Array pixels;
            IPixelBlock3 pPixelBlock3;
            bool corVal = false;
            do
            {
                int xunit = (int)pRasterCursor.TopLeft.X;
                int yunit = (int)pRasterCursor.TopLeft.Y;
                pPixelBlock3 = pRasterCursor.PixelBlock as IPixelBlock3;
                pixels = (Array)pPixelBlock3.get_PixelData(0);
                for (int i = 0; i < pPixelBlock3.Height; i++)
                {
                    for (int j = 0; j < pPixelBlock3.Width; j++)
                    {
                        PixelVal[yunit + i, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i));
                        if (PixelVal[yunit + i, xunit + j] == adjVal)
                        {
                            corVal = true; //判断此幅数据是否满足邻近性
                            break;
                        }
                    }
                }
            } while (pRasterCursor.Next());
            return corVal;
        }
        
        //图层Plus计算后的包含关系数值判断
        public static bool containCorrRel(IGeoDataset pGeodataset,int chkVal1,int chkVal2)
        {
            bool containRel = true;
            //利用栅格图层的属性表实验
            IRaster pRaster = pGeodataset as IRaster;
            IRasterBandCollection pRasterBandCollection = pRaster as IRasterBandCollection;
            IRasterBand pRasterBand = pRasterBandCollection.Item(0);
            ITable pTabel = pRasterBand.AttributeTable;
            IQueryFilter pQueryfilter = new QueryFilterClass();
            pQueryfilter.WhereClause = "";
            ICursor pCursor = pTabel.Search(pQueryfilter, false);
            IRow pRow = pCursor.NextRow();
            while (pRow != null)
            {
                long type = Convert.ToInt64(pRow.get_Value(pTabel.Fields.FindField("Value")));
                if (type != chkVal1 && type != chkVal2)
                {
                    containRel = false;
                    break;
                }
                pRow = pCursor.NextRow();
            }
            return containRel;
            
        }

        public static bool disJointRelation(IGeoDataset pGeodataset, int disjointVal)
        {
            IRaster pRaster = pGeodataset as IRaster;
            IRaster2 pRaster2 = pRaster as IRaster2;
            IRasterProps pRasterProps = pRaster as IRasterProps;
            //获取图层的行列值
            int height = pRasterProps.Height;
            int width = pRasterProps.Width;
            //定义并初始化数组，用于存储栅格内的所有像元值
            double[,] PixelVal = new double[height, width];
            //这是像素块大小
            IPnt blocksize = new PntClass();
            blocksize.SetCoords(256, 256);
            IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(blocksize);
            //存储像素块的行列值、像素值
            System.Array pixels;
            IPixelBlock3 pPixelBlock3;
            bool disjoint = true;
            do
            {
                int xunit = (int)pRasterCursor.TopLeft.X;
                int yunit = (int)pRasterCursor.TopLeft.Y;
                pPixelBlock3 = pRasterCursor.PixelBlock as IPixelBlock3;
                pixels = (Array)pPixelBlock3.get_PixelData(0);
                for (int i = 0; i < pPixelBlock3.Height; i++)
                {
                    for (int j = 0; j < pPixelBlock3.Width; j++)
                    {
                        PixelVal[yunit + i, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i));
                        if (PixelVal[yunit + i, xunit + j] == disjointVal)
                        {
                            disjoint = false; //判断此幅数据是否满足邻近性
                            break;
                        }
                    }
                }
            } while (pRasterCursor.Next());
            return disjoint;
        }
        //标注提取的未扩展的湿地对象的代表性点
        public static void labelObjPoint(IGeoDataset pGeodataset,int objVal,string errstring)
        {
            
            IRaster pRaster = pGeodataset as IRaster;
            IRaster2 pRaster2 = pRaster as IRaster2;
            IRasterProps pRasterProps = pRaster as IRasterProps;
            ISpatialReference pSR = pGeodataset.SpatialReference;
            //获取图层的行列值
            int height = pRasterProps.Height;
            int width = pRasterProps.Width;
            //定义并初始化数组，用于存储栅格内的所有像元值
            double[,] PixelVal = new double[height, width];
            //这是像素块大小
            IPnt blocksize = new PntClass();
            blocksize.SetCoords(256, 256);
            IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(blocksize);
            //存储像素块的行列值、像素值
            System.Array pixels;
            IPixelBlock3 pPixelBlock3;
            do
            {
                int xunit = (int)pRasterCursor.TopLeft.X;
                int yunit = (int)pRasterCursor.TopLeft.Y;
                pPixelBlock3 = pRasterCursor.PixelBlock as IPixelBlock3;
                pixels = (Array)pPixelBlock3.get_PixelData(0);
                for (int i = 0; i < pPixelBlock3.Height; i++)
                {
                    for (int j = 0; j < pPixelBlock3.Width; j++)
                    {
                        PixelVal[yunit + i, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i));
                        if (PixelVal[yunit + i, xunit + j] == objVal)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.SpatialReference = pSR;
                            pPoint.X = pRaster2.ToMapX(xunit+j);
                            pPoint.Y = pRaster2.ToMapY(yunit + i);
                            ChkMarkPoint.insertChkPoint("cglc_chkmark", pPoint as IGeometry, errstring);
                            //MessageBox.Show("Neighborhood Relation Checking Complete...");
                            return;
                        }
                    }
                }

            } while (pRasterCursor.Next());
        
        }
        //将检测的错误点添加到该图层，添加相应字段errdsp的错误文本描述
        public static void insertChkPoints(string Layername, ArrayList pPoints, string errdsp)
        {
            IFeatureLayer pFeatureLayer = getFeatureLayer(Layername);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IDataset pDataset = (IDataset)pFeatureClass;
            IWorkspace pWorkspace = pDataset.Workspace;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pWorkspace;
            //开始编辑cglc_chkmarkt图层
            pWorkspaceEdit.StartEditing(false);   //true编辑后点数据不显示
            pWorkspaceEdit.StartEditOperation();
            
            for (int i = 0; i < pPoints.Count; i++)
            {
                IFeature pFeature = pFeatureClass.CreateFeature();
                IPoint pPoint = pPoints[i] as IPoint;
                pFeature.Shape = (IPoint)pPoint;
                pFeature.set_Value(3, errdsp);
                pFeature.Store();
            }
            
            //结束图层编辑并保存
            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);
        }

        public static void insertVorChkPoints(string Layername, ArrayList pPoints)
        {
            IFeatureLayer pFeatureLayer = getFeatureLayer(Layername);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IDataset pDataset = (IDataset)pFeatureClass;
            IWorkspace pWorkspace = pDataset.Workspace;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pWorkspace;
            //开始编辑cglc_chkmarkt图层
            pWorkspaceEdit.StartEditing(false);
            pWorkspaceEdit.StartEditOperation();

            for (int i = 0; i < pPoints.Count; i++)
            {
                IFeature pFeature = pFeatureClass.CreateFeature();
                IPoint pPoint = pPoints[i] as IPoint;
                pFeature.Shape = (IPoint)pPoint;
                pFeature.Store();
            }
            //结束图层编辑并保存
            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);
        }

        //向属性表添加字段
        public static void AddField(IFeatureClass pFeatureClass, string name, esriFieldType field)
        {
            if (pFeatureClass.Fields.FindField(name) > -1)
            {
                MessageBox.Show("Field " + name + " already exists.");
                return;
            }
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = name;
            pFieldEdit.Type_2 = field;
            IClass pClass = pFeatureClass as IClass;
            pClass.AddField(pField);
        }
    }
}
