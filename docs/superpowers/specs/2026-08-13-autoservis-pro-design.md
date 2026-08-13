# Autoservis PRO – návrh aplikácie

## Cieľ
Vytvoriť jednu hlavnú webovú aplikáciu pre každodennú prevádzku autoservisu. Aplikácia musí spoľahlivo ukladať všetky údaje do cloudovej PostgreSQL databázy (Supabase), nie iba do lokálneho stavu prehliadača.

## Architektúra
- Frontend: Lovable full-stack TypeScript aplikácia.
- Databáza: Supabase PostgreSQL.
- Autentifikácia: používateľské účty s rolami majiteľ/admin, mechanik, prípadne recepcia.
- Súbory: Supabase Storage pre fotografie, PDF diagnostiky, protokoly a prílohy.
- GitHub: zdrojový kód a história verzií, nie prevádzkové dáta.

## Hlavný pracovný tok
Zákazník → vozidlo → objednanie/príjem → zákazka → diagnostika/kontrola → kalkulácia → schválenie → práca a diely → kontrola → faktúra → platba → odovzdanie → história vozidla.

## Dashboard
- tlačidlo + Nová zákazka,
- dnešné príjmy,
- rozpracované vozidlá,
- čaká na schválenie,
- čaká na diely,
- hotové na odovzdanie,
- nezaplatené faktúry,
- dnešná/mesečná tržba,
- náklady a hrubý zisk,
- upozornenia na termíny a rozpracované zákazky.

## Zákazníci
Polia:
- typ: fyzická osoba / firma,
- meno alebo obchodné meno,
- telefón,
- e-mail,
- ulica, mesto, PSČ, krajina,
- IČO, DIČ, IČ DPH,
- poznámka,
- preferovaný spôsob kontaktu,
- história vozidiel, zákaziek, faktúr a platieb.

## Vozidlá
Polia:
- EČV,
- VIN,
- značka,
- model,
- rok výroby,
- motor,
- kód motora,
- objem,
- výkon kW,
- palivo,
- prevodovka,
- pohon,
- aktuálny nájazd,
- farba,
- dátum STK,
- dátum EK,
- poznámky,
- fotografia vozidla,
- kompletná história návštev a opráv.

VIN a EČV musia byť vyhľadateľné z hlavného vyhľadávania.

## Zákazka / príjem vozidla
Polia:
- automatické číslo zákazky,
- zákazník,
- vozidlo,
- dátum a čas objednania,
- dátum a čas príjmu,
- plánované dokončenie,
- stav km pri príjme,
- stav paliva,
- popis požiadavky zákazníka,
- symptómy,
- interná poznámka,
- pridelený technik,
- priorita,
- predbežná cenová hranica,
- prílohy a fotografie.

Stavy:
Objednané → Prijaté → Diagnostika → Čaká na schválenie → Čaká na diely → Oprava → Kontrola → Hotové → Odovzdané.

## Diagnostika a digitálna kontrola
- riadiaca jednotka,
- DTC kód,
- text chyby,
- status chyby,
- live-data poznámky,
- namerané hodnoty,
- výsledok testu,
- príčina závady,
- odporúčaná oprava,
- fotografie,
- screenshoty z TEXA,
- PDF diagnostické protokoly,
- checklist položiek so stavom OK / upozornenie / nutná oprava.

## Práce
Každá pracovná položka:
- názov,
- popis,
- normohodiny,
- skutočný čas,
- sadzba €/h,
- technik,
- interný náklad,
- cena zákazníkovi,
- stav vykonania.

## Diely a materiál
Každá položka:
- názov,
- výrobca,
- OE/TecDoc/objednávacie číslo,
- dodávateľ,
- množstvo,
- nákupná cena,
- predajná cena,
- marža,
- stav objednávky: potrebné / objednané / dodané / namontované / vrátené,
- záruka,
- poznámka.

## Kalkulácia a schválenie
- práce,
- diely,
- materiál,
- ostatné poplatky,
- zľava,
- DPH,
- suma bez DPH,
- suma DPH,
- suma spolu,
- stav: návrh / odoslané / schválené / čiastočne schválené / zamietnuté,
- dátum schválenia,
- kto schválil,
- zamietnuté odporúčané práce zostávajú v histórii vozidla.

## Faktúry
Faktúra sa vytvorí zo zákazky jedným klikom a preberie zákazníka, položky, ceny aj údaje vozidla.

Polia:
- automatické číslo faktúry,
- variabilný symbol,
- dátum vystavenia,
- dátum dodania,
- dátum splatnosti,
- dodávateľ,
- odberateľ,
- IČO, DIČ, IČ DPH,
- bankový účet / IBAN,
- položky práce a dielov,
- množstvo, MJ, jednotková cena,
- zľava,
- sadzba DPH,
- základ DPH,
- DPH,
- celkom,
- spôsob úhrady: hotovosť / karta / prevod,
- stav: nezaplatená / čiastočne zaplatená / zaplatená / po splatnosti / stornovaná,
- uhradená suma,
- zostáva uhradiť,
- poznámka.

Funkcie:
- PDF faktúra vhodná na tlač a odoslanie,
- QR kód na bankovú platbu,
- prepojenie faktúry so zákazkou a vozidlom,
- evidencia čiastočných úhrad,
- nemožnosť ticho prepísať historickú faktúru bez evidencie zmeny/storna.

## Platby
- faktúra,
- dátum,
- suma,
- spôsob platby,
- referencia platby,
- poznámka,
- automatický prepočet zostatku.

## História vozidla
Na jednej obrazovke zobrazovať:
- všetky návštevy,
- nájazd pri každej návšteve,
- vykonané práce,
- použité diely,
- diagnostiku,
- fotografie a prílohy,
- odporúčané a odmietnuté práce,
- faktúry a úhrady.

## Kalendár
- objednávky,
- plánovaný príjem,
- plánované dokončenie,
- priradený technik,
- presun termínu drag-and-drop,
- prechod z termínu priamo do zákazky.

## Sklad – prvá verzia
- katalóg vlastných skladových položiek,
- aktuálne množstvo,
- minimálne množstvo,
- nákupná a predajná cena,
- dodávateľ,
- skladový pohyb pri použití na zákazke.

Rozsiahle automatické integrácie dodávateľov sa do prvej verzie nezaraďujú.

## Vyhľadávanie
Jedno globálne vyhľadávanie podľa:
- mena zákazníka,
- telefónu,
- EČV,
- VIN,
- čísla zákazky,
- čísla faktúry,
- DTC kódu.

## Použiteľnosť
- responzívne pre notebook, tablet aj telefón,
- povinné iba najdôležitejšie polia,
- pokročilé sekcie rozbaľovacie,
- autosave rozpracovaného formulára tam, kde je to bezpečné,
- jasné potvrdenie úspešného uloženia,
- pri chybe databázy sa nesmie tváriť, že boli údaje uložené,
- ochrana pred nechceným opustením formulára s neuloženými zmenami.

## Dátová integrita
- všetky prevádzkové dáta ukladať do Supabase,
- cudzie kľúče medzi zákazníkom, vozidlom, zákazkou, faktúrou a platbou,
- databázové transakcie pri kritických operáciách,
- audit dátumu vytvorenia a poslednej zmeny,
- žiadne demo dáta ani localStorage ako primárne úložisko.

## Oprávnenia
- Admin/Majiteľ: všetko vrátane financií, cien a nastavení,
- Mechanik: zákazky, diagnostika, práce, diely a fotky; bez citlivých ekonomických nastavení,
- ďalšia rola recepcie môže byť pridaná neskôr.

## Zálohy a bezpečnosť
- Supabase autentifikácia,
- RLS politiky podľa rolí,
- databáza neprístupná anonymnému používateľovi,
- pravidelné databázové zálohy podľa možností zvoleného Supabase plánu,
- citlivé kľúče iba v secrets/environment variables.

## Čo nie je v prvej verzii
- kompletné podvojné účtovníctvo,
- online e-shop,
- automatické objednávanie dielov cez API všetkých dodávateľov,
- viac samostatných prevádzok,
- plnohodnotný dochádzkový/mzdový systém.

## Kritériá úspechu
1. Zákazka vytvorená na notebooku je po obnovení stránky stále dostupná.
2. Rovnaká zákazka je po prihlásení viditeľná aj z iného zariadenia.
3. Zo zákazky možno vytvoriť faktúru bez opätovného prepisovania údajov.
4. Platba mení stav a zostatok faktúry správne.
5. História vozidla obsahuje všetky zákazky, práce, diely, diagnostiku a faktúry.
6. Vyhľadanie VIN/EČV otvorí správne vozidlo a jeho históriu.
7. Chyba databázového zápisu sa používateľovi zobrazí ako chyba, nie ako úspešné uloženie.
