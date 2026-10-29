"""Compose actual Blender renders into a labelled review sheet (Pillow)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json
root=Path(__file__).resolve().parents[2]
folder=root/'Docs/Art/Scrap'
records=json.loads((root/'Assets/Eco/Data/Models/OriginalScrap/catalog.json').read_text())
canvas=Image.new('RGB',(1800,1570),'#17272d'); draw=ImageDraw.Draw(canvas)
fonts=Path('C:/Windows/Fonts')
def font(size,bold=False): return ImageFont.truetype(str(fonts/('segoeuib.ttf' if bold else 'segoeui.ttf')),size)
draw.text((64,32),'ECO CART  /  SCRAP COLLECTION',fill='#f3eddc',font=font(43,True))
draw.text((66,94),'12 original low-poly props  ·  Food / Paper / Plastic / Glass',fill='#a9bdc0',font=font(23))
accent=['#ffdb36','#f7f8f6','#b879e7','#78c3f7']
names=['Apple core','Banana peel','Fish bones','Milk carton','Folded newspaper','Crumpled paper','Crushed bottle','Yogurt cup','Detergent jug','Glass bottle','Glass jar','Broken bottle']
for i,rec in enumerate(records):
    col=i//3; row=i%3; x=56+col*432; y=204+row*435
    if row==0:
        draw.rounded_rectangle((x,151,x+412,191),radius=9,fill=accent[col])
        draw.text((x+17,153),rec['category'].upper(),font=font(22,True),fill='#243137')
    draw.rounded_rectangle((x,y,x+412,y+416),radius=18,fill='#e8eddf')
    im=Image.open(folder/(rec['name']+'.png')).convert('RGBA'); im.thumbnail((408,348))
    canvas.paste(im,(x+(412-im.width)//2,y+4),im)
    draw.text((x+22,y+348),names[i],font=font(24,True),fill='#263b40')
    draw.text((x+22,y+383),f"{rec['triangles']} triangles",font=font(17),fill='#637a7c')
draw.text((64,1525),'FBX + editable Blender source  /  shared palette  /  Unity metre scale',font=font(19),fill='#a9bdc0')
canvas.save(folder/'ScrapCollection.png')
print(folder/'ScrapCollection.png')
