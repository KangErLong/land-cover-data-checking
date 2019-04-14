using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Drawing;

namespace Globe30Chk
{
    class HarrisDetector
    {
        //32-bit float image of corner strength
        private Image<Gray, float> _CornerStrength;
        //32-bit float image of thresholded corner
        private Image<Gray, float> _CornerTh;
        //size of neighborhood for derivatives smoothing
        int _Neighborhood;
        //aperture for gradient computation
        int _Aperture;
        //Harris parameter
        double _K;
        //maximum strength for threshold computation
        double _MaxStrength;
        //calculated threshold
        double _Threshold;
        /// <summary>
        /// HarrisDetector构造器
        /// </summary>
        public HarrisDetector()
        {
            this._Neighborhood = 5;
            this._Aperture = 3;
            this._K = 0.05;
            this._MaxStrength = 0.0;
            this._Threshold = 0.05;
        }
        /// <summary>
        /// Compute Harris Conrner
        /// </summary>
        /// <param name="img">Source Image</param>
        public void Detect(Image<Gray, Byte> img)
        {
            this._CornerStrength = new Image<Gray, float>(img.Size);
            //Harris computation
            CvInvoke.CornerHarris(
                img,
                this._CornerStrength,
                this._Aperture,
                this._Neighborhood,
                this._K);
            //internal threshold computation
            double[] maxStrength;
            double[] minStrength;  //not used
            Point[] minPoints;  //not used
            Point[] maxPoints;  //not used
            this._CornerStrength.MinMax(out minStrength, out maxStrength, out minPoints, out maxPoints);
            this._MaxStrength = maxStrength[0];
        }
        /// <summary>
        /// Get the Corner Map from the Computed Harris Value
        /// </summary>
        /// <param name="qualityLevel">Harris Values</param>
        /// <returns>CornerMap</returns>
        public Image<Gray, Byte> GetCornerMap(double qualityLevel)
        {
            Image<Gray, Byte> CornerMap;
            //thresholding the corner strengh
            this._Threshold = qualityLevel * this._MaxStrength;
            //图像二值化，大于This._Threshold的像素值赋值为白色255
            this._CornerTh = this._CornerStrength.ThresholdBinary(new Gray(this._Threshold), new Gray(255));
            //convert to 8-bit image
            CornerMap = this._CornerTh.Convert<Gray, Byte>();
            return CornerMap;
        }

        /// <summary>
        /// get the feature points from the computed Harris value
        /// </summary>
        /// <param name="cornerPoints">feature points</param>
        /// <param name="qualitylevel">Harris value</param>
        public void GetCorners(List<Point> cornerPoints, double qualitylevel)
        {
            Image<Gray, Byte> cornerMap = GetCornerMap(qualitylevel);
            GetCorners(cornerPoints, cornerMap);

        }
        //get the feature points from the computed corner map
        void GetCorners(List<Point> cornerPoints, Image<Gray, Byte> cornerMap)
        {
            //Iterate over the pixels to obtain all features
            for (int h = 0; h < cornerMap.Height; h++)
            {
                for (int w = 0; w < cornerMap.Width; w++)
                {
                    //if it is a feature point
                    if (cornerMap[h, w].Intensity > 0)
                    {
                        cornerPoints.Add(new Point(w, h));
                    }
                }
            }
        }
        /// <summary>
        /// Draw circle at feature point location on a image
        /// </summary>
        /// <param name="image">image</param>
        /// <param name="Points">feature point</param>
        public void DrawFeaturePoints(Image<Gray, Byte> image, List<Point> Points)
        {
            //for all corners
            foreach (Point point in Points)
            {
                //draw a circle at each corner location
                CircleF circle = new CircleF(new PointF(point.X, point.Y), 3);
                image.Draw(circle, new Gray(255), 1);
            }
        }
    }
}
