using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET.WindowsForms;
using GMap.NET;

namespace UAVLogViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // GMapControl oluşturuluyor ve özellikler ayarlanıyor
            this.gMapControl = new GMap.NET.WindowsForms.GMapControl();
            this.SuspendLayout();

            // gMapControl (Harita kontrolü)
            this.gMapControl.Dock = System.Windows.Forms.DockStyle.Left;  // Haritayı sol tarafa yaslıyoruz
            this.gMapControl.Width = 500;  // Harita genişliği 500px olacak
            this.gMapControl.MinZoom = 0;
            this.gMapControl.MaxZoom = 24;
            this.gMapControl.Location = new System.Drawing.Point(0, 0);
            this.gMapControl.Margin = new System.Windows.Forms.Padding(4);
            this.gMapControl.Name = "gMapControl";
            this.gMapControl.Size = new System.Drawing.Size(500, 600);  // Boyutlandırma
            this.gMapControl.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            this.gMapControl.Zoom = 10;

            // Chart (Grafik)
            this.chart1.Location = new System.Drawing.Point(510, 100); // Grafiği sağ tarafa yerleştiriyoruz
            this.chart1.Size = new System.Drawing.Size(800, 300);  // Chart boyutu
            this.chart1.Name = "chart1";

            // ListBox (Veri listesi)
            this.listBox1.Location = new System.Drawing.Point(510, 400); // ListBox'ı alttan yerleştiriyoruz
            this.listBox1.Size = new System.Drawing.Size(800, 200); // ListBox boyutu

            // Form1 (Formu tamamlıyoruz)
            this.Controls.Add(this.gMapControl);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.listBox1);
            this.Name = "Form1";
            this.Text = "UAV Log Viewer";
            this.ResumeLayout(false);

            // Arka planda yapılacak işlemleri tanımlıyoruz
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;

            // ProgressBar'ı güncellemeye izin veriyoruz
            backgroundWorker.WorkerReportsProgress = true;
        }

        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        private List<int> dataList = new List<int>(); // Sınıf düzeyinde veri listesi
        private GMap.NET.WindowsForms.GMapControl gMapControl; // GMapControl için alan

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                MessageBox.Show("The previous operation is still running. Please wait for it to complete.");
                return; // Eğer işlemi başlatmaya çalışıyorsanız, mevcut işlem devam ediyorsa yeni işlem başlatılmayacak
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
            openFileDialog.Title = "Select a .bin File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"File Selected: {filePath}");

                // Arka planda işlemi başlatıyoruz
                backgroundWorker.RunWorkerAsync(filePath);  // Dosya yolunu gönderiyoruz
            }
        }

        // BackgroundWorker'ın DoWork metodunu kullanarak işlemi başlatıyoruz
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string filePath = (string)e.Argument;
            List<int> tempDataList = new List<int>();

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        long totalLength = reader.BaseStream.Length;
                        long currentPosition = 0;
                        long maxRecords = 1000; // Okunacak maksimum veri sayısı

                        while (reader.BaseStream.Position < totalLength && tempDataList.Count < maxRecords)
                        {
                            int data = reader.ReadInt32(); // Örneğin her veriyi int32 olarak okuyoruz
                            tempDataList.Add(data);

                            // İlerlemeyi raporluyoruz
                            currentPosition = reader.BaseStream.Position;
                            int progressPercentage = (int)((currentPosition * 100) / totalLength);
                            backgroundWorker.ReportProgress(progressPercentage);
                        }
                    }
                }

                e.Result = tempDataList; // Okunan veriyi sonuç olarak gönderiyoruz
            }
            catch (Exception ex)
            {
                e.Result = "Error: " + ex.Message; // Hata mesajı gönder
            }
        }

        // Arka planda işlemi tamamladıktan sonra bu metodu çalıştırıyoruz
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("Error: " + e.Error.Message);
            }
            else
            {
                if (e.Result is List<int> tempDataList)
                {
                    dataList = tempDataList;

                    // ListBox'ı temizliyoruz ve verileri ekliyoruz
                    listBox1.Items.Clear();
                    foreach (var data in dataList)
                    {
                        listBox1.Items.Add(data);
                    }

                    // Chart'ı temizliyoruz ve verileri ekliyoruz
                    chart1.Series[0].Points.Clear();
                    foreach (var data in dataList)
                    {
                        chart1.Series[0].Points.AddY(data);
                    }
                }
                else
                {
                    MessageBox.Show("File processing failed!");
                }
            }

            // ProgressBar'ı sıfırlıyoruz
            progressBar1.Value = 0;
        }

        // ProgressChanged ile ilerleme yüzdesini güncelleme
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // ProgressBar'ı güncelliyoruz
            progressBar1.Value = e.ProgressPercentage;
        }

        // Filtreleme işlemi
        private void btnFilterData_Click(object sender, EventArgs e)
        {
            try
            {
                // Kullanıcıdan min ve max değerlerini alıyoruz
                int minValue = int.Parse(txtMinValue.Text);
                int maxValue = int.Parse(txtMaxValue.Text);

                // Filtrelenmiş verileri yeni bir listeye alalım
                List<int> filteredData = dataList.Where(x => x >= minValue && x <= maxValue).ToList();

                // ListBox'ı temizliyoruz ve filtrelenmiş verileri ekliyoruz
                listBox1.Items.Clear();
                foreach (var data in filteredData)
                {
                    listBox1.Items.Add(data);
                }

                // Chart'ı temizleyip, filtrelenmiş verileri ekliyoruz
                chart1.Series[0].Points.Clear();
                chart1.Series[0].Name = "Filtered Data"; // Filtreli veriler
                foreach (var data in filteredData)
                {
                    chart1.Series[0].Points.AddY(data);
                }

                MessageBox.Show("Data filtered successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering data: {ex.Message}");
            }
        }

        private void DrawFlightRoute(List<GMap.NET.PointLatLng> points)
        {
            // Harita üzerinde yol çizmek için poligon ekliyoruz
            GMap.NET.WindowsForms.GMapPolygon routePolygon = new GMap.NET.WindowsForms.GMapPolygon(points, "FlightRoute");
            GMap.NET.WindowsForms.GMapOverlay routeOverlay = new GMap.NET.WindowsForms.GMapOverlay("Route");
            routeOverlay.Polygons.Add(routePolygon);
            gMapControl.Overlays.Add(routeOverlay);
        }
    }
}
