"""Structured IPA catalog; sources and scope are documented in SOURCES.md."""
import json, os
from pathlib import Path

root = Path(os.path.abspath(__file__)).parents[1]
entries = []

def add(page, group, text, name, example='', wrap=True, display=None, keywords=''):
    entries.append(dict(id=f'{page}-{len(entries)+1:03}', text=text,
        display=display if display is not None else (f'/{text}/' if page == 'english' else text),
        name=name, example=example, page=page, group=group, wrap=wrap, keywords=keywords))

def rows(page, group, data, wrap=True):
    for line in data.strip().splitlines():
        parts = line.split('|')
        add(page, group, parts[0], parts[1], parts[2] if len(parts)>2 else '', wrap, keywords=parts[3] if len(parts)>3 else '')

rows('english','单元音', '''
iː|长元音 前高不圆唇|see
ɪ|短元音 次高前不圆唇|sit
e|短元音 前中不圆唇|bed
æ|短元音 前低不圆唇|cat
ɑː|长元音 后低不圆唇|father
ɒ|短元音 后低圆唇|hot
ɔː|长元音 后中圆唇|law
ʊ|短元音 次高后圆唇|book
uː|长元音 后高圆唇|blue
ʌ|短元音 后半低不圆唇|cup
ɜː|长元音 中央半低不圆唇|bird
ə|中央元音 schwa|about
''')
rows('english','双元音','''
eɪ|合口双元音|day
aɪ|合口双元音|eye
ɔɪ|合口双元音|boy
əʊ|合口双元音 英式|go
aʊ|合口双元音|now
ɪə|集中双元音 传统英式|near
eə|集中双元音 传统英式|hair
ʊə|集中双元音 传统英式|pure
''')
rows('english','辅音 · 爆破音','''
p|清双唇爆破音 voiceless bilabial plosive|pen
b|浊双唇爆破音 voiced bilabial plosive|book
t|清齿龈爆破音 voiceless alveolar plosive|tea
d|浊齿龈爆破音 voiced alveolar plosive|dog
k|清软腭爆破音 voiceless velar plosive|key
ɡ|浊软腭爆破音 voiced velar plosive|go|g
''')
rows('english','辅音 · 摩擦音','''
f|清唇齿摩擦音 voiceless labiodental fricative|fish
v|浊唇齿摩擦音 voiced labiodental fricative|voice
θ|清齿摩擦音 theta voiceless dental fricative|think
ð|浊齿摩擦音 eth voiced dental fricative|this
s|清齿龈摩擦音 voiceless alveolar fricative|sun
z|浊齿龈摩擦音 voiced alveolar fricative|zoo
ʃ|清龈后摩擦音 esh voiceless postalveolar fricative|ship
ʒ|浊龈后摩擦音 ezh voiced postalveolar fricative|vision
h|清声门摩擦音 voiceless glottal fricative|hat
''')
rows('english','辅音 · 破擦音','''
tʃ|清龈后破擦音 voiceless postalveolar affricate|chair
dʒ|浊龈后破擦音 voiced postalveolar affricate|job
''')
rows('english','辅音 · 鼻音','''
m|双唇鼻音 bilabial nasal|man
n|齿龈鼻音 alveolar nasal|no
ŋ|软腭鼻音 eng velar nasal|sing
''')
rows('english','辅音 · 近音与边音','''
l|齿龈边近音 alveolar lateral approximant|leg
r|英语 r 的宽式记法 常见语音实现为 ɹ|red
w|浊唇软腭近音 voiced labial velar approximant|we
j|硬腭近音 palatal approximant|yes
''')
rows('english','教材扩展 · 辅音组合','''
ts|辅音组合 t+s 通常非独立音位|cats
dz|辅音组合 d+z 通常非独立音位|beds
tr|辅音组合 t+r 通常非独立音位|tree
dr|辅音组合 d+r 通常非独立音位|dream
''')

rows('ipa','肺部气流辅音 · Pulmonic consonants','''
p|清双唇爆破音 voiceless bilabial plosive
b|浊双唇爆破音 voiced bilabial plosive
t|清齿龈爆破音 voiceless alveolar plosive
d|浊齿龈爆破音 voiced alveolar plosive
ʈ|清卷舌爆破音 voiceless retroflex plosive
ɖ|浊卷舌爆破音 voiced retroflex plosive
c|清硬腭爆破音 voiceless palatal plosive
ɟ|浊硬腭爆破音 voiced palatal plosive
k|清软腭爆破音 voiceless velar plosive
ɡ|浊软腭爆破音 voiced velar plosive||g
q|清小舌爆破音 voiceless uvular plosive
ɢ|浊小舌爆破音 voiced uvular plosive
ʔ|声门塞音 glottal plosive
m|双唇鼻音 bilabial nasal
ɱ|唇齿鼻音 labiodental nasal
n|齿龈鼻音 alveolar nasal
ɳ|卷舌鼻音 retroflex nasal
ɲ|硬腭鼻音 palatal nasal
ŋ|软腭鼻音 velar nasal
ɴ|小舌鼻音 uvular nasal
ʙ|双唇颤音 bilabial trill
r|齿龈颤音 alveolar trill
ʀ|小舌颤音 uvular trill
ⱱ|唇齿闪音 labiodental flap
ɾ|齿龈闪音 alveolar tap flap
ɽ|卷舌闪音 retroflex flap
ɸ|清双唇摩擦音 voiceless bilabial fricative
β|浊双唇摩擦音 voiced bilabial fricative
f|清唇齿摩擦音 voiceless labiodental fricative
v|浊唇齿摩擦音 voiced labiodental fricative
θ|清齿摩擦音 theta voiceless dental fricative
ð|浊齿摩擦音 eth voiced dental fricative
s|清齿龈摩擦音 voiceless alveolar fricative
z|浊齿龈摩擦音 voiced alveolar fricative
ʃ|清龈后摩擦音 esh voiceless postalveolar fricative
ʒ|浊龈后摩擦音 ezh voiced postalveolar fricative
ʂ|清卷舌摩擦音 voiceless retroflex fricative
ʐ|浊卷舌摩擦音 voiced retroflex fricative
ç|清硬腭摩擦音 voiceless palatal fricative
ʝ|浊硬腭摩擦音 voiced palatal fricative
x|清软腭摩擦音 voiceless velar fricative
ɣ|浊软腭摩擦音 voiced velar fricative
χ|清小舌摩擦音 voiceless uvular fricative
ʁ|浊小舌摩擦音 voiced uvular fricative
ħ|清咽摩擦音 voiceless pharyngeal fricative
ʕ|浊咽摩擦音 voiced pharyngeal fricative
h|清声门摩擦音 voiceless glottal fricative
ɦ|浊声门摩擦音 voiced glottal fricative
ɬ|清齿龈边擦音 voiceless alveolar lateral fricative
ɮ|浊齿龈边擦音 voiced alveolar lateral fricative
ʋ|唇齿近音 labiodental approximant
ɹ|齿龈近音 alveolar approximant English r
ɻ|卷舌近音 retroflex approximant
j|硬腭近音 palatal approximant
ɰ|软腭近音 velar approximant
l|齿龈边近音 alveolar lateral approximant
ɭ|卷舌边近音 retroflex lateral approximant
ʎ|硬腭边近音 palatal lateral approximant
ʟ|软腭边近音 velar lateral approximant
''')
rows('ipa','非肺部气流辅音 · Non-pulmonic consonants','''
ʘ|双唇搭嘴音 bilabial click
ǀ|齿搭嘴音 dental click
ǃ|齿龈搭嘴音 postalveolar click
ǂ|硬腭齿龈搭嘴音 palatoalveolar click
ǁ|齿龈边搭嘴音 alveolar lateral click
ɓ|浊双唇内爆音 voiced bilabial implosive
ɗ|浊齿龈内爆音 voiced alveolar implosive
ʄ|浊硬腭内爆音 voiced palatal implosive
ɠ|浊软腭内爆音 voiced velar implosive
ʛ|浊小舌内爆音 voiced uvular implosive
''')
add('ipa','非肺部气流辅音 · Non-pulmonic consonants','ʼ','挤喉音标记 ejective',wrap=False,display='◌ʼ')
rows('ipa','元音 · Vowels','''
i|闭前不圆唇元音 close front unrounded
y|闭前圆唇元音 close front rounded
ɨ|闭央不圆唇元音 close central unrounded
ʉ|闭央圆唇元音 close central rounded
ɯ|闭后不圆唇元音 close back unrounded
u|闭后圆唇元音 close back rounded
ɪ|次闭次前不圆唇元音 near-close near-front unrounded
ʏ|次闭次前圆唇元音 near-close near-front rounded
ʊ|次闭次后圆唇元音 near-close near-back rounded
e|半闭前不圆唇元音 close-mid front unrounded
ø|半闭前圆唇元音 close-mid front rounded
ɘ|半闭央不圆唇元音 close-mid central unrounded
ɵ|半闭央圆唇元音 close-mid central rounded
ɤ|半闭后不圆唇元音 close-mid back unrounded
o|半闭后圆唇元音 close-mid back rounded
ə|中央元音 schwa mid central
ɛ|半开前不圆唇元音 open-mid front unrounded
œ|半开前圆唇元音 open-mid front rounded
ɜ|半开央不圆唇元音 open-mid central unrounded
ɞ|半开央圆唇元音 open-mid central rounded
ʌ|半开后不圆唇元音 open-mid back unrounded
ɔ|半开后圆唇元音 open-mid back rounded
æ|次开前不圆唇元音 near-open front unrounded
ɐ|次开央元音 near-open central
a|开前不圆唇元音 open front unrounded
ɶ|开前圆唇元音 open front rounded
ɑ|开后不圆唇元音 open back unrounded
ɒ|开后圆唇元音 open back rounded
''')
diacritics = [
('0325','清化 voiceless'),('030A','清化 上置变体 voiceless above'),('032C','浊化 voiced'),('ʰ','送气 aspirated'),
('0339','更圆唇 more rounded'),('031C','较少圆唇 less rounded'),('031F','前移 advanced'),('0320','后移 retracted'),
('0308','央化 centralized'),('033D','中央化 mid-centralized'),('0329','成音节 syllabic'),('032F','非音节 non-syllabic'),('˞','卷舌化 rhoticity'),
('0324','气声 breathy voiced'),('0330','嘎裂声 creaky voiced'),('033C','舌唇音 linguolabial'),('ʷ','唇化 labialized'),('ʲ','腭化 palatalized'),
('ˠ','软腭化 velarized'),('ˤ','咽化 pharyngealized'),('0334','软腭化或咽化 velarized or pharyngealized'),
('031D','抬高 raised'),('031E','降低 lowered'),('0318','舌根前移 advanced tongue root'),('0319','舌根后移 retracted tongue root'),
('032A','齿化 dental'),('033A','舌尖 apical'),('033B','舌叶 laminal'),('0303','鼻化 nasalized'),('ⁿ','鼻音除阻 nasal release'),
('ˡ','边音除阻 lateral release'),('031A','无可闻除阻 no audible release')]
for code,name in diacritics:
    text = chr(int(code,16)) if len(code)==4 else code
    add('ipa','附加符号 · Diacritics',text,name,wrap=False,display='◌'+text)
for text,name in [('ˈ','主重音 primary stress'),('ˌ','次重音 secondary stress'),('ː','长音 long'),('ˑ','半长音 half-long'),('\u0306','超短音 extra-short'),('.','音节分界 syllable break'),('|','小韵律边界 minor group'),('‖','大韵律边界 major group'),('‿','连音 linking absence of a break')]:
    add('ipa','超音段 · Suprasegmentals',text,name,wrap=False,display='◌'+text if text=='\u0306' else text)
for text,name in [
('˥','特高平调 extra high level'),('˦','高平调 high level'),('˧','中平调 mid level'),('˨','低平调 low level'),('˩','特低平调 extra low level'),
('\u030B','特高调 附加符 extra high'),('\u0301','高调 附加符 high'),('\u0304','中调 附加符 mid'),('\u0300','低调 附加符 low'),('\u030F','特低调 附加符 extra low'),
('˩˥','升调 rising'),('˥˩','降调 falling'),('˧˥','高升调 high rising'),('˩˧','低升调 low rising'),('˧˦˧','升降调 rising-falling'),('\u030C','升调 附加符 rising'),('\u0302','降调 附加符 falling'),
('\u1DC4','高升调 附加符 high rising'),('\u1DC5','低升调 附加符 low rising'),('\u1DC8','升降调 附加符 rising-falling'),
('ꜛ','升阶 upstep'),('ꜜ','降阶 downstep'),('↗','整体升调 global rise'),('↘','整体降调 global fall')]:
    import unicodedata
    add('ipa','声调与词重音 · Tones and word accents',text,name,wrap=False,display='◌'+text if unicodedata.combining(text[0]) else text)
rows('ipa','其他符号 · Other symbols','''
ʍ|清唇软腭擦音 voiceless labial velar fricative
w|浊唇软腭近音 voiced labial velar approximant
ɥ|浊唇硬腭近音 voiced labial palatal approximant
ʜ|清会厌擦音 voiceless epiglottal fricative
ʢ|浊会厌擦音 voiced epiglottal fricative
ʡ|会厌塞音 epiglottal plosive
ɕ|清龈腭擦音 voiceless alveolo-palatal fricative
ʑ|浊龈腭擦音 voiced alveolo-palatal fricative
ɺ|浊齿龈边闪音 voiced alveolar lateral flap
ɧ|同时发 ʃ 和 x simultaneous postalveolar velar fricative
''')
for text,name in [('\u0361','上连音弧 tie bar above'),('\u035C','下连音弧 tie bar below')]:
    add('ipa','其他符号 · Other symbols',text,name,wrap=False,display='◌'+text+'◌')

for text,name in [('ˈ','主重音'),('ˌ','次重音'),('ː','长音'),('ˑ','半长音'),('.','音节分隔'),('|','小停顿'),('‖','大停顿'),('\u0303','鼻化符号'),('ʰ','送气符号')]:
    add('common','常用特殊符号',text,name,wrap=False,display='◌'+text if text in ['\u0303','ʰ'] else text)

path = root/'src'/'Data'/'catalog.json'
path.parent.mkdir(parents=True,exist_ok=True)
path.write_text(json.dumps(entries,ensure_ascii=False,indent=2),encoding='utf-8')
from collections import Counter
print('Total entries:',len(entries),'Unique copy strings:',len({e['text'] for e in entries}))
print(Counter(e['page'] for e in entries))
print(Counter(e['group'] for e in entries if e['page']=='ipa'))
