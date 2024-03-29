//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2010-2022  Quizo, Paul Accisano, indiff
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using QTPlugin;
using QTPlugin.Interop;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace QuizoPlugins {
    /// <summary>
    /// Plugin sample.
    /// 
    /// 	PluginAttribute
    ///			PluginType.Interactive      Indicates that the plugin has toolbar item and instantialized only when toolbar item is enabled.
    ///										It needs to inherit IBarButton or IBarCustomItem.
    ///										
    ///			PluginType.Background		Indicates that the plugin is instantialized even if toolbar item is not enabled.
    ///										This type plugin can have no toolbar item.
    ///		
    ///			Author, Name, Version, and Description are used in Options -> Plugins tab.
    /// </summary>
  //  [Plugin(PluginType.Interactive, Author = "Quizo", Name = "SampleClock", Version = "0.9.0.0", Description = "Sample clock plugin")]
    [Plugin(PluginType.Interactive, Author = "indiff", Name = "时钟", Version = "0.9.0.6", Description = "时钟插件;支持节假日,热点新闻;添加重加载;支持多屏")]
    public class Clock : IBarCustomItem {
        private ToolStripLabel labelTime;
        private Timer timer;
        private bool fOn;
        private bool fNewsOn ;
        private static DateTime LAST_TIME ;

        private IPluginServer pluginServer;
        private IShellBrowser shellBrowser;
        /*
        互斥锁lock
        使用：
        lock (m_objLock)
        {
        }
        作用：将会锁住代码块的内容，并阻止其他线程进入该代码块，直到该代码块运行完成，释放该锁。
         */
        // private static readonly object m_objLock = new object();


        /*
        互斥锁Monitor
        定义：
        private static readonly object m_objLock = new object();
        使用：
        Monitor.Enter(m_objLock);
        Monitor.Exit(m_objLock);
        作用：将会锁住代码块的内容，并阻止其他线程进入该代码块，直到该代码块运行完成，释放该锁。
        Monitor有TryEnter的功能，可以防止出现死锁的问题，lock没有。
         */
        // private static readonly object m_objLock = new object();



        /*
        互斥锁Mutex
        定义：
            private static readonly Mutex mutex = new Mutex();
            使用：
            mutex.WaitOne();
            mutex.ReleaseMutex();
            作用：将会锁住代码块的内容，并阻止其他线程进入该代码块，直到该代码块运行完成，释放该锁。
         * Mutex本身是可以系统级别的，所以是可以跨越进程的。
         */
        private static readonly Mutex M_MUTEX = new Mutex();

        #region IPluginClient Members

        public static void Uninstall()
        {
            NewsForm.GetInstance().Close();
        }

        public void Open(IPluginServer pluginServer, IShellBrowser shellBrowser) {
            this.pluginServer = pluginServer;
            this.shellBrowser = shellBrowser;
        }

        public void Close(EndCode code) {
            if(timer != null) {
                timer.Dispose();
            }

            if(labelTime != null) {
                labelTime.Dispose();
            }
        }

        public bool QueryShortcutKeys(out string[] actions) {
            actions = null;
            return false;
        }


        public void OnMenuItemClick(MenuType menuType, string menuText, ITab tab) {
        }

        public bool HasOption {
            get {
                return false;
            }
        }

        public void OnOption() {
        }

        public void OnShortcutKeyPressed(int iIndex) {
        }

        #endregion


        #region IBarCustomItem Members

        public ToolStripItem CreateItem(bool fLarge, DisplayStyle displayStyle) {
            if(labelTime == null) {
                labelTime = new ToolStripLabel();
                labelTime.DisplayStyle = ToolStripItemDisplayStyle.Text;
                labelTime.AutoSize = true;
                labelTime.Font = new Font("Courier New", labelTime.Font.SizeInPoints);
                labelTime.Alignment = ToolStripItemAlignment.Right;
                labelTime.Padding = new Padding(0, 0, 24, 0);
                labelTime.AutoToolTip = false;
            }

            if(timer == null) {
                timer = new Timer();
                timer.Interval = 1000;
                timer.Tick += timer_Tick;
            }
            timer.Start();
            NewsForm.GetInstance().clock = this;
            // labelTime.MouseUp += label_mouseover;
            // labelTime.MouseMove += label_mouseover;
            // labelTime.MouseEnter += label_mouseover;
            // labelTime.MouseHover += label_mouseover;
            // labelTime.MouseLeave += label_mouseLeave;
            labelTime.DoubleClickEnabled = true;
            labelTime.DoubleClick += label_timeDoubleClick;
            return labelTime;
        }

        #endregion

        private string REG_PERSONALIZE = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";


        public Image GetImage(bool fLarge)
        {
            // QTTabBar gets button image

            //   return fLarge ? Resource : Resource.SampleSplitButton_small;
            return fLarge ? Resource.Clock24 : Resource.Clock16;
        }

        #region 判断是否联网
        #region 第二种方法
        public static bool IsConnectedToInternet2()
        {
            string host = "www.baidu.com";
            bool result = false;
            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(host, 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch 
            {

            }
            return result;
        }
        #endregion


        //导入判断网络是否连接的 .dll //判断网络状况的方法,返回值true为连接，false为未连接 
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(ref int Description, int ReservedValue);

        /// <summary>
        /// 用于检查网络是否可以连接互联网,true表示连接成功,false表示连接失败
        /// </summary>
        /// <returns></returns>
        public static bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(ref Description, 0);
        }

        /// <summary>
        /// 判断本地的连接状态
        /// </summary>
        private static bool IsConnectedInternet()
        {
            int dwFlag = new int();
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                // Console.WriteLine("当前没有联网，请您先联网后再进行操作！");
                if ((dwFlag & 0x14) == 0) return false;
                //  System.Diagnostics.Debug.WriteLine("本地系统处于脱机模式。");
                return false;
            }
            else
            {
                if ((dwFlag & 0x01) != 0)
                {
                    //   Console.WriteLine("调制解调器上网。");
                    return true;
                }
                else if ((dwFlag & 0x02) != 0)
                {
                    //  Console.WriteLine("网卡上网。");
                    return true;
                }
                else if ((dwFlag & 0x04) != 0)
                {
                    //  Console.WriteLine("代理服务器上网。");
                    return true;
                }
                else if ((dwFlag & 0x40) != 0)
                {
                   // Console.WriteLine("虽然可以联网，但可能链接也可能不连接。");
                    return true;
                }
            }

            return false;
        }
        #endregion

        


        private void label_mouseLeave(object sender, EventArgs e)
        {
            /*if (timer == null)
            {
                timer = new Timer();
                timer.Interval = 1000;
                timer.Tick += timer_Tick;
            }

            if (timer != null){
                timer.Start();
            }*/

            // NEWSFORM.Hide();
        }


        private void label_timeDoubleClick(object sender, EventArgs e)
        {
            var uiCulture = System.Globalization.CultureInfo.InstalledUICulture.Name;
            var lUiCulture = uiCulture.ToLower();
            if (uiCulture.Equals("zh-CN") || lUiCulture.Equals("zh") || lUiCulture.Equals("cn"))
            {
                if (!fNewsOn)
                {
                    NewsForm.GetInstance().Show();

                    if (LAST_TIME != null)
                    {
                        var subTime = DateTime.Now - LAST_TIME;
                        if (subTime.TotalSeconds < 60d)
                        {
                            MessageBox.Show("间隔时间太短！");
                            return;
                        }
                    }

                    //使用Lamdba表达式
                    new Thread(() =>
                    {
                        M_MUTEX.WaitOne();
                        try
                        {
                            LoadNews();
                            if (labelTime != null)
                            {
                                labelTime.ForeColor = Color.Blue;
                            }
                            M_MUTEX.ReleaseMutex();
                        }
                        finally
                        {
                            
                        }
                    }).Start();
                }
                else
                {
                    if (labelTime != null)
                    {
                        labelTime.ForeColor = Color.Red;
                    }
                    NewsForm.GetInstance().Show();
                }
                fNewsOn = !fNewsOn;
            }
            else
            {
                MessageBox.Show("Sorry, Not support in your country!");
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
                DateTime dt = DateTime.Now;
                int h = dt.Hour;
                int m = dt.Minute;
                int _m = dt.Month;
                int _d = dt.Day;
                string sep = fOn ? " " : ":";
                ChineseCalendar cc = new ChineseCalendar(DateTime.Today);
                string holiday = "";

                string happyStr = "";
                switch (DateTime.Today.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        happyStr = "快乐星期天";
                        break;
                    case DayOfWeek.Monday:
                        happyStr = "星期一";
                        break;
                    case DayOfWeek.Tuesday:
                        happyStr = "星期二";
                        break;
                    case DayOfWeek.Wednesday:
                        happyStr = "星期三";
                        break;
                    case DayOfWeek.Thursday:
                        happyStr = "星期四";
                        break;
                    case DayOfWeek.Friday:
                        happyStr = "Happy Friday";
                        break;
                    default:
                        happyStr = "快乐星期六";
                        break;
                }

                if (!string.IsNullOrEmpty(cc.ChineseCalendarHoliday))
                {
                    holiday += " " + cc.ChineseCalendarHoliday;
                }

                if (!string.IsNullOrEmpty(cc.DateHoliday))
                {
                    holiday += " " + cc.DateHoliday;
                }

                if (!string.IsNullOrEmpty(happyStr))
                {
                    holiday += " " + happyStr;
                }

                // news = "\n load news empty!" + LAST_TIME;  加载新闻逻辑有问题  https://www.yuque.com/indiff/lc0r1g/fsdqn6
                /*if (IsConnectInternet() || IsConnectedToInternet2())
                {
                    var dateTime = DateTime.Now;
                    /*if ( LAST_TIME == null )
                    {
                        LoadNews();
                        LAST_TIME = dateTime;
                    }#1#
                    if (LAST_TIME != null)
                    {
                        var sub = dateTime - LAST_TIME;
                        if (sub.TotalSeconds > 3600d) // 一小时刷一次新闻
                        {
                            MessageBox.Show("加载热点新闻");
                            new Thread(() =>
                            {
                                LoadNews();
                            }).Start();
                        }
                    }
                }*/
                // labelTime.AutoToolTip = false;
                labelTime.ToolTipText = dt.ToLongDateString() + holiday + " 可双击🎅🎅";
                // labelTime.Text = h + sep + (m < 10 ? "0" : String.Empty) + m + "(" + _m + "." + _d + ")";
                labelTime.Text = h + sep + (m < 10 ? "0" : String.Empty) + m + "(" + dt.ToLongDateString() + holiday + ")";
                // HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
                using (var envKey = Registry.CurrentUser.OpenSubKey(REG_PERSONALIZE, true))
                {
                    if (envKey != null) // fix window server System.NullReferenceException: 未将对象引用设置到对象的实例。
                    {
                        object value = envKey.GetValue("AppsUseLightTheme");
                        if (value != null)
                        {
                            string useTheme = value.ToString();
                            if ("1".Equals(useTheme))
                            {
                                // the light
                                labelTime.ForeColor = Color.Black;
                            }
                            else
                            {
                                // the dark mode
                                labelTime.ForeColor = Color.White;
                            }
                        }
                        else
                        {
                            // default is light
                            labelTime.ForeColor = Color.Black;
                        }
                    }
                }

                // labelTime.BackColor
                fOn = !fOn;
           
        }

        

        public void LoadNews()
        {
            // M_MUTEX.WaitOne();
            // var jsonString = PostUrl("https://open.tophub.today/hot", "");
            var jsonString = GetUrl("https://open.tophub.today/hot");
            bool isLoaded = false;
            NewsForm.GetInstance().enableButtons(false );
            if (!string.IsNullOrEmpty(jsonString) && !jsonString.StartsWith("exception") )
            {
                // 反序列化，泛型集合
                try
                {
                    var theNew = parse<News>(jsonString);
                    if (null != theNew)
                    {
                        if (theNew.status == 200 || !theNew.error)
                        {
                            var theNewData = theNew.data;
                            if (null != theNewData && theNewData.items.Length > 0)
                            {
                                NewsForm.GetInstance().setItems( theNewData.items );
                                LAST_TIME = DateTime.Now;
                                isLoaded = true;
                            }
                        }
                    }
                }
                catch
                {
                    // return "\n parse exception";
                }
            }
            else
            {
                // return "\n" + jsonString;
            }


            if (!isLoaded)
            {
                NewsForm.GetInstance().enableButtons(true);
            }
            // return "\nnothing" + jsonString;
        }


        [DataContract]
        public class Item
        {
            [DataMember(Order = 0, IsRequired = true)]
            public string ID { get; set; }

            [DataMember(Order = 1)]
            public string title { get; set; }

            [DataMember(Order = 2)]
            public string description { get; set; }

            [DataMember(Order = 3)]
            public string thumbnail { get; set; }

            [DataMember(Order = 4)]
            public string url { get; set; }

            [DataMember(Order = 5)]
            public string md5 { get; set; }

            [DataMember(Order = 6)]
            public string extra { get; set; }

            [DataMember(Order = 7)]
            public string time { get; set; }

            [DataMember(Order = 8)]
            public string nodeids { get; set; }

            [DataMember(Order = 9)]
            public string topicid { get; set; }

            [DataMember(Order = 10)]
            public string domain { get; set; }

            [DataMember(Order = 11)]
            public string sitename { get; set; }

            [DataMember(Order = 12)]
            public string logo { get; set; }
 
            [DataMember(Order = 13)]
            public string views { get; set; }
        }

        [DataContract]
        public class Data
        {
            [DataMember(Order = 0, IsRequired = true)]
            public Item[] items { get; set; }

            [DataMember(Order = 1)]
            public string day { get; set; }

            [DataMember(Order = 2)]
            public int limit { get; set; }

            [DataMember(Order = 3)]
            public string filter { get; set; }
        }

        [DataContract]
        public class News
        {
            [DataMember(Order = 0, IsRequired = true)]
            public Data data { get; set; }

            [DataMember(Order = 1)]
            public int status { get; set; }

            [DataMember(Order = 2)]
            public bool error { get; set; }
        }


        public static T parse<T>(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }

        public static string stringify(object jsonObject)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /*
         *  url:POST请求地址
         *  postData:json格式的请求报文,例如：{"key1":"value1","key2":"value2"}
         */
        public static string PostUrl(string url, string postData)
        {
            string result = "exception:0";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.Timeout = 10000; //设置请求超时时间，单位为毫秒
                req.ContentType = "application/json";
                byte[] data = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                result = "exception:" + e.Message;
            }
            return result;
        }       
        
        /*
         *  url:GET请求地址
         *  postData:json格式的请求报文,例如：{"key1":"value1","key2":"value2"}
         */
        public string GetUrl(string url)
        {
            string result = "exception:0";
            try
            {
                // request.ContentType = "text/html;chartset=UTF-8";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 10000; //设置请求超时时间，单位为毫秒
                req.ContentType = "application/json";
                req.UserAgent =
                    "Mozilla / 5.0(Windows NT 10.0; Win64; x64; rv: 48.0) Gecko / 20100101 Firefox / 48.0"; //火狐用户代理
                /*byte[] data = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }*/
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                // len = stream.Length;
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                // stream.Close();
            }
            catch (Exception e)
            {
                result = "exception:" + e.Message;
            }
            finally
            {
            }
            return result;
        }
    }
}
