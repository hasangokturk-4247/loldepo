from pathlib import Path
p=Path('build.py')
s=p.read_text(encoding='utf-8')
s=s.replace("S=requests.Session(); S.headers.update({'User-Agent':'Mozilla/5.0 Chrome/126','Accept-Language':'tr-TR,tr;q=.9,en;q=.6'})", "S=requests.Session(); S.headers.update({'User-Agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126 Safari/537.36','Accept-Language':'tr-TR,tr;q=.9,en;q=.6'})")
s=s.replace("r=get(u); s=BeautifulSoup(r.text,'lxml')", "r=get(u); r.encoding='utf-8'; s=BeautifulSoup(r.text,'lxml')")
s=s.replace("""def get(u,n=4):
    e=None
    for i in range(n):
        try:
            r=S.get(u,timeout=60); r.raise_for_status(); return r
        except Exception as x: e=x; time.sleep(1+i)
    raise e
""", """def get(u,n=3):
    e=None
    for i in range(n):
        try:
            h={'Referer':'https://universe.leagueoflegends.com/','Origin':'https://universe.leagueoflegends.com'} if ('contentstack.io' in u or 'cmsassets.rgpub.io' in u) else None
            r=S.get(u,timeout=45,headers=h)
            if 400 <= r.status_code < 500 and r.status_code not in (408,429): r.raise_for_status()
            r.raise_for_status(); return r
        except Exception as x:
            e=x
            code=getattr(getattr(x,'response',None),'status_code',None)
            if code and 400 <= code < 500 and code not in (408,429): break
            time.sleep(1+i)
    raise e
""")
s=s.replace("""def img(u,key,dark=.94,maxpx=1350):
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
""", """def img(u,key,dark=.94,maxpx=1350,fallback=None):
    out=C/(safe(key)+'.jpg')
    if out.exists() and out.stat().st_size>1000:return out
    transformed=(u+('&' if '?' in u else '?')+'auto=webp&width=1600') if u and 'contentstack.io' in u else None
    for source in [x for x in (u,transformed,fallback) if x]:
        try:
            b=get(source,1).content; raw=C/(safe(key)+'.raw'); raw.write_bytes(b)
            with Image.open(raw) as im:
                im=im.convert('RGB'); scale=min(1,maxpx/max(im.size))
                if scale<1: im=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.LANCZOS)
                if dark!=1: im=ImageEnhance.Brightness(im).enhance(dark)
                im.save(out,'JPEG',quality=78,optimize=True,progressive=True)
            raw.unlink(missing_ok=True); return out
        except Exception as e: print('Görsel kaynağı atlandı',source,e)
    return None
""")
s=s.replace("""def para(c,t,x,yt,w,h,style=PB):
    p=Paragraph(t.replace('\\n\\n','<br/><br/>'),style); _,a=p.wrap(w,h); p.drawOn(c,x,yt-a); return a
""", """def para(c,t,x,yt,w,h,style=PB):
    text=html.escape(clean(t),quote=False)
    p=Paragraph(text,style); _,a=p.wrap(w,h); p.drawOn(c,x,yt-a); return a
""")
s=s.replace("""def alpha(c,col,a,x,y,w,h):
    c.saveState();
    try:c.setFillAlpha(a)
    except:pass
    c.setFillColor(col); c.rect(x,y,w,h,fill=1,stroke=0); c.restoreState()
""", """def alpha(c,col,a,x,y,w,h):
    c.saveState(); c.setFillColor(col)
    try:c.setFillAlpha(a)
    except:pass
    c.rect(x,y,w,h,fill=1,stroke=0); c.restoreState()
""")
s=s.replace("return {'u':u,'p':img(oi or fall,'champ_'+d['id'],.91),'story':paras(src,5),'desc':od}", "return {'u':u,'p':img(fall,'champ_'+d['id'],.91),'story':paras(src,5),'desc':od}")
s=s.replace("ver=get('https://ddragon.leagueoflegends.com/api/versions.json').json()[0]; base=get(f'https://ddragon.leagueoflegends.com/cdn/{ver}/data/tr_TR/champion.json').json()['data']; ds=[detail(ver,x['id']) for x in sorted(base.values(),key=lambda x:x['name'].casefold())];print(ver,len(ds))", "ver=get('https://ddragon.leagueoflegends.com/api/versions.json').json()[0]; base=get(f'https://ddragon.leagueoflegends.com/cdn/{ver}/data/tr_TR/champion.json').json()['data']; canonical=[x for x in base.values() if '_' not in x.get('id','')]; uniq={}\n    for x in canonical:\n        k=str(x.get('key') or x.get('name'))\n        if k not in uniq: uniq[k]=x\n    ds=[detail(ver,x['id']) for x in sorted(uniq.values(),key=lambda x:x['name'].casefold())];print(ver,len(ds))")
s=s.replace("ras=[]\n    for n,s,p in regs:", "ras=[]; regrep={'void':'Belveth','mount-targon':'Pantheon','freljord':'Ashe','shurima':'Azir','ixtal':'Qiyana','ionia':'Irelia','shadow-isles':'Viego','noxus':'Darius','demacia':'Garen','bilgewater':'MissFortune','piltover':'Caitlyn','zaun':'Jinx','bandle-city':'Teemo'}\n    for n,s,p in regs:")
s=s.replace("ras.append({'u':u,'p':img(oi,'reg_'+s,.92,1550),'desc':od,'texts':paras(src,6)})", "ras.append({'u':u,'p':img(oi,'reg_'+s,.92,1550,fallback=f\"https://ddragon.leagueoflegends.com/cdn/img/champion/splash/{regrep[s]}_0.jpg\"),'desc':od,'texts':paras(src,6)})")
s=s.replace("for a in aus:oi,od,_=og(a['url']);a['p']=img(oi,'au_'+a['name'],.95,1550);a['desc']=od", "altfb={'Yıldız Muhafızları':'Lux_7','Uzay Serüveni':'Yasuo_18','K/DA':'Ahri_15'}\n    for a in aus:oi,od,_=og(a['url']);a['p']=img(oi,'au_'+a['name'],.95,1550,fallback=f\"https://ddragon.leagueoflegends.com/cdn/img/champion/splash/{altfb[a['name']]}.jpg\");a['desc']=od")
s=s.replace("yy=226", "yy=225")
s=s.replace("concise(ds,175)", "concise(ds,135)")
s=s.replace("yy-=45", "yy-=39")
s=s.replace("c.setFillColor(colors.HexColor('#E8D9AF'));c.setFont('Body',6.2);c.drawString(41,47,f'Data Dragon {ver} • {a[\"u\"]}');b.end('ŞAMPİYON DOSYALARI',True)", "b.end('ŞAMPİYON DOSYALARI',True)")
p.write_text(s,encoding='utf-8')
print('hotfix applied')
