﻿using KMeansProject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace KMeansGUI
{
    public partial class frmImageSegmentation : Form
    {
        private KMeans objKMeans;
        private List<double[]> dataSetList;
        private BackgroundWorker objBackgroundWorker;
        private KMeansEventArgs kmeansEA;
        private EuclideanDistance objDistance;

        private Bitmap bmpImage;

        public frmImageSegmentation()
        {
            InitializeComponent();
            dataSetList = new List<double[]>();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string path = System.IO.Path.GetDirectoryName(asm.Location);
            string imageFilePath = Path.Combine(path, "cameraman.jpg");
            bmpImage = new Bitmap(imageFilePath);
            picImage.Image = bmpImage;
            for(int i = 0; i < bmpImage.Height; i++)
            {
                for(int j = 0; j < bmpImage.Width; j++)
                {
                    Color c = bmpImage.GetPixel(i, j);
                    double[] pixelArray = new double[] { c.R, c.G, c.B };
                    dataSetList.Add(pixelArray);
                }
            }

            objDistance = new EuclideanDistance();
            objKMeans = new KMeans((int)numericUpDown1.Value, new EuclideanDistance());

            objBackgroundWorker = new BackgroundWorker();
            objBackgroundWorker.WorkerReportsProgress = true;
            objBackgroundWorker.DoWork += ObjBackgroundWorker_DoWork;
            objBackgroundWorker.RunWorkerCompleted += ObjBackgroundWorker_RunWorkerCompleted;
            objBackgroundWorker.ProgressChanged += ObjBackgroundWorker_ProgressChanged;

            objBackgroundWorker.RunWorkerAsync(dataSetList.ToArray());
        }

        private void ObjBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            double[][] inputDataset = e.Argument as double[][];
            objKMeans.UpdateProgress += (x, y) => {
                objBackgroundWorker.ReportProgress(0, y);
            };
            e.Result = objKMeans.Run(inputDataset);
        }
        private void ObjBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Centroid[] centroids = e.Result as Centroid[];
            Console.WriteLine("Work is done!");
            button1.Enabled = true;

            UpdateImage(centroids);
        }

        private void ObjBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            System.Console.WriteLine(">>> Progress Changed");
            kmeansEA = e.UserState as KMeansEventArgs;
            if (kmeansEA != null)
            {
                foreach (Centroid centroid in kmeansEA.CentroidList)
                {
                    System.Console.WriteLine("Centroid: " + centroid.ToString());
                }
            }

            UpdateImage(kmeansEA.CentroidList.ToArray());
        }

        private void UpdateImage(Centroid[] centroids)
        {
            Bitmap bmpNew = new Bitmap(bmpImage.Width, bmpImage.Height);
            for (int i = 0; i < bmpImage.Width; i++)
            {
                for (int j = 0; j < bmpImage.Width; j++)
                {
                    Color c = bmpImage.GetPixel(i, j);
                    double[] pixelArray = new double[] { c.R, c.G, c.B };

                    double minDistance = Double.MaxValue;
                    Centroid centroidWinner = null;
                    foreach (Centroid centroid in centroids)
                    {
                        double distance = objDistance.Run(pixelArray, centroid.Array);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            centroidWinner = centroid;
                        }
                    }

                    bmpNew.SetPixel(i, j, centroidWinner.CentroidColor);
                }
            }
            //bmpNew.Save(if you want to save the path, ImageFormat.Jpeg);
            picImage.Image = bmpNew;
        }
    }
}
