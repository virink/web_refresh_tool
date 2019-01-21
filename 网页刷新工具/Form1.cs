using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace 网页刷新工具
{
    public partial class Form1 : Form
    {
        string sitefile = "C:\\sites.json";
        private Dictionary<string,System.Threading.Timer> _timer = new Dictionary<string, System.Threading.Timer>();
        private SynchronizationContext mainThreadSynContext;
        int timep = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.listView1.View = View.Details;
            this.listView1.FullRowSelect = true;
            this.listView1.Columns.Add("Sec", 50, HorizontalAlignment.Left);
            this.listView1.Columns.Add("URL", 500, HorizontalAlignment.Left);
            this.readSites();
            mainThreadSynContext = SynchronizationContext.Current;
        }

        private void readSites()
        {
            try { 
                StreamReader sr = new StreamReader(sitefile, Encoding.Default);
                string sitejson = sr.ReadToEnd();
                JArray jArray = (JArray)JsonConvert.DeserializeObject(sitejson);
                Console.WriteLine(jArray);
                if (jArray.Count > 0)
                {
                    for (int i = 0; i < jArray.Count; i++)
                    {
                        string sec = jArray[i]["Second"].ToString();
                        string url = jArray[i]["Url"].ToString();
                        this.addItem(sec, url);
                    }
                }
                sr.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void writeSites()
        {
            ArrayList list1 = new ArrayList();
            foreach (ListViewItem lvi in this.listView1.Items)
            {
                Site s = new Site { Second = lvi.SubItems[0].Text, Url = lvi.SubItems[1].Text };
                list1.Add(s);
            }
            try { 
                FileStream fs = new FileStream(sitefile, FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(JsonConvert.SerializeObject(list1, Formatting.None));
                sw.Flush();
                sw.Close();
                fs.Close();
                this.addLogs("Save sites to file success!");
            }
            catch(Exception eee)
            {
                Console.WriteLine(eee.ToString());
            }
        }

        private void setContent(string sec, string url)
        {
            this.t_sec.Text = sec;
            this.t_url.Text = url;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count > 0)
            {
                try
                {
                    string sec = this.listView1.SelectedItems[0].SubItems[0].Text.ToString();
                    string url = this.listView1.SelectedItems[0].SubItems[1].Text.ToString();
                    this.setContent(sec, url);
                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.Message);
                }
            }
        }

        private void delete_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in this.listView1.SelectedItems)
            {
                listView1.Items.RemoveAt(lvi.Index);
            }
            this.setContent("", "");
            this.writeSites();
        }

        private void add_Click(object sender, EventArgs e)
        {
            this.setContent("1", "");
        }

        private void addItem(string sec, string url)
        {
            ListViewItem[] p = new ListViewItem[1];
            p[0] = new ListViewItem(new string[] { sec,url });
            this.listView1.Items.AddRange(p);
            this.addLogs("Add site ["+ url+"] success!");
        }

        private void change_Click(object sender, EventArgs e)
        {
            if (this.t_sec.Text != "" && this.t_url.Text != "") {
                foreach (ListViewItem lvi in this.listView1.SelectedItems)
                {
                    listView1.Items.RemoveAt(lvi.Index);
                }
                this.addItem(this.t_sec.Text, this.t_url.Text);
                this.setContent("1", "");
            }
            this.writeSites();
        }

        private void start_Click(object sender, EventArgs e)
        {
            /*
            if (!this._timer.ContainsKey("clear"))
            {
                System.Threading.Timer _t = new System.Threading.Timer(OnClearLog, null, 0000, 60 * 1000);//2秒后第一次调用，每sec秒调用一次
                this._timer.Add("clear", _t);
            }
            */
            foreach (ListViewItem lvi in this.listView1.Items)
            {
                this.newTimer(int.Parse(lvi.SubItems[0].Text), lvi.SubItems[1].Text);
            }
        }

        private void newTimer(int sec,string url)
        {
            if(!this._timer.ContainsKey(url)){ 
                System.Threading.Timer _t = new System.Threading.Timer(httpGet, url, 2000 + timep, sec * 1000);//2秒后第一次调用，每sec秒调用一次
                this._timer.Add(url, _t);
                this.addLogs("Add Timer [" + url + "] success!");
                timep += 1000;
                Console.WriteLine(timep);
            }
        }

        private void OnConnected(object state)
        {
            this.addLogs((string)state);
        }

        /*
        private void OnClearLog(object state)
        {
            this.logs.Clear();
        }
        */

        private void httpGet(object state)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync(new Uri((string)state)).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                // this.addLogs("Req "+(string)state);
                mainThreadSynContext.Post(new SendOrPostCallback(OnConnected), "Req " + (string)state + " -> "+result.Substring(0,500));
            }
        }

        private void addLogs(string log)
        {
            this.logs.Text = string.Format("[+] {0} \r\n\r\n{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), log);
            //this.logs.AppendText();
            //this.logs.Select(this.logs.TextLength, 0);
        }

        private void stop_Click(object sender, EventArgs e)
        {
            foreach (var item in this._timer)
            {
                Console.WriteLine(item.Key);
                item.Value.Change(-1,1);
            }
            this._timer.Clear();
            timep = 0;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Site
    {
        [JsonProperty]
        public string Url { get; set; }
        [JsonProperty]
        [DefaultValue(1)]
        public string Second { get; set; }
    }
}
