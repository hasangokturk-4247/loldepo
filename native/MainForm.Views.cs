using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DecodGrmNative {
    public partial class MainForm {
        void ShowDashboard(){
            headerTitle.Text="Ana Rapor";
            var wrap=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(2),WrapContents=true};
            var cs=List("customers"); var vs=List("visits"); var qs=List("quotes");
            wrap.Controls.Add(StatCard("Müşteri",cs.Count.ToString()));
            wrap.Controls.Add(StatCard("Ziyaret",vs.Count.ToString()));
            wrap.Controls.Add(StatCard("Teklif",qs.Count.ToString()));
            wrap.Controls.Add(StatCard("Bekleyen Sync",J.S(root,"pendingSync","0")));
            var cloud=Card(740,125);cloud.Controls.Add(new Label{Text="G-Cloud",Left=16,Top=14,Width=180,Height=28,Font=new Font("Segoe UI Semibold",15),ForeColor=Navy});
            cloud.Controls.Add(new Label{Text=J.S(settings,"lastSyncStatus","Hazır")+"\r\nSon eşitleme: "+J.S(settings,"lastSyncAt","Henüz yok")+"   •   Çakışma: "+J.S(root,"cloudConflictCount","0"),Left=16,Top=49,Width=680,Height=55,ForeColor=Muted});
            wrap.Controls.Add(cloud);
            var due=J.LD(root.ContainsKey("due")?root["due"]:null);
            var box=Card(740,Math.Max(150,70+Math.Min(due.Count,7)*34));box.Controls.Add(new Label{Text="Yaklaşan / Geciken",Left=16,Top=12,Width=300,Height=28,Font=new Font("Segoe UI Semibold",15),ForeColor=Navy});
            int y=47;if(due.Count==0)box.Controls.Add(new Label{Text="Yaklaşan veya geciken kayıt yok.",Left=16,Top=y,Width=680,Height=26,ForeColor=Muted});
            foreach(var d in due.Take(7)){box.Controls.Add(new Label{Text=(J.B(d,"overdue")?"GECİKMİŞ  ":"") + J.S(d,"date")+"  "+J.S(d,"subject")+"  "+J.S(d,"person"),Left=16,Top=y,Width=690,Height=28,ForeColor=J.B(d,"overdue")?Color.Firebrick:TextC});y+=31;}
            wrap.Controls.Add(box);body.Controls.Add(wrap);
        }
        Panel StatCard(string title,string value){var c=Card(210,110);c.Margin=new Padding(0,0,14,14);c.Controls.Add(new Label{Text=title,Left=16,Top=13,Width=170,Height=25,ForeColor=Muted});c.Controls.Add(new Label{Text=value,Left=16,Top=42,Width=170,Height=48,Font=new Font("Segoe UI Semibold",25),ForeColor=Navy});return c;}

        void ShowCustomers(){
            headerTitle.Text="Müşteriler";var grid=Grid();var tb=Toolbar();
            Button add=Primary("+ Yeni Müşteri"); Button edit=new Button{Text="Düzenle",AutoSize=true,Height=34}; Button del=new Button{Text="Sil",AutoSize=true,Height=34}; Button exp=new Button{Text="Excel Dışa Aktar",AutoSize=true,Height=34};
            add.Enabled=J.Perm(root,"customer.edit");edit.Enabled=add.Enabled;del.Enabled=J.Perm(root,"customer.delete");exp.Enabled=J.Perm(root,"customer.export");
            add.Click+=(s,e)=>EditCustomer(null);edit.Click+=(s,e)=>EditCustomer(Selected(grid));del.Click+=(s,e)=>Delete("customer",Selected(grid),"code","Müşteri");exp.Click+=(s,e)=>Export("customers","G-Rm_Musteriler");
            tb.Controls.AddRange(new Control[]{add,edit,del,exp});body.Controls.Add(grid);body.Controls.Add(tb);
            AddCols(grid,new[]{"Kod","Firma","Şehir","İlçe","Sınıf","Grup","Telefon","Bölge"});
            foreach(var x in List("customers"))AddRow(grid,x,J.S(x,"code"),J.S(x,"name"),J.S(x,"city"),J.S(x,"district"),J.S(x,"customerClass"),J.S(x,"group"),J.S(x,"phone"),J.S(x,"region"));
            grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&add.Enabled)EditCustomer(Selected(grid));};
        }
        void EditCustomer(Dictionary<string,object> x){
            var defs=new List<FieldDef>{new FieldDef("Müşteri Kodu","code"),new FieldDef("Firma / Müşteri","name"),new FieldDef("Şehir","city"),new FieldDef("İlçe","district"),new FieldDef("Bölge","region"),new FieldDef("Adres","address","multiline"),new FieldDef("Telefon","phone"),new FieldDef("Vergi No","taxNo"),new FieldDef("Vergi Dairesi","taxOffice"),new FieldDef("Sınıf (A/B/C)","customerClass"),new FieldDef("Grup","group"),new FieldDef("İlgi Alanları","interests"),new FieldDef("İş Türü","businessType"),new FieldDef("Aktif","active","bool"),new FieldDef("Not","notes","multiline")};
            var seed=x??new Dictionary<string,object>{{"customerClass","B"},{"active",true}};
            using(var d=new EntityDialog(x==null?"Yeni Müşteri":"Müşteriyi Düzenle",defs,seed))if(d.ShowDialog(this)==DialogResult.OK){try{api.SaveEntity("customer",d.Result);Reload(false);}catch(Exception ex){Error(ex);}}
        }

        void ShowProducts(){
            headerTitle.Text="Ürünler";var grid=Grid();var tb=Toolbar();
            Button add=Primary("+ Yeni Ürün"); Button edit=new Button{Text="Düzenle",AutoSize=true,Height=34}; Button del=new Button{Text="Sil",AutoSize=true,Height=34}; Button exp=new Button{Text="Excel Dışa Aktar",AutoSize=true,Height=34};
            add.Enabled=J.Perm(root,"product.edit");edit.Enabled=add.Enabled;del.Enabled=J.Perm(root,"product.delete");exp.Enabled=J.Perm(root,"product.export");
            add.Click+=(s,e)=>EditProduct(null);edit.Click+=(s,e)=>EditProduct(Selected(grid));del.Click+=(s,e)=>Delete("product",Selected(grid),"code","Ürün");exp.Click+=(s,e)=>Export("products","G-Rm_Urunler");
            tb.Controls.AddRange(new Control[]{add,edit,del,exp});body.Controls.Add(grid);body.Controls.Add(tb);
            AddCols(grid,new[]{"Kod","Ürün","Marka","Kategori","Birim","Satış","Para","KDV"});
            foreach(var x in List("products"))AddRow(grid,x,J.S(x,"code"),J.S(x,"name"),J.S(x,"brand"),J.S(x,"category"),J.S(x,"unit"),J.S(x,"salesEx"),J.S(x,"currency"),J.S(x,"vat"));
            grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&add.Enabled)EditProduct(Selected(grid));};
        }
        void EditProduct(Dictionary<string,object> x){
            var defs=new List<FieldDef>{new FieldDef("Ürün Kodu","code"),new FieldDef("Ürün Adı","name"),new FieldDef("Birim","unit"),new FieldDef("Satış Fiyatı","salesEx","double"),new FieldDef("Para Birimi","currency"),new FieldDef("KDV %","vat","double"),new FieldDef("Marka","brand"),new FieldDef("Kategori","category"),new FieldDef("Aktif","active","bool")};
            var seed=x??new Dictionary<string,object>{{"unit","Adet"},{"currency","TRY"},{"vat",20.0},{"active",true}};
            using(var d=new EntityDialog(x==null?"Yeni Ürün":"Ürünü Düzenle",defs,seed))if(d.ShowDialog(this)==DialogResult.OK){try{api.SaveEntity("product",d.Result);Reload(false);}catch(Exception ex){Error(ex);}}
        }

        void ShowVisits(){
            headerTitle.Text="Ziyaretler";var grid=Grid();var tb=Toolbar();
            Button add=Primary("+ Yeni Ziyaret"); Button edit=new Button{Text="Düzenle",AutoSize=true,Height=34}; Button del=new Button{Text="Sil",AutoSize=true,Height=34}; Button exp=new Button{Text="Excel Dışa Aktar",AutoSize=true,Height=34};
            add.Enabled=J.Perm(root,"visit.edit");edit.Enabled=add.Enabled;del.Enabled=J.Perm(root,"visit.delete");exp.Enabled=J.Perm(root,"visit.view");
            add.Click+=(s,e)=>EditVisit(null);edit.Click+=(s,e)=>EditVisit(Selected(grid));del.Click+=(s,e)=>Delete("visit",Selected(grid),"id","Ziyaret");exp.Click+=(s,e)=>Export("visits","G-Rm_Ziyaretler");
            tb.Controls.AddRange(new Control[]{add,edit,del,exp});body.Controls.Add(grid);body.Controls.Add(tb);
            AddCols(grid,new[]{"Tarih","Müşteri","Görüşülen","Ziyaretçi","Durum","Sonraki Tarih","Not"});
            foreach(var x in List("visits").OrderByDescending(z=>J.S(z,"date")))AddRow(grid,x,J.S(x,"date"),J.S(x,"customerName"),J.S(x,"contactPerson"),J.S(x,"visitor"),J.S(x,"status"),J.S(x,"nextVisitDate"),J.S(x,"note"));
            grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&add.Enabled)EditVisit(Selected(grid));};
        }
        void EditVisit(Dictionary<string,object> x){
            var defs=new List<FieldDef>{new FieldDef("Tarih","date"),new FieldDef("Müşteri Kodu","customerCode"),new FieldDef("Müşteri Adı","customerName"),new FieldDef("Görüşülen Kişi","contactPerson"),new FieldDef("Ziyaretçi","visitor"),new FieldDef("Durum","status"),new FieldDef("Not","note","multiline"),new FieldDef("Sonraki Ziyaret","nextVisitDate")};
            var seed=x??new Dictionary<string,object>{{"date",DateTime.Today.ToString("yyyy-MM-dd")},{"visitor",J.S(root,"profileName")},{"status","Görüşüldü"}};
            using(var d=new EntityDialog(x==null?"Yeni Ziyaret":"Ziyareti Düzenle",defs,seed))if(d.ShowDialog(this)==DialogResult.OK){try{api.SaveEntity("visit",d.Result);Reload(false);}catch(Exception ex){Error(ex);}}
        }

        void ShowReminders(){
            headerTitle.Text="Ziyaret Hatırlatmaları";var grid=Grid();var tb=Toolbar();
            Button add=Primary("+ Hatırlatma"); Button edit=new Button{Text="Düzenle",AutoSize=true,Height=34}; Button done=new Button{Text="Tamamlandı",AutoSize=true,Height=34}; Button snooze=new Button{Text="1 Gün Ertele",AutoSize=true,Height=34}; Button del=new Button{Text="Sil",AutoSize=true,Height=34};
            bool can=J.Perm(root,"visit.edit");add.Enabled=edit.Enabled=done.Enabled=snooze.Enabled=can;del.Enabled=J.Perm(root,"visit.delete");
            add.Click+=(s,e)=>EditReminder(null);edit.Click+=(s,e)=>EditReminder(Selected(grid));done.Click+=(s,e)=>ReminderAction(Selected(grid),"done");snooze.Click+=(s,e)=>ReminderAction(Selected(grid),"snooze");del.Click+=(s,e)=>Delete("reminder",Selected(grid),"id","Hatırlatma");
            tb.Controls.AddRange(new Control[]{add,edit,done,snooze,del});body.Controls.Add(grid);body.Controls.Add(tb);
            AddCols(grid,new[]{"Tarih","Saat","Konu","Personel","Tür","Durum","Not"});
            foreach(var x in List("reminders").OrderBy(z=>J.S(z,"date")))AddRow(grid,x,J.S(x,"date"),J.S(x,"time"),J.S(x,"subject"),J.S(x,"person"),J.S(x,"kind"),J.S(x,"status"),J.S(x,"note"));
        }
        void EditReminder(Dictionary<string,object> x){
            var defs=new List<FieldDef>{new FieldDef("Konu","subject"),new FieldDef("Tarih","date"),new FieldDef("Saat","time"),new FieldDef("Personel","person"),new FieldDef("Tür","kind"),new FieldDef("Durum","status"),new FieldDef("Not","note","multiline")};
            var seed=x??new Dictionary<string,object>{{"date",DateTime.Today.ToString("yyyy-MM-dd")},{"person",J.S(root,"profileName")},{"status","Bekliyor"}};
            using(var d=new EntityDialog(x==null?"Yeni Hatırlatma":"Hatırlatmayı Düzenle",defs,seed))if(d.ShowDialog(this)==DialogResult.OK){try{api.SaveEntity("reminder",d.Result);Reload(false);}catch(Exception ex){Error(ex);}}
        }
        void ReminderAction(Dictionary<string,object> x,string action){if(x==null)return;try{api.ReminderAction(J.S(x,"id"),action);Reload(false);}catch(Exception ex){Error(ex);}}

        void ShowQuotes(){
            headerTitle.Text="Teklifler";var grid=Grid();var tb=Toolbar();
            Button add=Primary("+ Yeni Teklif"); Button edit=new Button{Text="Düzenle",AutoSize=true,Height=34}; Button del=new Button{Text="Sil",AutoSize=true,Height=34}; Button exp=new Button{Text="Excel Dışa Aktar",AutoSize=true,Height=34};
            add.Enabled=J.Perm(root,"quote.edit");edit.Enabled=add.Enabled;del.Enabled=J.Perm(root,"quote.delete");exp.Enabled=J.Perm(root,"quote.view");
            add.Click+=(s,e)=>EditQuote(null);edit.Click+=(s,e)=>EditQuote(Selected(grid));del.Click+=(s,e)=>Delete("quote",Selected(grid),"id","Teklif");exp.Click+=(s,e)=>Export("quotes","G-Rm_Teklifler");
            tb.Controls.AddRange(new Control[]{add,edit,del,exp});body.Controls.Add(grid);body.Controls.Add(tb);
            AddCols(grid,new[]{"Teklif No","Tarih","Müşteri","Hazırlayan","Durum","Takip","Kalem","Toplam"});
            foreach(var x in List("quotes").OrderByDescending(z=>J.S(z,"date"))){var lines=x.ContainsKey("lines")?J.LD(x["lines"]):new List<Dictionary<string,object>>();double total=0;foreach(var l in lines)total+=J.F(l,"amount");AddRow(grid,x,J.S(x,"no"),J.S(x,"date"),J.S(x,"customerName"),J.S(x,"preparedBy"),J.S(x,"status"),J.S(x,"followUpDate"),lines.Count.ToString(),total.ToString("N2"));}
            grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&add.Enabled)EditQuote(Selected(grid));};
        }
        void EditQuote(Dictionary<string,object> x){using(var d=new QuoteDialog(x??new Dictionary<string,object>(),J.S(root,"profileName")))if(d.ShowDialog(this)==DialogResult.OK){try{api.SaveEntity("quote",d.Result);Reload(false);}catch(Exception ex){Error(ex);}}}

        void ShowTeam(){
            headerTitle.Text="Ekip & Yetkiler";var grid=Grid();var tb=Toolbar();var edit=new Button{Text="Yetkileri Düzenle",AutoSize=true,Height=34};edit.Enabled=J.Perm(root,"user.manage");edit.Click+=(s,e)=>{var u=Selected(grid);if(u==null)return;using(var d=new PermissionsDialog(u))if(d.ShowDialog(this)==DialogResult.OK){try{api.SaveEntity("user",d.Result);Reload(false);}catch(Exception ex){Error(ex);}}};tb.Controls.Add(edit);body.Controls.Add(grid);body.Controls.Add(tb);
            AddCols(grid,new[]{"Ad Soyad","Rol","Lisans Yetkisi","Aktif","Cihaz","Son Güncelleme"});
            foreach(var x in List("users"))AddRow(grid,x,J.S(x,"name"),J.S(x,"role"),J.S(x,"licenseRole"),J.B(x,"active").ToString(),J.S(x,"deviceId"),UnixText(x,"updatedAt"));
        }

        void ShowSettings(){
            headerTitle.Text="Ayarlar";var panel=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Bg};var card=Card(760,545);card.Left=10;card.Top=10;
            var fields=new[]{new[]{"Firma Adı","companyName"},new[]{"Şehir","companyCity"},new[]{"Web","companyWeb"},new[]{"Telefon","companyPhone"},new[]{"E-posta","companyEmail"},new[]{"IBAN","companyIban"},new[]{"Hatırlatma (gün)","reminderDays"},new[]{"Sync aralığı (sn)","syncIntervalSec"}};
            var boxes=new Dictionary<string,TextBox>();int y=18;
            foreach(var f in fields){card.Controls.Add(new Label{Text=f[0],Left=18,Top=y+4,Width=185,Height=24});var t=new TextBox{Left=210,Top=y,Width=510,Height=28,Text=J.S(settings,f[1])};boxes[f[1]]=t;card.Controls.Add(t);y+=42;}
            CheckBox startup=new CheckBox{Text="Windows ile başlat",Left=210,Top=y,Width=230,Checked=J.B(settings,"startWithWindows")}; CheckBox traybox=new CheckBox{Text="Kapatınca sistem tepsisine küçült",Left=450,Top=y,Width=270,Checked=J.B(settings,"minimizeToTray",true)};card.Controls.Add(startup);card.Controls.Add(traybox);y+=46;
            var save=Primary("Ayarları Kaydet");save.Left=210;save.Top=y;save.Enabled=J.Perm(root,"settings.manage");save.Click+=(s,e)=>{try{var outp=J.Clone(settings);foreach(var kv in boxes){if(kv.Key=="reminderDays"||kv.Key=="syncIntervalSec"){int n;if(!int.TryParse(kv.Value.Text,out n))throw new Exception(kv.Key+" sayı olmalı");outp[kv.Key]=n;}else outp[kv.Key]=kv.Value.Text.Trim();}outp["startWithWindows"]=startup.Checked;outp["minimizeToTray"]=traybox.Checked;api.SaveSettings(outp);Reload(false);Info("Ayarlar kaydedildi.");}catch(Exception ex){Error(ex);}};card.Controls.Add(save);y+=50;
            var pair=Primary("Mobil Cihaz Eşleştir");pair.Left=210;pair.Top=y;pair.Width=180;pair.Enabled=J.B(root,"cloudReady");pair.Click+=(s,e)=>ShowPairing();card.Controls.Add(pair);
            var backup=new Button{Text="Yerel Yedek Al",Left=405,Top=y,Width=145,Height=34};backup.Enabled=J.Perm(root,"backup.restore");backup.Click+=(s,e)=>Backup();card.Controls.Add(backup);
            var restore=new Button{Text="Yedekten Dön",Left=565,Top=y,Width=145,Height=34};restore.Enabled=J.Perm(root,"backup.restore");restore.Click+=(s,e)=>Restore();card.Controls.Add(restore);y+=48;
            card.Controls.Add(new Label{Text="G-Cloud: "+J.S(settings,"lastSyncStatus","Hazır")+"   •   Son yedek: "+J.S(root,"cloudLastBackupAt","Henüz yok")+"\r\nYerel veri: "+J.S(root,"dataDir")+"\r\nYerel yedek: "+J.S(root,"localBackupDir"),Left=18,Top=y,Width=700,Height=72,ForeColor=Muted});
            panel.Controls.Add(card);body.Controls.Add(panel);
        }
        void ShowPairing(){try{var p=api.Pairing();DateTime exp;DateTime.TryParse(J.S(p,"expiresAt"),null,DateTimeStyles.RoundtripKind,out exp);using(var f=new PairingForm(J.S(p,"code"),J.S(p,"user"),J.S(p,"company"),exp.ToLocalTime()))f.ShowDialog(this);}catch(Exception ex){Error(ex);}}

        void Delete(string kind,Dictionary<string,object> x,string idKey,string label){if(x==null)return;if(!Ask(label+" silinsin mi?"))return;try{api.DeleteEntity(kind,J.S(x,idKey));Reload(false);}catch(Exception ex){Error(ex);}}
        void AddCols(DataGridView g,string[] cols){g.Columns.Clear();foreach(var c in cols)g.Columns.Add(c,c);}
        void AddRow(DataGridView g,Dictionary<string,object> tag,params string[] vals){int i=g.Rows.Add(vals);g.Rows[i].Tag=tag;}
        string UnixText(Dictionary<string,object> x,string key){long v=0;long.TryParse(J.S(x,key),out v);return v>0?DateTimeOffset.FromUnixTimeSeconds(v).LocalDateTime.ToString("dd.MM.yyyy HH:mm"):"";}
    }
}
