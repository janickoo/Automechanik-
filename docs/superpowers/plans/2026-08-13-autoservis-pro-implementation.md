# Autoservis PRO Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Vytvoriť jednu hlavnú webovú aplikáciu autoservisu s trvalou Supabase databázou, zákazkami, diagnostikou, dielmi, skladom, fakturáciou, platbami, kalendárom a históriou vozidla.

**Architecture:** Lovable full-stack TypeScript frontend bude pracovať výhradne nad PostgreSQL databázou Supabase. Prevádzkové dáta sa nesmú ukladať iba do localStorage alebo React state; klientsky stav slúži len na rozpracovaný formulár. Doménové časti budú oddelené na zákazníkov/vozidlá, zákazky, diagnostiku, diely/sklad, faktúry/platby a kalendár/dashboard.

**Tech Stack:** Lovable, TypeScript, React, Tailwind, shadcn/ui, Supabase PostgreSQL, Supabase Auth, Supabase Storage, Vitest/React Testing Library, Playwright alebo ekvivalent E2E podporovaný projektom.

## Global Constraints

- Všetky prevádzkové dáta ukladať do Supabase PostgreSQL.
- GitHub slúži na zdrojový kód a históriu verzií, nie ako databáza zákaziek.
- Žiadne demo dáta ani localStorage ako primárne úložisko.
- Pri zlyhaní zápisu do databázy UI nesmie oznámiť úspešné uloženie.
- Povinné polia musia byť obmedzené na minimum; odborné polia budú v rozbaľovacích sekciách.
- Aplikácia musí byť použiteľná na notebooku, tablete aj telefóne.
- Faktúra musí byť vždy spätne dohľadateľná k zákazke, vozidlu a zákazníkovi.
- Historická vystavená faktúra sa nesmie ticho prepísať bez evidencie zmeny alebo storna.
- Finančné a administrátorské nastavenia sú viditeľné iba pre rolu Admin/Majiteľ.

---

## File Structure

Plán predpokladá túto cieľovú štruktúru po vytvorení Lovable projektu:

- `src/lib/supabase.ts` – Supabase klient.
- `src/lib/money.ts` – výpočty cien, DPH, marže a zaokrúhlenia.
- `src/lib/invoice.ts` – číslovanie faktúr, variabilný symbol, výpočet stavov platieb.
- `src/lib/workOrders.ts` – pomocné funkcie a stavy zákaziek.
- `src/types/database.ts` – databázové/doménové typy.
- `src/features/customers/*` – zákazníci.
- `src/features/vehicles/*` – vozidlá a história.
- `src/features/work-orders/*` – príjem vozidla a zákazky.
- `src/features/diagnostics/*` – DTC, merania, fotky a protokoly.
- `src/features/inventory/*` – diely, materiál, skladové pohyby.
- `src/features/invoices/*` – faktúry a PDF.
- `src/features/payments/*` – úhrady a zostatky.
- `src/features/calendar/*` – termíny.
- `src/features/dashboard/*` – prevádzkový prehľad.
- `src/features/auth/*` – prihlásenie a roly.
- `supabase/migrations/*` – schéma, indexy, RLS a SQL funkcie.
- `tests/*` – jednotkové a integračné testy.

---

### Task 1: Vytvoriť hlavný Lovable projekt a databázu

**Files:**
- Create: Lovable project `Autoservis PRO`
- Create: `src/lib/supabase.ts`
- Create: `src/types/database.ts`

**Interfaces:**
- Produces: jeden hlavný Lovable `project_id`, pripojený Supabase projekt a inicializovaný klient `supabase`.

- [ ] **Step 1:** Vytvoriť nový Lovable projekt v používateľovom workspaci s názvom `Autoservis PRO` a požiadavkou, aby nevytváral demo/localStorage databázu.
- [ ] **Step 2:** Zapnúť cloud PostgreSQL databázu pre projekt.
- [ ] **Step 3:** Overiť stav databázy cez database status.
- [ ] **Step 4:** Vygenerovať základný Supabase klient v `src/lib/supabase.ts` iba z environment variables/secrets.
- [ ] **Step 5:** Vytvoriť jednoduchý integračný test, ktorý vykoná bezpečný `SELECT now()` alebo čítanie systémovej tabuľky a pri chybe skončí FAIL.
- [ ] **Step 6:** Commit: `feat: bootstrap Autoservis PRO with Supabase`.

**Acceptance:** Projekt sa otvorí v Lovable, databáza je enabled a aplikácia vie nadviazať spojenie bez hardcoded secret kľúča.

---

### Task 2: Databázová schéma a dátová integrita

**Files:**
- Create: `supabase/migrations/001_core_schema.sql`
- Modify: `src/types/database.ts`

**Interfaces:**
- Produces tabuľky: `profiles`, `customers`, `vehicles`, `work_orders`, `work_order_labor`, `work_order_parts`, `diagnostic_entries`, `inspection_items`, `attachments`, `inventory_items`, `inventory_movements`, `invoices`, `invoice_items`, `payments`, `appointments`, `audit_events`.

- [ ] **Step 1:** Vytvoriť SQL migráciu s UUID primárnymi kľúčmi, `created_at`, `updated_at` a relevantnými foreign keys.
- [ ] **Step 2:** Pridať unikátne/indexované polia: VIN, EČV podľa potreby, číslo zákazky, číslo faktúry, telefón, meno zákazníka a DTC kód.
- [ ] **Step 3:** Zaviesť enum/check hodnoty pre stavy zákazky: `booked`, `received`, `diagnostics`, `awaiting_approval`, `awaiting_parts`, `repair`, `quality_check`, `ready`, `delivered`.
- [ ] **Step 4:** Zaviesť stav faktúry: `draft`, `unpaid`, `partially_paid`, `paid`, `overdue`, `cancelled`.
- [ ] **Step 5:** Zaviesť stav schválenia: `draft`, `sent`, `approved`, `partially_approved`, `rejected`.
- [ ] **Step 6:** Pridať DB constraints: množstvo > 0, ceny >= 0, nájazd >= 0, dátum splatnosti nesmie byť pred dátumom vystavenia.
- [ ] **Step 7:** Spustiť SQL migráciu a overiť pomocou SELECT zoznamu tabuliek a FK.
- [ ] **Step 8:** Commit: `feat: add core workshop database schema`.

**Acceptance:** Databáza odmietne orphan faktúru bez zákazníka/zákazky, záporné ceny a neplatné statusy.

---

### Task 3: Prihlásenie, používateľské roly a RLS

**Files:**
- Create: `supabase/migrations/002_auth_rls.sql`
- Create: `src/features/auth/AuthProvider.tsx`
- Create: `src/features/auth/LoginPage.tsx`
- Create: `src/features/auth/RequireRole.tsx`

**Interfaces:**
- Produces roly `owner`, `mechanic` a funkciu/guard na overenie role v UI.

- [ ] **Step 1:** Aktivovať Supabase Auth pre email/password.
- [ ] **Step 2:** Vytvoriť `profiles` väzbu na `auth.users` a bezpečný trigger na založenie profilu.
- [ ] **Step 3:** Nastaviť RLS tak, aby anonymný používateľ nemal prístup k prevádzkovým tabuľkám.
- [ ] **Step 4:** Nastaviť `owner` na plný CRUD a `mechanic` na prevádzkové tabuľky bez administratívnych/finančných nastavení.
- [ ] **Step 5:** Napísať test: anonymous SELECT zákazníkov musí zlyhať, owner musí uspieť.
- [ ] **Step 6:** Commit: `feat: add authentication and workshop roles`.

**Acceptance:** Bez prihlásenia nie je možné čítať zákazky ani zákazníkov.

---

### Task 4: Zákazníci, vozidlá a globálne vyhľadávanie

**Files:**
- Create: `src/features/customers/CustomerForm.tsx`
- Create: `src/features/customers/CustomerDetail.tsx`
- Create: `src/features/vehicles/VehicleForm.tsx`
- Create: `src/features/vehicles/VehicleDetail.tsx`
- Create: `src/components/GlobalSearch.tsx`

**Interfaces:**
- Produces CRUD zákazníkov/vozidiel a globálne vyhľadávanie podľa mena, telefónu, EČV a VIN.

- [ ] **Step 1:** Implementovať zákazníka s typom osoba/firma a poľami meno/názov, telefón, email, adresa, IČO, DIČ, IČ DPH, poznámka.
- [ ] **Step 2:** Implementovať vozidlo s VIN, EČV, značka, model, rok, motor, kód motora, objem, výkon, palivo, prevodovka, pohon, nájazd, farba, STK, EK, poznámka.
- [ ] **Step 3:** Zabezpečiť, že uloženie vykoná skutočný INSERT/UPDATE a až po úspešnej odpovedi zobrazí potvrdenie.
- [ ] **Step 4:** Pri DB chybe zachovať formulár a ukázať konkrétnu chybu.
- [ ] **Step 5:** Implementovať globálne vyhľadávanie s debounce a indexovanými dotazmi.
- [ ] **Step 6:** Test: vytvoriť zákazníka + vozidlo, reload aplikácie, údaje musia zostať.
- [ ] **Step 7:** Commit: `feat: add customers vehicles and search`.

**Acceptance:** Zákazník vytvorený na notebooku je po reload stále dostupný a vozidlo možno nájsť cez VIN alebo EČV.

---

### Task 5: Príjem vozidla a pracovná zákazka

**Files:**
- Create: `src/features/work-orders/WorkOrderForm.tsx`
- Create: `src/features/work-orders/WorkOrderDetail.tsx`
- Create: `src/features/work-orders/WorkOrderBoard.tsx`
- Create: `src/lib/workOrders.ts`

**Interfaces:**
- Produces funkcie `createWorkOrder`, `updateWorkOrderStatus`, `addLaborItem`, `addPartItem`.

- [ ] **Step 1:** Zaviesť automatické číslo zákazky vo formáte `ZO-YYYY-NNNN` generované bezpečne na serverovej/databázovej úrovni.
- [ ] **Step 2:** Formulár: zákazník, vozidlo, termín, príjem, plánované dokončenie, km, palivo, požiadavka zákazníka, symptómy, interná poznámka, technik, priorita, cenová hranica.
- [ ] **Step 3:** Pridať pracovné položky s normohodinami, skutočným časom, sadzbou a technikom.
- [ ] **Step 4:** Pridať diely s množstvom, dodávateľom, nákupnou a predajnou cenou a maržou.
- [ ] **Step 5:** Implementovať board podľa stavov zákazky.
- [ ] **Step 6:** Testovať zmenu stavov a perzistenciu po reload.
- [ ] **Step 7:** Commit: `feat: add persistent work order workflow`.

**Acceptance:** Zákazka sa dá vytvoriť, upraviť a presúvať medzi stavmi bez straty dát.

---

### Task 6: Diagnostika, DTC, digitálna kontrola a prílohy

**Files:**
- Create: `src/features/diagnostics/DiagnosticEntryForm.tsx`
- Create: `src/features/diagnostics/InspectionChecklist.tsx`
- Create: `src/features/diagnostics/Attachments.tsx`

**Interfaces:**
- Produces diagnostické záznamy previazané s `work_order_id` a `vehicle_id`.

- [ ] **Step 1:** Formulár DTC: riadiaca jednotka, kód, text, status, namerané hodnoty, príčina, odporúčaná oprava.
- [ ] **Step 2:** Checklist so stavmi `ok`, `warning`, `repair_required`.
- [ ] **Step 3:** Pridať upload fotografií, screenshotov TEXA a PDF protokolov do Supabase Storage.
- [ ] **Step 4:** Ukladať iba metadáta/cestu k súboru do DB.
- [ ] **Step 5:** Test: upload prílohy, reload, príloha sa musí znovu načítať zo storage.
- [ ] **Step 6:** Commit: `feat: add diagnostics inspections and attachments`.

**Acceptance:** Každý diagnostický záznam ostáva súčasťou histórie vozidla.

---

### Task 7: Kalkulácia, schválenie, ceny a marža

**Files:**
- Create: `src/lib/money.ts`
- Create: `src/features/work-orders/EstimatePanel.tsx`
- Test: `tests/money.test.ts`

**Interfaces:**
- Produces čisté funkcie `calcLineNet`, `calcVat`, `calcGross`, `calcMargin`, `calcEstimateTotals`.

- [ ] **Step 1:** Napísať testy pre 0 %, 20 % a 23 % DPH, zľavu a zaokrúhlenie na 2 desatinné miesta.
- [ ] **Step 2:** Implementovať kalkuláciu bez použitia float porovnaní, ktoré môžu vytvárať centové chyby; používať centy alebo decimal-safe prístup.
- [ ] **Step 3:** Zobraziť práce, diely, materiál, zľavu, DPH, sumu bez DPH a sumu spolu.
- [ ] **Step 4:** Pridať stav schválenia a dátum/kto schválil.
- [ ] **Step 5:** Zamietnuté položky ponechať v histórii vozidla.
- [ ] **Step 6:** Commit: `feat: add estimates approval and margin calculations`.

**Acceptance:** Suma zákazky a faktúry sa zhoduje na cent a odmietnuté práce nezmiznú.

---

### Task 8: Faktúry a PDF doklady

**Files:**
- Create: `src/lib/invoice.ts`
- Create: `src/features/invoices/InvoiceCreate.tsx`
- Create: `src/features/invoices/InvoiceDetail.tsx`
- Create: `src/features/invoices/InvoicePdf.tsx`
- Test: `tests/invoice.test.ts`

**Interfaces:**
- Produces `createInvoiceFromWorkOrder(workOrderId)`, `getInvoiceBalance(invoiceId)`, PDF renderer.

- [ ] **Step 1:** Napísať test, že faktúra vytvorená zo zákazky preberá zákazníka, vozidlo, práce, diely, ceny a DPH.
- [ ] **Step 2:** Zaviesť bezpečné číslovanie faktúr `YYYYNNNN` alebo konfigurovateľný rad s databázovou ochranou proti duplicitám.
- [ ] **Step 3:** Variabilný symbol generovať z čísla faktúry bez nečíselných znakov.
- [ ] **Step 4:** Implementovať dátum vystavenia, dodania, splatnosti, IBAN, spôsob úhrady, poznámku.
- [ ] **Step 5:** Implementovať PDF faktúru vhodnú na A4 tlač.
- [ ] **Step 6:** Pridať QR platbu z IBAN, sumy a variabilného symbolu podľa formátu podporovaného pre slovenskú platbu.
- [ ] **Step 7:** Po vystavení uzamknúť ekonomický snapshot položiek; opravy riešiť evidovanou zmenou alebo stornom.
- [ ] **Step 8:** Commit: `feat: add invoices pdf and payment qr`.

**Acceptance:** Zo zákazky vznikne kompletná faktúra bez ručného prepisovania a jej PDF sa dá vytlačiť.

---

### Task 9: Platby a pohľadávky

**Files:**
- Create: `src/features/payments/PaymentForm.tsx`
- Create: `src/features/payments/Receivables.tsx`
- Modify: `src/lib/invoice.ts`

**Interfaces:**
- Produces `recordPayment`, automatický stav faktúry a zostatok.

- [ ] **Step 1:** Evidovať dátum, sumu, hotovosť/karta/prevod, referenciu a poznámku.
- [ ] **Step 2:** Pri čiastočnej úhrade nastaviť `partially_paid`, pri plnej `paid`.
- [ ] **Step 3:** Pri prekročení splatnosti a kladnom zostatku zobrazovať `overdue` bez ručného prepínania.
- [ ] **Step 4:** Zobraziť zoznam nezaplatených a po splatnosti.
- [ ] **Step 5:** Testovať dve čiastočné platby a presný výsledný zostatok.
- [ ] **Step 6:** Commit: `feat: add payments and receivables tracking`.

**Acceptance:** Faktúra 100 € s platbami 40 € + 60 € skončí ako zaplatená so zostatkom 0 €.

---

### Task 10: Sklad a skladové pohyby

**Files:**
- Create: `src/features/inventory/InventoryList.tsx`
- Create: `src/features/inventory/InventoryItemForm.tsx`
- Create: `src/features/inventory/InventoryMovements.tsx`

**Interfaces:**
- Produces skladové položky a automatické pohyby pri použití dielu na zákazke.

- [ ] **Step 1:** Implementovať SKU/názov/výrobcu/číslo/dodávateľa/nákupnú a predajnú cenu/množstvo/minimum.
- [ ] **Step 2:** Použitie skladového dielu na zákazke vytvorí výdajový pohyb.
- [ ] **Step 3:** Vrátenie dielu vytvorí opačný pohyb; história pohybov sa nemaže.
- [ ] **Step 4:** Upozorniť na množstvo <= minimum.
- [ ] **Step 5:** Testovať výdaj a vrátenie bez rozdielu v stave skladu.
- [ ] **Step 6:** Commit: `feat: add inventory and stock movements`.

**Acceptance:** Stav skladu sa mení iba prostredníctvom evidovaných pohybov.

---

### Task 11: Kalendár, dashboard a história vozidla

**Files:**
- Create: `src/features/calendar/WorkshopCalendar.tsx`
- Create: `src/features/dashboard/Dashboard.tsx`
- Create: `src/features/vehicles/VehicleHistory.tsx`

**Interfaces:**
- Produces prevádzkový prehľad, termíny a chronologickú históriu.

- [ ] **Step 1:** Kalendár: objednávka, príjem, plánované dokončenie, technik.
- [ ] **Step 2:** Presunutie termínu musí vykonať DB UPDATE a pri chybe vizuálne vrátiť pôvodný termín.
- [ ] **Step 3:** Dashboard: dnešné príjmy, rozpracované, čaká na schválenie/diely, hotové, nezaplatené, dnešná a mesačná tržba, náklady a hrubý zisk.
- [ ] **Step 4:** História vozidla chronologicky zobrazí zákazky, km, práce, diely, diagnostiku, prílohy, odmietnuté práce, faktúry a platby.
- [ ] **Step 5:** Commit: `feat: add calendar dashboard and vehicle history`.

**Acceptance:** Z jednej karty vozidla možno spätne dohľadať celý servisný príbeh vozidla.

---

### Task 12: UX ochrany, mobilné zobrazenie a end-to-end overenie

**Files:**
- Modify: formuláre všetkých features
- Create: `tests/e2e/workshop-flow.spec.ts`

**Interfaces:**
- Produces overený end-to-end tok autoservisu.

- [ ] **Step 1:** Pridať ochranu pred opustením formulára s neuloženými zmenami.
- [ ] **Step 2:** Autosave používať iba pre bezpečný draft, nie ako náhradu explicitného DB uloženia kritických operácií.
- [ ] **Step 3:** Upraviť formuláre pre desktop/tablet/mobile a zabezpečiť pohodlné ovládanie dotykom.
- [ ] **Step 4:** E2E scenár: login → nový zákazník → vozidlo → zákazka → diagnostika → práca/diel → schválenie → faktúra → čiastočná platba → doplatenie → história vozidla.
- [ ] **Step 5:** V polovici E2E scenára urobiť reload a potvrdiť, že údaje zostali v DB.
- [ ] **Step 6:** V druhom prihlásenom klientovi overiť rovnakú zákazku.
- [ ] **Step 7:** Otestovať DB failure: UI nesmie ukázať falošné `Uložené`.
- [ ] **Step 8:** Commit: `test: verify complete workshop workflow`.

**Acceptance:** Všetky tri hlavné kritériá úspechu zo špecifikácie sú splnené a aplikácia je pripravená na reálne zadávanie zákaziek.

---

## Delivery Order

1. Projekt + databáza.
2. Schéma + RLS.
3. Zákazníci a vozidlá.
4. Zákazky.
5. Diagnostika a prílohy.
6. Kalkulácie a schválenie.
7. Faktúry a platby.
8. Sklad.
9. Kalendár, dashboard, história.
10. Kompletné E2E testovanie a nasadenie.

## Final Verification

Pred označením projektu ako hotového treba preukázať:

- zákazka prežije reload stránky,
- rovnaké dáta sú viditeľné na inom zariadení po prihlásení,
- faktúra vznikne priamo zo zákazky bez opätovného prepisovania,
- čiastočné platby správne menia zostatok,
- diagnostické PDF/fotky zostanú dostupné,
- mechanik nevidí chránené ekonomické nastavenia,
- anonymný používateľ nečíta prevádzkové dáta,
- skladové množstvo sa nemení bez skladového pohybu,
- pri DB chybe nie je používateľovi oznámené úspešné uloženie.
