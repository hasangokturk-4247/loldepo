using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace DecodGrmNative {
    public class InputDialog : Form {
        public string Value { get { return box.Text; } }
        TextBox box = new TextBox();
        public InputDialog(string title,string label,bool password=false,string value="") {
            Text=title; Width=430; Height=180; StartPosition=FormStartPosition.CenterParent; Font=new Font("Segoe UI",10);
            var l=new Label{Text=label,Left=18,Top=18,Width=370,Height=24};
            box.SetBounds(18,48,375,28); box.Text=value; box.UseSystemPasswordChar=password;
            var ok=new Button{Text="Tamam",DialogResult=DialogResult.OK,Left=218,Top=92,Width=84,Height=32};
            var cancel=new Button{Text="İptal",DialogResult=DialogResult.Cancel,Left=309,Top=92,Width=84,Height=32};
            Controls.AddRange(new Control[]{l,box,ok,cancel}); AcceptButton=ok; CancelButton=cancel;
        }
    }

    public sealed class FieldDef {
        public string Label, Key, Type;
        public FieldDef(string label,string key,string type="text"){Label=label;Key=key;Type=type;}
    }

    public class EntityDialog : Form {
        readonly Dictionary<string,Control> controls=new Dictionary<string,Control>();
        readonly List<FieldDef> defs;
        readonly Dictionary<string,object> original;
        public Dictionary<string,object> Result {get;private set;}

        public EntityDialog(string title,List<FieldDef> defs,Dictionary<string,object> existing) {
            this.defs=defs; original=J.Clone(existing); Text=title; StartPosition=FormStartPosition.CenterParent;
            Width=620; Height=Math.Min(760,150+defs.Count*48); MinimumSize=new Size(520,320); Font=new Font("Segoe UI",9.5f);
            var panel=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(14)};
            int y=12;
            foreach(var f in defs){
                var lbl=new Label{Text=f.Label,Left=16,Top=y+4,Width=175,Height=24};
                Control c;
                if(f.Type=="bool"){
                    var cb=new CheckBox{Left=198,Top=y+3,Width=340,Height=26,Checked=J.B(existing,f.Key,false)}; c=cb;
                }else if(f.Type=="multiline"){
                    var tb=new TextBox{Left=198,Top=y,Width=360,Height=70,Multiline=true,ScrollBars=ScrollBars.Vertical,Text=J.S(existing,f.Key)}; c=tb; y+=44;
                }else{
                    var tb=new TextBox{Left=198,Top=y,Width=360,Height=27,Text=J.S(existing,f.Key)};
                    c=tb;
                }
                controls[f.Key]=c; panel.Controls.Add(lbl); panel.Controls.Add(c); y+=42;
            }
            var ok=new Button{Text="Kaydet",Width=100,Height=34,Left=358,Top=y+8,Anchor=AnchorStyles.Bottom|AnchorStyles.Right};
            var cancel=new Button{Text="İptal",Width=100,Height=34,Left=466,Top=y+8,DialogResult=DialogResult.Cancel,Anchor=AnchorStyles.Bottom|AnchorStyles.Right};
            ok.Click+=(s,e)=>{ if(BuildResult()){DialogResult=DialogResult.OK;Close();} };
            panel.Controls.Add(ok);panel.Controls.Add(cancel);Controls.Add(panel);AcceptButton=ok;CancelButton=cancel;
        }

        bool BuildResult(){
            var r=J.Clone(original);
            foreach(var f in defs){
                var c=controls[f.Key];
                if(f.Type=="bool"){r[f.Key]=((CheckBox)c).Checked;continue;}
                var t=((TextBox)c).Text.Trim();
                if(f.Type=="int"){int n;if(!int.TryParse(t,out n)){MessageBox.Show(f.Label+" sayı olmalı.");return false;}r[f.Key]=n;}
                else if(f.Type=="double"){double n;if(!double.TryParse(t.Replace(',','.'),NumberStyles.Any,CultureInfo.InvariantCulture,out n)){MessageBox.Show(f.Label+" sayı olmalı.");return false;}r[f.Key]=n;}
                else r[f.Key]=t;
            }
            Result=r; return true;
        }
    }

    public class QuoteDialog : Form {
        readonly Dictionary<string,object> original;
        readonly TextBox no,date,customerCode,customerName,status,preparedBy,followUp,notes,payment,delivery,validUntil;
        readonly DataGridView grid=new DataGridView();
        readonly List<Dictionary<string,object>> lines=new List<Dictionary<string,object>>();
        public Dictionary<string,object> Result {get;private set;}

        public QuoteDialog(Dictionary<string,object> q,string currentUser){
            original=J.Clone(q); Text=q==null||q.Count==0?"Yeni Teklif":"Teklifi Düzenle"; Width=980;Height=720;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.5f);
            var top=new Panel{Dock=DockStyle.Top,Height=205,Padding=new Padding(12)};
            no=TB(top,"Teklif No",10,10,210,J.S(q,"no",AutoNo()));
            date=TB(top,"Tarih",255,10,150,J.S(q,"date",DateTime.Today.ToString("yyyy-MM-dd")));
            status=TB(top,"Durum",435,10,165,J.S(q,"status","Taslak"));
            preparedBy=TB(top,"Hazırlayan",630,10,270,J.S(q,"preparedBy",currentUser));
            customerCode=TB(top,"Müşteri Kodu",10,70,210,J.S(q,"customerCode"));
            customerName=TB(top,"Müşteri Adı",255,70,345,J.S(q,"customerName"));
            followUp=TB(top,"Takip Tarihi",630,70,270,J.S(q,"followUpDate"));
            validUntil=TB(top,"Geçerlilik",10,130,210,J.S(q,"validUntil"));
            payment=TB(top,"Ödeme Şartı",255,130,345,J.S(q,"paymentTerms"));
            delivery=TB(top,"Teslimat",630,130,270,J.S(q,"deliveryTerms"));
            notes=new TextBox{Left=10,Top=182,Width=890,Height=1,Visible=false,Text=J.S(q,"notes")};
            top.Controls.Add(notes);

            grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            var arr=q!=null&&q.ContainsKey("lines")?J.LD(q["lines"]):new List<Dictionary<string,object>>(); foreach(var l in arr)lines.Add(J.Clone(l));
            RefreshLines();

            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=54,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};
            var save=new Button{Text="Kaydet",Width=100,Height=34};var cancel=new Button{Text="İptal",Width=100,Height=34,DialogResult=DialogResult.Cancel};
            var add=new Button{Text="+ Kalem",Width=100,Height=34};var edit=new Button{Text="Kalemi Düzenle",Width=120,Height=34};var del=new Button{Text="Kalemi Sil",Width=100,Height=34};
            save.Click+=(s,e)=>{if(Build()){DialogResult=DialogResult.OK;Close();}}; add.Click+=(s,e)=>EditLine(null,-1); edit.Click+=(s,e)=>{if(grid.SelectedRows.Count>0)EditLine(lines[grid.SelectedRows[0].Index],grid.SelectedRows[0].Index);}; del.Click+=(s,e)=>{if(grid.SelectedRows.Count>0){lines.RemoveAt(grid.SelectedRows[0].Index);RefreshLines();}};
            bottom.Controls.AddRange(new Control[]{save,cancel,del,edit,add});
            Controls.Add(grid);Controls.Add(bottom);Controls.Add(top);AcceptButton=save;CancelButton=cancel;
        }

        static TextBox TB(Control p,string label,int x,int y,int w,string value){
            var l=new Label{Text=label,Left=x,Top=y,Width=w,Height=20};var t=new TextBox{Left=x,Top=y+22,Width=w,Height=27,Text=value};p.Controls.Add(l);p.Controls.Add(t);return t;
        }
        static string AutoNo(){return "DCD-"+DateTime.Now.ToString("ddMMyyHHmm");}
        void RefreshLines(){
            grid.Columns.Clear();grid.Rows.Clear();
            foreach(var h in new[]{"Kod","Ürün","Miktar","Birim","Net Fiyat","Para","KDV","Tutar"})grid.Columns.Add(h,h);
            foreach(var l in lines)grid.Rows.Add(J.S(l,"productCode"),J.S(l,"productName"),J.S(l,"qty"),J.S(l,"unit"),J.S(l,"netPrice"),J.S(l,"currency","TRY"),J.S(l,"vat"),J.S(l,"amount"));
        }
        void EditLine(Dictionary<string,object> l,int index){
            var defs=new List<FieldDef>{new FieldDef("Ürün Kodu","productCode"),new FieldDef("Ürün Adı","productName"),new FieldDef("Marka","brand"),new FieldDef("Kategori","category"),new FieldDef("Miktar","qty","double"),new FieldDef("Birim","unit"),new FieldDef("Birim Fiyat","unitPrice","double"),new FieldDef("Liste Fiyatı","listPrice","double"),new FieldDef("İskonto %","discount","double"),new FieldDef("Net Fiyat","netPrice","double"),new FieldDef("Para Birimi","currency"),new FieldDef("KDV %","vat","double")};
            var seed=l==null?new Dictionary<string,object>{{"qty",1.0},{"unit","Adet"},{"currency","TRY"},{"vat",20.0}}:J.Clone(l);
            using(var d=new EntityDialog("Teklif Kalemi",defs,seed)){if(d.ShowDialog(this)==DialogResult.OK){var r=d.Result;double qty=J.F(r,"qty",1),net=J.F(r,"netPrice",J.F(r,"unitPrice",0)),vat=J.F(r,"vat",0);r["amount"]=qty*net*(1+vat/100.0);if(index>=0)lines[index]=r;else lines.Add(r);RefreshLines();}}
        }
        bool Build(){
            if(string.IsNullOrWhiteSpace(no.Text)||string.IsNullOrWhiteSpace(customerName.Text)){MessageBox.Show("Teklif no ve müşteri zorunludur.");return false;}
            var r=J.Clone(original);r["no"]=no.Text.Trim();r["date"]=date.Text.Trim();r["customerCode"]=customerCode.Text.Trim();r["customerName"]=customerName.Text.Trim();r["status"]=status.Text.Trim();r["preparedBy"]=preparedBy.Text.Trim();r["followUpDate"]=followUp.Text.Trim();r["notes"]=notes.Text;r["validUntil"]=validUntil.Text.Trim();r["paymentTerms"]=payment.Text.Trim();r["deliveryTerms"]=delivery.Text.Trim();r["lines"]=lines.ToArray();Result=r;return true;
        }
    }

    public class PairingForm : Form {
        readonly Label countdown=new Label();
        readonly Timer timer=new Timer();
        readonly DateTime expires;
        public PairingForm(string code,string user,string company,DateTime expiresAt){
            expires=expiresAt;Text="Mobil Cihaz Eşleştir";Width=520;Height=650;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Color.White;
            var title=new Label{Text="G-Rm Mobil Eşleştirme",Dock=DockStyle.Top,Height=48,Font=new Font("Segoe UI Semibold",18),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.FromArgb(11,43,73)};
            var sub=new Label{Text=company+"\r\n"+user+" kullanıcısı",Dock=DockStyle.Top,Height=54,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.DimGray};
            var pic=new PictureBox{Dock=DockStyle.Top,Height=400,SizeMode=PictureBoxSizeMode.CenterImage};
            var writer=new ZXing.BarcodeWriter{Format=BarcodeFormat.QR_CODE,Options=new QrCodeEncodingOptions{Width=360,Height=360,Margin=1,CharacterSet="UTF-8"}};
            pic.Image=writer.Write(code);
            countdown.Dock=DockStyle.Top;countdown.Height=38;countdown.TextAlign=ContentAlignment.MiddleCenter;countdown.ForeColor=Color.FromArgb(8,120,209);
            var info=new Label{Text="Telefonda G-Rm'i açın ve bu QR kodu okutun.\r\nBu QR kısa süre sonra geçersiz olur.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.TopCenter,Padding=new Padding(12),ForeColor=Color.DimGray};
            Controls.Add(info);Controls.Add(countdown);Controls.Add(pic);Controls.Add(sub);Controls.Add(title);
            timer.Interval=1000;timer.Tick+=(s,e)=>Tick();timer.Start();Tick();
        }
        void Tick(){var left=expires-DateTime.Now;if(left.TotalSeconds<=0){countdown.Text="QR süresi doldu — pencereyi kapatıp yenisini oluşturun.";countdown.ForeColor=Color.Firebrick;timer.Stop();}else countdown.Text="Geçerlilik: "+((int)left.TotalSeconds)+" saniye";}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }

    public class PermissionsDialog : Form {
        readonly Dictionary<string,CheckBox> boxes=new Dictionary<string,CheckBox>();
        readonly Dictionary<string,object> user;
        public Dictionary<string,object> Result {get;private set;}
        static readonly string[] Keys={"report.view","customer.view","customer.edit","customer.delete","customer.export","product.view","product.edit","product.delete","product.export","product.cost","visit.view","visit.edit","visit.delete","visit.all","quote.view","quote.edit","quote.delete","quote.all","template.manage","user.manage","settings.manage","backup.restore","sync.manage"};
        public PermissionsDialog(Dictionary<string,object> u){
            user=J.Clone(u);Text="Kullanıcı Yetkileri — "+J.S(u,"name");Width=620;Height=720;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.5f);
            var p=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(18)};
            var pd=u.ContainsKey("permissions")?J.D(u["permissions"]):new Dictionary<string,object>();
            foreach(var k in Keys){var c=new CheckBox{Text=k,Width=520,Height=26,Checked=J.B(pd,k)};boxes[k]=c;p.Controls.Add(c);}
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};
            var ok=new Button{Text="Kaydet",Width=100,Height=34};var cancel=new Button{Text="İptal",Width=100,Height=34,DialogResult=DialogResult.Cancel};
            ok.Click+=(s,e)=>{var perms=new Dictionary<string,object>();foreach(var kv in boxes)perms[kv.Key]=kv.Value.Checked;var r=J.Clone(user);r["permissions"]=perms;Result=r;DialogResult=DialogResult.OK;Close();};
            bottom.Controls.Add(ok);bottom.Controls.Add(cancel);Controls.Add(p);Controls.Add(bottom);AcceptButton=ok;CancelButton=cancel;
        }
    }
}
