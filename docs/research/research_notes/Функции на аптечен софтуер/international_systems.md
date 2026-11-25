# Feature sets of international pharmacy management / POS systems (2024–2026)

Method note: ~14 web searches in a restricted cloud session. Direct page fetches were blocked (e.g. www.pioneerrx.com did not resolve), so findings rest on search-result summaries of vendor pages, vendor help docs, trade press (Apotheke Adhoc, Pharmazeutische Zeitung, Drug Topics) and review sites. Nearly all feature claims are **vendor-stated**. No usable sources were found for Romanian systems, Liberty Software, Omnicell retail, or Lightspeed-type cloud POS (see Gaps).

## Which features are standard everywhere vs. premium?

### Takeaway
Min/max reorder points with order proposals, wholesaler electronic ordering, batch/expiry tracking, expiry and dead-stock reports, customer records, role-based permissions and online-shop/accounting interfaces appear in essentially every major system. Usage-based forecasting, multi-wholesaler price comparison, recommended returns, inter-store redistribution, loyalty, ESL integration and interaction checking at the till are either sold as separate modules/licences or reserved for higher plans.

### Cited Findings

**Inventory automation (standard core)**
- PioneerRx (US): auto reorder points = min/max balance-on-hand; when stock drops below min, an order is generated up to max (vendor example: min 99, max 300) — [PioneerRx blog: Auto Reorder Points](https://www.pioneerrx.com/blog/featured-feature-auto-reorder-points)
- PioneerRx: a "Reorder Point Wizard" calculates reorder points and the system can recalculate them daily as usage shifts with season or growth — [PioneerRx: 6 inventory features](https://www.pioneerrx.com/blog/6-pioneerrx-features-to-help-perfect-your-pharmacys-inventory)
- PioneerRx: usage-based ordering (reorder points generated from usage data) — [PioneerRx usage-based ordering](https://www.pioneerrx.com/blog/featured-feature-usage-based-ordering); [Red Sail Technologies](https://redsailtechnologies.com/pharmacy-software/pioneerrx)
- PioneerRx: "Recommended ordering" with colour-coded reorder status shown inside the workflow — [PioneerRx recommended ordering](https://pioneerrx.com/blog/featured-feature-recommended-ordering)
- PioneerRx: "Recommended return" feature (suggests stock to send back to the wholesaler); also multiple wholesalers, multiple inventories (e.g. 340B), drug loan tracking, shelf stickers, mobile inventory app — [PioneerRx recommended return](https://www.pioneerrx.com/blog/featured-feature-recommended-return); [multiple wholesalers](https://www.pioneerrx.com/blog/featured-feature-multiple-wholesalers); [multiple inventories](https://www.pioneerrx.com/blog/featured-feature-multiple-inventories); [SoftwareOne listing](https://platform.softwareone.com/product/pioneerrx/PCP-5180-2290); [PioneerRx Mobile Inventory app](https://apps.apple.com/us/app/-/id887579235)
- Pharmatechnik IXOS (DE): help documents exist for demand determination ("Bedarfsermittlung"), refill lists ("Auffülllisten"), handling of out-of-stock/back-ordered items ("Defekte und Nachlieferungen"), a shared order proposal ("Gemeinsamer Bestellvorschlag") with statistics print option (v2019.9), online availability inquiry/order to wholesaler from the article screen, and a "cheaper supplier" hint ("Hinweis günstiger Lieferant") — [Bedarfsermittlung PDF](https://www.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/Bedarfsermittlung.pdf); [Auffülllisten PDF](https://www.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/Auffuelllisten.pdf); [Defekte und Nachlieferungen PDF](https://www.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/Defekte_und_Nachlieferungen.pdf); [Hinweis günstiger Lieferant PDF](https://www.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/Hinweis_guenstiger_Lieferant.pdf); [Online-Anfrage/Bestellung PDF](https://www.pharmatechnik.de/media/82/9b/4e/1779110480/Online-Anfrage_Bestellung.pdf); [IXOS Versionsbeschreibung 2019.9](https://ixos-onlinehilfe.pharmatechnik.de/Default/Content/PT99_Sonstiges/Archiv-Versionsbeschreibung/2019/IXOS%20Versionsbeschreibung%202019.9.pdf)
- IXOS: reservations/pick-ups ("Abholungen, Reservierungen") — i.e. items reserved/owed to a customer and collected later — [Abholungen PDF](https://www.pharmatechnik.de/fileadmin/user_upload/Download/IXOS/Abholungen_Reservierungen_abholen.pdf)
- Pharmatechnik promoted an autonomous ordering system (2017), the founder comparing it to self-driving cars — [Apotheke Adhoc](https://www.apotheke-adhoc.de/nachrichten/detail/markt/pharmatechnik-warenwirtschaft-ohne-apotheker-apothekensoftware-edv/print.html)
- Cegedim Pharmacy Manager (UK, successor to Nexphase): in-built ordering including "cascading" (order falls through to next wholesaler), links to all major wholesalers and 3rd-party ordering tools, stock insights for dead stock and excess stock; Stock Inventory screen with "Often Owed" tile; icon on ordering screen when wholesaler responds (v14.6) — [Cegedim Pharmacy Manager](https://cegedimrx.co.uk/pharmacy-manager-new); [Bristec page](https://www.cegedimrx.co.uk/bristec); [Release Guide 14.6](https://help.cegedim-healthcare.co.uk/PDFs/Pharmacy_Manager/Release_Guides/Pharmacy_Manager_Release_Guide_14.6.pdf)
- Cegedim plan tiers: Core / Advanced / Ultimate, Core quoted at £350/month (single vendor source, unverified) — [Cegedim Pharmacy Manager](https://cegedimrx.co.uk/pharmacy-manager-software)
- Titan PMR (UK): a user review says a recent update added "full stock control" and their owed-items queue fell from ~150 to <20; others cite barcode validation as a safety feature (anecdotal Trustpilot) — [Trustpilot Titan PMR](https://uk.trustpilot.com/review/titanpmr.com?page=2)
- KS-AOW (Kamsoft, PL): data exchange with wholesalers requires a separately licensed module KS-EWD; complaints and returns to wholesalers can be filed from KS-AOW — [Farmacol KS-EWD manual](https://www.farmacol.com.pl/gfx/farmacol/userfiles/phypa/grafika/2020/reklamacje/instrukcja_ks-ewd.pdf); [Slawex KS-EWD manual](https://www.slawex.com.pl/wp-content/uploads/2023/06/INSTRUKCJA_KS-EWD_SLAWEX.pdf)
- KS-AOW 2025: structured e-invoices (KSeF) and stocktaking with handheld KS-APTIX — [Kamsoft KSeF Q&A 2025](https://api.kamsoft.pl/app-params/promos/000000/20250923_Q&A_Szkolenie_KSeF_Apteki.pdf); [OIA Warszawa invitation 2025](https://oia.waw.pl/system/files/2025-10/zaproszenie-nawigator-farmacja-2025.pdf)
- German vendor ad: "push-button stocktaking with continuous stock control" and automatic demand determination — [Apotheke Adhoc: digitale Warenwirtschaft](https://www.apotheke-adhoc.de/rubriken/detail/apo-business/digitale-warenwirtschaft-huerden-und-chancen/)

**Purchasing**
- CGM LAUER WINAPO: advance delivery and batch management named as functions in the WINAPO line — [Apotheke Adhoc branchennews](https://www.apotheke-adhoc.de/branchennews/alle-branchennews/branchennews-detail/software-hilft-den-betriebsaufwand-zu-reduzieren/)
- Multiple wholesalers per pharmacy is a recurring feature (PioneerRx, Cegedim cascading, IXOS cheaper-supplier hint) — sources above.

**Pricing / labels**
- Electronic shelf labels (ESL) now replace paper labels in many German pharmacies; linked to the inventory system they make slow movers and soon-expiring products easy to identify — [Pharmazeutische Zeitung: Digitale Preisschilder](https://www.pharmazeutische-zeitung.de/digitale-preisschilder-mehr-action-im-regal-153099/)
- PioneerRx offers shelf stickers — [SoftwareOne listing](https://platform.softwareone.com/product/pioneerrx/PCP-5180-2290)

**POS**
- BestRx BestPOS (US): configurable tender types; house charge accounts (store option whether balance prints on receipt); choosing "Charge Account" tender prompts for customer and offers to open an account; accounts are linked by family name so family members share one — [BestRx Tender Types](https://bestrxps.atlassian.net/wiki/spaces/BRXHLP/pages/1741258759/Tender+Types); [BestRx Store Setup](https://bestrxps.atlassian.net/wiki/spaces/BRXHLP/pages/1692434448/C.+Store+Setup)
- BestRx loyalty is built in but locked; must be unlocked/configured with vendor staff (i.e. premium/add-on); configurable rewards, values and expiry — [BestRx loyalty](https://bestrx.com/?p=5050); [BestPOS](https://bestrx.com/product/bestpos)
- PrimeRx PrimePOS: cloud-based POS; loyalty card number attached to patient account with points/promotions applied automatically — [PrimeRx POS](https://www.primerx.io/pharmacy-point-of-sale/)
- Fred IT (AU): Fred NXT combines dispense, POS, retail management; fingerprint login on POS; integrated EFTPOS (Tyro); Fred NXT Dispense touch-friendly, runs locally (only external services need internet) — [Fred NXT](https://www.fred.com.au/what-we-do/fred-nxt/); [Tyro partner listing](https://www.tyro.com/pos-partners/retail-management-hero-fred-pos/); [Fred newsletter Dec 2020](https://webhelp.fred.com.au/freddispense/Resources/PDF/paul-update-newsletter-dec-2020.pdf)
- Cegedim medEpos: EPOS integrated with the PMR at the counter; own or vendor hardware — [Cegedim medEpos](https://www.cegedim-healthcare.co.uk/medepos)
- WINAPO ux: function fields definable per workstation; touch support ("Ein Touch und so viel Komfort") — [Apotheke Adhoc: WINAPO ux kommt](https://www.apotheke-adhoc.de/nachrichten/detail/markt/winapo-ux-cgm-lauer-kommt-mit-neuer-software-warenwirtschaftssysteme/); [Apotheke Adhoc: Ein Touch](https://www.apotheke-adhoc.de/branchennews/alle-branchennews/branchennews-detail/cgm-lauer-fischer-winapo-ux-ein-touch-und-so-viel-komfort/)

**Cash management / shifts (general retail POS — no pharmacy-specific doc found)**
- Standard reconciliation = count cash at end of shift/day, compare with POS-recorded cash sales minus payouts minus opening float; breakdown per tender — [Shopify: balancing a cash drawer](https://www.shopify.com/blog/balancing-a-cash-drawer)
- Oracle Xstore cash drawer reconciliation report: declared vs. expected per tender, over/short, signature lines for cashier and manager — [Oracle Xstore report](https://docs.oracle.com/en/industries/retail/retail-xstore-point-of-service/21.0/rpsug/cash-drawer-reconciliation-report.htm)
- Aloha: manager-initiated blind count before end-of-day; limited attempts, then forced reconciliation by higher role — [NCR Aloha drawer reconciliation](https://docs.ncrvoyix.com/restaurant/aloha-pos/implementing/drawer_reconciliation/implementing_drawer_reconciliation)
- Toast: job-based (role) permissions recommended; granular rights like No Sale, Shift Review, Close Out Day, Cash Drawer Lockdown override — [Toast cash mgmt permissions](https://doc.toasttab.com/doc/platformguide/adminUserPermissionsCashMgmt.html)

**User management**
- WINAPO ux: permission concept with predefined roles per employee; ABDA IT-security checklist notes role/group access control — [Apotheke Adhoc WINAPO ux](https://www.apotheke-adhoc.de/nachrichten/detail/markt/winapo-ux-cgm-lauer-kommt-mit-neuer-software-warenwirtschaftssysteme/); [CGM ABDA checklist guide](https://www.cgm.com/deu_de/daten/downloads/apotheke/anleitung-abda-it-sicherheitspruefliste-fuer-apotheken.html)

**Clinical**
- German systems: all vendors let the pharmacy configure which interaction severities are shown at the till; PZ recommends only moderate and severe; relevance must be judged with the patient and the customer medication file ("Medikationsdatei") — [PZ: Interaktions-Check](https://www.pharmazeutische-zeitung.de/ausgabe-392007/der-interaktions-check-in-der-apotheke/)
- Study: with customer cards, 6-month medication history was available; 530 of 5,145 documented interaction alerts involved self-medication (OTC) products — [PZ: Interaktions-Check in Bayern](https://www.pharmazeutische-zeitung.de/ausgabe-292006/der-interaktions-check-in-bayern/)
- IXOS: customer-related interactions; resolved interactions can be marked with an expiry date after which they reappear at the till — [IXOS Kundenbezogene Interaktionen](https://apotheke.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/Kundenbezogene_Interaktionen.pdf)
- Interaction check data is licensed (Avoxa/ABDA database); due to EU MDR it is to be performed centrally by external Avoxa software, raising cost — [Apotheke Adhoc: Interaktionscheck wird teurer](https://www.apotheke-adhoc.de/nachrichten/detail/apothekenpraxis/interaktionscheck-warenwirtschaft-wird-teurer/)
- CGM LAUER: CAVE risk profile (needs customer consent), MediPlanOnline, SmartCourier delivery, GENIUS Sichtwahl (digital shelf display) with POS connection as customer-retention tools — [Apotheke Adhoc: Das neue WINAPO ux](https://www.apotheke-adhoc.de/branchennews/alle-branchennews/branchennews-detail/cgm-lauer-das-neue-winapo-ux-ist-da-5/)
- Fred Dispense Plus: MIMS drug interaction checking, clinical decision support, audit trail of every action on a script — [Fred Dispense Plus](https://www.fred.com.au/solutions/fred-dispense-plus/)

**Compliance**
- IXOS securPharm (FMD) module documented (2019.1) — [IXOS securPharm](https://www.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/IXOS_securPharm_2019.1.pdf)
- Mediox (CZ): FMD/NSOL registration configuration guide — [Apatyka servis FMD guide](https://www.apatykaservis.cz/wp-content/uploads/2019/01/fmd-registrace-konfigurace_mediox.pdf)

**Multi-store**
- McKesson EnterpriseRx: fully hosted; centralises Rx processing, inventory, pricing, reporting across multiple locations; supports centralised or location-based operating models; workload balancing — [McKesson EnterpriseRx](https://www.mckesson.com/pharmacy-management/software/enterpriserx-pharmacy-management-system); [McKesson story](https://www.mckesson.com/stories-insights/enterpriserx-is-built-for-modern-pharmacy-operations/)
- McKesson Pinpoint Transfer: separate product for redistributing inventory between pharmacies to maximise use and minimise returns; "Inventory Optimization Toolkit" activated in EnterpriseRx 7.1 — [McKesson pharmacy management software](https://www.mckesson.com/pharmacy-management/software); [Drug Store News](https://drugstorenews.com/pharmacy/mckesson-streamlines-workflow-enterpriserx-0-release)
- Mediox (CZ, Apatyka servis, >1/3 of Czech pharmacies): centralisation within a pharmacy group of assortment management, partner rights, orders and stock management — [Apatyka servis Okénko 2025](https://www.apatykaservis.cz/wp-content/uploads/2026/06/2025rijen-okenko-as.pdf)

**Integrations**
- WINAPO ux: online shops connected so customer orders arrive directly in the system; open items reconcilable with DATEV accounting — [Apotheke Adhoc WINAPO ux](https://www.apotheke-adhoc.de/nachrichten/detail/markt/winapo-ux-cgm-lauer-kommt-mit-neuer-software-warenwirtschaftssysteme/); [DATEV Marktplatz WINAPO ux](https://www.datev.de/web/de/marktplatz/winapo-ux); [DATEV Marktplatz IXOS](https://www.datev.de/web/de/marktplatz/pharmatechnik-ixos)

### Inferences
- "Standard everywhere": min/max reorder & order proposal, wholesaler e-ordering, owed/back-order tracking, batch/expiry, expiry and dead-stock lists, customer file, roles, accounting export.
- "Premium/add-on" signals: separately licensed modules (Kamsoft KS-EWD, BestRx locked loyalty, McKesson Pinpoint Transfer as separate product, Cegedim tiered plans, licensed interaction database).
- Cash-drawer reconciliation (blind count, declared vs. expected per tender, over/short) is generic retail POS practice and should be assumed table-stakes.

### Gaps
- No full feature matrix from any vendor; no pricing beyond Cegedim's single figure.
- Liberty Software, Omnicell retail, Lightspeed-type cloud POS: no feature content found.
- Romanian systems: no sources found at all. Polish/Czech details are partial and partly dated.
- Split payments / layaway not explicitly documented for any pharmacy vendor found.
- ABC/XYZ, margin-by-supplier/user and mass price-change features: no vendor-specific citations found (only general guides).

## Which features most improve pharmacy profitability and daily workflow (with any evidence)?

### Takeaway
Evidence is thin and mostly vendor/anecdotal. Strongest claims concern inventory: reducing surplus/dead stock, returning near-expiry or non-moving stock for credit, and redistributing between stores. Peer-reviewed evidence shows perpetual inventory improves accuracy; turnover gains were not statistically significant.

### Cited Findings
- Houston Methodist oncology pharmacy (AJHP 2024): EHR-integrated perpetual inventory raised accuracy by 6.02 percentage points; monthly turnover 2.03 → 2.23, not significant (P = 0.305); single site — [EBSCO abstract](https://www.ebsco.com/articles/health-and-medicine/2904c077-47e8-5b89-9693-9d476a84fbc2/implementation-and-evaluation-of-an-ehr-integrated-perpetual-inventory-system-in-a-large-tertiary-hospital-oncology-pharmacy)
- Greek public hospital pharmacy: new inventory framework improved inventory-cost KPIs by up to 25%; dead stock and average stock value fell, turnover rose — [Supply Chain Dive](https://supplychaindive.com/news/Greece-hospital-optimize-inventory-management-pharmacy/576380)
- Nebraska independent owner with perpetual inventory program cut inventory costs 19% (~$20,000) — [Drug Topics](https://drugtopics.com/view/perpetual-inventory-management-creates-cash-flow-advantage-for-pharmacy-owners)
- Vendor case (Datarithm): single-store pharmacy cut surplus by 76% in six months (vendor-reported) — [Constant Contact PDF](https://files.constantcontact.com/8dea0e28001/48eac6dc-acb9-41a1-87e9-ae68d10f54f4.pdf?rdr=true)
- Practice advice: run a report of items not dispensed in ~120 days and return them to wholesaler; check wholesaler return/dating terms — [PBA Health](https://www.pbahealth.com/elements/5-ways-to-profitably-manage-your-pharmacys-inventory/)
- UK vendor: analysis of historical dispensing producing weekly suggestions for returns to wholesaler; uncollected-prescription alerts help return stock before expiry — [Cegedim blog](https://cegedimrx.co.uk/blog/why-is-inventory-management-important-in-pharmacies)
- ABC analysis by annual spend (Pareto), often combined with VED criticality — [ComputerTalk](https://www.computertalk.com/challenges-and-solutions-for-pharmacy-supply-chain-management/)
- Titan PMR user: owed-items queue ~150 → <20 after stock control update (anecdote) — [Trustpilot](https://uk.trustpilot.com/review/titanpmr.com?page=2)
- ESL linked to inventory makes slow movers / soon-expiring products easy to find — [PZ](https://www.pharmazeutische-zeitung.de/digitale-preisschilder-mehr-action-im-regal-153099/)

### Inferences
- Highest-ROI ideas for a small Bulgarian retail pharmacy: (1) min/max + order proposal from sales history, (2) dead-stock report (no sale in N days) with stock value, (3) near-expiry value report + "return to supplier" document, (4) owed/reserved-items list, (5) cheaper-supplier hint once supplier prices are stored. Workflow: cash-drawer close with declared vs. expected.

### Gaps
- No independent evidence on loyalty programmes, ESL or interaction checks improving profitability.

## Which features are local-only (no government integration needed)?

### Takeaway
Almost all inventory, purchasing, pricing, CRM, reporting, multi-store, user/shift and usability features are local. Government-dependent items are e-prescription, reimbursement (NFZ/NHIF/PBS), FMD/serialization (national verification hub), and fiscal reporting.

### Cited Findings
- Fred NXT Dispense runs locally; only external services (PBS Online, eRx) need internet — [Fred newsletter Dec 2020](https://webhelp.fred.com.au/freddispense/Resources/PDF/paul-update-newsletter-dec-2020.pdf)
- KS-AOW government-tied features: e-Recepta, NFZ refund reports, vaccination settlement (KS-PPS), KSeF e-invoices — [OIA Kraków material](https://oia.krakow.pl/file/31412); [Kamsoft KSeF Q&A](https://api.kamsoft.pl/app-params/promos/000000/20250923_Q&A_Szkolenie_KSeF_Apteki.pdf)
- FMD needs registration with national organisation (CZ NSOL; DE securPharm) — [Mediox FMD guide](https://www.apatykaservis.cz/wp-content/uploads/2019/01/fmd-registrace-konfigurace_mediox.pdf); [IXOS securPharm](https://www.pharmatechnik.de/fileadmin/user_upload/Pharmatechnik/Downloads/IXOS/IXOS_securPharm_2019.1.pdf)
- Wholesaler EDI (KS-EWD) is commercial, not governmental — [Farmacol KS-EWD](https://www.farmacol.com.pl/gfx/farmacol/userfiles/phypa/grafika/2020/reklamacje/instrukcja_ks-ewd.pdf)

### Inferences
- Local-only candidates for the Bulgarian demo: reorder proposals, dead stock / expiry value, returns to supplier, owed items/reservations, price labels and mass price changes, customer accounts (deferred payment, family accounts), loyalty, shift close/Z-style cash report (non-fiscal), role permissions, stock transfers, ABC report, accounting export, configurable quick keys. Wholesaler EDI requires agreements with Bulgarian wholesalers (Phoenix, Sting, Sopharma) but no state integration. Interaction checks need a licensed drug database (commercial dependency).

### Gaps
- Bulgarian-specific wholesaler EDI formats not researched here.
