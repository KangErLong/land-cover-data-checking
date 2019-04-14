using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.SpatialAnalyst;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Display;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Net;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GISClient;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.CartoUI;
using System.Diagnostics;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;

namespace Globe30Chk
{
    public partial class Form1 : Form
    {
        public ITOCControl2 pTocControl;
        public IMapControl3 pMapControl;
        public IToolbarMenu pToolMenuMap;
        public IToolbarMenu pToolMenuLayer; 
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            ChkMarkPoint.Mapcontr = this.axMapControl1;
            ChangeInformation ci = new ChangeInformation();
        }
        //地表覆盖数据像素Null值检查
        private void nullValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Start to Null Value Error Checking...", "Message Info", MessageBoxButtons.YesNo);
            //Null值满足的条件为noDataValue and containedby the region.shp
            //读取shp矢量范围图层
            //IFeatureLayer pFeatureLayer = ChkMarkPoint.getFeatureLayer("SandongLQ_Project");
            MessageBox.Show("Choose the Administrative Area Data in Layer");
            AutoChooseFile acf = new AutoChooseFile();
            string adminarea = acf.getFileNameWithoutPostfix();
            IFeatureLayer pFeatureLayer = ChkMarkPoint.getFeatureLayer(adminarea);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            //获得指定index的要素
            IFeature pFeature = pFeatureClass.GetFeature(0);
            //获取指定要素的字段信息
            //IArea pArea = p as IArea;
            //double s = pArea.Area;
            IPolygon p = pFeature.Shape as IPolygon;
            IRelationalOperator pRO = p as IRelationalOperator;

            //遍历栅格数据读取像素点Null值，转为IPoint，判断是否在pRO内，即Contain
            //IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer("2010glc_sdlq.tif");
            MessageBox.Show("Choose Checking Raster Data in Layer");
            string sdlq = acf.getFileNameandPostfix();
            IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer(sdlq);
            IRaster pRaster = pRasterLayer.Raster;
            IRasterProps pRasterProps = (IRasterProps)pRaster;
            //获取地理坐标系
            IGeoDataset pGDT = pRaster as IGeoDataset;
            ISpatialReference pSpaR = pGDT.SpatialReference;

            //获取noDataValue值
            object nulldata = pRasterProps.NoDataValue;
            //获取数据arr[0] = 65535 = noDataValue
            Int16[] arr = (Int16[])nulldata;
            int nodataval = Convert.ToInt32(arr[0]);

            //IRasterCursor IPixelBlock遍历栅格数据，查询出满足错误约束条件的栅格点，提取出对应的矢量点
            int Height = pRasterProps.Height;//行数
            int Width = pRasterProps.Width;  //列数
            //创建矩形数组存储分类的像素值
            double[,] pixelvalue = new double[Height, Width];//地表覆盖分类数据类型Long
            //生成RasterCursor，参数null，内部制动设置PixelBlock大小
            IRaster2 pRaster2 = pRaster as IRaster2;
            //数据量大，设置较小的栅格块
            IPnt pPntBlockSize = new PntClass();
            pPntBlockSize.SetCoords(128, 128);
            IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(pPntBlockSize);
            //存储PixelBlock的长和宽，int32 maxvalue = 2147483648
            int blockheight = 0;
            int blockwidth = 0;
            IPixelBlock3 pPixelBlock3; //使用该接口获得像素值get_pixeldata
            System.Array pixels;//存储block像素值
            try
            {
                do
                {
                    //获取cursor的左上角坐标
                    int xunit = (int)pRasterCursor.TopLeft.X;
                    int yunit = (int)pRasterCursor.TopLeft.Y;
                    pPixelBlock3 = pRasterCursor.PixelBlock as IPixelBlock3;
                    blockheight = pPixelBlock3.Height;  // 默认行大小128（数据行2228）
                    blockwidth = pPixelBlock3.Width;    //默认列大小2077（数据列2077）
                    //MessageBox.Show(blockheight.ToString() + blockwidth.ToString());128*128
                    pixels = (System.Array)pPixelBlock3.get_PixelData(0);
                    //获取该Cursor中PixelBlock的像素值
                    for (int j = 0; j < blockwidth; j++)
                    {
                        for (int i = 0; i < blockheight; i++)
                        {
                            pixelvalue[yunit + i, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i));
                            //null值检测判断，栅格转化为点
                            IPoint pPoint = new PointClass();
                            pPoint.SpatialReference = pSpaR;
                            pPoint.X = pRaster2.ToMapX(xunit + j);
                            pPoint.Y = pRaster2.ToMapY(yunit + i);
                            if (pixelvalue[i, j] == nodataval && pRO.Contains(pPoint))
                                //将该错误点添加到错误记录点图层
                                ChkMarkPoint.insertChkPoint("cglc_chkmark", pPoint as IGeometry, "Null Value Error");
                        }
                    }

                } while (pRasterCursor.Next());
                MessageBox.Show("Null Value in GLC Data Structure Checking Completes！", "Message Info", MessageBoxButtons.OKCancel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        //生成矢量数据点的Voronoi数据结构
        private void createVorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Start to Create Voronoi Diagram Of Characteristic Points");
            CreatVor cv = new CreatVor();
            //输出GP创建Voronoi图的信息ChangeInformation:form2显示
            ChangeInformation ci = new ChangeInformation();
            ci.Show();
            AutoChooseFile acf = new AutoChooseFile();
            MessageBox.Show("Choose VorPoint.Shapefile");
            string VorPointsFilename = acf.getFileNameWithoutPostfix();
            cv.createVoronoiDiagram(VorPointsFilename);
            ci.MessageTextBox.AppendText(ChkMarkPoint.info);
            MessageBox.Show("Voronoi Diagram Constructed");
        }

        //地表覆盖数据“左右”图幅之间的数据噪声
        private void verticalGLCNoiseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SpatialAnalysis 输入图层“交+减”代数运算后的图层处理
            //获得相邻两幅地表覆盖数据叠置差结果图
            MessageBox.Show("Have Input the Left-Right__Overlay-Minus Adjacent GLC", "Confirm Message", MessageBoxButtons.OKCancel);
            //加载图层dif_n1920152000.img
            //IRasterLayer difRasterLayer = ChkMarkPoint.getRasterLayer("dif_n1920152000.img");
            AutoChooseFile acf = new AutoChooseFile();
            string verdif_filename = acf.getFileNameandPostfix();
            //IRasterLayer difRasterLayer = ChkMarkPoint.getRasterLayer("dif_n1920152000.img");
            IRasterLayer difRasterLayer = ChkMarkPoint.getRasterLayer(verdif_filename);
            IRaster difRaster = difRasterLayer.Raster;

            //栅格图层的属性接口
            IRasterProps difRasterProps = difRaster as IRasterProps;
            int width = difRasterProps.Width; //625
            int height = difRasterProps.Height;//18574
            //像素坐标与地理坐标的转换
            IGeoDataset pGDT = difRaster as IGeoDataset;
            ISpatialReference pSR = pGDT.SpatialReference;

            //栅格图层的空值
            object nodata = difRasterProps.NoDataValue;
            Int16[] arr = (Int16[])nodata;
            int nodataval = Convert.ToInt32(arr[0]); //nodatavalue值为：-32768

            double[,] pixelvalue = new double[height, width]; //行列与宽高注意
            IRaster2 pRaster2 = difRaster as IRaster2;
            IPnt pBlockSize = new PntClass();
            pBlockSize.SetCoords(128, 256);//设置像素的索引块大小
            IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(pBlockSize);
            IPixelBlock3 pPixelBlock3;
            System.Array pixels;
            ArrayList pArr = new ArrayList();
            do
            {
                //获取栅格索引像素块的左上角坐标
                int xunit = (int)pRasterCursor.TopLeft.X;
                int yunit = (int)pRasterCursor.TopLeft.Y;
                pPixelBlock3 = pRasterCursor.PixelBlock as IPixelBlock3;
                pixels = (System.Array)pPixelBlock3.get_PixelData(0);
                for (int j = 0; j < pPixelBlock3.Width; j++)
                {
                    for (int i = 0; i < pPixelBlock3.Height; i++)
                    {
                        pixelvalue[yunit + i, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i));
                        if (pixelvalue[yunit + i, xunit + j] == 0 || pixelvalue[yunit + i, xunit + j] == arr[0])
                        {
                            continue;
                        }
                        else
                        {
                            if ((i + 6) < pPixelBlock3.Height)
                            {
                                pixelvalue[yunit + i + 1, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i + 1));
                                pixelvalue[yunit + i + 2, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i + 2));
                                pixelvalue[yunit + i + 3, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i + 3));
                                pixelvalue[yunit + i + 4, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i + 4));
                                pixelvalue[yunit + i + 5, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i + 5));
                                pixelvalue[yunit + i + 6, xunit + j] = Convert.ToDouble(pixels.GetValue(j, i + 6));

                                if (pixelvalue[yunit + i + 1, xunit + j] == pixelvalue[yunit + i + 6, xunit + j])
                                {
                                    IPoint pPoint = new PointClass();
                                    pPoint.SpatialReference = pSR;
                                    pPoint.X = pRaster2.ToMapX(xunit + j);
                                    pPoint.Y = pRaster2.ToMapY(yunit + i);
                                    //加载图层cglc_chkmark
                                    //ChkMarkPoint.insertChkPoint("cglc_chkmark", pPoint as IGeometry, "Common Ajacent Classificatin Left-Right Error");
                                    pArr.Add(pPoint);
                                    i = i + 6;
                                }
                            }
                        }
                    }
                }

            } while (pRasterCursor.Next() == true);
            ChkMarkPoint.insertChkPoints("cglc_chkmark", pArr, "Common Ajacent Classificatin Left-Right Error");
            MessageBox.Show("Left-Right Noise Value Checking Completely!", "Notice", MessageBoxButtons.OKCancel);
        }

        //地表覆盖数据“上下”图幅之间的数据噪声
        private void horizontalGLCNoiseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SpatialAnalysis 输入图层“交+减”代数运算后的图层处理
            //获得相邻两幅地表覆盖数据叠置差结果图
            MessageBox.Show("Have Input the Top-Down__Overlay-Minus Adjacent GLC", "Confirm Message", MessageBoxButtons.OKCancel);
            //加载图层山东n50_30与n50_35的图像overlap差值结果
            //IRasterLayer difRasterLayer = ChkMarkPoint.getRasterLayer("dif_n5030n5035_2010edge.img");
            AutoChooseFile acf = new AutoChooseFile();
            string hordif_filename = acf.getFileNameandPostfix();
            //IRasterLayer difRasterLayer = ChkMarkPoint.getRasterLayer("dif_n5030n5035_2010edge.img");
            IRasterLayer difRasterLayer = ChkMarkPoint.getRasterLayer(hordif_filename);
            IRaster difRaster = difRasterLayer.Raster;
            //栅格图层的属性接口
            IRasterProps difRasterProps = difRaster as IRasterProps;
            int width = difRasterProps.Width;
            int height = difRasterProps.Height;
            //像素坐标与地理坐标的转换
            IGeoDataset pGDT = difRaster as IGeoDataset;
            ISpatialReference pSR = pGDT.SpatialReference;
            //栅格图层的空值获取
            object nodata = difRasterProps.NoDataValue;
            Int16[] arr = (Int16[])nodata;
            int nodataval = Convert.ToInt32(arr[0]); //nodatavalue值为：-32768
            double[,] pixelvalue = new double[height, width]; //行列与宽高注意
            IRaster2 pRaster2 = difRaster as IRaster2;
            IPnt pBlockSize = new PntClass();
            pBlockSize.SetCoords(256, 128);//设置像素的索引块大小
            IRasterCursor pRasterCursor = pRaster2.CreateCursorEx(pBlockSize);
            IPixelBlock3 pPixelBlock3;
            System.Array pixels;
            ArrayList pArr = new ArrayList();
            do
            {
                //获取栅格索引像素块的左上角坐标
                int xunit = (int)pRasterCursor.TopLeft.X;
                int yunit = (int)pRasterCursor.TopLeft.Y;
                pPixelBlock3 = pRasterCursor.PixelBlock as IPixelBlock3;
                pixels = (System.Array)pPixelBlock3.get_PixelData(0);
                for (int j = 0; j < pPixelBlock3.Height; j++)
                {
                    for (int i = 0; i < pPixelBlock3.Width; i++)
                    {
                        pixelvalue[yunit + j, xunit + i] = Convert.ToDouble(pixels.GetValue(i, j));
                        if (pixelvalue[yunit + j, xunit + i] == 0 || pixelvalue[yunit + j, xunit + i] == arr[0])
                        {
                            continue;
                        }
                        else
                        {
                            if ((i + 6) < pPixelBlock3.Width)
                            {
                                pixelvalue[yunit + j, xunit + i + 1] = Convert.ToDouble(pixels.GetValue(i + 1, j));
                                pixelvalue[yunit + j, xunit + i + 2] = Convert.ToDouble(pixels.GetValue(i + 2, j));
                                pixelvalue[yunit + j, xunit + i + 3] = Convert.ToDouble(pixels.GetValue(i + 3, j));
                                pixelvalue[yunit + j, xunit + i + 4] = Convert.ToDouble(pixels.GetValue(i + 4, j));
                                pixelvalue[yunit + j, xunit + i + 5] = Convert.ToDouble(pixels.GetValue(i + 5, j));
                                pixelvalue[yunit + j, xunit + i + 6] = Convert.ToDouble(pixels.GetValue(i + 6, j));

                                if (pixelvalue[yunit + j, xunit + i + 1] == pixelvalue[yunit + j, xunit + i + 6])
                                {
                                    IPoint pPoint = new PointClass();
                                    pPoint.SpatialReference = pSR;
                                    pPoint.X = pRaster2.ToMapX(xunit + i);
                                    pPoint.Y = pRaster2.ToMapY(yunit + j);
                                    //加载图层cglc_chkmark
                                    //ChkMarkPoint.insertChkPoint("cglc_chkmark", pPoint as IGeometry, "Common Ajacent Classificatin Top-Down Error");
                                    pArr.Add(pPoint);
                                    i = i + 6;
                                }
                            }
                        }
                    }
                }
            } while (pRasterCursor.Next() == true);
            ChkMarkPoint.insertChkPoints("cglc_chkmark", pArr, "Common Ajacent Classificatin Top-Down Error");
            MessageBox.Show("Top-Down Noise Value Checking Completely!", "Notice", MessageBoxButtons.OKCancel);
        }


        //利用众源数据检查地表覆盖数据更新中的错误，加载WMS/WCS服务来验证LC的一致性
        private void addNetReferenceMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ArcEngine加载WMS/WCS服务器中的地图
            ESRI.ArcGIS.esriSystem.IPropertySet pPropertyset = new ESRI.ArcGIS.esriSystem.PropertySetClass();

            //Corine地表覆盖数据服务
            MessageBox.Show("Input：Land Cover Map Service URL");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            string LCUrl = ci.MessageTextBox.Text;
            //服务地址："http://CLC.developpement-durable.gouv.fr/geoserver/wms?"
            //天地图影像服务地址："http://www.scgis.net.cn/imap/iMapServer/defaultRest/services/newtianditudom/WMS?" 
            ci.MessageTextBox.Clear();
            pPropertyset.SetProperty("url", LCUrl);
            
            IWMSConnectionName pWmsConnectionName = new WMSConnectionNameClass();
            pWmsConnectionName.ConnectionProperties = pPropertyset;
            ILayerFactory pLayerFactory = new EngineWMSMapLayerFactoryClass();

            //create an WMSMapLayer Instance - this will be added to the map later
            IWMSGroupLayer pWmsMapLayer = new WMSMapLayerClass();
            IDataLayer pDataLayer = pWmsMapLayer as IDataLayer;
            pDataLayer.Connect(pWmsConnectionName as IName);
            IWMSServiceDescription pWmsServiceDesc = pWmsMapLayer.WMSServiceDescription;

            for (int i = 0; i < pWmsServiceDesc.LayerDescriptionCount; i++)
            {
                IWMSLayerDescription pWmsLayerDesc = pWmsServiceDesc.get_LayerDescription(i);
                ILayer pNewLayer = null;
                if (pWmsLayerDesc.LayerDescriptionCount == 0)
                {
                    IWMSLayer pWmsLayer = pWmsMapLayer.CreateWMSLayer(pWmsLayerDesc);
                    pNewLayer = pWmsLayer as ILayer;
                }
                else
                {
                    IWMSGroupLayer pWmsGroupLayer = pWmsMapLayer.CreateWMSGroupLayers(pWmsLayerDesc);
                    for (int j = 0; j < pWmsGroupLayer.Count; j++)
                    {
                        ILayer layer = pWmsGroupLayer.get_Layer(j);
                        pWmsMapLayer.InsertLayer(layer, 0);
                        layer.Visible = true;
                    }
                }
            }
            ILayer pLayer = pWmsMapLayer as ILayer;
            pLayer.Name = pWmsServiceDesc.WMSTitle;
            pLayer.Visible = true;
            this.axMapControl1.AddLayer(pLayer, 0);
            this.axMapControl1.Refresh();
            MessageBox.Show("LoadingWMS Map Service  Successfully!");

        }
        

        //从文件菜单下的打开对话框工具文件路径和文件名方式加载栅格或矢量数据
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog pOpenFileDialog = new OpenFileDialog();
            pOpenFileDialog.Filter = "所有文件|*.*";
            string fullfilename = string.Empty;
            if (pOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                fullfilename = pOpenFileDialog.FileName;
                //从文件直接加载shp图层
                if (fullfilename.Contains("shp"))
                {
                    int index = fullfilename.LastIndexOf(@"\");
                    //文件所在的文件夹
                    string filefolder = fullfilename.Substring(0, index);
                    //文件名称
                    string filename = fullfilename.Substring(index + 1);
                    if (pOpenFileDialog.FilterIndex == 1)
                    {
                        //打开工作空间工厂
                        IWorkspaceFactory pWSF = new ShapefileWorkspaceFactoryClass();
                        IFeatureWorkspace pFWS;
                        IFeatureLayer pLayer = new FeatureLayerClass();
                        //打开路径
                        pFWS = pWSF.OpenFromFile(filefolder, 0) as IFeatureWorkspace;
                        //打开要素
                        pLayer.FeatureClass = pFWS.OpenFeatureClass(filename);
                        pLayer.Name = filename;
                        this.axMapControl1.AddLayer(pLayer);
                        axMapControl1.Refresh();
                    }
                }
                //从文件直接加载Raster图层
                else if (fullfilename.Contains("tif") || fullfilename.Contains("img"))
                {
                    IRasterLayer pRLayer = new RasterLayerClass();
                    pRLayer.CreateFromFilePath(fullfilename);
                    this.axMapControl1.AddLayer(pRLayer);
                }
                else
                {
                    MessageBox.Show("Wrong File Type", "Error");
                }
            }
        }

        //检查原型系统退出
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //文件退出该系统
            System.Environment.Exit(-1);
        }

        //计算出两期时序GLC数据的差值，并在窗口显示
        private void temporalExistingRulesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("GLC时序数据差值地图代数计算");
            //IRasterLayer pRasterLayer = new RasterLayerClass();
            //IRasterLayer pRL01 = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            //IRasterLayer pRL02 = ChkMarkPoint.getRasterLayer("2010glc_sdlq.tif");
            //地图代数运算
            //IGeoDataset pOutDS = ChkMarkPoint.rasterMapAlgebra(pRL01, pRL02);
            //pRasterLayer.CreateFromRaster(pOutDS as IRaster);
            //pRasterLayer.Name = "GLC temporal diff";
            //axMapControl1.AddLayer(pRasterLayer as ILayer);
            //axMapControl1.Refresh();
        }

        //更新期与基准期的两期地表覆盖变化定性判断
        private void qualityCheckingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //加载时序关系定性检查数据
            MessageBox.Show("Start to Check Temporal Transformation Qualitatively... ...");
            AutoChooseFile acf = new AutoChooseFile();
            MessageBox.Show("Choose Updating Raster Layer Name");
            string updating_name = acf.getFileNameandPostfix();
            //IRasterLayer pURL = ChkMarkPoint.getRasterLayer("2010glc_sdlq.tif");
            IRasterLayer pURL = ChkMarkPoint.getRasterLayer(updating_name);
            MessageBox.Show("Choose Baseline Raster Layer Name");
            string baseline_name = acf.getFileNameandPostfix();
            //IRasterLayer pBRL = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            IRasterLayer pBRL = ChkMarkPoint.getRasterLayer(baseline_name);
            IRaster pUR = pURL.Raster;
            IGeoDataset pGDT = pUR as IGeoDataset;
            ISpatialReference pSR = pGDT.SpatialReference;
            IRaster pBR = pBRL.Raster;
            //数据的属性接口
            IRasterProps pURP = pUR as IRasterProps;
            IRasterProps pBRP = pBR as IRasterProps;
            if (pURP.Width != pBRP.Width || pURP.Height != pBRP.Height)
            {
                MessageBox.Show("Columns and Rows are Un-Equal...Re-Sampling!");
                return;
            }

            //输入约束类型量化表达的数值
            MessageBox.Show("Input：Land Cover Type in Baseline");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            int base_type = Convert.ToInt32(ci.MessageTextBox.Text);//80
            ci.MessageTextBox.Clear();
            MessageBox.Show("Input：Forbidden Transformation Type in Updating");
            ci.ShowDialog();
            int update_type = Convert.ToInt32(ci.MessageTextBox.Text);//10
            ci.MessageTextBox.Clear();

            int uwidth = pURP.Width;
            int uheight = pURP.Height;
            double[,] upixel_values = new double[uheight, uwidth];
            int bwidth = pBRP.Width;
            int bheight = pBRP.Height;
            double[,] bpixel_values = new double[bheight, bwidth];

            IPnt blocksize = new PntClass();
            blocksize.SetCoords(256, 256);
            IRaster2 pUR2 = pUR as IRaster2;
            IRaster2 pBR2 = pBR as IRaster2;
            IRasterCursor pURC = pUR2.CreateCursorEx(blocksize);
            IRasterCursor pBRC = pBR2.CreateCursorEx(blocksize);

            System.Array upixelblock;
            System.Array bpixelblock;
            IPixelBlock3 uPB3;
            IPixelBlock3 bPB3;
            //地表覆盖转耕地约束的点数组
            ArrayList pAL = new ArrayList();
            do
            {
                int uxdis = (int)pURC.TopLeft.X;
                int uydis = (int)pURC.TopLeft.Y;
                uPB3 = pURC.PixelBlock as IPixelBlock3;
                upixelblock = (System.Array)uPB3.get_PixelData(0);
                for (int i = 0; i < uPB3.Height; i++)
                {
                    for (int j = 0; j < uPB3.Width; j++)
                    {
                        upixel_values[uydis + i, uxdis + j] = Convert.ToDouble(upixelblock.GetValue(j, i));
                    }
                }
            } while (pURC.Next());

            do
            {
                int bxdis = (int)pBRC.TopLeft.X;
                int bydis = (int)pBRC.TopLeft.Y;
                bPB3 = pBRC.PixelBlock as IPixelBlock3;
                bpixelblock = (System.Array)bPB3.get_PixelData(0);

                for (int u = 0; u < bPB3.Height; u++)
                {
                    for (int v = 0; v < bPB3.Width; v++)
                    {
                        bpixel_values[bydis + u, bxdis + v] = Convert.ToDouble(bpixelblock.GetValue(v, u));
                        //基准期人造转更新期耕地时间关系约束

                        if (bpixel_values[bydis + u, bxdis + v] == base_type && upixel_values[bydis + u, bxdis + v] == update_type)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.SpatialReference = pSR;
                            pPoint.X = pUR2.ToMapX(bxdis + v);
                            pPoint.Y = pUR2.ToMapY(bydis + u);
                            //ChkMarkPoint.insertChkPoint("cglc_chkmark", pPoint as IGeometry, "Inconsistency Detected: Trans_Artificial Surface to Cultivated Land.");
                            pAL.Add(pPoint);
                        }
                    }
                }
            } while (pBRC.Next());
            ChkMarkPoint.insertChkPoints("cglc_chkmark", pAL, base_type.ToString() + " impossible reversion transition to " + update_type.ToString());
            MessageBox.Show("Un-logical Spatiotemporal Type Transformation Checking Done！");
        }

        //更新期与基准期的两期地表覆盖变化定量判断
        private void quantityCheckingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Start to Check Temporal Transformation Quantitatively... ...", "Info Notice");
            //获取图层
            //IRasterLayer pRL_Update = ChkMarkPoint.getRasterLayer("2010glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string rluname = acf.getFileNameandPostfix();//raster layer updating name
            IRasterLayer pRL_Update = ChkMarkPoint.getRasterLayer(rluname);
            //IRasterLayer pRL_Base = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            string rlbname = acf.getFileNameandPostfix();//raster layer baseline name
            IRasterLayer pRL_Base = ChkMarkPoint.getRasterLayer(rlbname);
            //获取图层数据
            IRaster pR_Update = pRL_Update.Raster;
            IRaster pR_Base = pRL_Base.Raster;
            //获取图层数据属性接口
            IRasterProps pRP_Update = pR_Update as IRasterProps;
            IRasterProps pRP_Base = pR_Base as IRasterProps;
            //获取图层的height和width
            int uwidth = pRP_Update.Width;
            int uheight = pRP_Update.Height;
            double[,] pixelVal_Update = new double[uheight, uwidth];

            int bwidth = pRP_Base.Width;
            int bheight = pRP_Base.Height;
            double[,] pixelVal_Base = new double[bheight, bwidth];
            IGeoDataset pGDT = pR_Update as IGeoDataset;
            ISpatialReference pSR = pGDT.SpatialReference;
            //设置像素索引
            IPnt BlockSize = new PntClass();
            BlockSize.SetCoords(256, 256);
            IRaster2 pR2_Update = pR_Update as IRaster2;
            IRaster2 pR2_Base = pR_Base as IRaster2;
            IRasterCursor pRC_Update = pR2_Update.CreateCursorEx(BlockSize);
            IRasterCursor pRC_Base = pR2_Base.CreateCursorEx(BlockSize);
            //两期对应的像素块和像素值
            IPixelBlock3 pPB3_Update;
            IPixelBlock3 pPB3_Base;
            System.Array pixels_Update;
            System.Array pixels_Base;
            //两期地表覆盖数据对应类别的像素图斑数量
            double m10 = 0, m25 = 0, m26 = 0, m27 = 0, m30 = 0, m40 = 0, m50 = 0, m60 = 0, m70 = 0, m80 = 0, m90 = 0, m100 = 0;
            double n10 = 0, n25 = 0, n26 = 0, n27 = 0, n30 = 0, n40 = 0, n50 = 0, n60 = 0, n70 = 0, n80 = 0, n90 = 0, n100 = 0;

            do
            {
                int xunit = (int)pRC_Update.TopLeft.X;
                int yunit = (int)pRC_Update.TopLeft.Y;
                //更新影像像素块值
                pPB3_Update = pRC_Update.PixelBlock as IPixelBlock3;
                pixels_Update = (System.Array)pPB3_Update.get_PixelData(0);
                for (int i = 0; i < pPB3_Update.Height; i++)
                {
                    for (int j = 0; j < pPB3_Update.Width; j++)
                    {
                        pixelVal_Update[yunit + i, xunit + j] = Convert.ToDouble(pixels_Update.GetValue(j, i));
                        if (pixelVal_Update[yunit + i, xunit + j] == 10)
                            m10 = m10 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 25)
                            m25 = m25 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 26)
                            m26 = m26 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 27)
                            m27 = m27 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 30)
                            m30 = m30 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 40)
                            m40 = m40 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 50)
                            m50 = m50 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 60)
                            m60 = m60 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 70)
                            m70 = m70 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 80)
                            m80 = m80 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 90)
                            m90 = m90 + 1;
                        if (pixelVal_Update[yunit + i, xunit + j] == 100)
                            m100 = m100 + 1;
                    }
                }
            } while (pRC_Update.Next());

            do
            {
                int bxunit = (int)pRC_Base.TopLeft.X;
                int byunit = (int)pRC_Base.TopLeft.Y;
                //基准影像的像素块值
                pPB3_Base = pRC_Base.PixelBlock as IPixelBlock3;
                pixels_Base = (System.Array)pPB3_Base.get_PixelData(0);
                for (int u = 0; u < pPB3_Base.Height; u++)
                {
                    for (int v = 0; v < pPB3_Base.Width; v++)
                    {
                        pixelVal_Base[byunit + u, bxunit + v] = Convert.ToDouble(pixels_Base.GetValue(v, u));
                        if (pixelVal_Base[byunit + u, bxunit + v] == 10)
                            n10 = n10 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 25)
                            n25 = n25 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 26)
                            n26 = n26 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 27)
                            n27 = n27 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 30)
                            n30 = n30 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 40)
                            n40 = n40 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 50)
                            n50 = n50 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 60)
                            n60 = n60 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 70)
                            n70 = n70 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 80)
                            n80 = n80 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 90)
                            n90 = n90 + 1;
                        if (pixelVal_Base[byunit + u, bxunit + v] == 100)
                            n100 = n100 + 1;
                    }

                }
            } while (pRC_Base.Next());

            //时间关系定量判读错误信息提示
            ChangeInformation Errci = new ChangeInformation();
            Errci.ShowDialog();
            MessageBox.Show("Input Threshold Defined");
            int delta = Convert.ToInt32(Errci.MessageTextBox.Text);//500 units
            Errci.MessageTextBox.Clear();
            if (m10 - n10 >= delta)
            {
                string increment10 = ((m10 - n10) / m10 * 100).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Cultivated land Increased " +increment10 + "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m10 - n10 <= -delta)
            {
                string increment10 = Math.Abs((m10 - n10) / m10).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Cultivated land Decreased " + increment10  + "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if ((m25 - n25) + (m26 - n26) + (m27 - n27) >= delta)
            {
                string increment25 = ((m25 + m26 + m27 - n25 - n26 - n27) / (m25 + m26 + m27) * 100).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Forest Increased " +increment25+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if ((m25 - n25) + (m26 - n26) + (m27 - n27) <= -delta)
            {
                string increment25 = (Math.Abs((m25 + m26 + m27 - n25 - n26 - n27)) / (m25 + m26 + m27) * 100).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Forest Decreased " + increment25+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m30 - n30 >= delta)
            {
                string increment30 = ((m30 - n30) / m30).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Grassland Increased " +increment30+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m30 - n30 <= -delta)
            {
                string increment30 = (Math.Abs(m30 - n30) / m30).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updaing Grassland Decreased " +increment30+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m40 - n40 >= delta)
            {
                string increment40 = ((m40 - n40) / m40).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Shrubland Increased " +increment40+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m40 - n40 <= -delta)
            {
                string increment40 = (Math.Abs((m40 - n40)) / m40).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Shrubland Decreased " +increment40+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m50 - n50 >= delta)
            {
                string increment50 = ((m50 - n50) / m50).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Wetland Increased " +increment50+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");

            }
            else if (m50 - n50 <= -delta)
            {
                string increment50 = (Math.Abs((m50 - n50)) / m50).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Wetland Decreased " +increment50 +"\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m60 - n60 >= delta)
            {
                string increment60 = ((m60 - n60) / m60).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Water Body Increased " +increment60+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m60 - n60 <= -delta)
            {
                string increment60 = (Math.Abs((m60 - n60) / m60)).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Water Body Decreased " +increment60+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m70 - n70 >= delta)
            {
                string increment70 = ((m70 - n70) / m70).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Tundra Increased " +increment70+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m70 - n70 <= -delta)
            {
                string increment70 = (Math.Abs((m70 - n70)) / m70).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Tundra Decreased " +increment70+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m80 - n80 >= delta)
            {
                string increment80 = ((m80 - n80) / m80).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Artificial Surface Increased " +increment80+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m80 - n80 <= -delta)
            {
                string increment80 = (Math.Abs((m80 - n80)) / m80).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Artificial Surface Decreased " +increment80+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m90 - n90 >= delta)
            {
                string increment90 = ((m90 - n90) / m90).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Bareland Increased " +increment90+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m90 - n90 <= -delta)
            {
                string increment90 = (Math.Abs((m90 - n90)) / m90).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Bareland Decreased " +increment90+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            if (m100 - n100 >= delta)
            {
                string increment100 = ((m100 - n100) / m100).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Permanent Snow and Ice Increased " +increment100+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            else if (m100 - n100 <= -delta)
            {
                string increment100 = (Math.Abs((m100 - n100)) / m100).ToString("0.00%");
                Errci.MessageTextBox.AppendText("The Amount of Updating Permanent Snow and Ice Decreased " +increment100+ "\n");
                Errci.MessageTextBox.AppendText(" " + "\n");
            }
            Errci.Show();
            MessageBox.Show("Temporal Quantity Inconsistency Detecting Done.");
            //变量重置
            m10 = 0; m25 = 0; m26 = 0; m27 = 0; m30 = 0; m40 = 0; m50 = 0; m60 = 0; m70 = 0; m80 = 0; m90 = 0; m100 = 0;
            n10 = 0; n25 = 0; n26 = 0; n27 = 0; n30 = 0; n40 = 0; n50 = 0; n60 = 0; n70 = 0; n80 = 0; n90 = 0; n100 = 0;
        }

        //检查湿地附近没有水体的湿地错误
        private void neighborhoodRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Inconsistent Neighborhood Relation Checking... ...");
            //IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string touch_filename = acf.getFileNameandPostfix();
            IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer(touch_filename);
            IRaster pRaster = pRasterLayer.Raster;

            //创建一个查询语句
            IQueryFilter pQueryFilter = new QueryFilterClass();
            //查询湿地，湿地代码 = 50
            //int wetNO = 50;
            //pQueryFilter.WhereClause = "Value = "+ Convert.ToString(wetNO);
            MessageBox.Show("Input：Checking Type Value");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            string neighbor_type = ci.MessageTextBox.Text;
            ci.MessageTextBox.Clear();

            //邻近约束值
            MessageBox.Show("Input：Constraint Neighborhood Checking Type Value");
            ci.ShowDialog();
            int neighborcons_type = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            //计算算法运行时间
            Stopwatch sw = new Stopwatch();
            sw.Start();

            pQueryFilter.WhereClause = "Value = " + neighbor_type;
            //IRasterDescriptor描述
            IRasterDescriptor pRasterDescriptor = new RasterDescriptorClass();
            pRasterDescriptor.Create(pRaster, pQueryFilter, "Value");
            //ExtractionByAtrribute操作IExtracionOp
            ESRI.ArcGIS.SpatialAnalyst.IExtractionOp pExtractionByAttri = new ESRI.ArcGIS.SpatialAnalyst.RasterExtractionOpClass();
            IGeoDataset pOutGDByAttribute = pExtractionByAttri.Attribute(pRasterDescriptor);
            //Region Group操作IGeneralizeOp RegionGroup 八邻域对象
            ESRI.ArcGIS.SpatialAnalyst.IGeneralizeOp pGeneralizeOp = new ESRI.ArcGIS.SpatialAnalyst.RasterGeneralizeOpClass();
            var missing = Type.Missing;
            IGeoDataset pOutGDRegionGroup = pGeneralizeOp.RegionGroup(pOutGDByAttribute, true, true, true, ref missing);
            //从对象集pOutGDRegionGroup的OID提取出每个邻域对象作为一个Geodataset,Expand操作IGeneralizeOp Expand
            IRaster pRGRaster = pOutGDRegionGroup as IRaster;
            IRasterBandCollection pRGRasterBandCollection = pRGRaster as IRasterBandCollection;
            IRasterBand pRGRasterBand = pRGRasterBandCollection.Item(0);
            ITable pRGTable = pRGRasterBand.AttributeTable;
            //属性表逐条遍历
            IQueryFilter pRGQueryFilter = new QueryFilterClass();
            ICursor pRGCursor = pRGTable.Search(pRGQueryFilter, false);
            IRow pRGRow = pRGCursor.NextRow();
            //邻近关系的判断条件：扩张后的栅格对象与周边像素的差值不全相等
            while (pRGRow != null)
            {
                //提取对象
                int ObjNo = Convert.ToInt32(pRGRow.get_Value(pRGTable.FindField("Value")));
                //MessageBox.Show("Object No." + ObjNo.ToString());

                IQueryFilter ObjQueryFilter = new QueryFilterClass();
                ObjQueryFilter.WhereClause = "Value = " + Convert.ToString(ObjNo);
                IRasterDescriptor objRasterDescriptor = new RasterDescriptorClass();
                objRasterDescriptor.Create(pRGRaster, ObjQueryFilter, "Value");
                IExtractionOp objExtractionOp = new RasterExtractionOpClass();
                IGeoDataset objExGeoDataset = objExtractionOp.Attribute(objRasterDescriptor);
                //对象扩展Expand
                IGeneralizeOp objExpand = new RasterGeneralizeOpClass();
                object zonelist = new int[] { ObjNo };
                //扩展后的对象
                IGeoDataset objExpandGeodataset = objExpand.Expand(objExGeoDataset, 1, ref zonelist);
                // == 60+objNo,扩展后的对象与原图像像素和运算，IMapthOp.plus
                IMathOp pMathPlus = new RasterMathOpsClass();
                IGeoDataset pMathPlustGD = pMathPlus.Plus(objExpandGeodataset, pRaster as IGeoDataset);
                //若计算结果存在60+objNo，则其附近有水体；否则记录为错误分类
                int chkVal = neighborcons_type + ObjNo;
                //对象周围不存在水体，则标注该对象——————相离关系
                if (ChkMarkPoint.adjCorrectRelation(pMathPlustGD, chkVal) == false)
                {
                    string errdsp = neighbor_type + " non-adjacent with " + neighborcons_type.ToString();
                    ChkMarkPoint.labelObjPoint(objExGeoDataset, ObjNo, errdsp);
                }
                pRGRow = pRGCursor.NextRow();
            }

            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            MessageBox.Show("Computation time: "+ts.TotalMilliseconds.ToString()+"MS");

            MessageBox.Show("Inconsistent Neighborhood Relation Checking Done.");
        }

        //水体60、耕地10被人造80包含的空间关系约束
        private void containRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            MessageBox.Show("Inconsistent Contain Relationship Checking... ...");
            //IRasterLayer pFeatureLayer = ChkMarkPoint.getRasterLayer("2010glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string contain_filename = acf.getFileNameandPostfix();
            IRasterLayer pFeatureLayer = ChkMarkPoint.getRasterLayer(contain_filename);
            IRaster pRaster = pFeatureLayer.Raster;

            //面积过滤
            IGeoDataset sGD = pRaster as IGeoDataset;
            IEnvelope sEnv = sGD.Extent as IEnvelope;
            IArea sArea = sEnv as IArea;

            //查询水体
            MessageBox.Show("Input: Contain Checking Type Value");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            int check_value = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            MessageBox.Show("Input: Constraint Contain Checking Type Value");
            ci.ShowDialog();
            int constrain_containval = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            //计算算法运行时间
            Stopwatch sw = new Stopwatch();
            sw.Start();

            IQueryFilter pQueryFilter = new QueryFilterClass();
            pQueryFilter.WhereClause = "Value = " + check_value.ToString();
            //IRasterDescriptor描述
            IRasterDescriptor pRasterDescriptor = new RasterDescriptorClass();
            pRasterDescriptor.Create(pRaster, pQueryFilter, "Value");
            //ExtractByAttribute操作
            IExtractionOp pExtractOp = new RasterExtractionOpClass();
            IGeoDataset pGDExtraByAttri = pExtractOp.Attribute(pRasterDescriptor);
            //RegionGroup操作
            IGeneralizeOp pGeneralizeOp = new RasterGeneralizeOpClass();
            var missing = Type.Missing;
            IGeoDataset pGDRegionGroup = pGeneralizeOp.RegionGroup(pGDExtraByAttri, true, true, true, ref missing);
            //访问数据属性表
            IRaster RegionGroupRaster = pGDRegionGroup as IRaster;
            IRasterBandCollection RegionGroupBandCollection = RegionGroupRaster as IRasterBandCollection;
            IRasterBand RegionGroupBand = RegionGroupBandCollection.Item(0);
            ITable RegionGroupTable = RegionGroupBand.AttributeTable;
            //属性表对象的逐条遍历
            IQueryFilter RegionGroupQueryFilter = new QueryFilterClass();
            ICursor RegionGroupCursor = RegionGroupTable.Search(RegionGroupQueryFilter, false);
            IRow RegionGroupRow = RegionGroupCursor.NextRow();
            //逐个对象的提取、8邻域扩充、Plus计算、地表覆盖类型的数值判断
            while (RegionGroupRow != null)
            {
                //提取对象
                int objNO = Convert.ToInt32(RegionGroupRow.get_Value(RegionGroupTable.FindField("Value")));
                //MessageBox.Show("Obj NO." + objNO.ToString());
                IQueryFilter ObjQueryFilter = new QueryFilterClass();
                ObjQueryFilter.WhereClause = "Value = " + Convert.ToString(objNO);
                IRasterDescriptor ObjRasterDescriptor = new RasterDescriptorClass();
                ObjRasterDescriptor.Create(RegionGroupRaster, ObjQueryFilter, "Value");
                IExtractionOp objExtractionOp = new RasterExtractionOpClass();
                IGeoDataset objExtractionGeoDataset = objExtractionOp.Attribute(ObjRasterDescriptor);
                IEnvelope tEnv = objExtractionGeoDataset.Extent;
                IArea tArea = tEnv as IArea;
                if ((sArea.Area / 9) < tArea.Area)
                {
                    //扩展提取出的对象
                    IGeneralizeOp objExpand = new RasterGeneralizeOpClass();
                    object zonelist = new int[] { objNO };
                    //得到扩展后对象的数据集
                    IGeoDataset objExpandGeodataset = objExpand.Expand(objExtractionGeoDataset, 1, ref zonelist);
                    //Plus计算
                    IMathOp objPlus = new RasterMathOpsClass();
                    IGeoDataset objPlusGeodataset = objPlus.Plus(objExpandGeodataset, pRaster as IGeoDataset);
                    int chkVal1 = check_value + objNO; //60
                    int chkVal2 = constrain_containval + objNO; //80 
                    if (ChkMarkPoint.containCorrRel(objPlusGeodataset, chkVal1, chkVal2))
                    {
                        string errdsp = check_value.ToString() + " Contained-by " + constrain_containval.ToString();
                        ChkMarkPoint.labelObjPoint(objExtractionGeoDataset, objNO, errdsp);
                    }
                    RegionGroupRow = RegionGroupCursor.NextRow();
                }
                    
                }
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            MessageBox.Show("Computation time: "+ts.TotalMilliseconds.ToString()+"MS");
            MessageBox.Show("Inconsistent Contain Relation Checking Done.");
        }

        //利用GP处理两幅地表覆盖数据共同接边处的差值计算，为提取出数据噪声点作数据准备
        private void commonEdgeDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //IMathOp接口调用
            IMathOp pMathOp = new RasterMathOpsClass();
            //输入栅格图层，山东分幅为例
            MessageBox.Show("Choose the two neighboring IMG data");
            AutoChooseFile acf = new AutoChooseFile();
            string InputFL01Name = acf.getFileNameandPostfix();
            string InputFL02Name = acf.getFileNameandPostfix();
            //IRasterLayer pInputFL01 = ChkMarkPoint.getRasterLayer("n50_30_2010lc030_9.img");
            IRasterLayer pInputFL01 = ChkMarkPoint.getRasterLayer(InputFL01Name);
            //IRasterLayer pInputFL02 = ChkMarkPoint.getRasterLayer("n50_35_2010lc030_9.img");
            IRasterLayer pInputFL02 = ChkMarkPoint.getRasterLayer(InputFL02Name);
            IRaster pInputRaster01 = pInputFL01.Raster;
            IRaster pInputRaster02 = pInputFL02.Raster;
            IGeoDataset pInputGD01 = pInputRaster01 as IGeoDataset;
            IGeoDataset pInputGD02 = pInputRaster02 as IGeoDataset;
            //图层相减计算
            MessageBox.Show("Common Edge Difference Calculation... ...");
            IGeoDataset pOutGD = pMathOp.Minus(pInputGD01, pInputGD02);
            //保存计算结果
            IRaster pSaveRaster = pOutGD as IRaster;
            ISaveAs pSaveAs = pSaveRaster as ISaveAs;
            MessageBox.Show("Choose Workspace to Store the Diff_operator Data");
            string workspacepath = acf.getWorkspaceFileName();
            //IRasterWorkspace pRasterWorkspace = ChkMarkPoint.setRasterWorkspace(@"F:\temp_exp");
            IRasterWorkspace pRasterWorkspace = ChkMarkPoint.setRasterWorkspace(@workspacepath);
            IWorkspace pWorkspace = pRasterWorkspace as IWorkspace;
            string rename = acf.saveFileName();
            //pSaveAs.SaveAs("dif_n5030n5035_2010edge.img", pWorkspace, "IMAGINE Image");
            pSaveAs.SaveAs(rename, pWorkspace, "IMAGINE Image");
            MessageBox.Show("The Difference Operator Achieved.");
            //输出栅格图层
            //IRasterLayer pOutRL = new RasterLayerClass();
            //pOutRL.CreateFromRaster(pOutGD as IRaster);
            //axMapControl1.AddLayer(pOutRL as ILayer);
        }

        //根据地表覆盖分类的遥感影像数据(已转.bmp)提取出Harris代表类别连接的角点（与Blob特征点区别）
        private void rSFeaturePointsFPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Preliminary: Extracting Remote Sensing Harris Feature Points... ...");
            //加载更新的地表覆盖数据图层，使用MaptoX、MaptoY 
            //IRasterLayer pUR = ChkMarkPoint.getRasterLayer("stretch2010sdlq.tif");
            MessageBox.Show("Choose the Remote Sensing Image Loaded.");
            AutoChooseFile acf = new AutoChooseFile();
            string rasterfilename = acf.getFileNameandPostfix();
            IRasterLayer pUR = ChkMarkPoint.getRasterLayer(rasterfilename);
            IRaster2 pUR2 = pUR.Raster as IRaster2;
            IGeoDataset pGeoDataset = pUR.Raster as IGeoDataset;
            ISpatialReference pSR = pGeoDataset.SpatialReference;
            //获取bmp格式的数字图像
            MessageBox.Show("Choose the .bmp File of Remote Sensing Image Loaded.");
            string bmpfilepath = acf.getFullPathName();
            //Image<Gray, Byte> srcImg = new Image<Gray, Byte>(@"F:\山东临朐县实验区数据\2010RsSdlq\2010sdlq.bmp");
            Image<Gray, Byte> srcImg = new Image<Gray, Byte>(@bmpfilepath);
            //图像Bilateral Smooth平滑处理
            Image<Gray, Byte> srcBilateral = srcImg.SmoothBilatral(15, 35, 35);
            //检测Harris Corner
            HarrisDetector harris = new HarrisDetector();
            harris.Detect(srcBilateral);
            List<System.Drawing.Point> FeaturePoints = new List<System.Drawing.Point>();
            //提取出来的图像Harris特征点，将其存入featurepoints，存储特征点的行、列号point(weight,height)
            harris.GetCorners(FeaturePoints, 0.01); //0.01 - qualitylevel
            //存储FeaturePoints为shp格式文件
            ArrayList VorPoints = new ArrayList();
            for (int i = 0; i < FeaturePoints.Count; i++)
            {
                ESRI.ArcGIS.Geometry.IPoint vorpoint = new PointClass();
                vorpoint.SpatialReference = pSR;
                vorpoint.X = pUR2.ToMapX(FeaturePoints[i].X);
                vorpoint.Y = pUR2.ToMapY(FeaturePoints[i].Y);
                VorPoints.Add(vorpoint);
            }
            ChkMarkPoint.insertVorChkPoints("Voronoi_cglcmark", VorPoints);
            MessageBox.Show("Extracting Voronoi Points from Corners Completely... ...");
        }

        //Corner Voronoi Diagram(CVD) 空间关系计算
        private void constrainedVorAdjacentRuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //已利用ArcGIS的RasterValueToPoints计算提取出来带Range值的Voronoi生长源
            //利用ArcGIS的Spatial Join操作（带Range值的Voronoi生长源 + 其对应的Voronoi图），将Range字段添加至对应的Voronoi图要素字段
            //利用Voronoi的Range不为零字段的空间邻近关系过滤Range=0的Voronoi要素，将其字段修改为1，则剩余的Voronoi要素RANGE值为0的为检查的结果。
            MessageBox.Show("Input Voronoi Reginon File After With Sptial Join Operator");
            AutoChooseFile acf = new AutoChooseFile();
            string VoronoiRegionFile = acf.getFileNameWithoutPostfix();
            IFeatureClass pVorRegionFeatureClass = ChkMarkPoint.getFeatureLayer(VoronoiRegionFile).FeatureClass;
            IQueryFilter pVorRegionQueryFilter = new QueryFilterClass();

            pVorRegionQueryFilter.WhereClause = "RASTERVALU <> 0";
            IFeatureCursor pVorRegionFeatureCursor = pVorRegionFeatureClass.Update(pVorRegionQueryFilter, false);
            IFeature pVorRegionFeature = pVorRegionFeatureCursor.NextFeature();
            while (pVorRegionFeature != null)
            {
                pVorRegionFeature.set_Value(pVorRegionFeature.Fields.FindField("Error"), 1);
                pVorRegionFeatureCursor.UpdateFeature(pVorRegionFeature);
                IPolygon pVorPolygon = pVorRegionFeature.Shape as IPolygon;
                //ISpatialFilter查询
                ISpatialFilter pSpatialFilter = new SpatialFilterClass();
                pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
                pSpatialFilter.Geometry = pVorPolygon as IGeometry;
                IFeatureCursor pSpaFilterCursor = pVorRegionFeatureClass.Update(pSpatialFilter, false);
                IFeature pSpaFeature = pSpaFilterCursor.NextFeature();
                while (pSpaFeature != null)
                {
                    pSpaFeature.set_Value(pSpaFeature.Fields.FindField("Error"), 1);
                    pSpaFilterCursor.UpdateFeature(pSpaFeature);
                    pSpaFeature = pSpaFilterCursor.NextFeature();
                }
                pVorRegionFeature = pVorRegionFeatureCursor.NextFeature();
            }
            MessageBox.Show("Error Field -Inconsistency Checking- Value In Voronoi Region Shapefile Has Been Changed. ");
        }


        //Voronoi区域的Range统计
        private void vorZonalRangeStatisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("LC Voronoi Zonal Range Statistics... ...");
            //LC的Voronoi区域统计计算Zonal Operation
            //区域统计计算接口
            IZonalOp pZonalOp = new RasterZonalOpClass();
            //输入Voronoi区域图层
            MessageBox.Show("Choose Voronoi Zone .shp.");
            AutoChooseFile acf = new AutoChooseFile();
            string Voronoi_Zone = acf.getFileNameWithoutPostfix();
            IGeoDataset pZoneGeoDastset = ChkMarkPoint.getFeatureLayer(Voronoi_Zone).FeatureClass as IGeoDataset;
            //输入地表覆盖数据图层
            MessageBox.Show("Choose Land Cover Zone Data.");
            string Lc_Zone = acf.getFileNameandPostfix();
            IGeoDataset LcZoneGeodataset = ChkMarkPoint.getRasterLayer(Lc_Zone).Raster as IGeoDataset;
            //Voronoi区域统计计算的结果
            IGeoDataset pOutZoneStat = pZonalOp.ZonalStatistics(pZoneGeoDastset, LcZoneGeodataset, esriGeoAnalysisStatisticsEnum.esriGeoAnalysisStatsRange, true);
            //保存计算结果
            IRaster pSaveRaster = pOutZoneStat as IRaster;
            ISaveAs pSaveAs = pSaveRaster as ISaveAs;
            MessageBox.Show("Choose Workspace to Store the ZonalStats Data");
            string workspacepath = acf.getWorkspaceFileName();
            //IRasterWorkspace pRasterWorkspace = ChkMarkPoint.setRasterWorkspace(@"F:\temp_exp");
            IRasterWorkspace pRasterWorkspace = ChkMarkPoint.setRasterWorkspace(@workspacepath);
            IWorkspace pWorkspace = pRasterWorkspace as IWorkspace;
            string rename = acf.saveFileName();
            //pSaveAs.SaveAs("dif_n5030n5035_2010edge.img", pWorkspace, "IMAGINE Image");
            pSaveAs.SaveAs(rename, pWorkspace, "TIFF");
            //输出栅格图层
            MessageBox.Show("-----Voronoi LC Range Statistics Achieved-----");

        }

        private void deleteNoisePointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Choose Points File.");
            AutoChooseFile acf = new AutoChooseFile();
            string PointsFile = acf.getFileNameWithoutPostfix();
            IFeatureClass pFeaturePoints = ChkMarkPoint.getFeatureLayer(PointsFile).FeatureClass;
            MessageBox.Show("Choose Zone File");
            string ZoneFile = acf.getFileNameWithoutPostfix();
            IFeatureClass pFeatureZone = ChkMarkPoint.getFeatureLayer(ZoneFile).FeatureClass;
            IQueryFilter pzQueryFilter = new QueryFilterClass();
            pzQueryFilter.WhereClause = "";
            IFeatureCursor pzCursor = pFeatureZone.Search(pzQueryFilter, false);
            IFeature pzFeature = pzCursor.NextFeature();
            ITopologicalOperator pTopologicalOp = pzFeature.Shape as ITopologicalOperator;
            IGeometry pGeometry = pTopologicalOp.Buffer(-200);
            IPolygon ptGeoZone = pGeometry as IPolygon;
            IRelationalOperator ptRelationOperator = ptGeoZone as IRelationalOperator;

            IQueryFilter pQueryfilter = new QueryFilterClass();
            pQueryfilter.WhereClause = "";
            IFeatureCursor pFeatureCursor = pFeaturePoints.Search(pQueryfilter, false);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                IGeometry pPoint  = pFeature.Shape;
                if (ptRelationOperator.Contains(pPoint as IGeometry) == false)
                {
                    pFeature.Delete();
                }
                pFeature = pFeatureCursor.NextFeature();
            }
            MessageBox.Show("Delete Operator Archieved.");
        }

        private void yypeCompatibleJudgementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Example：Toponym/Geonames about CULT inconsistency Detection");
            //Extraction pixel value to Geoname point
            IExtractionOp2 pExtraOp2 = new RasterExtractionOpClass();
            
            //获取文件路径
            AutoChooseFile acf = new AutoChooseFile();
            MessageBox.Show("Choose Land Cover Data.");
            string rasterLyrName = acf.getFileNameandPostfix();
            IRaster pRaster = ChkMarkPoint.getRasterLayer(rasterLyrName).Raster as IRaster;

            MessageBox.Show("Choose Geonames Shapefile.");
            string shpLyrName = acf.getFileNameWithoutPostfix(); //加载shp文件不带有后缀名
            IFeatureClass pFClass = ChkMarkPoint.getFeatureLayer(shpLyrName).FeatureClass;

            IGeoDataset pGeoDT = pExtraOp2.ExtractValuesToPoints(pFClass as IGeoDataset, pRaster as IGeoDataset, false,true);
            //RASTERVALU
            IFeatureClass pGeoFea = pGeoDT as IFeatureClass;
            IFeatureCursor pFC = pGeoFea.Search(null, false);
            IFeature pFea = pFC.NextFeature();
            ArrayList arr = new ArrayList();
            while(pFea != null)
            {
               long val = Convert.ToInt64(pFea.get_Value(pGeoFea.FindField("RASTERVALU")));
               arr.Add(val);
               pFea = pFC.NextFeature();
            }

            IFeatureCursor pFeaCursor = pFClass.Update(null,false);   //set_value使用FeatureClass.Update()方法
            IFeature pFeature = pFeaCursor.NextFeature();
            int i = 0;
            while(pFeature!=null)
            {
                //将RasterToPoint的值RASTERVALU加入到原始SHP文件的新加字段RasterVal中
                //pFeature.set_Value(pFClass.FindField("RasterVal"),arr[i]);
                if (Convert.ToInt64(arr[i]) == 10)
                {
                    //如果Geoname的CULT对应的LAND COVER值是耕地值10，则将源SHP的ERR设置为1，否则默认为不一致值0
                    pFeature.set_Value(pFClass.FindField("ERR"), 1);
                    pFeaCursor.UpdateFeature(pFeature);
                }
                pFeature = pFeaCursor.NextFeature();
                i = i+1;
            }
            MessageBox.Show("Extract Values to Goenames Points Already Done.");
        }

        private void attriTabShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //创建DataTable，加载属性字段
            AutoChooseFile acf = new AutoChooseFile();
            string filename = acf.getFileNameWithoutPostfix();
            IFields pFields = ChkMarkPoint.getFeatureLayer(filename).FeatureClass.Fields;
            DataTable dt = new DataTable();
            for (int i = 0; i < pFields.FieldCount; i++)
            {
                string fieldname;
                fieldname = pFields.get_Field(i).AliasName;
                dt.Columns.Add(fieldname);
            }
            //加载字段值
            IFeatureCursor pFeaCursor = ChkMarkPoint.getFeatureLayer(filename).Search(null, false);
            IFeature pFeature = pFeaCursor.NextFeature();
            while (pFeature != null)
            {
                string fldValue;
                DataRow dr = dt.NewRow();
                for (int j = 0; j < pFields.FieldCount; j++)
                {
                    string fldname = pFields.get_Field(j).Name;
                    if (fldname == "Shape")
                    {
                        fldValue = Convert.ToString(pFeature.Shape.GeometryType);
                    }
                    else
                    {
                        fldValue = Convert.ToString(pFeature.get_Value(j));
                    }
                    dr[j] = fldValue;
                }
                dt.Rows.Add(dr);
                pFeature = pFeaCursor.NextFeature();
            }
            //绑定数据
            AttributeTab at = new AttributeTab();
            at.dGrideView.DataSource = dt;
            at.Show();
        }

        private void delLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //删除不一致性的标注点图层
            IFeatureLayer pLayer = ChkMarkPoint.getFeatureLayer("cglc_chkmark") as IFeatureLayer;
            axMapControl1.Map.DeleteLayer(pLayer);
            axMapControl1.ActiveView.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            // 取得 MapControl 和 PageLayoutControl 的引用   
            pTocControl = (ITOCControl2)axTOCControl1.Object;
            pMapControl = (IMapControl3)axMapControl1.Object;
            // 创建菜单   
            //pToolMenuMap = new ToolbarMenuClass();
            pToolMenuLayer = new ToolbarMenuClass();
            pToolMenuLayer.AddItem(new DelLayer(), -1, 0, true, esriCommandStyles.esriCommandStyleTextOnly);
            pToolMenuLayer.SetHook(pMapControl);
        }

        private void axTOCControl1_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            if (e.button != 2) return;
            esriTOCControlItem pTocItem = esriTOCControlItem.esriTOCControlItemNone;
            IBasicMap pBasicMap = null;
            ILayer pLayer = null;
            object oIndex = null;
            object other = null;
            this.axTOCControl1.HitTest(e.x, e.y, ref pTocItem, ref pBasicMap, ref pLayer, ref other, ref oIndex);
            if (e.button == 2)
            {
                if (pTocItem == esriTOCControlItem.esriTOCControlItemMap)
                {
                    this.pTocControl.SelectItem(pBasicMap, null);
                }
                else
                {
                    this.pTocControl.SelectItem(pLayer, null);
                }
                //设置CustomProperty为layer (用于自定义的Layer命令)   
                pMapControl.CustomProperty = pLayer;
                //弹出右键菜单   
                if (pTocItem == esriTOCControlItem.esriTOCControlItemMap)
                {
                    pToolMenuMap.PopupMenu(e.x, e.y, this.pTocControl.hWnd);
                }
                if (pTocItem == esriTOCControlItem.esriTOCControlItemLayer)
                {
                    pToolMenuLayer.PopupMenu(e.x, e.y, this.pTocControl.hWnd);
                }
            }  
        }

        private void numberOfTemproalRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double tempcount = 0;
            //功能提示
            MessageBox.Show("Function: Number of Temporal Relation between Pair of Objects.");
            //打开文件选择对话框
            AutoChooseFile acf = new AutoChooseFile();
            MessageBox.Show("Choose Updating Raster Layer");
            //通过文件名获取栅格图层
            string updating_name = acf.getFileNameandPostfix();
            IRasterLayer pURaterLayer = ChkMarkPoint.getRasterLayer(updating_name);

            MessageBox.Show("Choose Base Raster Layer");
            //通过文件名获取栅格图层
            string base_name = acf.getFileNameandPostfix();
            IRasterLayer pBRaterLayer = ChkMarkPoint.getRasterLayer(base_name);

            IRaster pURaster = pURaterLayer.Raster;
            IGeoDataset pUGD = pURaster as IGeoDataset;
            ISpatialReference pSR = pUGD.SpatialReference;
            IRaster pBRaster = pBRaterLayer.Raster;

            IRasterProps pURasterProps = pURaster as IRasterProps;
            IRasterProps pBRasterProps = pBRaster as IRasterProps;

            if (pURasterProps.Height != pBRasterProps.Height || pURasterProps.Width != pBRasterProps.Width)
            {
                MessageBox.Show("Rows and Colums are not equal.");
                return;
            }
            MessageBox.Show("Input land cover type in baseline:");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            int base_type = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            MessageBox.Show("Input land cover type in updating:");
            ci.ShowDialog();
            int update_type = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();

            int u_height = pURasterProps.Height;
            int u_width = pURasterProps.Width;
            double[,] u_pixelVal = new double[u_height,u_width];

            int b_height = pBRasterProps.Height;
            int b_width = pBRasterProps.Width;
            double[,] b_pixelVal = new double[b_height, b_width];

            IPnt blocksize = new PntClass();
            blocksize.SetCoords(256, 256);
            IRaster2 pUR2 = pURaster as IRaster2;
            IRaster2 pBR2 = pBRaster as IRaster2;
            IRasterCursor pURC = pUR2.CreateCursorEx(blocksize);
            IRasterCursor pBRC = pBR2.CreateCursorEx(blocksize);

            System.Array u_pixelblock;
            System.Array b_pixelblock;
            IPixelBlock3 u_PB3;
            IPixelBlock3 b_PB3;
            do
            {
                int uxdis = (int)pURC.TopLeft.X;
                int uydis = (int)pURC.TopLeft.Y;
                u_PB3 = pURC.PixelBlock as IPixelBlock3;
                u_pixelblock = (System.Array)u_PB3.get_PixelData(0);
                for (int i = 0; i < u_PB3.Height; i++)
                {
                    for (int j = 0; j < u_PB3.Width; j++)
                    {
                        u_pixelVal[uydis + i, uxdis + j] = Convert.ToDouble(u_pixelblock.GetValue(j, i));
                    }
                }

            } while (pURC.Next());
            do
            {
                int bxdis = (int)pBRC.TopLeft.X;
                int bydis = (int)pBRC.TopLeft.Y;
                b_PB3 = pBRC.PixelBlock as IPixelBlock3;
                b_pixelblock = (System.Array)b_PB3.get_PixelData(0);
                for (int u = 0; u < b_PB3.Height; u++)
                {
                    for (int v = 0; v < b_PB3.Width; v++)
                    {
                        b_pixelVal[bydis + u, bxdis + v] = Convert.ToDouble(b_pixelblock.GetValue(v, u));
                        if (b_pixelVal[bydis + u, bxdis + v] == base_type && u_pixelVal[bydis + u, bxdis + v] == update_type)
                        {
                            tempcount++;
                        }
                    }
                }
            }while(pBRC.Next());
            MessageBox.Show(tempcount.ToString());
        }

        private void numberToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void adjRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int countt = 0;
            //湿地附近一般有水体存在
            MessageBox.Show("adjRelation Times Counting... ...");
            //IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string touch_filename = acf.getFileNameandPostfix();
            IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer(touch_filename);
            IRaster pRaster = pRasterLayer.Raster;

            //创建一个查询语句
            IQueryFilter pQueryFilter = new QueryFilterClass();
            //查询湿地，湿地代码 = 50
            //int wetNO = 50;
            //pQueryFilter.WhereClause = "Value = "+ Convert.ToString(wetNO);
            MessageBox.Show("Input：Target Type Value");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            string neighbor_type = ci.MessageTextBox.Text;
            ci.MessageTextBox.Clear();

            //邻近约束值
            MessageBox.Show("Input：Constraint Neighborhood Type Value");
            //水体
            ci.ShowDialog();
            int neighborcons_type = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();

            pQueryFilter.WhereClause = "Value = " + neighbor_type;//湿地
            //IRasterDescriptor描述
            IRasterDescriptor pRasterDescriptor = new RasterDescriptorClass();
            pRasterDescriptor.Create(pRaster, pQueryFilter, "Value");
            //ExtractionByAtrribute操作IExtracionOp
            ESRI.ArcGIS.SpatialAnalyst.IExtractionOp pExtractionByAttri = new ESRI.ArcGIS.SpatialAnalyst.RasterExtractionOpClass();
            IGeoDataset pOutGDByAttribute = pExtractionByAttri.Attribute(pRasterDescriptor);
            //Region Group操作IGeneralizeOp RegionGroup 八邻域对象
            ESRI.ArcGIS.SpatialAnalyst.IGeneralizeOp pGeneralizeOp = new ESRI.ArcGIS.SpatialAnalyst.RasterGeneralizeOpClass();
            var missing = Type.Missing;
            IGeoDataset pOutGDRegionGroup = pGeneralizeOp.RegionGroup(pOutGDByAttribute, true, true, true, ref missing);
            //从对象集pOutGDRegionGroup的OID提取出每个邻域对象作为一个Geodataset,Expand操作IGeneralizeOp Expand
            IRaster pRGRaster = pOutGDRegionGroup as IRaster;
            IRasterBandCollection pRGRasterBandCollection = pRGRaster as IRasterBandCollection;
            IRasterBand pRGRasterBand = pRGRasterBandCollection.Item(0);
            ITable pRGTable = pRGRasterBand.AttributeTable;
            //属性表逐条遍历
            IQueryFilter pRGQueryFilter = new QueryFilterClass();
            ICursor pRGCursor = pRGTable.Search(pRGQueryFilter, false);
            IRow pRGRow = pRGCursor.NextRow();
            //邻近关系的判断条件：扩张后的栅格对象与周边像素的差值不全相等
            while (pRGRow != null)
            {
                //提取对象
                int ObjNo = Convert.ToInt32(pRGRow.get_Value(pRGTable.FindField("Value")));
                //MessageBox.Show("Object No." + ObjNo.ToString());

                IQueryFilter ObjQueryFilter = new QueryFilterClass();
                ObjQueryFilter.WhereClause = "Value = " + Convert.ToString(ObjNo);
                IRasterDescriptor objRasterDescriptor = new RasterDescriptorClass();
                objRasterDescriptor.Create(pRGRaster, ObjQueryFilter, "Value");
                IExtractionOp objExtractionOp = new RasterExtractionOpClass();
                IGeoDataset objExGeoDataset = objExtractionOp.Attribute(objRasterDescriptor);
                //对象扩展Expand
                IGeneralizeOp objExpand = new RasterGeneralizeOpClass();
                object zonelist = new int[] { ObjNo };
                //扩展后的对象
                IGeoDataset objExpandGeodataset = objExpand.Expand(objExGeoDataset, 1, ref zonelist);
                // == 60+objNo,扩展后的对象与原图像像素和运算，IMapthOp.plus
                IMathOp pMathPlus = new RasterMathOpsClass();
                IGeoDataset pMathPlustGD = pMathPlus.Plus(objExpandGeodataset, pRaster as IGeoDataset);
                //若计算结果存在60+objNo，则其附近有水体；否则记录为错误分类
                int chkVal = neighborcons_type + ObjNo;
                //对象周围不存在水体，则标注该对象
                if (ChkMarkPoint.adjCorrectRelation(pMathPlustGD, chkVal) == true)
                {
                    countt = countt + 1;
                }
                pRGRow = pRGCursor.NextRow();
            }
            MessageBox.Show("Neighborhood Relation Tiemes Counting: "+countt.ToString());
        }

        private void disjRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int countt = 0;
            //湿地附近一般有水体存在
            MessageBox.Show("disJointRelation Times Counting... ...");
            //IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string touch_filename = acf.getFileNameandPostfix();
            IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer(touch_filename);
            IRaster pRaster = pRasterLayer.Raster;

            //创建一个查询语句
            IQueryFilter pQueryFilter = new QueryFilterClass();
            //查询湿地，湿地代码 = 50
            //int wetNO = 50;
            //pQueryFilter.WhereClause = "Value = "+ Convert.ToString(wetNO);
            MessageBox.Show("Input：Target Type Value");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            string neighbor_type = ci.MessageTextBox.Text;
            ci.MessageTextBox.Clear();

            //邻近约束值
            MessageBox.Show("Input：Constraint Neighborhood Type Value");
            //水体
            ci.ShowDialog();
            int neighborcons_type = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();

            pQueryFilter.WhereClause = "Value = " + neighbor_type;//湿地
            //IRasterDescriptor描述
            IRasterDescriptor pRasterDescriptor = new RasterDescriptorClass();
            pRasterDescriptor.Create(pRaster, pQueryFilter, "Value");
            //ExtractionByAtrribute操作IExtracionOp
            ESRI.ArcGIS.SpatialAnalyst.IExtractionOp pExtractionByAttri = new ESRI.ArcGIS.SpatialAnalyst.RasterExtractionOpClass();
            IGeoDataset pOutGDByAttribute = pExtractionByAttri.Attribute(pRasterDescriptor);
            //Region Group操作IGeneralizeOp RegionGroup 八邻域对象
            ESRI.ArcGIS.SpatialAnalyst.IGeneralizeOp pGeneralizeOp = new ESRI.ArcGIS.SpatialAnalyst.RasterGeneralizeOpClass();
            var missing = Type.Missing;
            IGeoDataset pOutGDRegionGroup = pGeneralizeOp.RegionGroup(pOutGDByAttribute, true, true, true, ref missing);
            //从对象集pOutGDRegionGroup的OID提取出每个邻域对象作为一个Geodataset,Expand操作IGeneralizeOp Expand
            IRaster pRGRaster = pOutGDRegionGroup as IRaster;
            IRasterBandCollection pRGRasterBandCollection = pRGRaster as IRasterBandCollection;
            IRasterBand pRGRasterBand = pRGRasterBandCollection.Item(0);
            ITable pRGTable = pRGRasterBand.AttributeTable;
            //属性表逐条遍历
            IQueryFilter pRGQueryFilter = new QueryFilterClass();
            ICursor pRGCursor = pRGTable.Search(pRGQueryFilter, false);
            IRow pRGRow = pRGCursor.NextRow();
            //邻近关系的判断条件：扩张后的栅格对象与周边像素的差值不全相等
            while (pRGRow != null)
            {
                //提取对象
                int ObjNo = Convert.ToInt32(pRGRow.get_Value(pRGTable.FindField("Value")));
                //MessageBox.Show("Object No." + ObjNo.ToString());

                IQueryFilter ObjQueryFilter = new QueryFilterClass();
                ObjQueryFilter.WhereClause = "Value = " + Convert.ToString(ObjNo);
                IRasterDescriptor objRasterDescriptor = new RasterDescriptorClass();
                objRasterDescriptor.Create(pRGRaster, ObjQueryFilter, "Value");
                IExtractionOp objExtractionOp = new RasterExtractionOpClass();
                IGeoDataset objExGeoDataset = objExtractionOp.Attribute(objRasterDescriptor);
                //对象扩展Expand
                IGeneralizeOp objExpand = new RasterGeneralizeOpClass();
                object zonelist = new int[] { ObjNo };
                //扩展后的对象
                IGeoDataset objExpandGeodataset = objExpand.Expand(objExGeoDataset, 1, ref zonelist);
                // == 60+objNo,扩展后的对象与原图像像素和运算，IMapthOp.plus
                IMathOp pMathPlus = new RasterMathOpsClass();
                IGeoDataset pMathPlustGD = pMathPlus.Plus(objExpandGeodataset, pRaster as IGeoDataset);
                //若计算结果存在60+objNo，则其附近有水体；否则记录为错误分类
                int chkVal = neighborcons_type + ObjNo;
                //对象周围不存在水体，则标注该对象
                if (ChkMarkPoint.disJointRelation(pMathPlustGD, chkVal) == true)
                {
                    countt = countt + 1;
                }
                pRGRow = pRGCursor.NextRow();
            }
            MessageBox.Show("Disjoint Relation Tiemes Counting: " + countt.ToString());
        }

        private void conRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int ccount = 0;
            MessageBox.Show("Contained-by Relationship Counting... ...");
            //IRasterLayer pFeatureLayer = ChkMarkPoint.getRasterLayer("2010glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string contain_filename = acf.getFileNameandPostfix();
            IRasterLayer pFeatureLayer = ChkMarkPoint.getRasterLayer(contain_filename);
            IRaster pRaster = pFeatureLayer.Raster;
            //查询水体
            MessageBox.Show("Input: Target Checking Type Value");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            int check_value = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            MessageBox.Show("Input: Contained-by Type Value");
            ci.ShowDialog();
            int constrain_containval = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            //计算算法运行时间

            IQueryFilter pQueryFilter = new QueryFilterClass();
            pQueryFilter.WhereClause = "Value = " + check_value.ToString();
            //IRasterDescriptor描述
            IRasterDescriptor pRasterDescriptor = new RasterDescriptorClass();
            pRasterDescriptor.Create(pRaster, pQueryFilter, "Value");
            //ExtractByAttribute操作
            IExtractionOp pExtractOp = new RasterExtractionOpClass();
            IGeoDataset pGDExtraByAttri = pExtractOp.Attribute(pRasterDescriptor);
            //RegionGroup操作
            IGeneralizeOp pGeneralizeOp = new RasterGeneralizeOpClass();
            var missing = Type.Missing;
            IGeoDataset pGDRegionGroup = pGeneralizeOp.RegionGroup(pGDExtraByAttri, true, true, true, ref missing);
            //访问数据属性表
            IRaster RegionGroupRaster = pGDRegionGroup as IRaster;
            IRasterBandCollection RegionGroupBandCollection = RegionGroupRaster as IRasterBandCollection;
            IRasterBand RegionGroupBand = RegionGroupBandCollection.Item(0);
            ITable RegionGroupTable = RegionGroupBand.AttributeTable;
            //属性表对象的逐条遍历
            IQueryFilter RegionGroupQueryFilter = new QueryFilterClass();
            ICursor RegionGroupCursor = RegionGroupTable.Search(RegionGroupQueryFilter, false);
            IRow RegionGroupRow = RegionGroupCursor.NextRow();
            //逐个对象的提取、8邻域扩充、Plus计算、地表覆盖类型的数值判断
            while (RegionGroupRow != null)
            {
                //提取对象
                int objNO = Convert.ToInt32(RegionGroupRow.get_Value(RegionGroupTable.FindField("Value")));
                //MessageBox.Show("Obj NO." + objNO.ToString());
                IQueryFilter ObjQueryFilter = new QueryFilterClass();
                ObjQueryFilter.WhereClause = "Value = " + Convert.ToString(objNO);
                IRasterDescriptor ObjRasterDescriptor = new RasterDescriptorClass();
                ObjRasterDescriptor.Create(RegionGroupRaster, ObjQueryFilter, "Value");
                IExtractionOp objExtractionOp = new RasterExtractionOpClass();
                IGeoDataset objExtractionGeoDataset = objExtractionOp.Attribute(ObjRasterDescriptor);
                //扩展提取出的对象
                IGeneralizeOp objExpand = new RasterGeneralizeOpClass();
                object zonelist = new int[] { objNO };
                //得到扩展后对象的数据集
                IGeoDataset objExpandGeodataset = objExpand.Expand(objExtractionGeoDataset, 1, ref zonelist);
                //Plus计算
                IMathOp objPlus = new RasterMathOpsClass();
                IGeoDataset objPlusGeodataset = objPlus.Plus(objExpandGeodataset, pRaster as IGeoDataset);
                int chkVal1 = check_value + objNO; //60
                int chkVal2 = constrain_containval + objNO; //80 
                if (ChkMarkPoint.containCorrRel(objPlusGeodataset, chkVal1, chkVal2))
                {
                    ccount = ccount + 1;
                }
                RegionGroupRow = RegionGroupCursor.NextRow();
            }
            MessageBox.Show("Contained-by Relation Counting: "+ccount.ToString());
        }

        //检查湿地附近有水体的湿地错误
        private void disjointRelationDRToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Inconsistent Neighborhood Relation Checking... ...");
            //IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer("2000glc_sdlq.tif");
            AutoChooseFile acf = new AutoChooseFile();
            string touch_filename = acf.getFileNameandPostfix();
            IRasterLayer pRasterLayer = ChkMarkPoint.getRasterLayer(touch_filename);
            IRaster pRaster = pRasterLayer.Raster;

            //创建一个查询语句
            IQueryFilter pQueryFilter = new QueryFilterClass();
            //查询湿地，湿地代码 = 50
            //int wetNO = 50;
            //pQueryFilter.WhereClause = "Value = "+ Convert.ToString(wetNO);
            MessageBox.Show("Input：Checking Type Value");
            ChangeInformation ci = new ChangeInformation();
            ci.ShowDialog();
            string neighbor_type = ci.MessageTextBox.Text;
            ci.MessageTextBox.Clear();

            //邻近约束值
            MessageBox.Show("Input：Constraint Neighborhood Checking Type Value");
            ci.ShowDialog();
            int neighborcons_type = Convert.ToInt32(ci.MessageTextBox.Text);
            ci.MessageTextBox.Clear();
            //计算算法运行时间
            Stopwatch sw = new Stopwatch();
            sw.Start();

            pQueryFilter.WhereClause = "Value = " + neighbor_type;
            //IRasterDescriptor描述
            IRasterDescriptor pRasterDescriptor = new RasterDescriptorClass();
            pRasterDescriptor.Create(pRaster, pQueryFilter, "Value");
            //ExtractionByAtrribute操作IExtracionOp
            ESRI.ArcGIS.SpatialAnalyst.IExtractionOp pExtractionByAttri = new ESRI.ArcGIS.SpatialAnalyst.RasterExtractionOpClass();
            IGeoDataset pOutGDByAttribute = pExtractionByAttri.Attribute(pRasterDescriptor);
            //Region Group操作IGeneralizeOp RegionGroup 八邻域对象
            ESRI.ArcGIS.SpatialAnalyst.IGeneralizeOp pGeneralizeOp = new ESRI.ArcGIS.SpatialAnalyst.RasterGeneralizeOpClass();
            var missing = Type.Missing;
            IGeoDataset pOutGDRegionGroup = pGeneralizeOp.RegionGroup(pOutGDByAttribute, true, true, true, ref missing);
            //从对象集pOutGDRegionGroup的OID提取出每个邻域对象作为一个Geodataset,Expand操作IGeneralizeOp Expand
            IRaster pRGRaster = pOutGDRegionGroup as IRaster;
            IRasterBandCollection pRGRasterBandCollection = pRGRaster as IRasterBandCollection;
            IRasterBand pRGRasterBand = pRGRasterBandCollection.Item(0);
            ITable pRGTable = pRGRasterBand.AttributeTable;
            //属性表逐条遍历
            IQueryFilter pRGQueryFilter = new QueryFilterClass();
            ICursor pRGCursor = pRGTable.Search(pRGQueryFilter, false);
            IRow pRGRow = pRGCursor.NextRow();
            //邻近关系的判断条件：扩张后的栅格对象与周边像素的差值不全相等
            while (pRGRow != null)
            {
                //提取对象
                int ObjNo = Convert.ToInt32(pRGRow.get_Value(pRGTable.FindField("Value")));
                //MessageBox.Show("Object No." + ObjNo.ToString());

                IQueryFilter ObjQueryFilter = new QueryFilterClass();
                ObjQueryFilter.WhereClause = "Value = " + Convert.ToString(ObjNo);
                IRasterDescriptor objRasterDescriptor = new RasterDescriptorClass();
                objRasterDescriptor.Create(pRGRaster, ObjQueryFilter, "Value");
                IExtractionOp objExtractionOp = new RasterExtractionOpClass();
                IGeoDataset objExGeoDataset = objExtractionOp.Attribute(objRasterDescriptor);
                //对象扩展Expand
                IGeneralizeOp objExpand = new RasterGeneralizeOpClass();
                object zonelist = new int[] { ObjNo };
                //扩展后的对象
                IGeoDataset objExpandGeodataset = objExpand.Expand(objExGeoDataset, 1, ref zonelist);
                // == 60+objNo,扩展后的对象与原图像像素和运算，IMapthOp.plus
                IMathOp pMathPlus = new RasterMathOpsClass();
                IGeoDataset pMathPlustGD = pMathPlus.Plus(objExpandGeodataset, pRaster as IGeoDataset);
                //若计算结果存在60+objNo，则其附近有水体；否则记录为错误分类
                int chkVal = neighborcons_type + ObjNo;
                //对象周围不存在水体，则标注该对象——————相离关系
                if (ChkMarkPoint.disJointRelation(pMathPlustGD, chkVal) == false)
                {
                    string errdsp = neighbor_type + " non-adjacent with " + neighborcons_type.ToString();
                    ChkMarkPoint.labelObjPoint(objExGeoDataset, ObjNo, errdsp);
                }
                pRGRow = pRGCursor.NextRow();
            }

            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            MessageBox.Show("Computation time: " + ts.TotalMilliseconds.ToString() + "MS");

            MessageBox.Show("Inconsistent Neighborhood Relation Checking Done.");
        }
       }

     }

