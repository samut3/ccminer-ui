using ccminer_gui.Entities;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Serialization;
using System.Net;
using Newtonsoft.Json;
using System.Globalization;
using System.Diagnostics;

namespace ccminer_gui
{
    public partial class Miner : Form
    {
        int lastAcceptedShares;
        int lastRejectedShares;

        public Miner()
        {
            InitializeComponent();

            _miner = new CCMinerInterop();
            _miner.OutputDataReceived += _miner_OutputDataReceived;
            _miner.ErrorDataReceived += _miner_ErrorDataReceived;

            timer1.Enabled = true;
            timer1.Start();

            SetupUI();
        }

        private void AddItem(string data)
        {
            Invoke((MethodInvoker)(() =>
            {
                listBox1.Items.Add(data);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.ClearSelected();
                if (listBox1.Items.Count > 100)
                {
                    listBox1.Items.RemoveAt(0);
                }
            }));
        }

        private void ParseReport(MinerReport report)
        {
            Invoke((MethodInvoker)(() =>
            {
                acceptedSharesLabel.Text = report.AcceptedShares.ToString();
                totalSharesLabel.Text = report.TotalShares.ToString();
                staleSharesLabel.Text = report.StaleShares.ToString();

                totalHashrateLabel.Text = report.TotalHashrate.ToString() + " MH/s";
                blockDifficultyLabel.Text = report.BlockDifficulty.ToString();
                stratumDifficultyLabel.Text = report.StratumDifficulty.ToString();
                blockLabel.Text = report.Block.ToString();
                pingLabel.Text = report.Ping.ToString() + " ms";
            }));
        }

        private void _miner_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            AddItem(e.Data);
        }

        private void _miner_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            AddItem(e.Data);
        }

        private CCMinerInterop _miner;
        private CustomConfig _customConfig;

        private bool _isStarted = false;

        private string _path = Path.Combine(Environment.CurrentDirectory, "guiminer.conf");
        private BinaryFormatter _bin = new BinaryFormatter();

        private void SetupUI()
        {
            // Set all algorithms.
            foreach (var algorithm in _miner.GetList())
            {
                algorithmBox.Items.Add(algorithm);
            }

            if (!File.Exists(_path))
            {
                var dc = new DefaultConfig();
                algorithmBox.Text = dc.Algorithm;
                intensityBox.Text = dc.Intensity;
                gpuStatsBox.Value = dc.StatsAvg;
                usernameBox.Text = dc.Username;
                passwordBox.Text = dc.Password;
                poolUrlBox.Text = dc.PoolUrl;
                extraArgumentsBox.Text = dc.ExtraArgs;
            }
            else
            {
                using (FileStream fs = new FileStream(_path, FileMode.Open)) //double check that...
                {
                    var dc = (CustomConfig)_bin.Deserialize(fs);
                    algorithmBox.Text = dc.Algorithm;
                    intensityBox.Text = dc.Intensity;
                    gpuStatsBox.Value = dc.StatsAvg;
                    usernameBox.Text = dc.Username;
                    passwordBox.Text = dc.Password;
                    poolUrlBox.Text = dc.PoolUrl;
                    extraArgumentsBox.Text = dc.ExtraArgs;
                }
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            lastAcceptedShares = 0;
            lastRejectedShares = 0;

            _customConfig = new CustomConfig();
            _customConfig.SetConfig(intensityBox.Text, usernameBox.Text, passwordBox.Text, poolUrlBox.Text, Convert.ToInt32(gpuStatsBox.Value), algorithmBox.Text, extraArgumentsBox.Text);

            using (FileStream fs = new FileStream(_path, FileMode.Create))
            {
                _bin.Serialize(fs, _customConfig);
            }

            _isStarted = !_isStarted;

            if (!idleBox.Checked)
            {
                if (!_miner.IsRunning)
                {
                    AddItem("----------- Starting Miner -----------");
                    _miner.Run(_customConfig);
                }
            }
            else
            {
                if (!_miner.IsRunning && _isStarted)
                {
                    AddItem("----------- Waiting to Start Miner for PC Idle -----------");
                }
            }

            if (_isStarted)
            {
                startButton.Text = "Stop";
                // Disable UI items.
                algorithmBox.Enabled = false;
                intensityBox.ReadOnly = true;
                gpuStatsBox.ReadOnly = true;
                usernameBox.ReadOnly = true;
                passwordBox.ReadOnly = true;
                poolUrlBox.ReadOnly = true;
                extraArgumentsBox.ReadOnly = true;
                idleBox.Enabled = false;
            }
            else
            {
                _miner.Stop();
                _miner.ResetMinerReport();
                AddItem("----------- Stopped Miner -----------");
                startButton.Text = "Start";
                algorithmBox.Enabled = true;
                intensityBox.ReadOnly = false;
                gpuStatsBox.ReadOnly = false;
                usernameBox.ReadOnly = false;
                passwordBox.ReadOnly = false;
                poolUrlBox.ReadOnly = false;
                extraArgumentsBox.ReadOnly = false;
                idleBox.Enabled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Check status.
            statusLabel.Text = (_miner.IsRunning ? "Running." : "Stopped.");

            if (idleBox.Checked && _isStarted)
            {
                if (Win32.GetIdleTime() > (2 * 60 * 1000))
                {
                    if (!_miner.IsRunning)
                    {
                        AddItem("----------- Starting Miner (Detected PC Idle) -----------");
                        _miner.Run(_customConfig);
                    }
                }
                else
                {
                    if (_miner.IsRunning)
                    {
                        _miner.Stop();
                        _miner.ResetMinerReport();
                        AddItem("----------- Stopped Miner (Detected PC Input) -----------");
                    }
                }
            }
            else if (!idleBox.Checked && _isStarted)
            {
                // Crash checker.
                if (!_miner.IsRunning)
                {
                    AddItem("----------- Starting Miner (Previous Crashed?) -----------");
                    _miner.Run(_customConfig);
                }
            }

            // No matter what, just parse the report.
            if (_isStarted)
            {
                ParseReport(_miner.GetMinerReport());
            }
        }

        private void Miner_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_miner.IsRunning && _isStarted)
            {
                _isStarted = false;
                _miner.Stop();
                _miner.ResetMinerReport();
            }
        }

        private void Miner_Load(object sender, EventArgs e)
        {
            //for initialising the chart (otherwise strange artifacts show up)
            chaStat.Series[0].Points.Add(new DataPoint(DateTime.Now.ToOADate(), 0));
            chaStat.Series[1].Points.Add(new DataPoint(DateTime.Now.ToOADate(), 0));
            chaStat.Series[2].Points.Add(new DataPoint(DateTime.Now.ToOADate(), 0));

            chartTimer.Tick += ChartTimer_Tick;
            chartTimer.Interval = 60000;

            chartTimer.Start();
        }


        private void ChartTimer_Tick(object sender, EventArgs e)
        {
            var report = _miner.GetMinerReport();

            var time = DateTime.Now;

            int acceptedShares = report.AcceptedShares - lastAcceptedShares;
            int rejectedShares = report.StaleShares - lastRejectedShares;

            if (acceptedShares > 0)
            {
                chaStat.Series[0].Points.Add(new DataPoint(time.ToOADate(), acceptedShares));
            }

            if (rejectedShares > 0)
            {
                chaStat.Series[1].Points.Add(new DataPoint(time.ToOADate(), rejectedShares));
            }

            int count = chaStat.Series[0].Points.Count;
            int start = chaStat.Series[0].Points.Count - 5;

            start = start < 0 ? 0 : start;
            double avg = 0;
            int found = 5;
            DateTime avgStart = time.AddMinutes(-5);
            for (int i = start; i < count; i++)
            {
                if (chaStat.Series[0].Points[i].XValue >= avgStart.ToOADate())
                {
                    avg += chaStat.Series[0].Points[i].YValues[0];
                }
            }
            avg = avg / found;
            chaStat.Series[3].Points.Add(new DataPoint(time.ToOADate(), avg));

            DateTime scroll = time.AddHours(-1);
            chaStat.ChartAreas[0].AxisX.Minimum = scroll.ToOADate();
            chaStat.ChartAreas[0].AxisX.Maximum = DateTime.Now.AddMinutes(1).ToOADate();

            foreach (Series ser in chaStat.Series)
            {
                while (ser.Points.Count > 60)
                {
                    ser.Points.RemoveAt(0);
                }
            }


            lastAcceptedShares = report.AcceptedShares;
            lastRejectedShares = report.StaleShares;
        }

        private async void rvnTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                DateTime time = DateTime.Now;
                time = time.AddSeconds(time.Second * -1);
                DateTime scroll = time.AddDays(-1);

                //WebClient client = new WebClient();
                //string data = client.DownloadString("https://api.coingecko.com/api/v3/coins/ravencoin?localization=false");

                string data = await GetCoinGeckoAPI("ravencoin");

                dynamic dyn = JsonConvert.DeserializeObject(data);

                if (dyn.market_data != null)
                {
                    if (dyn.market_data.current_price != null)
                    {
                        if (dyn.market_data.current_price.usd != null)
                        {
                            string value = (string)dyn.market_data.current_price.usd;
                            double dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

                            chaRVN.Series[0].Points.Add(new DataPoint(time.ToOADate(), dValue));

                            rvnPriceLbl.Text = string.Format("${0:F5}", dValue);
                        }
                    }
                }

                data = await GetCoinGeckoAPI("bitcoin");

                dyn = JsonConvert.DeserializeObject(data);

                if (dyn.market_data != null)
                {
                    if (dyn.market_data.current_price != null)
                    {
                        if (dyn.market_data.current_price.usd != null)
                        {
                            string value = (string)dyn.market_data.current_price.usd;
                            double dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

                            btcPriceLbl.Text = string.Format("${0:F1}", dValue);
                        }
                    }
                }

                data = await GetCoinGeckoAPI("litecoin");

                dyn = JsonConvert.DeserializeObject(data);

                if (dyn.market_data != null)
                {
                    if (dyn.market_data.current_price != null)
                    {
                        if (dyn.market_data.current_price.usd != null)
                        {
                            string value = (string)dyn.market_data.current_price.usd;
                            double dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

                            ltcPriceLbl.Text = string.Format("${0:F1}", dValue);
                        }
                    }
                }

                data = await GetCoinGeckoAPI("ethereum");

                dyn = JsonConvert.DeserializeObject(data);

                if (dyn.market_data != null)
                {
                    if (dyn.market_data.current_price != null)
                    {
                        if (dyn.market_data.current_price.usd != null)
                        {
                            string value = (string)dyn.market_data.current_price.usd;
                            double dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

                            ethPriceLbl.Text = string.Format("${0:F1}", dValue);
                        }
                    }
                }

                chaRVN.ChartAreas[0].AxisX.Minimum = scroll.ToOADate();
                chaRVN.ChartAreas[0].AxisX.Maximum = DateTime.Now.AddMinutes(1).ToOADate();

                DataPoint dpmin = chaRVN.Series[0].Points.FindMinByValue("Y1");
                DataPoint dpmax = chaRVN.Series[0].Points.FindMaxByValue("Y1");

                double min = (dpmin.YValues[0] * 100) * 0.85D;
                double max = (dpmax.YValues[0] * 100) * 1.15D;

                chaRVN.ChartAreas[0].AxisY.Minimum = Math.Round(min / 100, 3, MidpointRounding.AwayFromZero);
                chaRVN.ChartAreas[0].AxisY.Maximum = Math.Round(max / 100, 3, MidpointRounding.AwayFromZero);

                if (chaRVN.Series[0].Points.Count > 8640)
                {
                    chaRVN.Series[0].Points.RemoveAt(0);
                }

            }
            catch
            {

            }
        }

        private async Task<string> GetCoinGeckoAPI(string coinID)
        {
            string data = "ERROR";
            WebClient client = new WebClient();
            await Task.Run(() =>
            {
                data = client.DownloadString("https://api.coingecko.com/api/v3/coins/" + coinID + "?localization=false");
            });

            return data;
        }
    }
}
