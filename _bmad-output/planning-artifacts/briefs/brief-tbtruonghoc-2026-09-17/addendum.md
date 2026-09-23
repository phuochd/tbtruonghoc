---
title: "Addendum: Ngọc Anh Multi-Site Rebuild"
created: 2026-09-17
updated: 2026-09-17
---

# Addendum

## Ràng buộc quy trình: Google Stitch

Claude không có quyền truy cập trực tiếp vào Google Stitch. Ở giai đoạn UX/Architecture, Claude sẽ soạn các prompt văn bản (dựa trên yêu cầu thương hiệu, nội dung sản phẩm, cấu trúc trang) để Phước copy/paste thủ công vào Google Stitch, xem kết quả, rồi phản hồi lại để tinh chỉnh prompt tiếp. Đây là quy trình lặp lại thủ công, không tự động hóa được — cần tính vào timeline của giai đoạn thiết kế UI.

## Ghi chú domain (đã cập nhật — xem override trong .memlog.md)

**Kế hoạch cũ (không còn áp dụng):** chuyển toàn bộ site từ domain hiện tại sang trongdoitam.net.

**Kế hoạch hiện tại:** domain hiện tại giữ lại cho Site A (tbtruonghoc, chỉ thiết bị trường học). Nội dung trống trên site cũ chuyển sang domain mới Site B (trongdoitam.net) — đây là redirect 301 **khác domain**, giữ được ít tín hiệu SEO hơn same-domain nhưng vẫn nên làm. Nội dung "quần áo nghi thức"/"cờ" — danh mục duy nhất hiện có traffic tìm kiếm tự nhiên thật — ở lại Site A, chỉ đổi URL nội bộ (redirect cùng domain, rủi ro thấp hơn). Chi tiết mapping URL cụ thể thuộc phạm vi Architecture.

## Rủi ro backlink chéo giữa các site cùng chủ sở hữu

Site A, Site B, và sau này Site C đều cùng một chủ sở hữu — thông tin này có thể tra được qua WHOIS/hosting dù không công bố công khai. Việc đặt link qua lại dày đặc, đồng nhất trên mọi trang giữa các domain cùng chủ là một pattern Google coi là thao túng (gần giống PBN — private blog network), có thể bị giảm giá trị link hoặc nặng hơn là bị action thủ công. Vì vậy chỉ nên cross-link theo kiểu biên tập/ngữ cảnh (nhắc tới tự nhiên trong nội dung), không đặt thành block link cố định lặp lại ở mọi trang. Chi tiết đầy đủ hơn xem addendum.md của PRD tại `_bmad-output/planning-artifacts/prds/prd-tbtruonghoc-2026-09-17/addendum.md`.
