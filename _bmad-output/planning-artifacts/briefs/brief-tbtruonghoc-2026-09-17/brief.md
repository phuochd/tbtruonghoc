---
title: "Product Brief: Ngọc Anh Multi-Site Rebuild (tbtruonghoc + trongdoitam.net + trongngocanh.com, để sau)"
status: final
created: 2026-09-17
updated: 2026-09-17
---

# Product Brief: Ngọc Anh Multi-Site Rebuild (tbtruonghoc + trongdoitam.net)

## Tóm tắt điều hành

Thiết Bị Trường Học Ngọc Anh đang viết lại toàn bộ hiện diện web (nền .NET thuần hiện tại) trên **Piranha CMS**, với giao diện mới được thiết kế qua **Google Stitch**. Mục đích là giải quyết một vấn đề cụ thể: gần như toàn bộ doanh số hiện nay đến từ gọi điện chủ động và khách quen giới thiệu, vì website gần như vô hình trên Google. Ngoại lệ duy nhất là một danh mục nhỏ ("quần áo nghi thức"), nơi từ khóa ít cạnh tranh đã chứng minh rằng khách hàng thực sự tìm và đặt hàng qua tìm kiếm tự nhiên.

Sau khi đào sâu, phạm vi tách thành **3 site riêng biệt** thay vì một site rồi đổi thương hiệu như dự tính ban đầu:

- **Site A — tbtruonghoc** (giữ domain hiện tại): chỉ bán thiết bị trường học — dù che nắng, nội thất mầm non, quần áo nghi thức, thiết bị âm thanh/máy chiếu, và các danh mục khác đã có trên site cũ.
- **Site B — trongdoitam.net** (domain mới): thương hiệu đồ gỗ thủ công Đọi Tam, đặt theo tên làng nghề trống Đọi Tam nơi bố vợ chủ doanh nghiệp là nghệ nhân và Chủ tịch Hiệp hội làng nghề, trực tiếp kiểm soát chất lượng sản phẩm — gồm toàn bộ các loại trống, cộng thêm hai dòng sản phẩm mới từ cùng xưởng gỗ: thùng rượu gỗ trang trí (mốc riêng: phải sẵn sàng trước Tết Âm lịch 2027, ~giữa tháng 2/2027, để kịp chạy quảng cáo trả phí) và bồn tắm gỗ.
- **Site C — trongngocanh.com** (đã có domain sẵn): site thương hiệu/công ty chung, xác nhận để giai đoạn sau, không thuộc phạm vi giai đoạn 1.

Ba site cross-link nhau theo kiểu biên tập/ngữ cảnh, không phải link hàng loạt toàn site — tránh rủi ro SEO khi Google phát hiện các domain cùng chủ sở hữu.

Mục tiêu đo được trước mắt: các từ khóa sản phẩm ưu tiên của từng site lên trang 2 trở lên (lý tưởng trang 1) trong vòng 3 tháng sau khi site đó ra mắt. Song song đó, cả Site A và Site B đều cần hạ tầng đo lường riêng (Analytics, Search Console) và landing page riêng, sẵn sàng cho quảng cáo trả phí dịp Tết trên Site B.

*(Chi tiết đầy đủ về kiến trúc 3 site, danh sách danh mục từng site, và các câu hỏi mở còn lại: xem PRD tại `_bmad-output/planning-artifacts/prds/prd-tbtruonghoc-2026-09-17/prd.md`.)*

## Vấn đề

Thiết Bị Trường Học Ngọc Anh hiện gần như vô hình trên Google. Ngoại trừ mục quần áo nghi thức — nơi từ khóa ít cạnh tranh nên khách tìm thấy và đặt hàng trực tiếp qua tìm kiếm tự nhiên — mọi đơn hàng khác đều đến từ việc chủ động gọi điện hoặc khách quen giới thiệu. Với các nhóm sản phẩm còn lại (dù che nắng sân trường, trống trường học, trống lễ hội, nội thất mầm non...), người có nhu cầu tìm kiếm trên Google gần như không bao giờ tìm ra website, dù công ty có sản phẩm phù hợp và giá cạnh tranh.

Việc này khiến toàn bộ doanh nghiệp phụ thuộc vào mạng lưới quan hệ và công sức gọi điện thủ công của chủ doanh nghiệp, thay vì có một kênh tìm kiếm tự nhiên hoạt động song song.

Vấn đề càng khó gỡ vì hệ thống .NET hiện tại khiến việc chỉnh SEO từng bài/từng trang (tiêu đề, mô tả, đường dẫn) gần như không thể thực hiện được, dù có ý muốn tối ưu dần từng sản phẩm.

## Giải pháp

Xây dựng lại trên Piranha CMS — nền tảng có sẵn field SEO (tiêu đề, mô tả, đường dẫn) cho từng trang/bài viết ngay trong lõi hệ thống, nên việc chọn Piranha giải quyết đúng điểm nghẽn của hệ thống .NET cũ mà không cần build thêm gì. Giao diện mới được thiết kế qua Google Stitch rồi hiện thực trên nền Piranha, hướng tới nhanh hơn, thân thiện di động hơn, đáng tin cậy hơn — để khách sẵn sàng đặt hàng thay vì chỉ gọi điện. Cả hai site A và B dùng chung nền tảng và quy trình thiết kế này.

Mỗi nhóm sản phẩm có trang riêng, tối ưu theo bộ từ khóa riêng, thay vì gộp chung — nhân rộng mô hình đã chứng minh hiệu quả ở mục quần áo nghi thức ra toàn bộ danh mục. Trên Site A (tbtruonghoc): dù che nắng sân trường, nội thất mầm non, quần áo nghi thức, thiết bị âm thanh/máy chiếu, và các danh mục thiết bị trường học khác đã có trên site cũ. Trên Site B (trongdoitam.net): toàn bộ các loại trống (trường học, lân, đội, lễ hội, chùa — gộp chung dưới một câu chuyện thương hiệu làng nghề, không tách theo công năng), thùng rượu gỗ trang trí, và bồn tắm gỗ. Cả hai site đều có mục blog/tin tức riêng để viết nội dung dài hơi phục vụ các tìm kiếm dạng "nên chọn loại nào", "cách chọn..." — kênh SEO thường mang lại thứ hạng bền hơn trang sản phẩm đơn thuần.

Vì khách hàng hiện quen gọi điện trực tiếp, cả hai site đều giữ Zalo chat, nút gọi nhanh, Google Maps nổi bật trên mọi trang — không ép khách đổi thói quen, chỉ mở thêm kênh tìm-đến-tự-nhiên song song.

Site A (tbtruonghoc) tiếp tục vận hành trên domain hiện tại sau khi rebuild — không còn kế hoạch đổi domain toàn bộ danh mục sang trongdoitam.net như định hướng ban đầu. Thay vào đó, trongdoitam.net ra đời như một site độc lập (Site B) ngay từ đầu, mang riêng câu chuyện làng nghề Đọi Tam cho nhóm sản phẩm gỗ thủ công. Một site thứ ba, trongngocanh.com (Site C), sẽ là nơi hợp nhất thương hiệu chung — xác nhận để giai đoạn sau.

## Điểm khác biệt

Gốc gác không phải marketing: bố vợ của chủ doanh nghiệp là nghệ nhân, đồng thời là Chủ tịch Hiệp hội làng nghề Đọi Tam — người trực tiếp đóng vai trò kỹ sư trưởng, kiểm soát chất lượng sản phẩm. Hiếm đối thủ bán thiết bị trường học nào có thể chứng minh gốc nghề thật và tay nghề nghệ nhân đứng sau sản phẩm, đặc biệt với dòng trống — đây là lợi thế uy tín khó sao chép, và cũng là chất liệu nội dung mạnh cho SEO (câu chuyện thật, có nhân vật, có chức danh xác thực).

Khác với nhiều nơi chỉ bán rồi thuê ngoài lắp đặt, công ty có đội thi công riêng cho các sản phẩm cần lắp đặt tại chỗ (dù che nắng sân trường...) — khách nhận trọn gói từ bán đến lắp, không phải tự tìm đơn vị thi công khác rồi lo trách nhiệm chồng chéo khi có sự cố.

Dòng thùng rượu gỗ trang trí mới (gỗ sồi, ngựa kéo, 1 ngựa, 2 ngựa) ra đời từ cùng xưởng gỗ/thợ mộc làm trống, nên thừa hưởng trực tiếp câu chuyện tay nghề này thay vì là một mặt hàng nhập/kinh doanh thêm không liên quan.

## Đối tượng phục vụ

**Trường học và trường mầm non** — nhóm khách rộng nhất, mua đa dạng trên cả hai site: nội thất mầm non, dù che nắng sân trường, quần áo nghi thức, thiết bị âm thanh/máy chiếu (Site A), và trống nghi thức (Site B). Đây là nhóm hiện đang gọi điện đặt hàng nhiều nhất, cần được chuyển một phần sang tìm-thấy-qua-Google.

**Khu vui chơi, công viên có sân rộng** — cần che nắng cho khu vực ngoài trời, tìm đến chủ yếu vì nhu cầu dù che nắng quy mô lớn, thường đi kèm yêu cầu thi công lắp đặt.

**Đình, chùa, nhà thờ dòng họ** — nhóm khách gắn trực tiếp với dòng sản phẩm trống lễ hội/nghi thức, nơi câu chuyện nghệ nhân Đọi Tam có sức nặng nhất: khách nhóm này quan tâm đến tính xác thực, tay nghề, và ý nghĩa văn hóa của sản phẩm hơn là chỉ giá cả.

**Khách mua quà/trang trí dịp Tết** — nhóm khách mới, tìm thùng rượu gỗ sồi trang trí (ngựa kéo, 1 ngựa, 2 ngựa) làm quà biếu hoặc trang trí nhà/văn phòng dịp Tết, cùng xưởng gỗ với dòng trống nên tận dụng được câu chuyện nghệ nhân Đọi Tam. Nhóm này sẵn sàng tiếp cận qua quảng cáo trả phí, không chỉ SEO tự nhiên như các nhóm khác.

Với nhóm cần lắp đặt tại chỗ (dù che nắng, Site A), phạm vi phục vụ giới hạn ở khu vực Miền Bắc đến Thanh Hóa; các sản phẩm có thể vận chuyển — nội thất, quần áo nghi thức (Site A), trống, thùng rượu gỗ, bồn tắm gỗ (Site B) — phục vụ toàn quốc.

## Tiêu chí thành công

**Tiêu chí chính (đo được):** Trong vòng 3 tháng kể từ khi **từng site ra mắt** (không còn một mốc "đổi domain" chung như trước — Site A ra mắt trên domain hiện tại, Site B ra mắt trên domain mới trongdoitam.net), các từ khóa ưu tiên sau đạt trang 2 trở lên, lý tưởng là trang 1 trên Google:
- Site A: dù che nắng, dù che nắng sân trường
- Site B: trống lễ hội, trống chùa, trống trường học

*(Lưu ý: Site B khởi đầu trên domain hoàn toàn mới, không có tín hiệu SEO tích lũy sẵn để kế thừa — mốc 3 tháng này khá tham vọng dù từ khóa ít cạnh tranh. `[NOTE FOR PM: mốc 3 tháng gốc trong bản brief đầu tiên được neo theo một sự kiện "đổi domain" duy nhất; sự kiện đó không còn tồn tại. Cách neo lại theo "ngày ra mắt của từng site" ở trên là suy luận hợp lý nhưng chưa được xác nhận lại trực tiếp với chủ doanh nghiệp — xem Open Question liên quan trong PRD.]`)*

**Tiêu chí phụ `[ASSUMPTION]`:** Các nhóm sản phẩm ngoài "quần áo nghi thức" bắt đầu phát sinh liên hệ/đơn hàng đến từ tìm kiếm Google, thay vì gần như 100% đến từ gọi điện chủ động/giới thiệu như hiện tại — đo bằng cách hỏi khách mới "biết đến qua đâu" trong 3 tháng đầu và qua Google Analytics/Search Console mới được tích hợp.

## Phạm vi

**Trong phạm vi giai đoạn 1 — cả Site A và Site B, xây song song:**
- ⚠️ **Mốc riêng, có thể chi phối thứ tự làm:** trang/nội dung cho dòng thùng rượu gỗ trên Site B (kể cả landing page quảng cáo) phải xong trước Tết Âm lịch 2027 (~giữa tháng 2/2027) để kịp chạy quảng cáo trả phí — có thể cần làm trước phần còn lại. `[NOTE FOR PM: xây 2 site cùng lúc, một site gần như từ số 0 về nội dung/ảnh cho 2/3 dòng sản phẩm, là khối lượng công việc lớn hơn đáng kể so với kế hoạch 1-site ban đầu — cần thống nhất thứ tự ưu tiên trước khi sang Architecture/Sprint Planning; chi tiết xem PRD.]`
- Viết lại trên Piranha CMS cho cả hai site, nội dung dựa trên trang cũ nhưng viết lại theo chuẩn SEO (riêng bồn tắm gỗ là dòng mới hoàn toàn, chưa có nội dung/ảnh/giá sẵn có).
- Giao diện mới thiết kế qua Google Stitch cho cả hai site. *(Quy trình cụ thể — ai làm gì, thao tác thế nào — xem addendum.md.)*
- **Site A (tbtruonghoc, domain hiện tại):** trang riêng cho từng nhóm thiết bị trường học — dù che nắng (sân trường), nội thất mầm non, quần áo nghi thức, thiết bị âm thanh/máy chiếu, và các danh mục khác đã có trên site cũ.
- **Site B (trongdoitam.net, domain mới):** trang riêng cho toàn bộ các loại trống (không tách theo công năng), thùng rượu gỗ trang trí (mới), và bồn tắm gỗ (mới).
- Mỗi site có mục blog/tin tức riêng cho nội dung SEO dài hơi.
- Zalo chat, nút gọi nhanh, Google Maps trên mọi trang, ở cả hai site.
- Tích hợp Google Analytics + Google Search Console riêng cho từng site để đo lường traffic/thứ hạng tự động.
- Chức năng tạo landing page riêng làm trang đích cho quảng cáo Google Ads (tách biệt khỏi trang sản phẩm SEO tự nhiên) trên Site B — phục vụ trước mắt cho chiến dịch thùng rượu dịp Tết, và các chiến dịch trả phí sau này.
- Landing page thùng rượu hiển thị giá cụ thể từng loại (khác các dòng sản phẩm khác chỉ ghi "liên hệ báo giá") — vẫn không có giỏ hàng/thanh toán online thật, nút "đặt mua" dẫn tới liên hệ để chốt đơn.
- Cross-link biên tập/ngữ cảnh giữa Site A và Site B (không phải link hàng loạt toàn site — rủi ro SEO khi các domain cùng chủ sở hữu, xem addendum.md).
- Kế hoạch redirect 301: nội dung trống hiện có trên domain cũ chuyển sang Site B (redirect khác domain); nội dung quần áo nghi thức/cờ ở lại Site A nhưng đổi URL theo cấu trúc mới (redirect cùng domain).
- Không có giỏ hàng/thanh toán online ở cả hai site — chỉ giới thiệu + liên hệ.

**Ngoài phạm vi giai đoạn 1 (để sau):**
- **Site C (trongngocanh.com)** — site thương hiệu/công ty chung, xác nhận để giai đoạn sau, chưa có mốc thời gian.
- Giỏ hàng, thanh toán online (cả hai site).
- Vận hành/quản lý ngân sách quảng cáo trả phí (brief này đảm bảo Site B có landing page + đo lường sẵn sàng, không bao gồm việc chạy campaign).
- Bảng cấu hình đặt trống theo tùy chọn (loại/kích thước/phụ kiện) trên Site B — có thì tốt nhưng không bắt buộc, có thể lùi lại nếu cạnh tranh thời gian với deadline Tết.

## Tầm nhìn

Trong 2-3 năm tới, trongdoitam.net trở thành điểm đến được biết đến rộng rãi cho cả trống và đồ gỗ thủ công (thùng rượu, bồn tắm, và có thể thêm sản phẩm gỗ khác), gắn liền với câu chuyện nghệ nhân làng nghề Đọi Tam thật — không phải một thương hiệu vay mượn tên gọi — trong khi tbtruonghoc tiếp tục là thương hiệu riêng, tập trung cho thiết bị trường học. trongngocanh.com, khi được xây dựng, sẽ là nơi hợp nhất câu chuyện của cả hai mảng kinh doanh dưới một thương hiệu công ty chung.

Các nhóm hàng có thể vận chuyển toàn quốc trên cả hai site (Site A: nội thất, quần áo nghi thức...; Site B: trống, thùng rượu gỗ, bồn tắm gỗ...) có giỏ hàng và thanh toán online, để khách ở xa đặt hàng trực tiếp mà không cần gọi điện. Danh mục sản phẩm gỗ thủ công trên Site B có thể mở rộng thêm ngoài trống, thùng rượu và bồn tắm — không phải mục tiêu bắt buộc, vì bản thân làng nghề không có quá nhiều loại sản phẩm, nhưng sẵn sàng thêm khi cơ hội đến.
