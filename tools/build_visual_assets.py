"""Build the code-authored pixel motifs used by the machine series (requires CairoSVG).
The AI-authored machine_panels.png is intentionally kept as a checked-in source asset.
"""
from pathlib import Path
import cairosvg

ROOT = Path(__file__).resolve().parents[1] / 'mod/src/main/resources/assets/forever_skyblock'

def svg(name, width, height, parts):
    data = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" shape-rendering="crispEdges">' + ''.join(parts) + '</svg>'
    source = Path(__file__).parent / 'visual_sources' / (name + '.svg')
    source.parent.mkdir(parents=True, exist_ok=True)
    source.write_text(data)
    dest = ROOT / 'textures' / (name + '.png')
    dest.parent.mkdir(parents=True, exist_ok=True)
    cairosvg.svg2png(bytestring=data.encode(),write_to=str(dest))

def rect(x,y,w,h,c):
    opacity = f' fill-opacity="{int(c[7:9],16)/255}"' if c.startswith('#') and len(c)==9 else ''
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{c[:7]}"{opacity}/>' 
def path(d,c): return f'<path d="{d}" fill="{c}"/>'
def line(x,y,xx,yy,c): return f'<path d="M{x} {y}H{xx}" stroke="{c}"/>' if y==yy else f'<path d="M{x} {y}L{xx} {yy}" stroke="{c}"/>'

def plate(name,base,edge,accent,kind='panel'):
    p=[rect(0,0,32,32,base),rect(1,1,30,1,edge),rect(1,2,1,29,edge),rect(1,30,30,1,'#111c26'),rect(30,1,1,30,'#111c26')]
    if kind=='wood':
        for y in (8,16,24):p += [rect(2,y,28,1,'#382c26'),rect(2,y+1,28,1,'#927052')]
        for x,y in ((6,5),(19,11),(10,22),(23,27)):p+=[rect(x,y,5,1,'#a07c57')]
    elif kind=='soil':
        for i in range(45):p += [rect(2+(i*7)%28,2+(i*11)%28,2,1,['#463b2b','#62523a','#787046'][i%3])]
    elif kind=='ice':
        p += [path('M3 24L24 3H28L7 28H3Z','#9fdee8'),path('M16 29L29 16V21L21 29Z','#d9f6f5'),path('M3 12L12 3H16L3 16Z','#5ca6bf')]
    elif kind=='metal':
        for y in (6,13,20,27):p += [rect(4,y,24,1,'#ffffff0c')]
        p += [rect(4,26,24,1,accent)]
    elif kind=='basin':
        for y in (6,12,18,24):p += [rect(4,y,24,1,edge)]
        for x in range(5,29,4):p += [rect(x,4,1,24,'#253643')]
    elif kind=='bolt':
        p += [rect(5,5,22,22,edge),rect(8,8,16,16,base),rect(11,11,10,10,accent),rect(13,13,6,6,'#14242d')]
    else:
        p += [rect(4,5,24,21,'#15232e'),rect(5,6,22,1,edge)]
        for y in (10,14,18):p += [rect(7,y,18,2,'#080f16'),rect(7,y+2,18,1,'#3c5360')]
        p += [rect(6,23,20,1,accent)]
    for x in (3,27):
        for y in (3,27):p += [rect(x,y,2,2,'#0d1720'),rect(x,y,1,1,edge)]
    svg('block/'+name,32,32,p)

# Six independent UV cells; preserve the original 3-by-2 sampling contract.
for name,colors in {
    'machine_materials':['#273440','#94664a','#b5bfc1','#697d87','#172531','#8c7844'],
    'machine_materials_ii':['#273d4c','#397e88','#c1d6d7','#7b939e','#11232e','#a1ad77'],
    'ascension_materials':['#293247','#73668e','#d4d8e3','#8792b4','#151e32','#9b82b7']}.items():
    p=[]
    for i,c in enumerate(colors):
        x=(i%3)*32;y=(i//3)*32;p += [rect(x,y,32,32,c)]
        for a in range(1,32,4):p += [rect(x+1,y+a,30,1,'#ffffff08')]
        p += [rect(x,y,32,1,'#ffffff20'),rect(x,y+31,32,1,'#00000035'),rect(x,y,1,32,'#ffffff16')]
    svg('block/'+name,96,64,p)
svg('block/machine_lights',64,16,[rect(i*16,0,16,16,c) for i,c in enumerate(['#52d9d4','#b69bfa','#edf6ed','#253d49'])])
for name,base,edge,accent,kind in [
 ('freezing_metal','#718994','#b8cfd1','#67ddeb','metal'),('freezing_side','#243b50','#7c9ca9','#70d8eb','panel'),
 ('freezing_front','#213849','#82a5b6','#c0f6fb','panel'),('freezing_inner','#223747','#506e83','#75d9ef','basin'),
 ('freezing_ice','#69bcd4','#aae5ec','#e5ffff','ice'),('freezing_bolt','#835e3a','#c7a36a','#efe4bd','bolt'),
 ('mixing_basin','#637075','#a4b2ad','#d9d9a2','basin'),('mixing_wood','#70543e','#a78a60','#dcbe82','wood'),
 ('washing_ceramic','#405e64','#8fbbc0','#62d8d5','basin'),('washing_rim','#775c46','#bd9b6e','#bce5dd','wood'),
 ('washing_base','#344450','#6f8591','#89b9c5','panel'),('crucible_body','#2c303b','#666675','#dd9d61','panel'),
 ('crucible_rim','#49404a','#a18a88','#edbc87','basin'),('crucible_metal','#745443','#b19a7b','#ffb765','bolt'),
 ('crucible_heated','#4a2830','#945341','#f9aa57','panel'),('garden_base','#233b3c','#607f79','#8fd4a6','panel'),
 ('garden_steel','#729592','#b9d2c0','#a2dbaf','metal'),('garden_copper','#8d684b','#cfab74','#8dd8a7','metal'),
 ('garden_soil','#45372b','#726443','#a2b76c','soil'),('refining_condenser_side','#283549','#7785aa','#b59df1','panel'),
 ('refining_condenser_top','#36435b','#aab6ce','#8de1e3','bolt'),('refining_condenser_front','#253043','#919cbd','#bc9df1','basin'),
 ('condenser_mark_i_side','#37424a','#a7b8b8','#dfaa70','panel'),('condenser_mark_i_top','#795b42','#c5a476','#8bddd4','bolt'),
 ('condenser_mark_i_bottom','#263440','#596c78','#9aa9b4','basin'),('ascension_side','#353c55','#bac4de','#b49afa','panel'),
 ('ascension_top','#353c55','#bac4de','#b49afa','bolt'),('ascension_front','#353c55','#bac4de','#b49afa','basin')]:plate(name,base,edge,accent,kind)
# Clear cutout glazing; transparent interiors keep the live crop visible.
svg('block/garden_glass',32,32,[rect(0,0,32,1,'#8dbfb3'),rect(0,31,32,1,'#608c85'),rect(0,0,1,32,'#8dbfb3'),rect(31,0,1,32,'#608c85'),path('M3 10L10 3H12L3 12Z','#b7ded3'),path('M22 28L28 22V24L24 28Z','#729d93')])
# Native vector silhouettes generate crisp, consistent 32px inventory icons.
for name,color,glyph in [
 ('processing_speed_card','#efbb73','M13 8H20L16 14H21L12 24L15 17H11Z'),
 ('garden_accelerator_card','#8ed3a0','M16 24V14M16 19L10 13V9L16 13L22 8V14Z'),
 ('buffer_expansion_card','#82c9e0','M10 10H22V14H10ZM10 16H22V20H10ZM10 22H22V24H10Z'),
 ('batch_processing_card','#b49cf3','M10 9H15V15H10ZM18 9H23V15H18ZM10 18H15V24H10ZM18 18H23V24H18Z')]:
    p=[path('M6 3H24L27 6V28H5V5Z','#0e1923'),rect(7,5,18,20,'#415767'),rect(8,6,16,18,'#1e303f'),path(glyph,color),rect(8,26,16,2,'#d5bb7a'),rect(8,6,16,1,'#a5bcc3')]
    svg('item/'+name,32,32,p)
svg('item/forever_core',32,32,[path('M16 1L29 10V22L16 31L3 22V10Z','#182334'),path('M16 3L27 11V21L16 29L5 21V11Z','#afbbd6'),path('M16 7L23 12V20L16 25L9 20V12Z','#60467f'),path('M16 8L22 16L16 24L10 16Z','#ba8ff5'),path('M16 10L19 15L16 19L13 15Z','#e6d9ff'),path('M6 11H9V21H6ZM23 11H26V21H23Z','#527483')])
svg('item/stone_fragment',32,32,[path('M5 22L8 12L17 5L24 10L28 23L21 28H10Z','#384b59'),path('M7 21L10 13L17 7L23 11L25 21L20 25H11Z','#8b9b9e'),path('M10 14L17 7L20 12L16 20L9 23Z','#c2cecb'),path('M18 18L23 13L25 21L20 25Z','#647c89')])
for name,colors in {
 'stone_meal':['#e0e3d4','#a5b7b4','#718897'], 'nether_brick_dust':['#d89985','#a34e55','#612f44'],
 'nether_concentrate':['#ffe0a0','#b36865','#523147'], 'mineral_blend':['#d0e8e0','#68b5b2','#b689bc']}.items():
    p=[path('M3 23L7 17L11 16L15 10L20 13L24 20L28 22V26L20 29H8L3 26Z','#20313c'),path('M5 23L9 19L13 18L16 12L19 15L22 21L26 23L20 26H9Z',colors[1])]
    for x,y in [(11,19),(16,14),(20,22),(8,23),(17,23)]:p += [rect(x,y,3,2,colors[0])]
    for x,y in [(13,22),(23,24),(17,18)]:p += [rect(x,y,2,2,colors[2])]
    svg('item/'+name,32,32,p)
svg('item/carbon_matrix',32,32,[path('M5 9L24 5L28 11L25 24L8 28L3 22Z','#101d29'),path('M7 11L23 7L25 12L22 23L9 25L6 21Z','#354b5c')]+[path(f'M{x} {y}l3 -1l1 3l-3 1Z','#8aa8b7') for x,y in [(9,13),(15,11),(20,10),(10,19),(16,17),(21,16),(16,23)]])
svg('item/reinforced_netherite_ingot',32,32,[path('M3 17L11 9L27 13V21L20 27L3 23Z','#172230'),path('M5 17L12 11L25 14L19 20Z','#b1a2bb'),path('M5 19L19 22L25 16V20L19 25L5 22Z','#645471'),path('M11 13L21 15L17 18L8 16Z','#8f75b2'),rect(17,15,2,2,'#e5d9ff')])
svg('item/mortar',32,32,[path('M2 16H28L25 24L20 28H10L5 24Z','#263c4b'),path('M4 17H26L23 23L19 26H11L7 23Z','#7b929b'),path('M5 15L11 12H21L27 15L22 19H10Z','#b3c5c2'),path('M8 15L13 13H20L23 15L20 17H12Z','#324c56'),path('M15 14L22 3L26 5L19 17Z','#c1a16b'),path('M15 14L22 3L23 4L17 15Z','#eee0b4')])
svg('item/world_supply_cache',32,32,[path('M4 10L16 4L28 10V25L16 30L4 25Z','#1a2937'),path('M6 11L16 6L26 11L16 16Z','#b79765'),path('M6 13L15 18V27L6 23Z','#725e49'),path('M17 18L26 13V23L17 27Z','#465f67'),path('M13 7L21 12V25L18 27V15L10 10Z','#86cfc3')])
