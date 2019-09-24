﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SKCOMLib;

namespace SKOrderTester
{
    public partial class Form1 : Form
    {
        public delegate void OrderHandler(string strLogInID, bool bAsyncOrder, FUTUREORDER pStock);
        public event OrderHandler OnFutureOrderSignal;

        #region Environment Variable
        //----------------------------------------------------------------------
        // Environment Variable
        //----------------------------------------------------------------------
        int m_nCode;
        private string connectionstr = System.Configuration.ConfigurationManager.AppSettings.Get("Connectionstring");
        private string FutureAccount = System.Configuration.ConfigurationManager.AppSettings.Get("FutureAccount");
        private string stratpath = System.Configuration.ConfigurationManager.AppSettings.Get("Stratpath");
        private string pythonpath = System.Configuration.ConfigurationManager.AppSettings.Get("Pythonpath");
        private string linepushpath = System.Configuration.ConfigurationManager.AppSettings.Get("LinePushpath");

        SKCenterLib m_pSKCenter;
        SKCenterLib m_pSKCenter2;
        SKOrderLib m_pSKOrder;
        SKReplyLib m_pSKReply;

        AccurateTimer mTimer1;//mTimer2
        public int interval;


        #endregion

        #region Initialize
        //----------------------------------------------------------------------
        // Initialize
        //----------------------------------------------------------------------

        public Form1()
        {
            InitializeComponent();
            m_pSKCenter = new SKCenterLib();
            m_pSKCenter2 = new SKCenterLib();   

            m_pSKOrder = new SKOrderLib();
            skOrder1.OrderObj = m_pSKOrder;

            m_pSKReply = new SKReplyLib();
            skReply1.SKReplyLib = m_pSKReply;           
   
            m_pSKCenter2.OnShowAgreement += new _ISKCenterLibEvents_OnShowAgreementEventHandler(this.OnShowAgreement);

            txtAccount.Text = System.Configuration.ConfigurationManager.AppSettings.Get("Username");
            txtPassWord.Text = System.Configuration.ConfigurationManager.AppSettings.Get("Password");
        }

        #endregion
        private void Form1_Load(object sender, EventArgs e)
        {
            OnFutureOrderSignal += new Form1.OrderHandler(this.MyOnFutureOrderSignal);
            btnInitialize.PerformClick();
            Button orderbtnInitialize = skOrder1.Controls.Find("OrderInitialize", true).FirstOrDefault() as Button;
            Button getAccbtnInitialize = skOrder1.Controls.Find("btnGetAccount", true).FirstOrDefault() as Button;
            Button getCertbtnInitialize = skOrder1.Controls.Find("btnReadCert", true).FirstOrDefault() as Button;
            
            orderbtnInitialize.PerformClick();
            getAccbtnInitialize.PerformClick();
            getCertbtnInitialize.PerformClick();

            label7.Text = GetSkipOrdersCount().ToString();

            DateTime dt1 = DateTime.Now;
            DateTime dt2 = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd") + " 08:50:00");
            //DateTime dt2 = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd") + " 00:25:00");
            TimeSpan span = dt2 - dt1;
            //timer1.Interval = (int)span.TotalMilliseconds;

            if (Math.Round(span.TotalMilliseconds,0) > 0)
            {
                interval = (int)Math.Ceiling(span.TotalSeconds) * 1000;
                //timer1.Enabled = true;
                button1.Enabled = false;
                button2.Enabled = true;
                label3.Text = dt1.AddMilliseconds(interval).ToString();

                mTimer1 = new AccurateTimer(this, new Action(TimerTick1), interval);
                RecordLog("1. Timer Start First time");
            }
            else
            {
                RecordLog("1. Form loaded");
                button2.Enabled = false;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            DateTime dt1 = DateTime.Now;
            DateTime dt2 = dateTimePicker1.Value;

            TimeSpan span = dt2 - dt1;

            if (Math.Ceiling(span.TotalSeconds) > 0)
            {
                interval = (int)Math.Ceiling(span.TotalSeconds) * 1000;
                mTimer1 = new AccurateTimer(this, new Action(TimerTick1), interval);
                //timer1.Enabled = true;
                label3.Text = dt1.AddMilliseconds(interval).ToString();
                RecordLog("1. Timer Starts at specified time");
                button1.Enabled = false;
                button2.Enabled = true;
            }
            else
            {
                MessageBox.Show("Specify time that greater than current time", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            mTimer1.Stop();
            //timer1.Enabled = false;//stop timer
            label3.Text = "";//reset timer label
        }

        private void btnInitialize_Click(object sender, EventArgs e)
        {
            m_nCode = m_pSKCenter.SKCenterLib_Login(txtAccount.Text.Trim().ToUpper(), txtPassWord.Text.Trim());

            if (m_nCode == 0)
            {
                WriteMessage("登入成功");
                skOrder1.LoginID = txtAccount.Text.Trim().ToUpper();
                skReply1.LoginID = txtAccount.Text.Trim().ToUpper();           
            }
            else
                WriteMessage(m_nCode);
        }

        public void WriteMessage(string strMsg)
        {
            listInformation.Items.Add(strMsg);

            listInformation.SelectedIndex = listInformation.Items.Count - 1;

            //listInformation.HorizontalScrollbar = true;

            // Create a Graphics object to use when determining the size of the largest item in the ListBox.
            Graphics g = listInformation.CreateGraphics();

            // Determine the size for HorizontalExtent using the MeasureString method using the last item in the list.
            int hzSize = (int)g.MeasureString(listInformation.Items[listInformation.Items.Count - 1].ToString(), listInformation.Font).Width;
            // Set the HorizontalExtent property.
            listInformation.HorizontalExtent = hzSize;
        }

        public void WriteMessage(int nCode)
        {
            listInformation.Items.Add( m_pSKCenter.SKCenterLib_GetReturnCodeMessage(nCode) );

            listInformation.SelectedIndex = listInformation.Items.Count - 1;

            //listInformation.HorizontalScrollbar = true;

            // Create a Graphics object to use when determining the size of the largest item in the ListBox.
            Graphics g = listInformation.CreateGraphics();

            // Determine the size for HorizontalExtent using the MeasureString method using the last item in the list.
            int hzSize = (int)g.MeasureString(listInformation.Items[listInformation.Items.Count - 1].ToString(), listInformation.Font).Width;
            // Set the HorizontalExtent property.
            listInformation.HorizontalExtent = hzSize;
        }

        private void GetMessage(string strType, int nCode, string strMessage)
        {
            string strInfo = "";

            if (nCode != 0)
                strInfo ="【"+ m_pSKCenter.SKCenterLib_GetLastLogInfo()+ "】";

            WriteMessage("【" + strType + "】【" + strMessage + "】【" + m_pSKCenter.SKCenterLib_GetReturnCodeMessage(nCode) + "】" + strInfo);
        }

        void OnShowAgreement(string strData)
        {
            MessageBox.Show(strData);
        }

        private void TimerTick1()
        {
            //Option 1, run at preset time before day close
            if (TimeSpan.Parse(DateTime.Now.ToString("HH:mm")) == TimeSpan.Parse("13:40"))
            {
                interval = 297000;//Call the last order at 13:44:47
                RecordLog("2. Last Ticks");
            }
            else if(Convert.ToInt32(textBox1.Text.ToString())!=interval)
            {
                interval = Convert.ToInt32(textBox1.Text.ToString());
                mTimer1.Stop();
                mTimer1 = new AccurateTimer(this, new Action(TimerTick1), interval);
                RecordLog("2. Timer Ticks, interval changed");
            }
            else
            {
                RecordLog("2. Timer Ticks");
            }
            RunBacktrader();
            GetCurrentOrder();
            label3.Text = DateTime.Now.AddMilliseconds(interval).ToString("HH:mm:ss");
            label7.Text = GetSkipOrdersCount().ToString();
            /*//Option 2, run on a normal interval
            if (TimeSpan.Parse(DateTime.Now.ToString("HH:mm")) >= TimeSpan.Parse("08:45") && TimeSpan.Parse(DateTime.Now.ToString("HH:mm")) <= TimeSpan.Parse("13:45"))
            {
                RunBacktrader();
                GetCurrentOrder();
                label3.Text = DateTime.Now.AddMilliseconds(timer1.Interval).ToString("HH:mm:ss");
            }
            */
        }

        private void RunBacktrader()
        {
            try
            {
                RecordLog("3. Python Starts");
                string scriptName = stratpath;

                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(pythonpath, scriptName)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch
            {  }
        }

        private void OrderPushToLine()
        {
            try
            {
                RecordLog("4.2 Order Push To Line !!");
                string scriptName = linepushpath;

                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(pythonpath, scriptName)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch
            { }
        }

        private void RecordLog(string message)
        {
            using (SqlConnection connection = new SqlConnection(connectionstr))
            {
                SqlCommand sqlcmd = new SqlCommand();
                connection.Open();
                sqlcmd.Connection = connection;
                sqlcmd.CommandText = "INSERT INTO [Stock].[dbo].[ATM_DailyLog] (ExecTime, Steps) VALUES ('" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + "','" + message + "')";
                sqlcmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public int GetSkipOrdersCount()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstr))
                {
                    int skipcount=0;
                    SqlCommand sqlcmd = new SqlCommand();
                    connection.Open();
                    sqlcmd.Connection = connection;
                    sqlcmd.CommandText = "SELECT [value] FROM[Stock].[dbo].[ATM_Enviroment] WHERE Parameter='SkipOrderCount'";
                    string returnval = sqlcmd.ExecuteScalar().ToString();
                    if (returnval != null)
                    {
                        skipcount = Convert.ToInt16(returnval);
                    }
                    connection.Close();
                    return skipcount;
                }
            }
            catch
            {
                return 0;
            }
        }

        public void DecreaseSkipOrdersCount()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstr))
                {
                    SqlCommand sqlcmd = new SqlCommand();
                    connection.Open();
                    sqlcmd.Connection = connection;
                    sqlcmd.CommandText = "UPDATE [Stock].[dbo].[ATM_Enviroment] SET[value] =[value] - 1 WHERE Parameter = 'SkipOrderCount'";
                    sqlcmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch
            { }
        }

        private void GetCurrentOrder()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstr))
                {
                    connection.Open();
                    int intervalMins = Convert.ToInt32(textBox1.Text.ToString()) / 1000 / 60;//convert to minute
                    SqlCommand sqlcmd = new SqlCommand();
                    sqlcmd.Connection = connection;

                    RecordLog("4.2 GetCurrentOrder Start");

                    sqlcmd.CommandText = "SELECT * FROM [Stock].[dbo].[Orders] WHERE SignalTime BETWEEN " +
                                        " FORMAT(DATEADD(minute,-" + (intervalMins+1) + ",GETDATE()),'yyyy-MM-dd HH:mm')+':00' AND " +
                                        " FORMAT(DATEADD(minute,-" + (intervalMins-1) + ",GETDATE()),'yyyy-MM-dd HH:mm')+':00'";
                    
                    //sqlcmd.CommandText = "SELECT TOP 2* FROM [Stock].[dbo].[Orders] ";
                    using (SqlDataReader reader = sqlcmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RecordLog("4.0 Order is ready to be placed !");
                            FUTUREORDER pFutureOrder = new FUTUREORDER();
                            pFutureOrder.bstrFullAccount = FutureAccount;
                            pFutureOrder.bstrPrice = "M";
                            pFutureOrder.bstrStockNo = reader["stockNo"].ToString();
                            pFutureOrder.nQty = Convert.ToInt32(reader["Size"].ToString());
                            pFutureOrder.sBuySell = (short)Convert.ToInt32(reader["BuyOrSell"].ToString());
                            pFutureOrder.sDayTrade = 0;
                            //(short)Convert.ToInt32(reader["DayTrade"].ToString()); //當沖0:否 1:是
                            pFutureOrder.sTradeType = 1;
                            //(short)Convert.ToInt32(reader["TradeType"].ToString()); ;//0:ROD  1:IOC  2:FOK

                            //0新倉 1平倉 2自動
                            pFutureOrder.sNewClose = 2;

                            if(GetSkipOrdersCount()==0)
                            {
                                OnFutureOrderSignal?.Invoke("", false, pFutureOrder);
                                OrderPushToLine();
                            }
                            else
                            {
                                DecreaseSkipOrdersCount();
                                OrderPushToLine();
                            }
                        }
                    }
                    RecordLog("4.3 GetCurrentOrder Done");
                    connection.Close();
                }
            }
            catch 
            {}
        }

        private void MyOnFutureOrderSignal(string strLogInID, bool bAsyncOrder, FUTUREORDER pStock)
        {
            string strMessage = "";
            m_nCode = m_pSKOrder.SendFutureOrder(strLogInID, bAsyncOrder, pStock, out strMessage);
            RecordLog("4.1 Order is issued !");
            WriteMessage("期貨委託：" + strMessage);
        }


    }
}
