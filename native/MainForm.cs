using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DecodGrmNative {
    public partial class MainForm : Form {
        readonly ApiClient api;
        readonly Panel body=new Panel();
        readonly Label headerTitle=new Label(), headerMeta=new Label();
        readonly Button syncButton=new Button();
        readonly NotifyIcon tray=new NotifyIcon();
        Dictionary<string,object> root=new Dictionary<string,object>();
        Dictionary<string,object> state=new Dictionary<string,object>();
        Dictionary<string,object> settings=new Dictionary<string,object>();
        string currentView="dashboard";
        bool allowExit=false;

        readonly Color Navy=Color.FromArgb(11,43,73), Blue=Color.FromArgb(8,120,209), Bg=Color.FromArgb(245,247,250), TextC=Color.FromArgb(31,41,55), Muted=Color.FromArgb(100,116,139);

        public MainForm(ApiClient api){
            this.api=api;
            Text="G-Rm"; Width=1320;Height=820;MinimumSize=new Size(1050,680);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",9.5f);BackColor=Bg;
            BuildChrome(); BuildTray();
            Shown+=(s,e)=>InitialLoad();
        }

        void BuildChrome(){
            var left=new Panel{Dock=DockStyle.Left,Width=205,BackColor=Navy,Padding=new Padding(10,16,10,10)};
            var brand=new Label{Text="DECOD\r\nG-Rm",Dock=DockStyle.Top,Height=80,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",20),TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(12,0,0,0)};
            left.Controls.Add(brand);
            string[,] nav={{"Ana Rapor","dashboard"},{"Müşteriler","customers"},{"Ziyaretler","visits"},{"Hatırlatmalar","reminders"},{"Teklifler","quotes"},{"Ürünler","products"},{"Ekip & Yetkiler","team"},{"Ayarlar","settings"}};
            int top=96;
            for(int i=0;i<nav.GetLength(0);i++){
                string label=nav[i,0],key=nav[i,1];
                var b=new Button{Text=label,Left=10,Top=top,Width=185,Height=42,FlatStyle=FlatStyle.Flat,ForeColor=Color.White,BackColor=Navy,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(12,0,0,0),Cursor=Cursors.Hand};
                b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(18,65,104);b.Click+=(s,e)=>Navigate(key);left.Controls.Add(b);top+=46;
            }
            var header=new Panel{Dock=DockStyle.Top,Height=74,BackColor=Color.White,Padding=new Padding(18,8,18,8)};
            headerTitle.Text="G-Rm";headerTitle.Font=new Font("Segoe UI Semibold",18);headerTitle.ForeColor=Navy;headerTitle.Left=18;headerTitle.Top=9;headerTitle.Width=520;headerTitle.Height=30;
            headerMeta.ForeColor=Muted;headerMeta.Left=20;headerMeta.Top=42;headerMeta.Width=720;headerMeta.Height=22;
            syncButton.Text="Şimdi Eşitle";syncButton.Width=125;syncButton.Height=36;syncButton.Top=18;syncButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;syncButton.BackColor=Blue;syncButton.ForeColor=Color.White;syncButton.FlatStyle=FlatStyle.Flat;syncButton.FlatAppearance.BorderSize=0;syncButton.Click+=(s,e)=>SyncNow();
            header.Controls.Add(headerTitle);header.Controls.Add(headerMeta);header.Controls.Add(syncButton);header.Resize+=(s,e)=>syncButton.Left=header.ClientSize.Width-syncButton.Width-18;
            body.Dock=DockStyle.Fill;body.BackColor=Bg;body.Padding=new Padding(18);
            Controls.Add(body);Controls.Add(header);Controls.Add(left);
        }

        void BuildTray(){
            tray.Text="G-Rm";tray.Visible=true;try{tray.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
            var menu=new ContextMenuStrip();
            menu.Items.Add("G-Rm Aç",null,(s,e)=>ShowFromTray());
            menu.Items.Add("Hatırlatmalar",null,(s,e)=>{ShowFromTray();Navigate("reminders");});
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Çıkış",null,(s,e)=>ExitApp());
            tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>ShowFromTray();
        }
        void ShowFromTray(){Show();WindowState=FormWindowState.Normal;Activate();}
        void ExitApp(){allowExit=true;tray.Visible=false;try{api.Shutdown();}catch{}Close();}
        protected override void OnFormClosing(FormClosingEventArgs e){
            if(!allowExit && e.CloseReason==CloseReason.UserClosing && J.B(settings,"minimizeToTray",true)){e.Cancel=true;Hide();tray.ShowBalloonTip(1200,"G-Rm","G-Rm arka planda çalışmaya devam ediyor.",ToolTipIcon.Info);return;}
            tray.Visible=false;base.OnFormClosing(e);
        }

        void InitialLoad(){
            try{Reload(true);}catch(Exception ex){Error(ex);}
        }

        public void Reload(bool route){
            root=api.Bootstrap();
            if(!J.B(root,"licenseValid")){
                state=J.D(root.ContainsKey("state")?root["state"]:null);settings=J.D(state.ContainsKey("settings")?state["settings"]:null);ShowActivation();return;
            }
            if(J.B(root,"authRequired")&&!J.B(root,"authenticated")){
                using(var d=new InputDialog("G-Rm Girişi","Profil PIN / Şifre",true)){if(d.ShowDialog(this)!=DialogResult.OK){Close();return;}api.Login(d.Value);}
                root=api.Bootstrap();
            }
            state=J.D(root["state"]);settings=J.D(state["settings"]);
            headerMeta.Text=J.S(root,"profileName")+"  •  "+J.S(root,"licenseCompany")+"  •  v"+J.S(root,"version")+"  •  "+J.S(settings,"lastSyncStatus","Hazır");
            syncButton.Enabled=J.Perm(root,"sync.manage");
            if(route)Navigate(currentView);else RenderView();
        }

        void ShowActivation(){
            body.Controls.Clear();headerTitle.Text="G-Rm Aktivasyon";headerMeta.Text="Kurulum Kodu: "+J.S(root,"installationCode");syncButton.Visible=false;
            var card=Card(520,270);card.Left=Math.Max(30,(body.ClientSize.Width-card.Width)/2);card.Top=55;
            var t=new Label{Text="Bu bilgisayar için G-Rm lisansı gerekli",Left=25,Top=24,Width=470,Height=34,Font=new Font("Segoe UI Semibold",16),ForeColor=Navy};
            var code=new TextBox{Left=25,Top=75,Width=470,Height=32,ReadOnly=true,Text=J.S(root,"installationCode"),Font=new Font("Consolas",11)};
            var copy=new Button{Text="Kodu Kopyala",Left=25,Top=122,Width=140,Height=34};copy.Click+=(s,e)=>{Clipboard.SetText(code.Text);Info("Kurulum kodu kopyalandı.");};
            var load=new Button{Text=".dgrm Lisansı Yükle",Left=180,Top=122,Width=180,Height=34,BackColor=Blue,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
            load.Click+=(s,e)=>{using(var f=new OpenFileDialog{Filter="G-Rm Lisansı (*.dgrm)|*.dgrm"})if(f.ShowDialog(this)==DialogResult.OK){try{api.UploadLicense(f.FileName);Reload(true);}catch(Exception ex){Error(ex);}}};
            card.Controls.AddRange(new Control[]{t,code,copy,load});body.Controls.Add(card);
        }

        void Navigate(string key){currentView=key;syncButton.Visible=true;RenderView();}
        void RenderView(){
            body.Controls.Clear();
            switch(currentView){
                case "customers":ShowCustomers();break;case "visits":ShowVisits();break;case "reminders":ShowReminders();break;case "quotes":ShowQuotes();break;case "products":ShowProducts();break;case "team":ShowTeam();break;case "settings":ShowSettings();break;default:ShowDashboard();break;
            }
        }

        void SyncNow(){
            syncButton.Enabled=false;syncButton.Text="Eşitleniyor…";
            Task.Run(()=>{Exception err=null;try{api.Sync();}catch(Exception ex){err=ex;}BeginInvoke((Action)(()=>{syncButton.Text="Şimdi Eşitle";syncButton.Enabled=true;if(err!=null)Error(err);else{Info("G-Cloud eşitlemesi tamamlandı.");Reload(false);}}));});
        }

        public Dictionary<string,object> StateListItem(string listKey,int index){
            var list=J.LD(state.ContainsKey(listKey)?state[listKey]:null);if(index<0||index>=list.Count)return null;return list[index];
        }
        List<Dictionary<string,object>> List(string key){return J.LD(state.ContainsKey(key)?state[key]:null).Where(x=>!J.B(x,"deleted")).ToList();}

        Panel Card(int w=360,int h=130){return new Panel{Width=w,Height=h,BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Padding=new Padding(14)};}
        Label H(string text,int size=17){return new Label{Text=text,AutoSize=false,Height=32,Dock=DockStyle.Top,Font=new Font("Segoe UI Semibold",size),ForeColor=Navy};}
        Button Primary(string text){var b=new Button{Text=text,Height=34,AutoSize=true,Padding=new Padding(14,0,14,0),BackColor=Blue,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};b.FlatAppearance.BorderSize=0;return b;}
        FlowLayoutPanel Toolbar(){return new FlowLayoutPanel{Dock=DockStyle.Top,Height=48,FlowDirection=FlowDirection.LeftToRight,Padding=new Padding(0,4,0,4),WrapContents=false};}
        DataGridView Grid(){return new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.FixedSingle,RowHeadersVisible=false};}
        void Error(Exception ex){MessageBox.Show(ex.Message,"G-Rm",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        void Info(string s){MessageBox.Show(s,"G-Rm",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        bool Ask(string s){return MessageBox.Show(s,"G-Rm",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;}

        Dictionary<string,object> Selected(DataGridView g){
            if(g.SelectedRows.Count==0)return null;return g.SelectedRows[0].Tag as Dictionary<string,object>;
        }

        void Export(string kind,string prefix){
            using(var f=new SaveFileDialog{Filter="Excel Dosyası (*.xlsx)|*.xlsx",FileName=prefix+"_"+DateTime.Now.ToString("yyyyMMdd_HHmm")+".xlsx"})if(f.ShowDialog(this)==DialogResult.OK){try{api.Download("/api/export/"+kind,f.FileName);Info("Excel dosyası oluşturuldu.");}catch(Exception ex){Error(ex);}}
        }
        void Backup(){
            using(var f=new SaveFileDialog{Filter="G-Rm Yedeği (*.grmbak)|*.grmbak",FileName="G-Rm_Yedek_"+DateTime.Now.ToString("yyyyMMdd_HHmm")+".grmbak"})if(f.ShowDialog(this)==DialogResult.OK){try{api.Download("/api/backup",f.FileName);Info("Yedek oluşturuldu.");}catch(Exception ex){Error(ex);}}
        }
        void Restore(){
            using(var f=new OpenFileDialog{Filter="G-Rm Yedeği (*.grmbak)|*.grmbak"})if(f.ShowDialog(this)==DialogResult.OK&&Ask("Bu yedek mevcut yerel verinin üzerine geri yüklenecek. Devam edilsin mi?")){try{api.RestoreBackup(f.FileName);Reload(false);Info("Yedek geri yüklendi.");}catch(Exception ex){Error(ex);}}
        }
    }
}
