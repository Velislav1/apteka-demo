# Bulgarian regulation for pharmacy software (2024–2026)

> Research note, 2026-11-25. Method limitation: in this session WebFetch could not reach most primary hosts (mh.government.bg and fiscal-requirements.com did not resolve; other government hosts were not tried after that). So the notes come from search-result snippets and the documents they name. They are **not** from full reads of the consolidated legal texts. Every claim is cited to the source the snippet came from. Anything taken from background knowledge and not checked is marked **[unverified]**, placed under Inferences or Gaps, and must be checked against lex.bg / dv.parliament.bg before it is used for compliance.

## 1. Наредба Н-18 / СУПТО: what it requires and whether a pharmacy program is СУПТО

### Takeaway
A pharmacy is a retail outlet for goods. Its sales require a fiscal receipt under чл. 3 of Наредба Н-18. A program that manages those sales counts as СУПТО if it fits the ЗДДС definition, and it must then meet Приложение № 29 of Н-18 and be on the НАП public list. The obligation to declare conformity falls on the software producer or distributor. A 2025 draft (Budget 2026) to make СУПТО mandatory for all such traders from 1 Jan / 1 Apr 2026 was **apparently not adopted** in that form, so as of late 2026 using a СУПТО is not mandatory. Sources conflict on this, so verify ЗДДС as amended in ДВ бр. 69/2026.

### Cited Findings
- In a retail outlet for goods, including a pharmacy, sales require a fiscal receipt under чл. 3 of Н-18. Software that manages these sales is СУПТО and must meet the ordinance's requirements — [Yanaksoft explainer on СУПТО](https://yanaksoft.com/wp-content/uploads/sites/3/2019/02/СУПТО.pdf)
- НАП: the software must meet Приложение № 29 of Н-18 — [kik-info, НАП Q&A](https://kik-info.com/novini/novini-i-akcenti/NAP-s-nova-portsiya-vaprosi-i-otgovori-za.151410.php)
- A trader that uses such software may use only a product from НАП's public list of declared software — [kik-info, НАП position](https://kik-info.com/novini/novini-i-akcenti/Otnosno-softuerite-za-upravlenie-na-prodazhbite-i-deklariraneto.143029.php)
- About 6,000 outlets were using СУПТО (НАП figure). The Budget 2026 draft planned to make СУПТО mandatory through a ЗДДС change, with the Н-18 technical requirements unchanged — [kik-info](https://kik-info.com/novini/nap/Blizo-6000-targovski-obekta-izpolzvat-v-momenta-softuer.15119.php); [vatupdate, Nov 2025](https://www.vatupdate.com/2025/11/21/bulgaria-to-mandate-supto-pos-software-and-gps-tracking-for-high-risk-goods-in-2026-budget/); [Capital, Nov 2025](https://www.capital.bg/biznes/stoki_i_prodajbi/2025/11/12/4851259_riteil_novini_danuchnite_vuzkresiavat_supto_oshte/)
- Ministry of Finance: the mandatory measure was planned from April 2026. The declaration obligation is on the producer or distributor, not the trader — [Economy.bg](https://www.economy.bg/economy/view/62834/MF-Vyvezhdaneto-na-SUPTO-shte-byde-ot-april-2026); [News.bg](https://news.bg/economics/mf-ne-se-vavezhda-zadalzhenie-za-izpolzvane-na-supto-razraboten-ot-nap.html); [abtta.com, МФ разяснение](https://abtta.com/mf-publikuva-razyasnenie-za-izpolzvaneto-na-supto~246)
- **Conflict / latest status:** a blog dated 28.09.2026 says СУПТО is *not* mandatory and that the draft change is not in ЗДДС (last amended ДВ бр. 69 of 31.07.2026). Hellopos also says the trader "may choose" software from the НАП list — [Simplixo](https://simplixo.bg/blog/supto); [Hellopos, Н-18 (2026)](https://www.hellopos.pro/naredba-n18). Both are commercial, not primary, sources.
- Political pressure ("Да, България") to drop the mandatory СУПТО — [BTA](https://www.bta.bg/bg/news/bulgaria/1011178)
- Article-level analysis of the Budget 2026 СУПТО text — [aparati.bg](https://aparati.bg/supto-v-budget-2026-i-eu-regulacii/); [Club Счетоводство](https://www.clubschetovodstvo.bg/statii/danachno-oblagane-i-taksi-116/biudjet-2026_promeni-v-danachnata-politika-2775.html)
- Scheme options for СУПТО / ERP compliance — [kik-info](https://kik-info.com/novini/novini-i-akcenti/Shemi-SUPTO-ERP-raznosna-targoviya-i-vazmozhnosti-za.154467.php)

### Inferences
- Technical contents of Приложение 29, from background knowledge **[unverified, check against the text]**:
  - Each sale gets a **УНП** (уникален номер на продажбата) in the form `<8-char ФУ serial>-<4-char operator code>-<7-digit sequential number>`. It is generated when the order/sale is opened, and the fiscal receipt and storno must carry it.
  - No deleting or editing of completed sales or orders. Corrections only through storno documents linked to the original УНП and fiscal receipt number.
  - An unchangeable audit/log of operations.
  - A standardized XML export of sales data (per-УНП) to НАП on request (Приложение 38).
  - User/operator identification.
  - The software works only with a fiscal device (ФУ) and cannot print receipts that look fiscal.
- **External vs local:** everything except the fiscal device link can be built locally: УНП generation, no-delete, storno links, audit, export file. **Listing in the НАП register** is an external, administrative step (a declaration by the producer). The fiscal printer protocol is an external integration with a certified ФУ type.
- The current demo design (append-only `StockMovement`, `Reversal` instead of delete, `AuditLog`, sequence-based `Sale.Number`) already lines up with the core СУПТО principles. A УНП field, an operator code and the XML export would be the main additions.
- If a pharmacy uses only a fiscal device (ЕКАФП) and does not run sales through software that fits the ЗДДС definition, Н-18 Приложение 29 does not apply. The ЗДДС definition determines whether a pharmacy program "is" СУПТО **[unverified wording]**.

### Gaps
- Could not read the consolidated Н-18 text (Приложение 29, 33, 38) or the current ЗДДС §1 definition of СУПТО. The access failure is described at the top of this note.
- No primary confirmation (ДВ) of whether the Budget 2026 mandatory-СУПТО change was adopted. Sources conflict.
- Whether NHIF-reimbursed dispensing (the part paid by НЗОК) needs a fiscal receipt for the full amount or only for the patient co-payment was not found.

## 2. Е-рецепта / НЗИС: retrieve, dispense, partial dispense, report back, paper prescriptions

### Takeaway
Electronic prescribing and dispensing were added to Наредба № 4 от 2009 г. in 2020. Since 2022 a move to fully electronic prescribing has been allowed. Since 16 Oct 2023, diabetes medicines and systemic antibiotics can be prescribed only electronically. For NHIF prescriptions, the pharmacy must check eligibility through the НЗОК e-service and get from НЗИС a number for the dispensing (НРН) before it dispenses. All of this needs integration with НЗИС (his.bg) using a certified qualified electronic signature (КЕП). It cannot be built purely locally.

### Cited Findings
- A 2020 amendment to Наредба № 4/2009 introduced a procedure for electronic prescribing and dispensing. A 2022 amendment (ДВ бр. 53/2022) allowed a move to fully electronic prescribing after 1 Nov 2022 — [МЗ справка, обществено обсъждане Н4 (2022)](https://old.mh.government.bg/media/filer_public/2022/10/19/spravka_obshchestveno_obszhdane_n_4_ot_2009.pdf); [МЗ справка 2021](https://old.mh.government.bg/media/filer_public/2021/05/31/spravka_obshchestveno_obszhdane_naredba_4_300121_280221.pdf)
- A 2023 amendment (ДВ бр. 85 of 10 Oct 2023) led to citizen complaints to the Ombudsman — [Омбудсман](https://www.ombudsman.bg/storage/pub/files/20231020112518_Nar_4-NEW.pdf); MЗ motives mention a transitional option tied to integration with an information system — [МЗ мотиви 2023](https://old.mh.government.bg/media/filer_public/2023/11/23/motivi.pdf)
- From 16 Oct 2023, diabetes medicines and systemic antibiotics are prescribed only by e-prescription (secondary source) — [LinkedIn post](https://bg.linkedin.com/in/ivan-grozev-2252a1b3)
- A proposal to remove the "max 3 medicines per prescription form" limit, so that a pharmacy could dispense the rest within 24 h or another pharmacy could do it. Adoption not confirmed — [strategy.bg consultation](https://www.strategy.bg/bg/public-consultations/8212/export)
- A draft amendment discussed registering the dispensing in НЗИС, including for paper prescriptions (draft only) — [strategy.bg 8212](https://www.strategy.bg/bg/public-consultations/8212/export)
- A further draft amendment to Наредба 4 was in public consultation 21 Jun – 4 Jul 2024. Outcome not found — [strategy.bg 10410](https://www.strategy.bg/bg/public-consultations/10410/export)
- For NHIF dispensing, the dispenser checks the data through the НЗОК e-service and receives from НЗИС a dispensing number (НРН) before filling (draft НЗОК conditions, 18.03.2025) — [НЗОК проект УР](https://nhif.bg/upload/26894/%D0%9F%D1%80%D0%BE%D0%B5%D0%BA%D1%82%20%20%D0%A3%D0%A0%20%D0%B7%D0%B0%20%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%B8%20%D0%B4%D0%BE%D0%BF%D1%8A%D0%BB%D0%BD%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%BD%D0%B0%20%D0%A3%D1%81%D0%BB%D0%BE%D0%B2%D0%B8%D1%8F%20%D0%B8%20%D1%80%D0%B5%D0%B4%2018%2003%202025.docx); adopted version (ДВ бр. 28, 01.04.2025) — [НЗОК УР ДВ 28/2025](https://nhif.bg/upload/27054/%D0%A3%D1%81%D0%BB%D0%BE%D0%B2%D0%B8%D1%8F%20%D0%B8%20%D1%80%D0%B5%D0%B4%20%D0%B7%D0%B0%20%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%B8%20%D0%B4%D0%BE%D0%BF%D1%8A%D0%BB%D0%BD%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%BD%D0%B0%20%D0%A3%D1%81%D0%BB%D0%BE%D0%B2%D0%B8%D1%8F%20%D0%B8%20%D1%80%D0%B5%D0%B4%20%D0%94%D0%92%20%D0%B1%D1%80.28_01.04.25.docx)
- A 2026 review describes the Bulgarian model as a system that combines BgMVO, НЗОК and НЗИС, with automated real-time verification of unique identifiers — [Bulgarian Medical Journal, MU-Sofia](https://journals.mu-sofia.bg/index.php/bmj/article/view/793)

### Inferences
- What the software must do technically (generic НЗИС dispensing flow) **[unverified detail, needs the his.bg technical specs]**:
  - fetch the e-prescription by its НРН plus the patient identifier;
  - show the prescribed items;
  - register the dispensing (full or partial), get a dispensing НРН and sign the message with a КЕП;
  - support cancelling or correcting a dispensing.
- This needs registration with НЗИС, a test environment and a certificate. **Not buildable without an external integration.** Fully outside the demo (CLAUDE.md lists НЗИС / е-рецепти as out of scope).

### Gaps
- The exact current rules on partial dispensing were not found: how many times, the deadline for the rest, and whether the rest may be dispensed at another pharmacy.
- No primary confirmation of which prescriptions may still be on paper in 2026 (for example non-reimbursed Rx, or narcotics on special forms), or whether paper prescriptions must be registered in НЗИС.
- The his.bg technical specifications (API, message formats, КЕП requirements) could not be fetched.

## 3. НЗОК: reimbursed drugs, reports, price lists, co-payment, separate stock

### Takeaway
A pharmacy that dispenses reimbursed drugs signs an individual contract with НЗОК (via РЗОК) under the published "Условия и ред". It must use software that meets НЗОК's software requirements, which include keeping a **separate NHIF stock** and submitting reports. Contracts for 2026 run from 1 Jan 2026. The work needs НЗОК's price and reimbursement lists (ПЛС / НСЦРЛП) and submissions to НЗОК, so it is an external integration.

### Cited Findings
- НЗОК has published "Изисквания към аптечния софтуер" for pharmacies under contract (2012; may be outdated). Among other things, partly rejected prescriptions go into corrective specifications to НЗОК — [НЗОК, изисквания към аптечния софтуер](https://www.nhif.bg/upload/12501/software-apteki-iziskvanija-16-11-2012.doc)
- The amended "Условия и ред за сключване на индивидуални договори за заплащане на лекарствени продукти" were published in ДВ бр. 28 of 01.04.2025 — [НЗОК](https://nhif.bg/upload/27054/%D0%A3%D1%81%D0%BB%D0%BE%D0%B2%D0%B8%D1%8F%20%D0%B8%20%D1%80%D0%B5%D0%B4%20%D0%B7%D0%B0%20%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%B8%20%D0%B4%D0%BE%D0%BF%D1%8A%D0%BB%D0%BD%D0%B5%D0%BD%D0%B8%D0%B5%20%D0%BD%D0%B0%20%D0%A3%D1%81%D0%BB%D0%BE%D0%B2%D0%B8%D1%8F%20%D0%B8%20%D1%80%D0%B5%D0%B4%20%D0%94%D0%92%20%D0%B1%D1%80.28_01.04.25.docx)
- An НЗОК Board decision (12.02.2026) says contracts are in force from 1 Jan 2026, using the 2025 mechanism for the direct payment amount — [НЗОК решения НС 12.02.2026](https://www.nhif.bg/upload/30507/%D1%80%D0%B5%D1%88%D0%B5%D0%BD%D0%B8%D1%8F%20%D0%9D%D0%A1%2012022026.pdf)
- BTA: pharmacies are to receive 15 % of the value of dispensed medicines as a pharmacy fee (date of report unclear) — [BTA video](https://www.bta.bg/bg/videos/21492)

### Inferences
- What the software would need **[partly unverified]**:
  - load НЗОК lists (reimbursed products with НЗОК code, reference/reimbursement price, reimbursement level);
  - compute the patient co-payment = retail price − НЗОК-paid amount;
  - keep a separate NHIF stock (the project already models `StockType.Nhif`);
  - produce periodic (monthly / bi-monthly) financial and statistical reports and specifications in НЗОК's XML format;
  - handle НЗОК rejections and corrective specifications.
- Contracts and the reporting channel are external. List import and stock separation can be built locally once the list format is known.

### Gaps
- The current report frequency and deadlines (monthly vs twice a month) and the current XML schema were not found in primary sources.
- The current co-payment calculation rules and the ПЛС price-list download format (from НСЦРЛП / ncpr.bg) were not fetched.

## 4. FMD / BgMVO: DataMatrix scanning, decommissioning, alerts

### Takeaway
Under Directive 2011/62/EU and Delegated Regulation (EU) 2016/161, applied in Bulgaria since 9 Feb 2019, a pharmacy must verify the safety features and **decommission** the unique identifier (2D DataMatrix) in the national system (BgMVS, run by BgMVO) when it dispenses a pack, and must handle alerts by the БДА procedure. This needs a connection to BgMVO through an onboarded end-user client. **Not buildable without external onboarding.**

### Cited Findings
- БДА guideline v1.0 (2019) on verification and decommissioning after 9 Feb 2019 in the Bulgarian system — [БДА, Managing Alerts Guidelines BG v1.0](https://www.bda.bg/images/stories/documents/verifikacia/Managing%20Alerts%20Guidelines%20BG%20v%201.0.pdf)
- БДА guideline v2.0 (Feb 2021) for wholesalers and pharmacies on managing alerts in the Bulgarian Medicines Verification System — [БДА, Managing Alerts WH and Pharmacies v2.0](https://www.bda.bg/images/stories/documents/verifikacia/Managing%20Alerts%20Guidelines%20WH%20and%20Pharmacies_v2.0.pdf)
- The MAH guideline cites Directive 2011/62/EU and Delegated Regulation 2016/161 as the legal basis — [БДА, MAH guideline v1.0](https://www.bda.bg/images/stories/documents/verifikacia/Managing%20Alerts%20Guideline%20MAH_v1.0.pdf)
- The Bulgarian model combines BgMVO, НЗОК and НЗИС for real-time verification of unique identifiers — [Bulgarian Medical Journal 2026](https://journals.mu-sofia.bg/index.php/bmj/article/view/793)

### Inferences
- From Regulation 2016/161 (art. 25 dispensing, art. 30 suspected falsification, art. 13 reversal window of 10 days) — background knowledge **[unverified here]**. The software must:
  - parse GS1 DataMatrix: GTIN/product code (01), serial (21), batch (10), expiry (17), and the national reimbursement number if present;
  - call verify + decommission ("dispensed") at the point of sale;
  - support undo (reactivation) within 10 days if the pack was not dispensed;
  - not dispense when an alert is raised, then quarantine the pack and follow the БДА alert procedure;
  - store the alert ID.
- Local preparation is possible: parse DataMatrix and fill batch and expiry from the scan, which fits the existing `Batch`. The BgMVO API itself is external.

### Gaps
- BgMVO onboarding steps and the API spec (bgmvo.bg) could not be fetched.
- Whether НЗОК now requires the FMD unique identifier in its dispensing reports (the integration hinted at by the MU-Sofia review) is not confirmed.

## 5. Controlled substances (ЗКНВП + Наредба № 21/2000)

### Takeaway
Every operation with narcotic substances or medicinal products that contain them must be documented. Each operation is recorded in a **register certified by the narcotics inspector**. Corrections may only be made with the certification of the responsible pharmacist (чл. 34 ЗКНВП). Quantities are totalled after the end of each month and the register is certified. The register's obligations can be built locally, but paper or official certification and reporting to the authorities are external or organisational. The demo forbids selling narcotics entirely, which avoids the issue.

### Cited Findings
- Наредба № 21/2000 sets the documentation requirements for production, processing, trade, storage etc. of narcotic substances. Each operation is recorded in a register certified by the narcotics inspector. A 2013 amendment extended the scope to "medicinal products containing narcotic substances" — [МЗ, Наредба 21 (обн. 27.09.2013)](https://www.mh.government.bg/upload/4368/naredba-21-21-10-2000-obn-27-09-2013.pdf)
- Corrections in the register only with certification by the pharmacist under чл. 34 ЗКНВП. Quantities are totalled after the end of each month and the register is certified — [МЗ, изм. Наредба 21 (2015)](https://old.mh.government.bg/media/filer_public/2015/04/17/naredbaizm-dop-naredba21-ot-2000g-deinosti-narkotichni-veshtestva-tehnite-preparati.pdf)
- An amendment to the ЗКНВП (2010) — [МЗ](https://www.mh.government.bg/upload/165/zakon-izmenenie-dopalnenie-zakon-za-kontrol-narkotichni-veshtestva_19-03-2010.pdf); a 2020 draft change to Наредба 28 on narcotics — [МЗ проект](https://old.mh.government.bg/media/filer_public/2020/10/13/proekt_naredba_28_narkotitsi__spisk_nov.pdf)

### Inferences
- Background knowledge **[unverified]**: narcotic drugs from the ЗКНВП lists are prescribed on special prescription forms (Наредба 4: yellow form for list II substances, green form for some others). The pharmacy keeps these prescriptions for a set period, often cited as **5 years**, and reports quarterly or annually to the МЗ / РЗИ.
- The software would need:
  - a per-substance register with running balance and each receipt/dispense operation;
  - a month-end total;
  - a printable form for certification;
  - linking to the prescription form number.

### Gaps
- Confirmed form colours, retention periods and reporting frequency were not found in fetched text.
- Whether narcotic e-prescriptions are now mandatory in 2026 was not found.

## 6. Наредба 28 (pharmacy books/registers), retention, GDPR, maximum markups

### Takeaway
Наредба № 28 от 2008 г. (ДВ бр. 109/2008, last amended 2023 by ДВ бр. 85 and 105, with a further draft consulted May–June 2025) sets the organisation of pharmacies and the books they keep. The exact list of books could not be retrieved. Price regulation of prescription and reimbursed drugs (including retail markups) is in the Наредба за условията, правилата и реда за регулиране и регистриране на цените на лекарствените продукти (ПМС № 97/2013), amended through a 2025 draft. The exact markup bands could not be retrieved. These are local obligations: no external system, except that price ceilings come from the НСЦРЛП register.

### Cited Findings
- Наредба № 28/2008 on the organisation and work of pharmacies, published ДВ бр. 109/2008. Last amendments ДВ бр. 85/2023 and 105/2023 — [МЗ, изм. Наредба 28](https://www.mh.government.bg/upload/4289/naredba-izmenenie-dopulnenie-naredba28ot2008-ustrojstvoto-reda-organizaciqta-rabota-apteki.pdf); [МЗ, изм. Наредба 28](https://www.mh.government.bg/upload/4075/naredba-izmdop-naredba28-2008.pdf)
- A draft amendment to Наредба 28 (automats/list), consulted 10.05.2025–09.06.2025 — [МЗ проект 09.05.2025](https://www.mh.government.bg/upload/14166/%D0%9F%D1%80%D0%BE%D0%B5%D0%BA%D1%82%20%D0%9D%D0%B0%D1%80%D0%B5%D0%B4%D0%B1%D0%B0%2028%20%20%D0%B0%D0%B2%D1%82%D0%BE%D0%BC%D0%B0%D1%82%D0%B8%20%D1%81%D0%BF%D0%B8%D1%81%D1%8A%D0%BA%20-%2009.05.2025.pdf)
- Справка on Наредба 28 consultation (scales, etc.) — [МЗ](https://old.mh.government.bg/media/filer_public/2020/12/22/spravka_obshchestveno_obszhdane_naredba_28_vesi_i_nadeto.pdf)
- The price ordinance was adopted by ПМС № 97 of 19.04.2013 (ДВ бр. 40/2013). A May 2025 draft amending decree was consulted 17.05–30.05.2025 (measure 185 of the 2025 plan). Industry objected to a rule capping re-registered prices at last price + inflation — [МЗ ПМС проект 16.05.2025](https://www.mh.government.bg/upload/14226/PMS%20ceni%20lekarstva%2016.05.2025.pdf); [Справка ОО](https://mh.government.bg/upload/14767/%D0%A1%D0%BF%D1%80%D0%B0%D0%B2%D0%BA%D0%B0%20%D0%9E%D0%9E%20%D0%9F%D0%9F%D0%9C%D0%A1-%D0%9D%D0%B0%D1%80%D0%B5%D0%B4%D0%B1%D0%B0%20%D1%86%D0%B5%D0%BD%D0%B8%20%D0%BD%D0%B0%20%D0%9B%D0%9F.pdf); [strategy.bg 11111](https://www.strategy.bg/bg/public-consultations/11111/export)
- Base text of the price ordinance — [МЗ](https://www.mh.government.bg/upload/4403/naredba-regulirane-registrirane-tsenite-na-lekarstvenite-produkti.pdf)
- The Health Minister ordered drug prices to be published in euro, with registers adapted by 1 Jan 2026 — [BTA](https://www.bta.bg/en/news/bulgaria/938988-health-minister-orders-drug-prices-to-be-published-in-euro-ahead-of-currency-tra)

### Inferences
- Background knowledge **[unverified]**: Наредба 28 requires books and records such as:
  - a prescription book (рецептурна книга) for magistral and officinal preparations;
  - a book for quality control of prepared medicines;
  - a book of narcotics (per Наредба 21);
  - temperature and humidity records for storage rooms and fridges;
  - records of recalls and complaints (БДА recall signals: stop sale, quarantine, return).
  All of these can be built locally; FEFO batch and expiry tracking (already in the demo) supports recall by batch number.
- Background knowledge **[unverified]**:
  - the price ordinance caps pharmacy markups only on prescription drugs (ПЛС / registered prices), in price bands of decreasing percentage with fixed amounts at the top;
  - OTC prices are free.
  The software would need to enforce "sale price ≤ registered maximum retail price" for Rx products. The demo's free 25 % default markup is fine for OTC only.
- GDPR (Regulation 2016/679) plus the Bulgarian ЗЗЛД: ЕГН and health data are special categories (art. 9). The software needs role-based access, an access log, minimised display, encryption at rest and in transit, a retention policy and data subject rights. These are local obligations, though a DPO, a ROPA (record of processing activities) and a DPIA are organisational. The demo's rule (only fake ЕГН, audit log, no password in audit) fits.

### Gaps
- The exact list of required books in Наредба 28, plus temperature-log rules, was not retrieved.
- The current pharmacy markup bands and percentages under ПМС 97/2013 as amended in 2025 were not retrieved, and the conversion of the bands to EUR is unknown.
- Retention periods were not found for: sales/fiscal records (accounting law is generally 10 years for registers and 3–5 years for other documents **[unverified]**), prescriptions, and the narcotics register.
- No source found on GDPR guidance specific to pharmacies from КЗЛД (the Bulgarian data protection authority).

## 7. Euro adoption (1 Jan 2026): dual display, rounding, conversion

### Takeaway
Bulgaria adopted the euro on 1 Jan 2026 at a fixed rate of 1.95583 BGN/EUR. Dual price display (BGN + EUR) was mandatory from 8 Aug 2025 to **8 Aug 2026**. After that, prices are shown only in euro, and showing BGN is voluntary and only for reference. From 1 Jan 2026 fiscal devices record and report in euro and cents. Pharmacies were specifically inspected for wrong conversions. This is purely local logic, plus fiscal-device firmware.

### Cited Findings
- Mandatory dual pricing 8 Aug 2025 – 8 Aug 2026 — [CMS](https://cms.law/en/bgr/legal-updates/bulgaria-updates-euro-introduction-law-info-on-key-changes-and-act-s-guidelines); [Sofia Globe](https://sofiaglobe.com/2025/08/08/bulgaria-and-the-euro-mandatory-dual-pricing-comes-into-effect/); [BTA, until 8 Aug](https://www.bta.bg/en/news/1056337-euro-coordination-centre-chair-reminds-traders-dual-price-labeling-continues-unt)
- Dual display ended 8 Aug 2026; after that, euro only, BGN voluntary for reference — [Sofia Globe](https://sofiaglobe.com/?p=128567); [BNR](https://bnrnews.bg/en/post/512683/deadline-for-indicating-prices-in-both-euro-and-lev-expires)
- Anti-speculation measures were extended to 8 Aug 2026 (the original draft said 31 Dec 2026) — [BTA](https://www.bta.bg/bg/news/economy/939081-merkite-sreshtu-eventualna-spekula-s-tsenite-pri-vavezhdane-na-evroto-u-nas-shte); [BTA](https://www.bta.bg/bg/news/economy/934280-targovtsite-ne-mogat-da-vdigat-tsenite-neobosnovano-do-kraya-na-2026-g-ili-gi-g)
- Inspections in pharmacies found incorrect currency conversion in dual prices — [BTA](https://www.bta.bg/en/news/929100-checks-in-pharmacies-reveal-cases-of-incorrect-currency-conversion-in-double-mar); [Novinite](https://www.novinite.com/articles/233494/Consumer+Watchdog+Finds+Dual+Pricing+Violations+in+Bulgarian+Pharmacies)
- From 1 Jan 2026, fiscal devices register and report only in euro and cents (unofficial guide) — [kik-info guide](https://kik-info.com/forum/index.php?topic=37543)
- Rate 1.95583 BGN = 1 EUR — [BNB](https://bnb.bg/AboutUs/PressOffice/POAccessionToTheEuroArea/POAEFIInformationalMaterials/PO_AEFIINFORMMATERIALS_A2_BG); law text — [ЗВЕРБ](https://elhovo-rs.justice.bg/upload/136279/Закон%20за%20въвеждане%20на%20еврото%20в%20РБ.pdf); [FSC copy](https://fsc.bg/wp-content/uploads/2024/08/zverb.pdf); amendments promulgated — [BTA](https://www.bta.bg/bg/news/bulgaria/945172-promenite-v-zakona-za-vavezhdane-na-evroto-sa-obnarodvani-v-darzhaven-vestnik-)

### Inferences
- Background knowledge **[unverified here]**:
  - ЗВЕРБ conversion rounds to the nearest cent using the third decimal (half up);
  - during the 1–31 Jan 2026 dual-circulation period, receipts showed the total in both currencies and change was given in euro.
- For the demo (EUR-only, `1 234,50 €`, decimal money) no further work is needed after 8 Aug 2026. Historical BGN data imported from Аптека Експерт 2 would need conversion at 1.95583 with cent rounding.

### Gaps
- The exact article text on rounding rules (ЗВЕРБ) and the fiscal receipt rules during dual circulation could not be fetched.

## Summary table: external vs local

| Obligation | Legal source | External system / certification? | Buildable locally? |
|---|---|---|---|
| Fiscal receipt | Н-18 чл. 3 | Yes: certified ФУ + НАП link | Only driver integration |
| СУПТО (УНП, no delete, storno, audit, XML export) | Н-18 Прил. 29; ЗДДС | НАП list declaration by producer | Logic yes; listing external |
| E-prescription retrieve/dispense | Наредба 4/2009; ЗЗ | Yes: НЗИС + КЕП | No |
| НЗОК reimbursement, reports, NHIF stock | ЗЗО; НЗОК Условия и ред (ДВ 28/2025) | Yes: contract, НЗОК lists, report submission | Stock separation and list import only |
| FMD verify/decommission | Dir. 2011/62/EU; Reg. 2016/161; БДА guidelines | Yes: BgMVO onboarding | DataMatrix parsing only |
| Narcotics register | ЗКНВП; Наредба 21 | Certification by inspector (organisational) | Yes |
| Pharmacy books, temperature logs, recalls | Наредба 28/2008 | No (БДА recall signal is input) | Yes |
| Price ceilings / markups on Rx | ПМС 97/2013 | Price register (НСЦРЛП) as input | Yes, once the list is imported |
| GDPR for ЕГН / health data | GDPR, ЗЗЛД | No (DPO/DPIA organisational) | Yes |
| Euro display / conversion | ЗВЕРБ | No | Yes |
