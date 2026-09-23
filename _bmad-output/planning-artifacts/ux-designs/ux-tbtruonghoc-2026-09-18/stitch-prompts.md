---
purpose: Google Stitch prompts for Site A (tbtruonghoc.com), assembled from DESIGN.md + EXPERIENCE.md
usage: Phước chạy thủ công tại https://stitch.withgoogle.com — dán từng prompt, lưu output (HTML/Figma/ảnh) vào thư mục này (ví dụ stitch-output/)
status: draft — chưa chạy qua Stitch, chưa có output nào
created: 2026-09-18
---

# Cách dùng

1. Vào [stitch.withgoogle.com](https://stitch.withgoogle.com), tạo project mới cho "tbtruonghoc.com — Site A".
2. Dán **Prompt 0 (Hệ thống thiết kế)** trước tiên để Stitch nắm bối cảnh thương hiệu — đây không phải màn hình cụ thể, chỉ để "mồi" style cho các prompt sau trong cùng project.
3. Dán lần lượt từng prompt màn hình (1–5). Nếu Stitch hỗ trợ "giữ style từ ảnh trước", tham chiếu ảnh Prompt 0 hoặc màn hình đã tạo trước đó.
4. Lưu kết quả (ảnh/HTML/Figma link) vào `stitch-output/` trong thư mục này — mỗi file đặt tên theo số prompt, ví dụ `stitch-output/1-homepage.png`.
5. Nếu kết quả lệch khỏi DESIGN.md/EXPERIENCE.md (màu, bố cục, component), đó là Stitch cần chỉnh, không phải spine cần đổi — hai file spine luôn thắng khi có xung đột (theo quy ước của dự án).
6. Quay lại báo Claude khi có output — Claude sẽ đối chiếu với 2 spine, cập nhật EXPERIENCE.md nếu Stitch phát sinh hành vi mới cần ghi nhận.

---

## Prompt 0 — Hệ thống thiết kế (chạy trước, không phải màn hình cụ thể)

```
Thiết lập một hệ thống thiết kế (design system) cho website B2B bán thiết bị trường học tại Việt Nam, tên thương hiệu "Ngọc Anh" (Thiết Bị Trường Học Ngọc Anh). Đối tượng: hiệu trưởng, chủ trường mầm non, nhân viên mua sắm của trường học/công viên — không phải người tiêu dùng cá nhân. Phong cách: chuyên nghiệp, đáng tin cậy, tươi sáng (không u ám, không quá nghiêm nghị) — giống một nhà cung cấp thiết bị lớn, có năng lực, không phải một cửa hàng bán lẻ giảm giá.

Bảng màu (dùng chính xác các mã hex sau):
- Màu chủ đạo (teal): #0E7A6C — dùng cho thanh điều hướng, banner hero, footer, icon danh mục
- Màu nhấn hành động (amber): #E3A63B, chữ trên nút màu #2B1B02 — dùng cho MỌI nút CTA chính
- Nền trang: #FCFEFD (gần trắng, hơi mát)
- Chữ chính: #123832 (đen-teal đậm, không dùng đen tuyệt đối #000)
- Viền/đường phân cách: #E3ECE9
- Nền khối thông tin phụ (price block, service-area, process-strip): #EFF8F6 với viền #BFE3DA
- Cảnh báo (không phải lỗi, màu hổ phách): nền #FFF7E8, viền #F0D7A0, chữ #6B4E10

Font chữ: Mulish (Google Font) cho toàn bộ giao diện — tiêu đề đậm (weight 800), chữ thường (weight 400). Không dùng font serif hay font thủ công/heritage.

Bo góc: vừa phải, không sắc cạnh, không bo tròn hoàn toàn — 6-12px tuỳ kích thước thành phần.

Độ sâu/bóng đổ: hạn chế tối đa, chỉ dùng cho phần tử nổi thật sự (dropdown menu, modal, chip liên hệ nổi) — mọi thẻ/khối nội dung khác phẳng, chỉ phân tách bằng viền và nền màu nhạt.

Nút chính (CTA): nền amber #E3A63B, chữ #2B1B02, bo góc nhỏ. Nút phụ: viền teal #0E7A6C, chữ teal, nền trong suốt. Không bao giờ đảo ngược cặp màu này.

Thiết kế RESPONSIVE — BA breakpoint: desktop (~1440px), tablet/iPad (~820px), điện thoại (~390px). Trên cả tablet và điện thoại: thanh nav thu gọn thành logo bên trái + icon hamburger (≡) bên phải, nhấn vào mở một menu trượt ra từ bên phải che toàn màn hình (nền teal đặc, chữ trắng), liệt kê Trang chủ · Giới thiệu · Sản phẩm (mở rộng dạng accordion xổ xuống đủ 11 tên nhóm khi nhấn) · Tin tức · Liên hệ; chip liên hệ (hotline/Zalo) vẫn hiển thị dạng icon nhỏ ngoài menu, không cần mở hamburger mới gọi được. Mọi lưới nhiều cột trên desktop chuyển thành 2 cột trên tablet, 1 cột trên điện thoại, giữ nguyên khoảng cách/màu sắc, chỉ đổi bố cục.

QUY TẮC RIÊNG cho hero/banner ảnh slide ở trang chủ: desktop và tablet/iPad đều giữ carousel ảnh slide đầy đủ (chỉ co nhỏ tỷ lệ trên tablet). Trên điện thoại (<768px), LUÔN bỏ hẳn ảnh slide, thay bằng nền màu teal đặc #0E7A6C (không ảnh, không carousel) — vì màn hình điện thoại nhỏ, ảnh slide nặng và dễ xung đột thao tác vuốt với việc cuộn trang.

TUYỆT ĐỐI TRÁNH: banner giảm giá màu đỏ kiểu "Giảm giá %", bất kỳ tông màu nâu gỗ/thủ công/heritage nào, font chữ serif.
```

---

## Prompt 1 — Trang chủ (Homepage)

```
Dùng hệ thống thiết kế đã thiết lập (teal #0E7A6C, amber #E3A63B, nền #FCFEFD, font Mulish). Thiết kế trang chủ desktop-first (rộng ~1440px) cho website B2B thiết bị trường học "Ngọc Anh", phong cách thương hiệu tự tin, thoáng đãng (không dày đặc).

Bố cục từ trên xuống:
1. Thanh điều hướng (nav): nền teal đặc #0E7A6C, chữ trắng. Logo/wordmark "NGỌC ANH" bên trái. Menu ngang: Trang chủ · Giới thiệu · Sản phẩm (có dấu mũi tên dropdown) · Tin tức · Liên hệ. Bên phải: một chip liên hệ nổi (bo tròn, nền trắng mờ trong suốt) hiển thị số hotline + icon Zalo — luôn hiển thị, không mất khi cuộn.
2. Hero lớn, chiếm gần trọn màn hình đầu: ảnh nền là ảnh thật một mái dù che sân trường học hoặc nội thất mầm non (nếu chưa có ảnh, dùng nền màu teal đặc thay thế) — QUAN TRỌNG: ảnh phải hiện RÕ, không bị lớp phủ màu che mờ toàn bộ. Chỉ phủ một lớp gradient RẤT NHẸ, gần như trong suốt ở phần trên và giữa ảnh, chỉ tối dần nhẹ ở khoảng 1/4 đáy ảnh (nơi đặt tiêu đề/nút) để chữ trắng đủ tương phản đọc được — phần lớn bức ảnh (trên và giữa) phải trong, nhìn thấy chi tiết ảnh thật rõ ràng, không phải một mảng màu teal phủ kín. Tiêu đề lớn (font Mulish đậm): "Thiết bị trường học chuyên nghiệp, đồng hành cùng hàng trăm ngôi trường". Dòng phụ màu trắng-mint nhạt. Hai nút: nút chính amber "Nhận báo giá", nút phụ viền trắng "Đăng ký khảo sát miễn phí". Nếu hero dùng ảnh, thêm chấm tròn nhỏ (dot indicator) phía dưới gợi ý đây là slide ảnh xoay vòng.
3. Phần "Nhóm sản phẩm nổi bật": chỉ hiển thị 6 thẻ danh mục lớn (không phải cả 11) dạng lưới 3 cột, mỗi thẻ có icon vuông nền teal, tên danh mục, mô tả ngắn 1 dòng. Các tên gợi ý: Dù che nắng sân trường, Nội thất mầm non, Bảng tương tác, Thiết bị âm thanh — máy chiếu, Phòng thí nghiệm Lý-Hóa-Sinh, Thiết bị mầm non ngoài trời. Cuối lưới có link "Xem tất cả 11 nhóm sản phẩm →" màu teal.
4. Dải thống kê tin cậy (trust band), nền màu xanh nhạt #EFF8F6: 4 con số lớn kiểu "500+ trường đã lắp đặt", "20 năm kinh nghiệm", "100% bảo hành chính hãng", "Hỗ trợ 24/7" — mỗi số kèm nhãn nhỏ bên dưới, không dùng khung pill.
5. Footer: thanh mỏng nền teal đặc, tên thương hiệu bên trái chữ trắng đậm, thông tin liên hệ (điện thoại/Zalo/email) bên phải chữ trắng nhạt.

Vẽ THÊM hai phiên bản responsive của toàn trang:
- **Tablet/iPad (~820px):** nav thu gọn logo + hamburger; hero VẪN giữ ảnh slide carousel, chỉ co nhỏ tỷ lệ theo chiều rộng; 6 thẻ danh mục xếp 2 cột; dải thống kê 2×2; footer gần giống desktop.
- **Điện thoại (~390px):** nav thu gọn logo + hamburger; hero BỎ HẲN ảnh, thay bằng khối nền teal đặc #0E7A6C (không carousel, không ảnh) — tiêu đề cỡ chữ nhỏ hơn nhưng vẫn đậm, 2 nút CTA xếp chồng full-width; 6 thẻ danh mục xếp 1 cột; dải thống kê xếp lưới 2×2; footer xếp chồng logo trên, liên hệ dưới.

Toàn bộ chữ trên trang bằng tiếng Việt, giọng điệu chuyên nghiệp — không dùng chữ mẫu (lorem ipsum).
```

---

## Prompt 2 — Trang "Danh mục sản phẩm" (tổng hợp toàn bộ 11 nhóm)

```
Dùng hệ thống thiết kế đã thiết lập (teal #0E7A6C, amber #E3A63B, nền #FCFEFD, font Mulish). Thiết kế trang "Danh mục sản phẩm" — trang liệt kê ĐẦY ĐỦ tất cả nhóm sản phẩm, khác với trang chủ chỉ hiện 6 nhóm nổi bật.

Bố cục: thanh nav giống trang chủ (nền teal, có mục "Sản phẩm" đang active). Tiêu đề trang "Danh mục sản phẩm" + một câu giới thiệu ngắn. Bên dưới là lưới 4 cột hiển thị đủ 11 thẻ danh mục, mỗi thẻ giống thẻ ở trang chủ (icon vuông nền teal, tên, mô tả ngắn): Dù che nắng sân trường, Nội thất mầm non, Quần áo nghi thức & cờ đội, Thiết bị âm thanh – máy chiếu, Bảng tương tác, Màn hình LED hiển thị, Thiết bị văn phòng, Phòng thí nghiệm Lý-Hóa-Sinh, Bàn thí nghiệm, Thiết bị – đồ dùng dạy học, Thiết bị mầm non ngoài trời.

Thêm MỘT thẻ thứ 12 ở cuối lưới có kiểu dáng khác biệt rõ (viền đứt nét, nền nhạt hơn, không có icon) với chữ mờ "+ Thêm nhóm sản phẩm mới (qua CMS)" — minh hoạ rằng lưới này có thể mở rộng thêm nhóm trong tương lai, không cố định 11.

Footer giống trang chủ. Tiếng Việt toàn bộ, không dùng chữ mẫu.

Vẽ THÊM hai phiên bản responsive: tablet/iPad (~820px) lưới 4 cột chuyển thành 2 cột; điện thoại (~390px) chuyển thành 1 cột xếp chồng (đủ 11 thẻ + thẻ placeholder thứ 12). Cả hai đều dùng nav thu gọn hamburger như đã mô tả.
```

---

## Prompt 3 — Trang chi tiết sản phẩm CẦN THI CÔNG (ví dụ: Dù che sân trường học)

```
Dùng hệ thống thiết kế đã thiết lập. Thiết kế trang chi tiết sản phẩm cho "Dù che sân trường học" — đây là loại sản phẩm CẦN KHẢO SÁT/LẮP ĐẶT (khác với sản phẩm ship thẳng ở prompt sau).

Bố cục 2 cột (gallery ảnh bên trái, thông tin bên phải):
- Gallery: ảnh sản phẩm/lắp đặt thật (dù che sân trường), có vài ảnh thumbnail nhỏ bên dưới ảnh chính.
- Tên sản phẩm "Dù che sân trường học" + mô tả ngắn.
- Bảng thông số kỹ thuật: kích thước, chất liệu khung, chất liệu bạt, màu sắc.
- Khối giá: vì sản phẩm này KHÔNG có giá cố định (phụ thuộc khảo sát thực tế), hiển thị chữ "Liên hệ để nhận báo giá" thay vì số tiền, trong khối nền xanh nhạt #EFF8F6.
- Khối "Khu vực phục vụ": tiêu đề nhỏ màu teal "Khu vực phục vụ: Miền Bắc – Thanh Hóa", văn bản mô tả ngắn, kèm ghi chú nhỏ màu hổ phách rằng khu vực ngoài phạm vi vẫn được tiếp nhận đăng ký.
- Dải quy trình 3 bước (process strip) nền xanh nhạt: Khảo sát → Hợp đồng → Thi công, mỗi bước có số thứ tự tròn nền teal.
- Hai nút CTA: nút chính amber "Đăng ký khảo sát miễn phí", nút phụ viền teal "Gọi ngay / Zalo tư vấn".

Nav và footer giống trang chủ, mục "Sản phẩm" trong nav đang mở dropdown liệt kê đơn giản 11 tên nhóm sản phẩm (danh sách một cột, không phải mega-menu nhiều cột), có link "Xem tất cả" ở cuối. Tiếng Việt toàn bộ.

Vẽ THÊM phiên bản mobile (~390px): bố cục 2 cột (gallery/thông tin) chuyển thành 1 cột xếp chồng — gallery ảnh trên cùng (vuốt ngang để xem thumbnail), rồi lần lượt: tên/mô tả, khối giá, khối khu vực phục vụ, dải quy trình 3 bước (có thể xếp dọc thay vì ngang), 2 nút CTA full-width xếp chồng. Nav thu gọn hamburger như đã mô tả.
```

---

## Prompt 4 — Trang chi tiết sản phẩm SHIP THẲNG (ví dụ: Giường lưới nhựa PE mầm non)

```
Dùng hệ thống thiết kế đã thiết lập. Thiết kế trang chi tiết sản phẩm cho "Giường lưới nhựa PE mầm non" — sản phẩm SHIP THẲNG TOÀN QUỐC, không cần khảo sát/lắp đặt (đơn giản hơn nhiều so với sản phẩm cần thi công).

Bố cục 2 cột giống mẫu trước (gallery trái, thông tin phải) NHƯNG:
- KHÔNG có khối "khu vực phục vụ" và KHÔNG có dải quy trình 3 bước — vì sản phẩm này không cần khảo sát/lắp đặt.
- Khối giá: sản phẩm này CÓ giá thật, hiển thị con số cụ thể lớn, nổi bật, ví dụ "185.000đ / chiếc", kèm ghi chú nhỏ "Giá tham khảo, liên hệ để nhận báo giá số lượng lớn".
- Thêm một dòng ghi chú giao hàng nhỏ dạng chip: "Giao hàng toàn quốc — 3–7 ngày làm việc tùy khu vực".
- Bảng thông số: kích thước, chất liệu, tải trọng, số lượng/thùng.
- Nút CTA chính amber: "Yêu cầu báo giá" (khác với nút "Đăng ký khảo sát miễn phí" ở sản phẩm cần thi công). Nút phụ viền teal: "Gọi ngay / Zalo tư vấn".

Bố cục nhìn tổng thể phải "gọn và đơn giản hơn" trang sản phẩm cần thi công — vì không có 2 khối bổ sung kia. Nav/footer giống các trang trước. Tiếng Việt toàn bộ.

Vẽ THÊM phiên bản mobile (~390px): cùng nguyên tắc xếp chồng 1 cột như trang sản phẩm cần thi công (gallery trên, thông tin dưới, giá nổi bật, chip giao hàng, nút CTA full-width). Nav thu gọn hamburger.
```

---

## Prompt 5 — Form "Đăng ký khảo sát miễn phí" (overlay/modal)

```
Dùng hệ thống thiết kế đã thiết lập. Thiết kế một modal/overlay form tên "Đăng ký khảo sát miễn phí", hiện lên giữa màn hình, nền phía sau bị làm mờ/tối (backdrop teal tối trong suốt), modal có bóng đổ mạnh nhất trên toàn site (đây là overlay thật sự, không phải khối nội dung phẳng).

Các trường trong form theo đúng thứ tự:
1. "Sản phẩm quan tâm" — trường này đã được điền sẵn và khoá (không cho sửa), có nền màu xanh nhạt khác biệt để báo hiệu "đã điền sẵn từ trang sản phẩm", ví dụ giá trị "Dù che sân trường học".
2. "Địa điểm / địa chỉ" (*bắt buộc, có dấu sao màu cam đất) — ô nhập text.
3. "Họ tên" (*bắt buộc) — ô nhập text.
4. "Số điện thoại" (*bắt buộc) — ô nhập text.
5. Nút gửi toàn chiều rộng, nền amber, chữ "Gửi đăng ký khảo sát".

Vẽ HAI phiên bản của form này cạnh nhau để so sánh trạng thái:
- Phiên bản (a) — bình thường: không có cảnh báo gì, các trường trống hoặc điền mẫu hợp lệ.
- Phiên bản (b) — cảnh báo ngoài khu vực phục vụ: ngay dưới trường "Địa điểm", xuất hiện một banner cảnh báo màu hổ phách (không phải màu đỏ/lỗi) với icon dấu chấm than, tiêu đề đậm và nội dung: "Địa điểm bạn nhập nằm ngoài khu vực phục vụ trực tiếp (Miền Bắc – Thanh Hóa). Chúng tôi vẫn ghi nhận và sẽ liên hệ để tư vấn phương án phù hợp." — viền trường "Địa điểm" cũng chuyển sang màu hổ phách nhạt. QUAN TRỌNG: nút gửi form VẪN Ở TRẠNG THÁI BẬT (không bị vô hiệu hoá) ở cả hai phiên bản — đây là cảnh báo, không phải lỗi chặn gửi.

Tiếng Việt toàn bộ, không dùng chữ mẫu.

Vẽ THÊM phiên bản mobile (~390px) của phiên bản (a): trên mobile modal này chiếm toàn màn hình (full-screen sheet trượt lên từ đáy) thay vì hộp thoại nhỏ giữa màn hình — vẫn giữ nguyên thứ tự trường, màu sắc, nút gửi full-width ở đáy.
```

---

# Vòng 2 — Sửa lỗi từ bản export đầu tiên (TÙY CHỌN — không bắt buộc chạy lại Stitch)

**Quyết định của Phước:** vì giao diện đã có sẵn (export vòng 1), các lỗi/bổ sung dưới đây để **dev tự sửa/dựng thẳng trong code** khi triển khai, thay vì render lại qua Stitch — tránh rủi ro Stitch sửa lan sang chỗ khác không mong muốn khi re-prompt. Các prompt dưới đây vẫn giữ lại (đã viết sẵn) phòng khi sau này muốn chạy lại Stitch cho mục đích khác, nhưng **không phải bước bắt buộc của UX phase này**.

Đã xem bản export đầu (`stitch-output/stitch_tbtruonghoc_site_a/`) và đối chiếu với DESIGN.md/EXPERIENCE.md. Phần lớn đúng (overlay ảnh hero, hero-fallback trên điện thoại, nav dropdown 11 nhóm, 2 layout PDP, state giá, form cảnh báo...). Danh sách việc cần dev xử lý khi lên code (tham khảo `stitch-output/` làm nền, không cần ảnh mẫu mới cho các mục 5):

**Fix (từ export vòng 1):**
1. Trang chủ desktop — bổ sung thanh nav ngang bị thiếu (Trang chủ · Giới thiệu · Sản phẩm · Tin tức · Liên hệ).
2. Trang danh mục tổng hợp bản tablet — gộp lại thành 1 thanh nav duy nhất (đang bị lặp 2 thanh).
3. Trang sản phẩm ship thẳng — sửa chữ nút CTA thành đúng "Yêu cầu báo giá" (đang bị thừa chữ).
4. Menu hamburger mở ra + accordion "Sản phẩm" 11 nhóm — dựng theo mô tả trong DESIGN.md/EXPERIENCE.md (chưa có ảnh mẫu, nhưng hành vi đã đặc tả rõ: trượt từ phải, nền teal, accordion xổ 11 tên nhóm).

**Dựng mới (chưa có trong export vòng 1, đã đặc tả trong DESIGN.md Components + EXPERIENCE.md Component Patterns):**
5. `breadcrumb`, `category-search` (tìm kiếm + filter chip), `photo-badge` ("Hình ảnh thi công thực tế"), badge chứng nhận trên `category-tile` — xem chi tiết token/vị trí trong 2 file spine, không cần ảnh mẫu Stitch.

Các prompt dán-sẵn cho Stitch (nếu sau này vẫn muốn dùng) giữ nguyên bên dưới:

## Sửa 1 — Trang chủ desktop thiếu thanh nav

```
Trang "Trang chủ" bản desktop (1440px) hiện đang THIẾU thanh menu ngang (chỉ có logo + hotline). Vẽ lại đúng thanh nav như các trang khác trong cùng project: nền teal đặc, logo bên trái, MENU NGANG đầy đủ "Trang chủ · Giới thiệu · Sản phẩm · Tin tức · Liên hệ" ở giữa/phải, chip liên hệ hotline+Zalo ở ngoài cùng bên phải. Giữ nguyên phần hero và các phần còn lại của trang.
```

## Sửa 2 — Trang danh mục tổng hợp bản tablet bị lặp nav

```
Trang "Danh mục sản phẩm" bản tablet (820px) hiện có HAI thanh điều hướng chồng lên nhau (một thanh kiểu desktop đầy đủ + một thanh teal riêng có hamburger). Vẽ lại CHỈ MỘT thanh nav duy nhất cho tablet: nền teal đặc, logo bên trái, icon hamburger (≡) bên phải mở menu trượt — giống hệt cách nav thu gọn ở các trang tablet khác trong project (ví dụ trang chủ bản tablet). Giữ nguyên lưới 2 cột danh mục bên dưới.
```

## Sửa 3 — Chữ nút CTA sản phẩm ship thẳng bị sai

```
Ở trang chi tiết sản phẩm "Giường lưới nhựa PE mầm non" (cả bản desktop và mobile), nút CTA chính hiện ghi "Yêu cầu báo giá số lượng lớn" / "Yêu cầu báo giá dự án" — sửa lại đúng thành "Yêu cầu báo giá" (ngắn gọn, không thêm chữ). Giữ nguyên toàn bộ phần còn lại của trang.
```

## Sửa 4 — Chưa có màn hình menu hamburger mở ra (chưa kiểm chứng được)

```
Vẽ một màn hình mới: trạng thái menu hamburger ĐANG MỞ trên mobile (~390px), lấy nền là trang chủ mobile. Menu trượt ra từ bên phải che toàn màn hình, nền teal đặc #0E7A6C, chữ trắng, liệt kê: Trang chủ · Giới thiệu · Sản phẩm (mục này đang MỞ RỘNG dạng accordion, xổ xuống đủ tên 11 nhóm sản phẩm ngay bên dưới, thụt lề nhẹ) · Tin tức · Liên hệ. Có nút đóng (×) ở góc trên.
```

## Sửa 5 (bổ sung tính năng mới, không bắt buộc redo toàn bộ) — 4 thành phần mới đã chốt thêm vào spine

Không cần vẽ lại toàn bộ, chỉ cần bổ sung nếu muốn cập nhật các màn hiện có:

```
Bổ sung các thành phần sau vào hệ thống thiết kế đã có, dùng đúng tông teal/amber/Mulish hiện tại:

1. Breadcrumb: một dòng chữ nhỏ ngay dưới thanh nav, TRÊN các trang danh mục/chi tiết sản phẩm/danh mục tổng hợp (KHÔNG có ở trang chủ) — dạng "Trang chủ / Sản phẩm / [Tên danh mục] / [Tên sản phẩm]", các bước trước là link màu teal, bước cuối (trang hiện tại) màu chữ thường, không phải link.

2. Ô tìm kiếm + chip lọc trên trang "Danh mục sản phẩm": ngay dưới tiêu đề trang, trên lưới danh mục — một ô tìm kiếm nhỏ và một hàng chip có thể bấm chọn (dạng viên thuốc, viền teal nhạt, khi active chuyển nền xanh nhạt #DFF6EE) nhóm các danh mục theo chủ đề, ví dụ "Mầm non & Vận động," "Thiết bị công nghệ."

3. Nhãn nhỏ "Hình ảnh thi công thực tế" đè lên góc ảnh gallery ở trang chi tiết sản phẩm CẦN THI CÔNG (dù che sân trường) — dạng viên thuốc nhỏ, nền trắng/nhạt hơi trong suốt để đọc được trên mọi ảnh, chữ nhỏ màu teal đậm.

4. Trên trang "Danh mục sản phẩm", thêm 1-2 badge nhỏ dạng viên thuốc bên dưới tên mỗi thẻ danh mục cho MỘT VÀI thẻ (không phải tất cả) để minh hoạ chứng nhận/tiêu chuẩn, ví dụ "Đạt chuẩn ASTM" trên thẻ "Thiết bị mầm non ngoài trời". Đây chỉ là ví dụ minh hoạ bố cục — khi triển khai thật, badge này CHỈ hiện khi có chứng nhận thật, không mặc định.
```
