---
purpose: Google Stitch prompts for Site B (trongdoitam.net), assembled from DESIGN.md + EXPERIENCE.md
usage: Phước chạy thủ công tại https://stitch.withgoogle.com — dán từng prompt, lưu output (HTML/Figma/ảnh) vào thư mục này (ví dụ stitch-output/)
status: vòng 1 đã chạy, output đã review — xem "Vòng 2" cuối file cho danh sách fix/dựng mới/loại bỏ giao cho dev
created: 2026-09-18
---

# Cách dùng

1. Vào [stitch.withgoogle.com](https://stitch.withgoogle.com), tạo project mới cho "trongdoitam.net — Site B".
2. Dán **Prompt 0 (Hệ thống thiết kế)** trước tiên để Stitch nắm bối cảnh thương hiệu — đây không phải màn hình cụ thể, chỉ để "mồi" style cho các prompt sau trong cùng project.
3. Dán lần lượt từng prompt màn hình (1–7). Nếu Stitch hỗ trợ "giữ style từ ảnh trước", tham chiếu ảnh Prompt 0 hoặc màn hình đã tạo trước đó.
4. Lưu kết quả (ảnh/HTML/Figma link) vào `stitch-output/` trong thư mục này — mỗi file đặt tên theo số prompt, ví dụ `stitch-output/1-homepage.png`.
5. Nếu kết quả lệch khỏi DESIGN.md/EXPERIENCE.md (màu, bố cục, component), đó là Stitch cần chỉnh, không phải spine cần đổi — hai file spine luôn thắng khi có xung đột (theo quy ước của dự án).
6. Quay lại báo Claude khi có output — Claude sẽ đối chiếu với 2 spine, cập nhật EXPERIENCE.md nếu Stitch phát sinh hành vi mới cần ghi nhận.

Lưu ý riêng cho Site B (khác Site A): đây là site **mobile-first** (Site A là desktop-first) — mọi prompt dưới đây yêu cầu Stitch vẽ bản điện thoại làm bản chính, tablet/desktop là bản mở rộng theo sau, ngược thứ tự với Site A.

---

## Prompt 0 — Hệ thống thiết kế (chạy trước, không phải màn hình cụ thể)

```
Thiết lập một hệ thống thiết kế (design system) cho website thương hiệu thủ công mỹ nghệ làng nghề tại Việt Nam, tên "trongdoitam.net" (Trống Đọi Tam) — bán trống các loại, thùng rượu gỗ trang trí, bồn tắm gỗ. Đối tượng: thủ từ đình/chùa/nhà thờ dòng họ mua trống nghi lễ (thường lớn tuổi, ít quen web, ưu tiên gọi điện/Zalo), và khách mua thùng rượu gỗ làm quà Tết qua quảng cáo. Phong cách: "một chút cổ điển vì là làng nghề truyền thống, nhưng màu sắc đơn giản, không sặc sỡ" — ấm, mộc mạc, đáng tin, giống một xưởng gỗ lâu đời chứ không phải một cửa hàng trang trí kiểu cách.

Bảng màu (dùng chính xác các mã hex sau):
- Nền trang: #F5EFE6 (trắng ngà ấm, không phải trắng lạnh)
- Bề mặt/thẻ: #FFFFFF
- Bề mặt trũng (dùng cho khối tham chiếu/bảng cấu hình): #EFE7D8
- Chữ chính: #3A2E22 (đen-nâu ấm, không dùng đen tuyệt đối #000)
- Chữ phụ/mô tả: #7A6A56
- Màu chủ đạo (nâu gỗ, dùng cho MỌI nút CTA và hành động): #8B5A2B, chữ trên nút màu trắng #FFFFFF
- Màu phụ (hổ phách nhạt hơn, chỉ dùng cho nhãn/danh mục, KHÔNG dùng cho nút hành động): #A97142, chữ trên nền phụ trắng #FFFFFF
- Viền/đường phân cách: #DCCFB8
- Viền nhạt (dùng nội bộ trong form, giữa các trường): #EAE1CD
- Lỗi form (đỏ gạch ấm, không dùng đỏ lạnh kiểu Material): #A63B2E, chữ trắng

QUY TẮC MÀU: primary (#8B5A2B) chỉ dùng cho hành động (nút, link, trạng thái active) — nếu nó xuất hiện, nghĩa là mời bấm vào. secondary (#A97142) chỉ dùng cho nhãn/danh mục (label/eyebrow) — không bao giờ dùng cho nút. Không đảo ngược hai vai trò này. TUYỆT ĐỐI TRÁNH màu đỏ/cam bão hòa mang tính trang trí, gradient trang trí tùy tiện (chỉ có một gradient duy nhất được phép: secondary→primary, dùng làm khung ảnh placeholder cho sản phẩm, gợi vân gỗ), và chữ đen/trắng tuyệt đối.

Font chữ: một font sans nhân văn (humanist sans) DUY NHẤT cho toàn bộ giao diện — khuyến nghị "Be Vietnam Pro" (hỗ trợ tốt dấu tiếng Việt, ấm hơn Arial/Helvetica mặc định). KHÔNG dùng font serif cho tiêu đề — đây là quyết định có chủ đích (đã cân nhắc và từ chối hướng serif cổ điển để giữ cảm giác đáng tin, dễ đọc). Cỡ chữ ưu tiên DỄ ĐỌC hơn mức thông thường vì đối tượng lớn tuổi: tiêu đề lớn nhất (hero) 28px đậm; H1 trang 24px đậm; H2 trong trang 20px đậm; tiêu đề thẻ sản phẩm 17px đậm vừa; nội dung 16px thường, dòng cách rộng (line-height 1.6); mô tả nhỏ/nhãn form 14px; chú thích ảnh 13px; nhãn danh mục viết hoa 12px có giãn chữ nhẹ. HAI vai trò chữ đặc biệt PHẢI to hơn bình thường: số điện thoại (20px đậm) và giá (18px đậm) — đây là hai thông tin khách lớn tuổi không được phải nheo mắt đọc. Chữ trên nút (CTA) cũng đậm 16px, không thu nhỏ để tiết kiệm chỗ.

Bo góc: mềm mại, không bao giờ sắc cạnh và không bao giờ bo tròn hoàn toàn (pill) cho các thành phần cấu trúc — sắc cạnh quá công nghiệp, bo tròn hết lại giống app công nghệ, không hợp thương hiệu làng nghề. Nút/chip nhỏ: bo 6px. Thẻ/khối/form: bo 10px (bán kính chủ đạo). Khối nội dung lớn (ảnh câu chuyện nghệ nhân): bo 14px. Bo tròn hoàn toàn (pill) CHỈ dùng cho nhãn danh mục dạng chip và huy hiệu "Mới"/Tết — không dùng cho bất kỳ thành phần cấu trúc nào khác.

Độ sâu/bóng đổ: TỐI THIỂU — hầu hết thẻ/khối không có bóng đổ, chỉ phân tách bằng viền mỏng và chênh lệch nền/bề mặt. Bóng đổ CHỈ xuất hiện ở một chỗ duy nhất: thanh liên hệ dính đáy màn hình (sticky bar) có bóng đổ nhẹ hắt lên trên, màu nâu ấm (không phải đen) — đây là nơi duy nhất bóng đổ có vai trò thật sự.

Thiết kế MOBILE-FIRST (khác site chị em site A — site này ưu tiên điện thoại là bản chính). Lưới thẻ sản phẩm: 2 cột trên điện thoại, mở rộng 3–4 cột trên tablet/desktop, không đổi hình dạng thẻ. Lề hai bên mobile 16px, desktop 24px — không bao giờ tràn viền (full-bleed).

THÀNH PHẦN ĐẶC BIỆT bắt buộc xuất hiện ở MỌI trang (trừ landing page quảng cáo riêng): một thanh liên hệ dính đáy màn hình (sticky-contact-bar), luôn hiển thị không ẩn khi cuộn, chia 3 phần bằng nhau: "Gọi ngay" (chữ thường), "Chat Zalo" (nổi bật nhất — nền màu primary #8B5A2B, chữ trắng, vì đây là kênh ưa thích cho tư vấn cấu hình phức tạp), "Bản đồ" (chữ thường).

TUYỆT ĐỐI TRÁNH: font serif, bảng màu sặc sỡ/nhiều màu, hiệu ứng chuyển động/carousel tự động, ô vuông sắc cạnh, chữ nhỏ hơn mức đã nêu ở trên cho giá/số điện thoại/nút.
```

---

## Prompt 1 — Trang chủ (Homepage)

```
Dùng hệ thống thiết kế đã thiết lập (nâu gỗ #8B5A2B, nền #F5EFE6, font Be Vietnam Pro). Thiết kế trang chủ MOBILE-FIRST (~390px là bản chính) cho trongdoitam.net, phong cách "Danh Mục Nhanh" — dày dặn kiểu catalog, sản phẩm và giá phải thấy được trong một màn hình đầu tiên, không phải cuộn xuống mới thấy — nhưng vẫn ấm áp, không lạnh lùng kiểu công nghệ.

Bố cục từ trên xuống (bản điện thoại):
1. Thanh điều hướng (nav): nền trắng, viền dưới mỏng màu be. Chữ thương hiệu "Trống **Đọi Tam**" (chữ "Đọi Tam" tô màu phụ #A97142, phần còn lại màu chữ chính). Icon hamburger bên phải.
2. Khối "danh mục sản phẩm" — QUAN TRỌNG: đây là một khối LINH HOẠT do CMS quyết định số lượng, KHÔNG PHẢI cố định 3 ô — vẽ minh hoạ với 3 thẻ hiện có (Trống, Thùng rượu gỗ trang trí, Bồn tắm gỗ) NHƯNG bố cục lưới phải rõ ràng là "co giãn được", ví dụ thêm một viền đứt nét mờ ở ô thứ 4 với chữ nhạt "+ (sẽ thêm khi có sản phẩm mới)" để minh hoạ tính linh hoạt. Mỗi thẻ: nền trắng, viền be, bo góc 10px, nhãn nhỏ màu phụ, tên danh mục đậm.
3. Dải giới thiệu ngắn về nghệ nhân Phạm Trí Trong (1-2 dòng + link "Xem câu chuyện làng nghề →"), nền be nhạt hơn nền trang một chút.
4. Lưới sản phẩm nổi bật (2 cột trên điện thoại): 4-6 thẻ sản phẩm mẫu (product-card) — mỗi thẻ có khung ảnh placeholder gradient nâu-hổ phách (chưa có ảnh thật), nhãn danh mục nhỏ màu phụ, tên sản phẩm đậm, mô tả 1 dòng mờ, rồi dòng giá: một vài thẻ hiện "Liên hệ báo giá" (chữ mờ, KHÔNG phải số tiền — dùng cho trống/bồn tắm), nút CTA nền nâu chữ trắng bo góc nhỏ.
5. Thanh liên hệ dính đáy màn hình 3 phần (Gọi ngay / Chat Zalo nổi bật / Bản đồ) — LUÔN hiển thị, không biến mất khi cuộn.

Vẽ THÊM bản tablet (~820px, lưới 3 cột) và desktop (~1440px, lưới 4 cột) — cùng nội dung, chỉ đổi số cột lưới, giữ nguyên thanh liên hệ dính đáy ở mọi kích thước màn hình.

Toàn bộ chữ tiếng Việt, không dùng chữ mẫu (lorem ipsum). Giọng điệu ấm, điềm đạm, không hô hào ("Mua ngay kẻo lỡ" — KHÔNG dùng kiểu này).
```

---

## Prompt 2 — Trang danh mục con "Trống — Chùa" (đại diện cho 5 trang con loại trống)

```
Dùng hệ thống thiết kế đã thiết lập. Thiết kế trang danh mục "Trống Chùa" — MỘT trong 5 trang con của "Trống" (các trang còn lại: trống trường học, trống lân, trống đội, trống lễ hội — cùng khuôn mẫu bố cục, chỉ đổi tên/ảnh).

Bố cục (mobile-first):
1. Nav giống trang chủ, có breadcrumb nhỏ ngay dưới: "Trang chủ / Trống / Trống chùa".
2. Tiêu đề trang "Trống chùa" (H1 24px đậm) + đoạn giới thiệu ngắn 2-3 dòng về trống chùa trong bối cảnh câu chuyện làng nghề Đọi Tam.
3. Lưới sản phẩm (product-card, 2 cột mobile) hiển thị NHIỀU MẪU/LOẠI trống chùa cụ thể (ví dụ "Trống chùa loại 1", "Trống chùa loại 2", "Trống chùa loại 3") — mỗi thẻ là một MẪU CỤ THỂ có thể bấm vào để xem trang chi tiết riêng (không phải chỉ xem tổng quan danh mục). Mỗi thẻ: khung ảnh gradient nâu-hổ phách, tên mẫu, "Liên hệ báo giá" (mờ, không phải giá thật), nút CTA nâu.
4. (Tuỳ chọn, có thể vẽ ở trạng thái riêng) một khối "Bảng tham khảo cấu hình" nền be trũng (#EFE7D8), dạng bảng đơn giản liệt kê kích thước / loại 1-2-3 / bánh xe / sơn / vẽ mặt trống — kết thúc bằng cùng một nút CTA nâu "Liên hệ tư vấn cấu hình" — đây là bảng tham khảo, KHÔNG có tính năng tính giá tự động.
5. Thanh liên hệ dính đáy 3 phần, giống mọi trang.

Vẽ THÊM bản tablet/desktop — lưới sản phẩm mở rộng 3-4 cột, phần còn lại giữ nguyên bố cục.

Tiếng Việt toàn bộ, không chữ mẫu.
```

---

## Prompt 3 — Trang chi tiết một mẫu trống cụ thể ("Trống chùa loại 2")

```
Dùng hệ thống thiết kế đã thiết lập. Thiết kế trang chi tiết sản phẩm cho một mẫu trống cụ thể "Trống chùa loại 2" (được bấm vào từ lưới sản phẩm ở trang danh mục con "Trống chùa").

Bố cục (mobile-first, xếp dọc 1 cột trên điện thoại):
1. Nav + breadcrumb: "Trang chủ / Trống / Trống chùa / Trống chùa loại 2".
2. Bộ sưu tập ảnh (gallery) — ảnh chính lớn + vài ảnh thumbnail nhỏ bên dưới (dùng khung placeholder gradient nâu-hổ phách vì chưa có ảnh thật), bo góc 10px.
3. Tên mẫu "Trống chùa loại 2" (H1) + nhãn danh mục nhỏ màu phụ "Trống chùa".
4. Bảng thông số kỹ thuật của RIÊNG mẫu này — nền be trũng (#EFE7D8), các dòng: kích thước, loại (loại 2), bánh xe, sơn, vẽ mặt trống — chỉ hiển thị thông số của mẫu này, không phải bảng so sánh toàn bộ loại 1-2-3.
5. Dòng giá: "Liên hệ báo giá" (chữ mờ #7A6A56, KHÔNG phải số tiền — trống luôn là sản phẩm liên hệ báo giá).
6. Form liên hệ/yêu cầu báo giá nhúng ngay trong trang (không phải modal riêng) — các trường: Họ tên, Số điện thoại, Sản phẩm quan tâm (đã điền sẵn "Trống chùa loại 2", có nền khác biệt báo hiệu đã điền sẵn), Lời nhắn. Nút gửi nền nâu chữ trắng full-width.
7. Thanh liên hệ dính đáy 3 phần.

Vẽ THÊM bản desktop (~1440px): chuyển sang bố cục 2 cột (gallery trái, thông tin+form phải), giữ nguyên nội dung.

Tiếng Việt toàn bộ, không chữ mẫu.
```

---

## Prompt 4 — Trang câu chuyện nghệ nhân (Câu chuyện làng nghề Đọi Tam)

```
Dùng hệ thống thiết kế đã thiết lập. Thiết kế trang "Câu chuyện nghệ nhân" — trang xây dựng lòng tin, kể câu chuyện nghệ nhân Phạm Trí Trong, Chủ tịch Hiệp hội làng nghề Đọi Tam, trực tiếp kiểm định từng sản phẩm.

Bố cục (mobile-first):
1. Nav.
2. Khối ảnh/video lớn ở đầu trang, bo góc 14px (bán kính lớn nhất trong hệ thống, dành riêng cho khối nội dung lớn/chiêm nghiệm) — ảnh nghệ nhân Phạm Trí Trong tại xưởng (dùng khung placeholder vì chưa có ảnh thật), có dòng chú thích nhỏ mờ bên dưới kiểu "Nghệ nhân Phạm Trí Trong tại xưởng làng nghề Đọi Tam".
3. Tiêu đề "Nghệ nhân Phạm Trí Trong — Chủ tịch Hiệp hội làng nghề Đọi Tam" (H1).
4. Nội dung câu chuyện dạng văn bản dài vừa phải (2-3 đoạn placeholder ngắn bằng tiếng Việt, giọng điệu ấm, cụ thể, không hô hào) — ví dụ nhấn mạnh việc ông trực tiếp kiểm định từng sản phẩm, không phải marketing chung chung.
5. Một dải nhỏ liên kết ngược lại 3 dòng sản phẩm (Trống / Thùng rượu gỗ / Bồn tắm gỗ) dạng 3 thẻ nhỏ ngang hàng — vì trang này được liên kết TỪ cả 3 trang sản phẩm và cũng dẫn NGƯỢC LẠI.
6. Thanh liên hệ dính đáy.

QUAN TRỌNG: vẽ trang này ở trạng thái CHỈ CÓ ẢNH, không có video (vì video chưa chắc có sẵn) — không được để lại khoảng trống hay khung video vỡ/placeholder "video sắp có" nào, bố cục phải nhìn hoàn chỉnh dù chỉ có ảnh.

Vẽ THÊM bản tablet/desktop — khối ảnh/nội dung có thể chuyển sang bố cục rộng hơn (ảnh lớn hơn, văn bản max-width vừa đọc), không đổi nguyên tắc.

Tiếng Việt toàn bộ, không chữ mẫu.
```

---

## Prompt 5 — Landing page quảng cáo Tết "Thùng rượu gỗ" (mẫu riêng, KHÔNG dùng chung layout site)

```
Dùng bảng màu/font đã thiết lập (nâu gỗ #8B5A2B, nền #F5EFE6, font Be Vietnam Pro) NHƯNG thiết kế một TRANG RIÊNG BIỆT, không phải theo khuôn các trang site còn lại — đây là landing page đích cho quảng cáo trả phí dịp Tết, mục tiêu duy nhất là chuyển đổi, không phải điều hướng khám phá site.

Bố cục (mobile-first — đa số khách vào từ quảng cáo trên điện thoại):
1. KHÔNG có thanh nav đầy đủ, KHÔNG có menu — chỉ có logo nhỏ "Trống Đọi Tam" ở góc trên, không click được đi đâu (trang này không có lối "thoát" sang phần còn lại của site, theo thiết kế).
2. Tiêu đề lớn, đậm: "Thùng rượu gỗ trang trí — Quà Tết ý nghĩa từ làng nghề Đọi Tam" + ảnh sản phẩm lớn ngay đầu trang (khung placeholder gradient nâu-hổ phách).
3. Lưới 4 thẻ biến thể sản phẩm, MỖI THẺ CÓ GIÁ THẬT hiển thị rõ (số tiền cụ thể, cỡ chữ lớn đậm — đây là trang DUY NHẤT trên site B hiển thị giá thật cho mọi dòng): "Gỗ sồi — Ngựa kéo", "Gỗ sồi — 1 ngựa", "Gỗ sồi — 2 ngựa" (mỗi thẻ có nhãn biến thể dạng chip nhỏ + giá lớn đậm ngay dưới tên). Mỗi thẻ có nút "Đặt mua ngay" nền nâu chữ trắng, kích thước nút LỚN HƠN nút CTA thông thường ở các trang khác (đây là trang có một nhiệm vụ duy nhất: chuyển đổi).
4. Form liên hệ nhúng ngay bên dưới lưới sản phẩm (không phải modal, không chuyển trang khi bấm "Đặt mua ngay" — cuộn xuống form ngay trên trang) — Họ tên, Số điện thoại, Biến thể quan tâm, nút gửi full-width.
5. KHÔNG có thanh liên hệ dính đáy 3 phần như các trang khác — CHỈ có một nút/dải CTA đơn full-width dính đáy màn hình (không chia 3 phần) để trang này chỉ có một hành động duy nhất.

Vẽ THÊM bản desktop (~1440px): lưới 4 thẻ biến thể xếp ngang trong 1 hàng, form bên dưới rộng hơn.

Tiếng Việt toàn bộ, giọng điệu ấm nhưng rõ ràng về giá — không hô hào kiểu "Giảm giá sốc", giữ đúng tinh thần thương hiệu làng nghề.
```

---

## Prompt 6 — Blog: trang danh sách + trang chi tiết bài viết

```
Dùng hệ thống thiết kế đã thiết lập. Vẽ HAI màn hình cạnh nhau:

**(a) Trang danh sách Blog/Tin tức** — nav bình thường, tiêu đề "Blog / Tin tức", lưới bài viết (2 cột mobile) mỗi thẻ có ảnh đại diện (placeholder), tiêu đề bài viết đậm, ngày đăng nhỏ mờ, đoạn tóm tắt ngắn 1-2 dòng. Nội dung bài mẫu xoay quanh chủ đề: cách chọn trống nghi lễ, bảo quản đồ gỗ, quà tặng Tết từ làng nghề.

**(b) Trang chi tiết MỘT bài viết** (bấm vào từ thẻ ở trang danh sách) — nav + breadcrumb "Trang chủ / Blog / [Tên bài viết]". Tiêu đề bài lớn (H1), dòng ngày đăng nhỏ mờ ngay dưới tiêu đề. Nội dung bài dạng văn bản dài (vài đoạn placeholder tiếng Việt, có 1 tiêu đề phụ H2 giữa bài để minh hoạ cấu trúc). Cuối bài: một dải nhỏ "Bài viết liên quan" gồm 2-3 thẻ bài viết khác cùng kiểu thẻ như trang danh sách — nếu không có bài liên quan thì chỉ hiện link "← Quay lại Blog" đơn giản (vẽ minh hoạ dải "Bài viết liên quan" có nội dung là đủ, không cần vẽ riêng trạng thái rỗng).

Cả hai màn hình đều có thanh liên hệ dính đáy 3 phần.

Vẽ THÊM bản desktop cho cả hai (lưới bài viết rộng hơn 3-4 cột ở trang danh sách; trang chi tiết bài viết giới hạn độ rộng văn bản để dễ đọc, không kéo dài hết màn hình).

Tiếng Việt toàn bộ, không chữ mẫu.
```

---

## Prompt 7 — Form "Yêu cầu báo giá" (trạng thái focus, lỗi, thành công)

```
Dùng hệ thống thiết kế đã thiết lập. Vẽ BA trạng thái của cùng một form "Yêu cầu báo giá" (form này KHÔNG PHẢI modal/overlay — nó luôn nhúng ngay trong trang, ví dụ dưới cùng trang chi tiết sản phẩm) cạnh nhau để so sánh:

**(a) Trạng thái bình thường / đang focus một trường:** form với các trường Họ tên, Số điện thoại, Sản phẩm quan tâm (đã điền sẵn ví dụ "Trống chùa loại 2", nền khác biệt nhẹ báo hiệu đã điền sẵn từ trang sản phẩm, không cho sửa), Lời nhắn (không bắt buộc). Trường "Số điện thoại" đang ở trạng thái focus: viền chuyển từ be (#DCCFB8) sang nâu đậm (#8B5A2B), không có hiệu ứng gì khác ngoài đổi màu viền.

**(b) Trạng thái lỗi:** trường "Số điện thoại" để trống, bấm gửi — viền trường chuyển sang màu lỗi (#A63B2E), ngay dưới trường xuất hiện dòng chữ nhỏ màu lỗi "Vui lòng nhập số điện thoại." Nút gửi vẫn ở trạng thái bình thường (không bị vô hiệu hoá, không đổi màu).

**(c) Trạng thái gửi thành công:** toàn bộ form được THAY THẾ tại chỗ (không phải modal, không chuyển trang) bằng một khối xác nhận đơn giản: dấu tick hoặc icon nhỏ + dòng chữ "Đã nhận yêu cầu, chúng tôi sẽ liên hệ sớm." — nền be nhạt, không có nút nào khác trong khối này.

Tiếng Việt toàn bộ, không chữ mẫu. Vẽ ở kích thước mobile (~390px), đây là bối cảnh sử dụng chính.
```

---

# Vòng 2 — Rà soát bản export đầu tiên (TÙY CHỌN — không bắt buộc chạy lại Stitch)

**Quyết định của Phước:** giống Site A, các lỗi/bổ sung dưới đây để **dev tự sửa/dựng thẳng trong code** khi triển khai, thay vì render lại qua Stitch — tránh rủi ro Stitch sửa lan sang chỗ khác không mong muốn khi re-prompt.

Đã xem bản export đầu (`stitch-output/stitch_tr_ng_i_tam/`, 18 màn hình) và đối chiếu với DESIGN.md/EXPERIENCE.md. Phần lớn đúng (màu, chữ, bo góc, thanh liên hệ dính đáy, khối danh mục linh hoạt, quy tắc "liên hệ báo giá" cho trống, product-detail-page, craftsman-story-block, form 3 trạng thái, landing page tách biệt hoàn toàn).

**Fix (từ export vòng 1):**
1. **[Nghiêm trọng]** Trang chủ (cả 3 breakpoint) hiện giá thật bịa cho "Thùng gỗ sồi 50L" (3.200.000đ) và "Bồn tắm bầu dục gỗ pơ mu" (4.800.000đ) — sai quy tắc contact-for-quote (chỉ landing page quảng cáo Tết được hiện giá thật per FR-13); bồn tắm gỗ đặc biệt sai vì dòng này chưa có giá thật nào. Sửa: cả 2 thẻ đổi về "Liên hệ báo giá" (chữ mờ, giống các thẻ trống).
2. **[Nghiêm trọng]** 4 màn Blog (danh sách + chi tiết bài viết, mobile + desktop) thiếu hẳn thanh liên hệ dính đáy (sticky-contact-bar), và dùng nav khác toàn bộ phần còn lại site — nav ngang đầy đủ 5 mục + icon tài khoản, không thu gọn hamburger trên bản mobile. Sửa: áp dụng đúng `nav` + `sticky-contact-bar` như mọi trang khác (nav 56px + hamburger trên mobile, không có icon tài khoản — site không có đăng nhập).
3. Ảnh thẻ blog dùng ảnh thật thay vì khung placeholder gradient nâu-hổ phách như mọi thẻ khác trên site — đồng bộ lại cho nhất quán (ảnh thật sẽ thay placeholder khi có ảnh CMS thật, không phải bây giờ).
4. Form nhúng trên landing page thùng rượu thiếu trường "Lời nhắn" — bổ sung theo đúng field tối thiểu của `quote-request-form` (tên, SĐT, sản phẩm quan tâm, lời nhắn).
5. Badge "TẾT 2025" trên thẻ blog có vẻ ngả cam/đỏ hơi bão hòa so với các badge nâu khác cùng lưới — kiểm tra lại đúng mã `{colors.secondary}` #A97142, không dùng cam/đỏ bão hòa (quy tắc Do's and Don'ts).

**Dựng mới (đã chốt vào spine — xem DESIGN.md Components + EXPERIENCE.md Component Patterns, không cần ảnh mẫu Stitch):**
6. `trust-block` — khối trích dẫn/testimonial nghệ nhân Phạm Trí Trong, nhúng ngay trên trang danh mục con Trống và `product-detail-page` (Stitch tự thêm ở bản export đầu, đáng giữ).
7. Số hotline hiển thị trên `nav` ở tablet/desktop (Stitch tự thêm, đáng giữ — không cần trên mobile vì đã có ở sticky-contact-bar).
8. `sku-code` (mã sản phẩm, vd. "Mã: TC-L2-160") trên `product-detail-page`, và dòng disclaimer "Bảng tham khảo, không tính giá tự động" dưới `drum-config-reference` (cả hai Stitch tự thêm, đáng giữ).

**Loại bỏ (Stitch tự thêm ở bản export đầu, quyết định KHÔNG dùng):**
9. Icon tài khoản/đăng nhập trên nav Blog — site không có hệ thống tài khoản, dễ gây hiểu nhầm có chức năng không tồn tại.
10. Số "lượt thích" trên trang chi tiết bài blog — lệch giọng điệu điềm đạm/không phô trương đã chốt ở Voice and Tone; nếu sau này muốn dùng, cần quyết định rõ ràng với khách hàng trước, không mặc định bật.
