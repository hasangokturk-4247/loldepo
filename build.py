import os,re,html,time,zipfile,unicodedata,concurrent.futures
from pathlib import Path
from urllib.parse import urljoin
import requests
from bs4 import BeautifulSoup
from PIL import Image,ImageEnhance
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_JUSTIFY

R=Path(__file__).parent; O=R/'output'; C=R/'.cache'; O.mkdir(exist_ok=True); C.mkdir(exist_ok=True)
S=requests.Session(); S.headers.update({'User-Agent':'Mozilla/5.0 Chrome/126','Accept-Language':'tr-TR,tr;q=.9,en;q=.6'})
W,H=A4; GOLD=colors.HexColor('#C89B3C'); NAVY=colors.HexColor('#071521'); NAVY2=colors.HexColor('#0A2234'); PAPER=colors.HexColor('#F5F1E7'); INK=colors.HexColor('#171A1F'); MUTED=colors.HexColor('#68707B')

def get(u,n=4):
    e=None
    for i in range(n):
        try:
            r=S.get(u,timeout=60); r.raise_for_status(); return r
        except Exception as x: e=x; time.sleep(1+i)
    raise e

def clean(t): return re.sub(r'\s+',' ',html.unescape(re.sub(r'<[^>]+>',' ',t or ''))).strip()
def safe(t):
    t=unicodedata.normalize('NFKD',t); return re.sub(r'[^A-Za-z0-9_.-]+','_',''.join(x for x in t if not unicodedata.combining(x))).strip('_')
def concise(t,n=210):
    t=clean(t)
    if len(t)<=n:return t
    q=t[:n]; k=q.rfind('. '); return q[:k+1] if k>n*.55 else q.rsplit(' ',1)[0]+'...'
def og(u):
    try:
        r=get(u); s=BeautifulSoup(r.text,'lxml')
        def m(k):
            z=s.find('meta',attrs={'property':k}) or s.find('meta',attrs={'name':k}); return z.get('content') if z else ''
        im=m('og:image') or m('twitter:image'); im=urljoin(r.url,im) if im else ''
        return im,clean(m('og:description') or m('description')),r.text
    except Exception as e: print('OG hata',u,e); return '','',''
def paras(src,limit=5):
    if not src:return []
    s=BeautifulSoup(src,'lxml'); out=[]; seen=set(); bad=('cookie','çerez','gizlilik','keşfet','hikâyeyi oku','oyun bilgisi')
    for z in s.find_all(['p','blockquote','div']):
        t=clean(z.get_text(' ',strip=True)); k=safe(t[:100]).lower()
        if 120<len(t)<1600 and k not in seen and not any(b in t.lower() for b in bad): seen.add(k); out.append(t)
    out.sort(key=len,reverse=True); return out[:limit]
def img(u,key,dark=.94,maxpx=1350):
    if not u:return None
    out=C/(safe(key)+'.jpg')
    if out.exists() and out.stat().st_size>1000:return out
    try:
        b=get(u).content; raw=C/(safe(key)+'.raw'); raw.write_bytes(b)
        with Image.open(raw) as im:
            im=im.convert('RGB'); scale=min(1,maxpx/max(im.size));
            if scale<1: im=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.LANCZOS)
            if dark!=1: im=ImageEnhance.Brightness(im).enhance(dark)
            im.save(out,'JPEG',quality=78,optimize=True,progressive=True)
        raw.unlink(missing_ok=True); return out
    except Exception as e: print('Görsel hata',u,e); return None

def fonts():
    fd=C/'fonts'; fd.mkdir(exist_ok=True)
    urls=['https://cmsassets.rgpub.io/sanity/files/dsfx7636/news/4e0e30c1ebe0e4f8e86c80d45c9ce96ff469689f.zip','https://cmsassets.rgpub.io/sanity/files/dsfx7636/news/5997e78145e4c250ffed3cd3d76bb96d82f22553.zip']
    if not list(fd.rglob('*.ttf')):
        for i,u in enumerate(urls):
            try:
                z=fd/f'f{i}.zip'; z.write_bytes(get(u).content); zipfile.ZipFile(z).extractall(fd)
            except Exception as e: print('Font hata',e)
    fs=list(fd.rglob('*.ttf'))
    def pick(word,bold=False):
        a=[f for f in fs if word in f.name.lower() and ('bold' in f.name.lower())==bold and 'italic' not in f.name.lower()]
        if not a:a=[f for f in fs if word in f.name.lower() and 'italic' not in f.name.lower()]
        return a[0] if a else Path('/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf' if bold else '/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf')
    for n,p in [('Head',pick('beaufort')),('HeadB',pick('beaufort',True)),('Body',pick('spiegel')),('BodyB',pick('spiegel',True))]: pdfmetrics.registerFont(TTFont(n,str(p)))
fonts()
PB=ParagraphStyle('b',fontName='Body',fontSize=10.6,leading=15,textColor=INK,alignment=TA_JUSTIFY,spaceAfter=7)
PS=ParagraphStyle('s',fontName='Body',fontSize=8.1,leading=10.4,textColor=INK,alignment=TA_JUSTIFY)
PW=ParagraphStyle('w',fontName='Body',fontSize=7.2,leading=9.1,textColor=colors.white,alignment=TA_JUSTIFY)

def para(c,t,x,yt,w,h,style=PB):
    p=Paragraph(t.replace('\n\n','<br/><br/>'),style); _,a=p.wrap(w,h); p.drawOn(c,x,yt-a); return a
def crop(c,p,x,y,w,h):
    with Image.open(p) as im: iw,ih=im.size
    q=max(w/iw,h/ih); dw,dh=iw*q,ih*q; c.drawImage(str(p),x+(w-dw)/2,y+(h-dh)/2,dw,dh,mask='auto')
def alpha(c,col,a,x,y,w,h):
    c.saveState();
    try:c.setFillAlpha(a)
    except:pass
    c.setFillColor(col); c.rect(x,y,w,h,fill=1,stroke=0); c.restoreState()
def title(c,t,x,y,size=26,mw=None):
    while mw and pdfmetrics.stringWidth(t.upper(),'HeadB',size)>mw and size>12:size-=.7
    c.setFillColor(GOLD); c.setFont('HeadB',size); c.drawString(x,y,t.upper())
def foot(c,n,sec,dark=False):
    c.setStrokeColor(GOLD if dark else colors.HexColor('#D2C6A8')); c.line(38,27,W-38,27); c.setFillColor(colors.HexColor('#E8D9AF') if dark else MUTED); c.setFont('Body',7); c.drawString(38,15,sec); c.drawRightString(W-38,15,str(n))
class Book:
    def __init__(self,p): self.c=canvas.Canvas(str(p),pagesize=A4,pageCompression=1); self.n=1
    def end(self,sec,dark=False,footer=True):
        if footer:foot(self.c,self.n,sec,dark)
        self.c.showPage(); self.n+=1
    def left(self):
        if self.n%2: self.c.setFillColor(PAPER); self.c.rect(0,0,W,H,fill=1,stroke=0); self.end('LOL ANSİKLOPEDİ')

def slug(name,id):
    mp={'MonkeyKing':'wukong','Nunu':'nunu','Chogath':'chogath','Khazix':'khazix','Kaisa':'kaisa','RekSai':'reksai','Velkoz':'velkoz','Belveth':'belveth','KSante':'ksante','DrMundo':'drmundo','JarvanIV':'jarvaniv','MissFortune':'missfortune','TahmKench':'tahmkench','TwistedFate':'twistedfate','MasterYi':'masteryi','XinZhao':'xinzhao','AurelionSol':'aurelionsol','Renata':'renata-glasc'}
    return mp.get(id,re.sub('[^a-z0-9]','',unicodedata.normalize('NFKD',name).encode('ascii','ignore').decode().lower()))
def role(tags):
    m={'Fighter':'Dövüşçü','Tank':'Tank','Mage':'Büyücü','Assassin':'Suikastçı','Marksman':'Nişancı','Support':'Destek'}; return ' / '.join(m.get(x,x) for x in tags)
def detail(ver,id): return get(f'https://ddragon.leagueoflegends.com/cdn/{ver}/data/tr_TR/champion/{id}.json').json()['data'][id]
def champ_asset(ver,d):
    u=f"https://universe.leagueoflegends.com/tr_TR/champion/{slug(d['name'],d['id'])}/"; oi,od,src=og(u)
    fall=f"https://ddragon.leagueoflegends.com/cdn/img/champion/splash/{d['id']}_0.jpg"
    return {'u':u,'p':img(oi or fall,'champ_'+d['id'],.91),'story':paras(src,5),'desc':od}

def cover(b,im,ver,count):
    c=b.c; c.setFillColor(NAVY); c.rect(0,0,W,H,fill=1,stroke=0)
    if im:crop(c,im,0,0,W,H);alpha(c,NAVY,.66,0,0,W,H)
    c.setStrokeColor(GOLD);c.setLineWidth(1.2);c.rect(28,28,W-56,H-56,fill=0,stroke=1)
    c.setFillColor(GOLD);c.setFont('HeadB',37);c.drawCentredString(W/2,H*.62,'LOL ANSİKLOPEDİ')
    c.setFillColor(colors.white);c.setFont('Head',16);c.drawCentredString(W/2,H*.56,'RUNETERRA • ALTERNATİF EVRENLER • ŞAMPİYONLAR')
    c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('BodyB',9.5);c.drawCentredString(W/2,H*.50,f'{count} ŞAMPİYON • İKİ SAYFALIK KARŞILIKLI DOSYALAR')
    c.setFont('Body',7.5);c.drawCentredString(W/2,58,f'Riot Games Universe ve Data Dragon • {ver}');b.end('',True,False)
def toc(b,regs,chs):
    c=b.c;c.setFillColor(PAPER);c.rect(0,0,W,H,fill=1,stroke=0);title(c,'İçindekiler',45,H-70,28)
    para(c,'Bölgeler yaklaşık tarihsel katmanlarına göre; şampiyonlar alfabetik olarak sıralanmıştır. Her şampiyon solda görsel, özellikler ve yetenekler; sağda resmî Universe biyografisi olacak şekilde iki sayfaya ayrılmıştır.',48,H-105,W-96,90)
    y=H-175;c.setFont('HeadB',13);c.setFillColor(GOLD);c.drawString(48,y,'I. BÖLGELER');y-=23
    c.setFont('Body',8);c.setFillColor(INK)
    for i,r in enumerate(regs,1):c.drawString(56,y,f'{i:02d}. {r[0]}');c.setFillColor(MUTED);c.drawRightString(W-48,y,r[2][:50]);c.setFillColor(INK);y-=15
    y-=8;c.setFont('HeadB',13);c.setFillColor(GOLD);c.drawString(48,y,'II. ALTERNATİF EVRENLER');y-=22;c.setFont('Body',8.5);c.setFillColor(INK)
    for x in ['Yıldız Muhafızları','Uzay Serüveni','K/DA']:c.drawString(56,y,x);y-=16
    y-=8;c.setFont('HeadB',13);c.setFillColor(GOLD);c.drawString(48,y,'III. ŞAMPİYONLAR');y-=25
    letters=[]
    for x in chs:
        if x['name'][0].upper() not in letters:letters.append(x['name'][0].upper())
    c.setFillColor(NAVY2);c.setFont('HeadB',17);c.drawString(56,y,'  •  '.join(letters));b.end('İÇİNDEKİLER')
def region(b,r,a):
    b.left();c=b.c;name,sl,period=r;c.setFillColor(NAVY);c.rect(0,0,W,H,fill=1,stroke=0)
    if a['p']:crop(c,a['p'],0,0,W,H);alpha(c,NAVY,.62,0,0,W,H)
    c.setStrokeColor(GOLD);c.rect(28,28,W-56,H-56,fill=0,stroke=1);title(c,name,44,H-105,29,W-88);c.setFillColor(colors.white);c.setFont('Head',12);c.drawString(44,H-135,period)
    alpha(c,NAVY,.87,38,52,W-76,120);para(c,concise(a['desc'] or (a['texts'][0] if a['texts'] else ''),430),52,154,W-104,90,PW);c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('Body',6.5);c.drawString(52,64,a['u']);b.end('BÖLGELER',True)
    c.setFillColor(PAPER);c.rect(0,0,W,H,fill=1,stroke=0);title(c,name,45,H-65,24,W-90);c.setFillColor(MUTED);c.setFont('BodyB',8);c.drawString(47,H-88,'RUNETERRA BÖLGE DOSYASI');y=H-120
    ts=a['texts'] or [a['desc']];heads=['Coğrafya ve Kimlik','Tarih ve Köken','Toplum ve Güç','Güncel Çatışmalar']
    for h,t in zip(heads,ts[:4]):c.setFillColor(GOLD);c.setFont('HeadB',12);c.drawString(47,y,h.upper());y-=16;z=para(c,t,47,y,W-94,max(90,y-60),PS if len(t)>900 else PB);y-=z+10
    b.end('BÖLGELER')
def alt(b,a):
    b.left();c=b.c;c.setFillColor(NAVY);c.rect(0,0,W,H,fill=1,stroke=0)
    if a['p']:crop(c,a['p'],0,0,W,H);alpha(c,NAVY,.57,0,0,W,H)
    c.setStrokeColor(GOLD);c.rect(28,28,W-56,H-56,fill=0,stroke=1);title(c,a['name'],44,H-105,28,W-88);c.setFillColor(colors.white);c.setFont('Head',12);c.drawString(44,H-135,a['tag'])
    alpha(c,NAVY,.87,38,52,W-76,122);para(c,concise(a['desc'] or a['sum'],430),52,156,W-104,92,PW);b.end('ALTERNATİF EVRENLER',True)
    c.setFillColor(PAPER);c.rect(0,0,W,H,fill=1,stroke=0);title(c,a['name'],45,H-65,24,W-90);c.setFillColor(MUTED);c.setFont('BodyB',8);c.drawString(47,H-88,'ALTERNATİF EVREN DOSYASI');y=H-125
    for h,t in [('EVRENİN ÇEKİRDEĞİ',a['sum']),('ÖNE ÇIKAN KARAKTERLER',' • '.join(a['cast'])),('KANON NOTU',a['note'])]:c.setFillColor(GOLD);c.setFont('HeadB',12);c.drawString(47,y,h);y-=17;z=para(c,t,47,y,W-94,220,PB);y-=z+18
    b.end('ALTERNATİF EVRENLER')
def champ_left(b,d,a,ver):
    c=b.c;c.setFillColor(NAVY);c.rect(0,0,W,H,fill=1,stroke=0)
    if a['p']:crop(c,a['p'],0,0,W,H);alpha(c,NAVY,.24,0,0,W,H)
    alpha(c,NAVY,.91,0,H-130,W,130);title(c,d['name'],38,H-56,28,W-76);c.setFillColor(colors.white);c.setFont('Head',12);c.drawString(40,H-82,d.get('title','').upper());c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('BodyB',8);c.drawString(40,H-104,role(d.get('tags',[]))+' • '+d.get('partype',''))
    alpha(c,NAVY,.91,27,38,W-54,340);x=41;y=355;c.setFillColor(GOLD);c.setFont('HeadB',11);c.drawString(x,y,'ÖZELLİKLER');info=d.get('info',{});y-=20
    for lab,key in [('Saldırı','attack'),('Savunma','defense'),('Büyü','magic'),('Zorluk','difficulty')]:
        c.setFillColor(colors.HexColor('#B9C0C8'));c.roundRect(x,y,118,4,2,fill=1,stroke=0);c.setFillColor(GOLD);c.roundRect(x,y,11.8*info.get(key,0),4,2,fill=1,stroke=0);c.setFillColor(colors.white);c.setFont('BodyB',6.2);c.drawString(x,y+7,lab.upper());y-=20
    st=d.get('stats',{});c.setFillColor(colors.white);c.setFont('Body',6.7);sy=335
    for k,v in [('Can',st.get('hp')),('Hareket',st.get('movespeed')),('Zırh',st.get('armor')),('Saldırı',st.get('attackdamage')),('Menzil',st.get('attackrange'))]:c.drawString(180,sy,f'{k}: {v}');sy-=14
    c.setFillColor(GOLD);c.setFont('HeadB',11);c.drawString(x,247,'YETENEKLER');abs=[('P',d.get('passive',{}).get('name','Pasif'),d.get('passive',{}).get('description',''))]+[(k,s.get('name',''),s.get('description','')) for k,s in zip('QWER',d.get('spells',[]))]
    yy=226
    for k,nm,ds in abs[:5]:
        c.setFillColor(GOLD);c.circle(x+10,yy-6,10,fill=1,stroke=0);c.setFillColor(NAVY);c.setFont('BodyB',7);c.drawCentredString(x+10,yy-9,k);c.setFillColor(GOLD);c.setFont('HeadB',8.5);c.drawString(x+28,yy,nm);para(c,concise(ds,175),x+28,yy-5,W-x-65,33,PW);yy-=45
    c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('Body',6.2);c.drawString(41,47,f'Data Dragon {ver} • {a["u"]}');b.end('ŞAMPİYON DOSYALARI',True)
def champ_right(b,d,a):
    c=b.c;c.setFillColor(PAPER);c.rect(0,0,W,H,fill=1,stroke=0);c.setFillColor(NAVY2);c.rect(0,H-118,W,118,fill=1,stroke=0);title(c,d['name'],44,H-57,27,W-88);c.setFillColor(colors.white);c.setFont('Head',12);c.drawString(46,H-83,d.get('title','').upper());c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('BodyB',7.5);c.drawString(46,H-103,'RESMÎ UNIVERSE BİYOGRAFİSİ')
    y=H-145;ps=a['story'] or [d.get('lore') or d.get('blurb','')];
    for t in ps[:4]:
        z=para(c,t,46,y,W-92,max(90,y-135),PB);y-=z+10
        if y<185:break
    if y>170:c.setFillColor(GOLD);c.setFont('HeadB',11);c.drawString(46,y,'OYUN KİMLİĞİ');y-=17;para(c,f"{d['name']}, {role(d.get('tags',[])).lower()} rol çizgisinde yer alır. {d.get('passive',{}).get('name','Pasif')} pasifi ve Q-W-E-R yetenekleri karakterin temel oyun döngüsünü oluşturur.",46,y,W-92,80,PB)
    c.setFillColor(NAVY2);c.roundRect(44,47,W-88,65,5,fill=1,stroke=0);c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('BodyB',7);c.drawString(55,93,'RESMÎ KAYNAK');c.setFillColor(colors.white);c.setFont('Body',6.4);c.drawString(55,77,a['u']);c.drawString(55,62,'Görsel ve biyografi: Riot Games Universe. Oyun verisi: Riot Data Dragon.');b.end('ŞAMPİYON DOSYALARI')

def main():
    ver=get('https://ddragon.leagueoflegends.com/api/versions.json').json()[0]; base=get(f'https://ddragon.leagueoflegends.com/cdn/{ver}/data/tr_TR/champion.json').json()['data']; ds=[detail(ver,x['id']) for x in sorted(base.values(),key=lambda x:x['name'].casefold())];print(ver,len(ds))
    assets={}
    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as ex:
        fs={ex.submit(champ_asset,ver,d):d['id'] for d in ds}
        for i,f in enumerate(concurrent.futures.as_completed(fs),1):assets[fs[f]]=f.result();print('assets',i,'/',len(ds)) if i%20==0 else None
    regs=[('Hiçlik','void','Kâinatın doğuşuyla birlikte - zaman dışı tehdit'),('Targon','mount-targon','Göksel çağlar - en eski inanç katmanları'),('Freljord','freljord','Üç Kız Kardeş ve Gözcüler çağı'),('Shurima','shurima','Yükselmiş İmparatorluk çağı'),('Ixtal','ixtal','Erken imparatorluklar ve element ustalığı'),('Ionia','ionia','Kadim ruhsal uygarlıklar'),('Gölge Adalar','shadow-isles','Kutsanmış Adalar ve Mahvoluş sonrası'),('Noxus','noxus','İmparatorluğun yükselişi'),('Demacia','demacia','Rün Savaşları sonrası krallık'),('Bilgewater','bilgewater','Deniz ticareti ve korsan şehir çağı'),('Piltover','piltover','İlerleme Şehri ve hextech çağı'),('Zaun','zaun',"Oshra Va'Zaun'dan modern yeraltı şehrine"),('Bandle Şehri','bandle-city','Ruh âlemi - doğrusal zamanın dışında')]
    ras=[]
    for n,s,p in regs:
        u=f'https://universe.leagueoflegends.com/tr_TR/region/{s}/';oi,od,src=og(u);ras.append({'u':u,'p':img(oi,'reg_'+s,.92,1550),'desc':od,'texts':paras(src,6)})
    aus=[{'name':'Yıldız Muhafızları','tag':'Kozmik ışık, dostluk ve fedakârlık','url':'https://universe.leagueoflegends.com/tr_TR/star-guardian/','sum':'İlk Yıldız tarafından kozmik karanlığa karşı seçilen genç koruyucuların evrenidir. Parlak okul yaşamının ardında görev yükü, kayıp, yozlaşma ve yeniden doğuş bulunur.','cast':['Lux','Ahri','Jinx',"Kai'Sa",'Akali','Xayah','Rakan','Syndra','Zoe','Soraka','Ekko','Nilah'],'note':'Ana Runeterra kanonundan bağımsızdır; şampiyonların karakter çekirdeklerini büyülü gençlik anlatısı içinde yeniden kurar.'},{'name':'Uzay Serüveni','tag':'Ora, imparatorluklar ve galaktik kaçaklar','url':'https://universe.leagueoflegends.com/tr_TR/odyssey/','sum':'Galaksinin değerli maddesi Ora etrafında kurulan bilimkurgu evrenidir. Sabah Yıldızı mürettebatı özgürlük ve hayatta kalma peşindeyken Kayn’ın imparatorluk hırsıyla karşılaşır.','cast':['Yasuo','Jinx','Malphite','Sona','Ziggs','Kayn','Zed','Sivir','Aatrox',"Kha'Zix"],'note':'Ana kanondan bağımsız uzay operasıdır; şampiyonları yıldız gemileri ve kozmik fraksiyonlarla yeniden yorumlar.'},{'name':'K/DA','tag':'Sahne, kimlik ve küresel pop yıldızlığı','url':'https://universe.leagueoflegends.com/tr_TR/kda/','sum':"Ahri, Akali, Evelynn ve Kai'Sa'nın küresel pop grubunu merkezine alan modern müzik evrenidir. Seraphine iş birliğiyle performans, öz ifade ve ekip olmanın bedeli öne çıkar.",'cast':['Ahri','Akali','Evelynn',"Kai'Sa",'Seraphine'],'note':'Ana Runeterra büyü ve savaş sistemlerinden bağımsız çağdaş müzik dünyasıdır.'}]
    for a in aus:oi,od,_=og(a['url']);a['p']=img(oi,'au_'+a['name'],.95,1550);a['desc']=od
    b=Book(O/'LOL_Ansiklopedi_Riot_2026.pdf');cov=next((x['p'] for x in ras if x['p']),None) or next((x['p'] for x in assets.values() if x['p']),None);cover(b,cov,ver,len(ds));toc(b,regs,ds)
    for r,a in zip(regs,ras):region(b,r,a)
    for a in aus:alt(b,a)
    b.left()
    for i,d in enumerate(ds,1):champ_left(b,d,assets[d['id']],ver);champ_right(b,d,assets[d['id']]);print('pdf',i,'/',len(ds)) if i%10==0 else None
    b.c.setTitle('LOL Ansiklopedi - Riot Universe');b.c.setAuthor('Resmî olmayan hayran ansiklopedisi');b.c.save();print('DONE',b.n-1,(O/'LOL_Ansiklopedi_Riot_2026.pdf').stat().st_size)
if __name__=='__main__':main()
