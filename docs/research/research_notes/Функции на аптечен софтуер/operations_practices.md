# Operational and business-management practices in independent retail pharmacies (BG/EU) that software should support

Note on method: web search worked, but fetching full pages was blocked for several hosts in this session (open-exam-prep.com, msh.org, pbahealth.com could not be resolved). Most cited findings come from search-result summaries, not full-text reads. Facts marked "Inference" are standard inventory/retail practice or design conclusions from the cited material, not sourced statements. The legacy program's feature list (local file `docs/legacy/apteka-expert-2-analysis.md`) is cited as a source for what a Bulgarian pharmacy program already offers.

## Which practices bring the most value for a small pharmacy (evidence, typical gains)?

### Takeaway
The best evidence of value is for inventory control: ordering from sales history with a reorder point, keeping stock turning about 10–12 times a year, finding dead and slow stock, and getting short-dated goods back to the wholesaler (about 70–80 % of cost recovered when pulled about 6 months before expiry). Supplier payables, mass price updates, deferred customer payment and shift and cash handling are expected basics in Bulgaria, since the legacy program already has most of them. Loyalty and promotions have weak independent evidence, and for medicines they are limited by price regulation.

### Cited Findings
- Benchmarks for inventory turns: the average independent pharmacy turns its inventory about 11 times a year. A target of about 15 turns has been proposed, and fewer than 12 turns "often" means excess inventory — [PBA Health, via search](https://www.pbahealth.com/?p=29327); [Drug Topics, perpetual inventory](https://drugtopics.com/view/perpetual-inventory-management-creates-cash-flow-advantage-for-pharmacy-owners).
- NCPA figures: the front end (OTC/parapharmacy) turns about 4 times a year and the whole pharmacy about 11.75 — [Pharmacy Times](https://www.pharmacytimes.com/view/front-end-mix-allows-pharmacies-to-control-costs-boost-bottom-line). A Canadian source gives 12 turns for the dispensary and 2–4 for the front store — [Canadian Healthcare Network](https://www.canadianhealthcarenetwork.ca/node/74239).
- Anecdote on dead stock: one pharmacist found an extra $8,000 of dead stock "just sitting on the shelves". I found no percentage benchmark for dead stock — [Drug Topics](https://www.drugtopics.com/view/finance-strategies-for-independent-pharmacies-facing-shrinking-margins).
- Returning short-dated goods: many manufacturers and wholesalers credit only short-dated products, and the rule of thumb is to pull them 6 months before expiry — [PBA Health](https://www.pbahealth.com/elements/how-to-get-the-most-out-of-your-returns/). Eligible returns may be worth 70–80 % of original cost, with wide variation — [AmerisourceBergen](https://www.amerisourcebergen.com/insights/pharmacies/recouping-your-losses-on-unsaleable-pharmaceuticals). One manufacturer example gives 75 % credit for returns with no more than 6 months left — [American Regent policy 2019](https://careers.americanregent.com/media/2487/americanregent-return-policy_wd-final-2019.pdf). Credit is set by contract or policy, not by law — [CASRAI guide](https://casrai.org/guides/pharmaceutical-returns).
- ABC/VED in pharmacy inventory: the classic hospital-pharmacy study (421 items) recommends ABC and VED as routine practice to optimise spending and remove stock-outs — [PGIMER study, PMC3021698](https://pmc.ncbi.nlm.nih.gov/articles/PMC3021698). In a 2024 study, class I (ABC-VED matrix) was 20.62 % of items but 73.82 % of annual expenditure — [PMC, rural western India](https://pmc.ncbi.nlm.nih.gov/articles/PMC11415163). These studies are from hospitals and LMICs, not EU community pharmacies.
- Loyalty evidence is weak. In the 1998 "Well Worth It" tests across 24 stores, only half the chains reported higher gross-margin percentages — [Supermarket News](https://www.supermarketnews.com/grocery-operations/drug-chains-find-rewards-plan-boosts-non-rx-sales). The Walgreens Balance Rewards study shows only an association with flu vaccination — [Walgreens abstract 2016](https://www.walgreens.com/assets/healthcare-solutions/pdf/abstracts/ah_brhc_fluvac_2016_FINAL.pdf). Most other sources are vendor blogs — [Antavo](https://antavo.com/blog/pharmacy-loyalty-programs/).
- Russian/CIS pharmacy software sells loyalty and bonus cards and supplier ordering as standard features, for example Saby (orders to supplier, cumulative cards, bonuses, offline mode) — [Saby](https://saby.ru/articles/retail/programma_dlya_apteki_2024). 1C:Retail Pharmacy covers purchasing, retail, warehouse, cashier, marketing and analytics — [1C](https://solutions.1c.ru/upload/reestr/002/djbceqoaelvsb8ylabvgrncxzs6prcfh/Apteka-Fun-Voz.pdf).
- The Bulgarian legacy program "Аптека Експерт 2" already offers: заявка за доставка (also online, with a fulfilment check), откази, плащания към доставчици, промяна на продажни цени, разходи; плащания от клиенти, кредитно известие, рекламация; консигнация; оборотна ведомост, статистика на продажбите и доставките, реализация за период, задължения към доставчици, дневник на покупки, свръхналичности, откази (лост продажби); бърза инвентаризация; supplier code mapping for electronic deliveries; ordering by supplier delivery schedule (`Routes.xml`: days, time windows, payment days) — local file `docs/legacy/apteka-expert-2-analysis.md`.

### Inferences
- A suggested priority for a small Bulgarian pharmacy, by value per effort. The first three are basics that every BG pharmacy program has:
  1. Supplier payables with due dates and payments.
  2. A suggested order from sales history (min/max or ROP), plus a missing-products list (дефектура/откази).
  3. Near-expiry alerts and returns to the supplier.
  4. Dead-stock and slow-mover reports, plus оборотна ведомост and margin by group or supplier.
  5. Shift handling (opening float, cash in/out, cash count).
  6. Deferred payment for customers (продажба на отложено плащане), and invoices or credit notes to companies.
  7. Mass price updates and shelf labels.
  8. Loyalty and promotions, last. In Bulgaria they can only target non-Rx and parapharmacy goods.
- Each turn above the current figure frees working capital. Example: €60,000 of inventory at cost and €600,000 COGS a year gives 10 turns (36.5 days of stock). Reaching 12 turns cuts inventory to €50,000 and frees €10,000 of cash.
- Lost-sale recording (откази) is cheap to build. It feeds both reordering (true demand = sales + refused requests) and the decision to list a product.

### Gaps
- No peer-reviewed EU or Bulgarian data on gains from automatic reordering in community pharmacies. No Bulgarian benchmark for turns or dead stock (the BG Pharmaceutical Union and BAPA materials were not found online).
- No evidence on gains from customer credit, price labels or mass repricing.

## What are the standard formulas and parameters (with numeric examples)?

### Takeaway
Use a periodic-review min/max (order-up-to) model driven by average daily sales over the last N days. Min = reorder point = daily sales × (lead time + review period) + safety stock. Max = min + daily sales × order cycle, or a days-of-cover target. Safety stock = Z × σ(daily demand) × √L, with Z = 1.65 for a 95 % service level. Classify items with ABC (value) and XYZ (variability) and set the service level and review frequency per class.

### Cited Findings
- Reorder point = average daily usage × lead time in days + safety stock. Pharmacy example: 10 units/day, 3-day wholesaler lead time, 15 units safety stock, so ROP = 45. More variability or less tolerance for stock-outs means more safety stock — [open-exam-prep PTCB guide, via search](https://open-exam-prep.com/study-guides/ptcb-supply-chain/inventory-methods/abc-analysis-min-max-par-reorder); [Shopify](https://www.shopify.com/blog/safety-stock-vs-reorder-point).
- Simple ("max-min") safety stock = max daily sales × max lead time − avg daily sales × avg lead time — [Shopify](https://www.shopify.com/blog/safety-stock-vs-reorder-point).
- Statistical safety stock = Z × σd × √L. Z = 1.65 for 95 %, 1.28 for 90 %, 2.33 for 99 %. Example: 20/day, σd = 5, L = 9 gives SS ≈ 25 and ROP = 20 × 9 + 25 = 205. When lead time also varies: SS = Z × √(L·σd² + d²·σL²) — [Amazon Business](https://business.amazon.com/en/blog/safety-stock); [Wikipedia, Safety stock](https://www.Wikipedia.org/wiki/Buffer_stock); [NCSU SCM](https://scm.ncsu.edu/scm-articles/article/safety-stock). Wikipedia warns the formula overestimates when σ/mean is high (skewed demand), which is typical of slow pharmacy items.
- Health-supply practice (MSH MDS-3): safety stock can be set simply as lead time × average monthly consumption. Max stock = min + consumption over the review period. Consumption should be adjusted for stock-out days — [MSH MDS-3 ch. 23](https://msh.org/wp-content/uploads/2013/04/mds3-ch23-inventorymgmt-mar2012.pdf) (the PDF could not be fetched; the formula comes from the search summary).
- ABC/VED combined matrix: Category I = AV + AE + AD + BV + CV, II = BE + CE + BD, III = CD. ABC by annual consumption value (A ≈ top 70 % of value, B next 20 %, C last 10 %) — [PMC3021698](https://pmc.ncbi.nlm.nih.gov/articles/PMC3021698); [PMC11415163](https://pmc.ncbi.nlm.nih.gov/articles/PMC11415163).
- One Ethiopian study uses "XYZ" for the value of stock on hand (not variability), combined with FSN (fast, slow, non-moving) to decide what to discard — [AAU thesis](https://etd.aau.edu.et/bitstreams/750b5412-e17f-47d2-bc40-8d6b1b37749b/download). Note the conflicting definitions: in European retail and supply-chain usage, XYZ means demand variability (coefficient of variation).
- Inventory turnover = COGS / average inventory at cost. Days on hand = 365 / turns. The figure is most useful compared over time or with peers — [open-exam-prep KPI guide, via search](https://open-exam-prep.com/study-guides/ptcb-supply-chain/inventory-kpis/inventory-turnover-days-on-hand).
- Bulgarian price regulation: a retailer may not sell above the approved, ceiling (пределна) or registered price. No retail markup applies to medicines reimbursed 100 %. The sale price must be marked on the pack — [Наредба за регулиране и регистриране на цените, МЗ](https://www.mh.government.bg/upload/4447/naredba-regulirane-registrirane-tsenite-na-lekarstvata.pdf). Historic (2004) regressive retail markups were 28 / 25 / 20 % by price band. Current values were not confirmed — [ПМС 257/2004](https://www.mh.government.bg/upload/3973/pms-257_21-09-2004_tseni-lekarstva-na-drebno.pdf). The National Council on Prices sets the regulated prices — [Капитал 2022](https://www.capital.bg/politika_i_ikonomika/zdraveopazvane/2022/10/19/4404908_poskupvat_li_ili_poevtiniavat_lekarstvata/).
- Fiscal and cash handling (Н-18): the opening amount and every change in cash outside sales must be registered in the fiscal device as "служебно въведени/изведени" — [НАП становище via kik-info](https://kik-info.com/stanovishta-na-nap/394c0d2316c92caa41684651f37694a7). A proposed amendment would have SUPTO software produce a "Дневен паричен отчет по оператори" with the Z report. Its status was not confirmed — [МФ справка Н-18](https://www.minfin.bg/upload/38512/Spravka%20N-18.pdf).
- Invoices and credit notes: the mandatory content is in чл. 114 ЗДДС, and a credit note must contain the чл. 114 details plus those of чл. 115 — [НАП, Фиш VII.3](https://kik-info.com/danatsi/naruchnik-zdds/v/2025/FISH-VII.3.php); [Фиш V.10](https://kik-info.com/danatsi/naruchnik-zdds/v/2025/FISH-V.10.php).
- Substitution in Bulgaria: a trade-name prescription forbids generic substitution. A checkbox lets the prescriber allow it for non-narcotics. Pharmacists have been asking for a general substitution right, which suggests they do not have one — [Biotechnol. & Biotechnol. Eq. 2016](https://informahealthcare.com/doi/full/10.1080/13102818.2016.1208061); [BNR interview](https://bnr.bg/en/post/101373443/medicines-shortage-can-be-compensated-for-by-generic-medicines-prof-asena-serbezova?page_1_3=7). Doctors prescribe by trade name. Pharmacies may offer a generic when the medicine is not reimbursed — [OECD BG contribution](https://one.oecd.org/document/DAF/COMP/WD(2014)43/en/pdf).
- Recalls and alerts: BDA publishes guidelines for managing alerts in verification and decommissioning (BgMVS/FMD) for wholesalers and pharmacies (v2.0, 2021) — [BDA](https://www.bda.bg/images/stories/documents/verifikacia/Managing%20Alerts%20Guidelines%20WH%20and%20Pharmacies_v2.0.pdf). EU regulators issue batch-level recalls and suspensions at pharmacy level, for example a heparin batch suspension in Poland in 2025 — [Medexpress](https://www.medexpress.pl/en/drugs-medical-technology/gif-series-anticoagulant-drug-withdrawn-from-turn) and [BASG Austria](https://www.basg.gv.at/en/market-surveillance/official-announcements/detail/atofab-10-mg-hartkapseln).

### Inferences (implementation recipes; standard practice, not individually sourced)
**1. Suggested order (заявка), periodic review min/max**
- Daily demand d = units sold (base unit) over the last N days / N. Use N = 30–90, and add refused requests (откази). Count only days the item was in stock: d = sales / days_in_stock. This is the MSH "adjust for stock-out days" rule.
- Seasonality: d_season = average daily sales over the same window last year × (trend = last 90 days this year / same 90 days last year). Or blend the two: d = 0.5·d_last30 + 0.5·d_sameMonthLastYear. Typical seasonal groups are cold/flu (Oct–Mar), allergy (Apr–Jun) and sun care.
- L = lead time in days. Bulgarian wholesalers (Phoenix, Sting, Sopharma Trading) deliver once or twice a day, so L ≈ 0.5–1. R = review period (days between orders, e.g. 1).
- Min (ROP) = ceil(d × (L + R) + SS). Max (order-up-to, S) = ceil(d × (L + R + C) + SS), where C = target days of cover, e.g. 7–14.
- Suggested qty = max(0, S − (on hand + on order − reserved for customer orders)), rounded up to the pack or supplier pack multiple. If the product has manual MinStock/MaxStock (as in this demo's Product), use those instead.
- Example: 28-day sales = 56 boxes, so d = 2/day. L = 1, R = 1. σd = 1.2 and Z = 1.65 give SS = 1.65 × 1.2 × √2 ≈ 2.8, so 3. Min = ceil(2 × 2 + 3) = 7, Max = ceil(2 × (2 + 7) + 3) = 21. With 5 on hand and 0 on order, order 16.
- For slow items (d < 0.1/day), use a fixed "keep 1–2 packs" rule or order only on demand. The normal formula is not valid for intermittent demand.

**2. Supplier selection and price comparison**
- Keep a per-supplier price list (supplier product code, net price, discount %, availability). Choose the supplier with the lowest landed net price = price × (1 − discount) among those that have stock and fit the delivery schedule. Split the order by supplier. Show the last purchase price, last supplier and the price difference in %.

**3. Supplier payables**
- Due date = invoice date + payment days (this demo already stores both). Payable = Σ posted deliveries ± corrections − payments. A payment allocation table links each payment to deliveries, oldest first by default.
- Reports: aged payables (not due, 1–30, 31–60, over 60 days overdue) and "due this week". Partial payments are allowed. Like everything else, payments are reversed, never deleted.

**4. Mass price updates**
- Scope: a group, a supplier, a manufacturer, or "on new delivery price".
- Actions: set a new markup %, or change by ±x %. New price = purchase × (1 + markup) × (1 + VAT), rounded to €0.01 or to "psychological" endings such as .49 or .99.
- Always clamp to the regulated ceiling price for Rx and NHIF medicines. The update must be previewable (old/new/difference), audited and reversible.
- Revaluing stock already on the shelf at batch level means the shelf label must be reprinted.

**5. Shelf/price labels**
- A label holds name, unit, price, price per base unit (unit price per Directive 98/6/EC where applicable), barcode (EAN-13 or Code 128) and date. Print automatically for products whose price changed since the last print.

**6. Promotions**
- Types: percent off, fixed price, buy X get Y (cheapest free), bundle price, and time-limited by date and hours.
- Scope: product, group or manufacturer. Exclude Rx and regulated-price medicines.
- Evaluate with a precedence rule: best for the customer, or a defined priority, and never stack with a manual discount unless that is allowed. Record the promotion id on the sale line.

**7. Loyalty**
- Points: e.g. 1 point per €1 spent on eligible lines. 100 points = €1 voucher, so 1 % back. Points expire after 12 months.
- Tiered discount: e.g. 3 % after €200 a year, 5 % after €500.
- Points are an append-only ledger (earn, redeem, expire, reversal) like StockMovement. A sale reversal reverses its points.

**8. Deferred payment (customer credit)**
- The sale is completed with PaymentMethod = OnAccount, which needs a customer. Check the credit limit.
- Receivable = Σ on-account sales − returns − customer payments. Customer payments are a document and are allocated to sales.
- Reports: the customer statement and overdue receivables.

**9. Invoice from a sale, and credit notes**
- Create an invoice from one or more completed sales for a company: EIK, VAT no., МОЛ (manager), address. Use its own numbering sequence (10 digits under ЗДДС) and the чл. 114 details.
- A sale return or reversal of an invoiced sale creates a credit note that references the invoice.

**10. Cash shift**
- Open the shift with an opening float, which is a "служебно въведени" fiscal operation.
- Cash in/out are documents with a reason.
- Expected cash = float + cash sales − cash refunds + cash in − cash out.
- At close, count by denomination. Difference = counted − expected; store it and require a reason above a threshold (e.g. €1).
- The X report is a read-only interim report; the Z report closes the fiscal day. Both come from the fiscal printer, which is out of demo scope. The software keeps its own shift report per operator.

**11. Permissions**
- Use a permission matrix (role → set of permissions such as Sale.Reverse, Price.Edit, Delivery.Post, Payables.Pay, Discount.Above10, User.Manage, Report.Margin) instead of hard-coded roles. Allow a per-user override and a manager "approve with PIN" for single actions.

**12. Dead stock and slow movers**
- Dead = on hand > 0 and no sale for 90 days (configurable to 180). Slow = days of cover = on hand / d > 180, or fewer than X sales in 90 days.
- Value = on-hand qty × purchase price. Overstock (свръхналичности) = on hand > Max.
- Actions: return to supplier, transfer to another pharmacy, promote (non-Rx) or write off.

**13. ABC/XYZ**
- ABC: sort products by 12-month sales value (or gross margin) descending. Cumulative share ≤ 80 % → A, ≤ 95 % → B, the rest C.
- XYZ: coefficient of variation of weekly or monthly sales, CV = σ/mean. CV ≤ 0.25 → X, ≤ 0.5 → Y, otherwise Z. Pharmacy demand is very intermittent, so many items will be Z.
- Policy example: AX gets a 98 % service level and daily review; CZ gets order-on-demand.

**14. Оборотна ведомост (turnover sheet)**
- For a period [from, to], per product or batch, at cost and at sale value: opening qty and value + receipts (deliveries, corrections+) − issues (sales, write-offs, corrections−, returns to supplier) = closing qty and value.
- Opening is computed as Σ movements before "from". It can be built entirely from StockMovement joined with batch prices.

**15. Margin by group or supplier**
- Gross margin = revenue without VAT − cost of goods sold (batch purchase price × base qty from SaleLineBatch). Margin % = margin / revenue without VAT.
- Example: revenue €1,200 incl. 9 % VAT gives €1,100.92 net. COGS €850 gives a margin of €250.92, or 22.8 %.
- Note that markup % (on cost) is not the same as margin % (on price): 25 % markup = 20 % margin.

**16. Returns to supplier**
- A document against a supplier, with lines on specific batches. It creates negative movements (a new kind, e.g. SupplierReturn) and expects a supplier credit note number.
- It reduces the payable, at the original purchase price or the agreed credit %.

**17. Transfers between pharmacies**
- A transfer-out document creates a negative movement at the source. A transfer-in creates a new batch with the same BatchNo, expiry and cost at the destination, plus a positive movement.
- Keep an "in transit" status until the destination confirms receipt.

**18. Expiry management**
- Alerts at 90, 180 and 365 days. Exclude expired batches from sale (this demo already does). Pull returnable goods about 6 months before expiry.
- FEFO is the default for picking. Expired batches can only be written off or returned to the supplier.

**19. Recall / batch blocking**
- A Batch block flag with reason, source (BDA notice or supplier) and date. A blocked batch is excluded from FEFO and from sale, like an expired one.
- The report shows the quantity on hand and which customers bought it (via SaleLineBatch → Sale.CustomerId), so they can be contacted.

**20. Temperature log**
- Readings per storage location (fridge 2–8 °C, room up to 25 °C), twice daily or from a data logger. Flag out-of-range readings with a corrective-action note. The log is append-only.

**21. Customer orders and дефектура**
- A customer order line holds product (or free text), qty, customer, phone, deposit and status (New → Ordered → Arrived → Notified → Collected/Cancelled). It adds to the suggested order.
- A refused request (отказ) records product and qty with no customer and feeds the demand calculation.

**22. Substitution suggestions**
- List products with the same ActiveSubstance/ATC (5th level), strength and form, sorted by price, with stock on hand.
- For Rx items, show only as information when the prescription allows substitution. This follows the Bulgarian rules cited above.

**23. Dosage and patient notes**
- A free-text note on the sale line or customer card (e.g. "1 tab. 2× daily"), printed on a small label. Only fictitious data in the demo (CLAUDE.md rule 8).

**24. Barcode/QR labels**
- Code 128 or EAN-13 for internal codes. Products without a manufacturer barcode get an internal EAN-13 in the 20–29 "in-store" prefix range with a check digit.

### Gaps
- Current Bulgarian regressive retail markup percentages for Rx and reimbursed medicines were not confirmed (only the 2004 values, 28/25/20 %). Check the current Наредба on mh.government.bg.
- No source checked whether the Bulgarian ban on advertising Rx medicines (ЗЛПХМ, advertising chapter) covers in-pharmacy promotions and loyalty points on Rx/reimbursed medicines. Needs legal confirmation.
- Exact temperature-log requirements for Bulgarian pharmacies (Наредба 28/2008, GDP) were not found in the search results.
- Typical lead times and ordering windows for Bulgarian wholesalers were not confirmed from public sources. The legacy `Routes.xml` suggests per-supplier day and time windows.
- No source confirmed whether the deadline for issuing a credit note is 5 days.

## What are common pitfalls?

### Takeaway
The biggest pitfalls are distorted demand (stock-outs, one-off bulk sales, seasonality), normal-distribution formulas applied to slow intermittent items, confusing markup with margin, repricing that breaks regulated ceiling prices, cash and receivables that are not tied to append-only documents, and denormalised totals that drift over time.

### Cited Findings
- Demand variability: a high σ/mean skews demand, and the standard safety stock formula then overestimates — [Wikipedia, Safety stock](https://www.Wikipedia.org/wiki/Buffer_stock). Use the combined lead-time and demand formula only with reliable data for both — [search summary of SPS Commerce](https://www.spscommerce.com/community/articles/how-to-calculate-safety-stock-formulas-and-methods-that-fit-your-data).
- Consumption must be adjusted for days out of stock, or demand is understated — [MSH MDS-3](https://msh.org/wp-content/uploads/2013/04/mds3-ch23-inventorymgmt-mar2012.pdf) (from the search summary).
- Return-credit policies vary and can change without notice, so per-supplier terms must be configurable — [IPC returns policy](https://order.ipcrx.com/docs/ipc-returns-policy.pdf); [AmerisourceBergen](https://www.amerisourcebergen.com/insights/pharmacies/recouping-your-losses-on-unsaleable-pharmaceuticals).
- Prices may not exceed the regulated ceiling, and no retail markup is allowed on 100 %-reimbursed medicines — [МЗ Наредба](https://www.mh.government.bg/upload/4447/naredba-regulirane-registrirane-tsenite-na-lekarstvata.pdf).
- Every cash change outside sales must be registered as служебно въведени/изведени on the fiscal device — [НАП via kik-info](https://kik-info.com/stanovishta-na-nap/394c0d2316c92caa41684651f37694a7).
- The legacy program kept stock, prices and payable totals denormalised and needed recalculation scripts, because the totals drifted — local file `docs/legacy/apteka-expert-2-analysis.md`.
- FMD/BgMVS produces a high rate of false alerts, so blocking logic needs a manual review step — [EU Council doc 2024](https://data.consilium.europa.eu/doc/document/ST-12096-2024-INIT/en/pdf).
- The definition of "XYZ" is inconsistent across the literature: inventory value in [AAU thesis](https://etd.aau.edu.et/bitstreams/750b5412-e17f-47d2-bc40-8d6b1b37749b/download) against demand variability in common supply-chain practice.

### Inferences
- Exclude from demand history: sales to institutions or one-off bulk sales (flag them or cap outliers, e.g. at 3σ), returns and reversals, and days with zero stock. Add refused requests.
- Do not auto-order Z/C items or items with an active customer order without a human check. Always show the suggested order for editing; never send it automatically in an MVP.
- Markup and margin: label both clearly in the UI. Example: 25 % markup = 20 % margin.
- Mass repricing: apply VAT after the markup, round once at the end, and keep ceiling prices for Rx/NHIF. Repricing must not change the prices of historic batches unless the user explicitly chooses "revalue stock".
- Payables and receivables: derive balances from documents (deliveries, corrections, returns, payments), as stock is derived from movements. Do not keep running totals that can drift. Allocate payments explicitly.
- Shift: one open shift per cash register and user. A sale is not allowed without an open shift (when shifts are on). The count difference is stored and never "fixed" by editing.
- Promotions and loyalty: define stacking rules up front. Reverse points and promotion effects on returns proportionally. Exclude Rx/regulated items.
- Transfers and returns create new movement kinds. Keep the original batch identity (BatchNo, expiry) for traceability and recalls.
- Expiry: alerts are only useful when tied to an action (a return list or a write-off). Too many alerts lead to alert fatigue. Set the thresholds per group.
- Permissions: avoid an explosion of roles. Use a permission matrix plus per-action manager approval, and keep the whole matrix in the audit log.

### Gaps
- No Bulgarian sources were found on common errors in pharmacy software (e.g. NHIF inspection findings or BDA audit findings on stock or temperature records).
- No quantitative data on how often suggested orders are overridden, or how accurate they are, in community pharmacies.
