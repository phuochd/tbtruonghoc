---
title: "Addendum: tbtruonghoc.com PRD"
created: 2026-09-17
updated: 2026-09-17
---

# Addendum

## Legacy site audit (reference for content migration)

Audited live at https://tbtruonghoc.com during PRD discovery. Carried forward for whoever executes content migration/Architecture, not restated in the PRD body.

**Current main navigation:** Trang chủ, Giới thiệu, Nội thất mầm non, Sản phẩm trống, Sản phẩm Dù, Liên hệ.

**Sub-categories found:**
- Nội thất mầm non: Bàn/ghế mầm non; Phản nằm, giường lưới; Giá/kệ/đồ chơi gỗ; Tủ đựng tư trang; Tủ đồ chơi gỗ; Thiết bị nhà bếp mầm non.
- Sản phẩm trống: Trống trường, Trống lân, Trống đội.
- Sản phẩm Dù: Dù tròn, Dù 2 trụ, Dù dây cáp.
- Present on-site but not in main nav, found via sidebar/"Top sản phẩm nổi bật": Thiết bị mầm non ngoài trời, Thiết bị-nội thất trường học, Thiết bị-đồ dùng dạy học, Phòng thí nghiệm Lý-Hóa-Sinh, Máy chiếu màn chiếu, Bảng tương tác, Âm thanh trường học, Màn hình Led hiển thị, Thiết bị văn phòng, Bàn thí nghiệm.

**Contact info on legacy site:** Phone 0917.004.648, 24/7 support 0973.955.430, email thietbitruonghoc.ngocanh@gmail.com.

**Traced source of the "quần áo nghi thức" organic-search niche (PRD §4.1):** two sidebar/"Top sản phẩm nổi bật" pages, not in main nav, both indexed and apparently ranking on their own:
- https://tbtruonghoc.com/bo-dong-phuc-nghi-thuc-doi/ — "BỘ ĐỒNG PHỤC NGHI THỨC ĐỘI" (1 áo, 1 quần, 1 mũ cano...; SKU NAQA01; breadcrumb Trang chủ > Top sản phẩm nổi bật).
- https://tbtruonghoc.com/cac-loai-co/ — "CỜ TỔ QUỐC, CỜ VẪY" / "CÁC LOẠI CỜ" (cờ dây, cờ chuối, cờ tổ quốc, cờ đoàn, cờ đội, cờ vẫy, cờ nhất tuần, cờ nheo; SKU TNA07_CACLOAICO; breadcrumb Trang chủ > Top sản phẩm nổi bật).

Hypothesis (unconfirmed, worth testing with real Search Console data once available): these rank because they match the specific long-tail query pattern around school flag-raising/ceremony gear ("chào cờ nghi thức đội"), a low-competition niche distinct from the broader, more competitive category terms (dù che nắng, trống trường học, etc.) the rest of the catalog is chasing.

Both pages show weak/missing meta descriptions and short, unoptimized title tags — i.e., they rank despite thin SEO, not because of strong SEO. This is a signal that intent-matching content, not technical polish alone, is doing the work here; worth preserving in the rewritten copy.

## Google Stitch workflow constraint

Claude has no direct access to Google Stitch. During UX/Architecture work, Claude will draft text prompts (grounded in brand, product content, and page structure requirements from this PRD) for Phước to manually paste into Google Stitch. Phước reviews Stitch's output and reports back for prompt refinement. This is a manual, iterative loop that cannot be automated — factor it into the UI-design phase timeline.

## Domain/content split note (superseded plan, current plan)

**Superseded:** the brief's original plan was a single site on the current domain, later cut over wholesale to trongdoitam.net. That plan is dropped.

**Current plan:** the current domain stays as Site A (tbtruonghoc, rebuilt, school-equipment scope only). All trống content currently on the legacy site moves to the new Site B domain (trongdoitam.net) instead of staying put. This means the redirect problem is now a **cross-domain split**, not a single-domain migration:

- Legacy trống pages (currently on tbtruonghoc.com) need 301 redirects pointing to their new trongdoitam.net equivalents — a cross-domain redirect, which preserves less link equity than a same-domain 301 but is still worth doing.
- Legacy "quần áo nghi thức" and "cờ" pages (the one category with real existing organic traffic — see audit below) stay on the same domain (Site A) but likely move to new URLs as part of the rebuild — a same-domain redirect, lower risk, should preserve most of the existing signal.
- Everything else (nội thất mầm non, etc.) redirects within Site A only.

Full old-URL → new-URL mapping is Architecture's job, not this PRD's; flagged here so the split isn't lost between documents.

## Cross-site backlink risk (detail)

Site A and Site B (and eventually Site C) share an owner, and that's discoverable via WHOIS/hosting/registrant info even without it being publicly announced. Google's link-scheme guidance treats heavy, reciprocal, sitewide linking between commonly-owned domains as a manipulative pattern (private-blog-network-adjacent), regardless of intent — it can lead to the links being devalued or, in more aggressive cases, to a manual action against the sites involved.

The PRD's FR-5 asks for editorial, contextual links only (a mention in running content, not a persistent footer/sidebar block repeated on every page). If the business wants heavier cross-promotion later, the safer patterns are: a shared "về chúng tôi/thương hiệu" mention with a single link, customer-facing case studies that naturally reference both product lines, or routing that traffic through Site C (trongngocanh.com) once it exists, rather than direct A↔B link farms.
