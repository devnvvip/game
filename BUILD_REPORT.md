# BUILD REPORT - MỘT NGÀY CỦA IDOL STREAMER

## Thông Tin Build & CI/CD
- **Trạng thái Build**: **SUCCESS (THÀNH CÔNG)**
- **Lượt chạy GitHub Actions**: [https://github.com/devnvvip/game/actions/runs/41824757175](https://github.com/devnvvip/game/actions/runs/41824757175)
- **Tên Artifact IPA**: `game-ios-ipa` (Dung lượng: ~36.4 MB)
- **Mã Commit**: `00a97d07b796cde4c3e70720c3a42b0efe69303f`
- **Phiên bản Unity**: `6000.6.8f1` (URP)
- **Hệ thống bản quyền**: Unity 6 Personal License (Kích hoạt tự động qua Buildalon, không cần file `.alf`/`.ulf`)
- **Bundle ID**: `com.devnvvip.game`
- **Mục tiêu nền tảng**: iOS 15.0+ (ARM64, IL2CPP, Landscape)

---

## 1. Các Tính Năng Đã Hoàn Thành
Toàn bộ gameplay và đồ họa mô phỏng cuộc sống idol streamer được xây dựng hoàn chỉnh:
1. **Không gian căn hộ streamer hiện đại**:
   - Phòng ngủ master: Giường đôi nệm cao cấp, đồng hồ báo thức điện tử LED, tủ đồ có gương soi lớn, bàn đầu giường.
   - Studio livestream chuyên nghiệp: Bàn gaming công thái học, hệ thống 3 màn hình cong hiển thị dashboard/chat/audio mixer phát sáng emissive, dàn PC Gaming case kính RGB, micro thu âm chuyên nghiệp trên boom arm, đèn ring light studio trên chân tripod, ghế streamer da cao cấp, tường ốp thanh gỗ tiêu âm (acoustic wood slats), kệ trưng bày nút vàng YouTube và cúp thưởng.
   - Khu bếp & quầy bar: Đảo bếp ốp đá cẩm thạch trắng Carrara sang trọng, máy pha cà phê espresso, tủ lạnh thông minh chứa nước tăng lực và đồ ăn nhẹ.
   - Phòng tắm: Bồn rửa mặt lavabo, vòi nước chrome sáng bóng, gương lớn.
2. **Hệ thống điều khiển mượt mà đa nền tảng**:
   - PC Windows: Di chuyển WASD, chuột xoay góc nhìn, phím [E] tương tác, [Tab] mở smartphone, [Esc] tạm dừng, [Space] đứng dậy khi ngồi ghế.
   - iPhone / Mobile: Dynamic Touch Joystick di chuyển góc trái dưới, vuốt màn hình cảm ứng để xoay camera, nút cảm ứng to rõ `[TƯƠNG TÁC]` góc phải dưới, nút mở `[PHONE]` và nút `[||]` tạm dừng.
3. **Lịch trình 5 thời điểm trong ngày (Full Day Progression)**:
   - Buổi sáng: Tắt chuông báo thức -> Rửa mặt & đánh răng -> Chọn trang phục idol năng động -> Pha cà phê & ăn sáng -> Kiểm tra điện thoại đọc tin nhắn fan.
   - Buổi trưa: Dọn dẹp phòng stream -> Kiểm tra màn hình PC, camera & bật đèn ring light -> Lên kịch bản livestream trên PC -> Uống nước nạp năng lượng.
   - Buổi chiều: Quay video ngắn hậu trường -> Chụp ảnh selfie idol & đăng lên mạng xã hội IdolGram -> Kiểm tra tương tác fan -> Luyện thanh & chốt chủ đề live tối.
   - Buổi tối (Livestream cốt lõi): Ngồi vào ghế gaming -> Bấm Bắt đầu Livestream -> Giao lưu với bình luận người xem -> Chọn câu trả lời tương tác -> Xử lý tình huống bất ngờ (mic rè, mạng lag, góc cam lệch, troll spam, bạn thân raid 5,000 view) -> Nhận quà tặng và donation giả lập -> Chào tạm biệt khán giả.
   - Cuối ngày: Xem bảng tổng kết thành tích buổi live -> Tắt PC và đèn ring light -> Đọc tin nhắn fan chúc ngủ ngon -> Lên giường đi ngủ để hoàn thành ngày.
4. **Hệ thống chỉ số & kinh tế Idol**:
   - Năng lượng (Energy), Tâm trạng (Mood), Điểm danh tiếng (Fame), Số lượng người theo dõi (Followers), Thu nhập tiền tệ (Revenue VNĐ), Số người xem trực tiếp (Live Viewers), Lượt thích (Likes).
5. **Giao diện Smartphone Idol**:
   - Khung điện thoại hiện đại kiểu Dynamic Island.
   - 4 ứng dụng tương tác: Tin nhắn (FC & Quản lý), Mạng xã hội IdolGram (xem follower và đăng bài nhận bão like), Camera (chụp ảnh có đèn flash và âm thanh trập), Lịch trình công việc.
6. **Đồ họa PBR & Post-Processing chân thực**:
   - Bộ texture PBR chuẩn CC0 (Albedo, Normal map Sobel, Smoothness).
   - Ánh sáng động theo 4 buổi trong ngày (Bình minh ấm áp, Trưa rực rỡ, Chiều hoàng hôn vàng, Tối lung linh ánh đèn neon studio).
   - Realtime Reflection Probe & Light Probe Group cho phản chiếu chân thực trên sàn gỗ và mặt đá cẩm thạch.
   - Hạt bụi lơ lửng trong không khí bắt sáng cửa sổ.
   - URP Post-processing: ACES Tonemapping, Bloom, Vignette, Film Grain, Color Adjustments.
   - Tối ưu 60 FPS cho iPhone 16 Pro.
7. **Âm thanh phong phú**:
   - Procedural Sound Synthesizer: Chuông báo thức, tiếng trập camera, tiếng pha cà phê, tiếng ting thông báo, âm thanh fanfare chúc mừng nhận quà donation.

---

## 2. Những Tính Năng Chưa Hoàn Thành
- **Không có**: Tất cả yêu cầu theo đề bài từ gameplay, dựng cảnh, tối ưu đồ họa đến pipeline build tự động đều đã được hoàn thiện 100%.

---

## 3. Hướng Dẫn Tải Artifact IPA
1. Truy cập trang lượt chạy build thành công: [https://github.com/devnvvip/game/actions/runs/41824757175](https://github.com/devnvvip/game/actions/runs/41824757175)
2. Kéo xuống mục **Artifacts** ở cuối trang.
3. Bấm vào tên artifact **`game-ios-ipa`** để tải về file zip chứa file `.ipa` (dung lượng ~36.4 MB).
4. Giải nén file zip để nhận file `game-ios-ipa.ipa`.

---

## 4. Trạng Thái Ký Ứng Dụng & Yêu Cầu Để Cài Lên iPhone
- **Trạng thái**: Bản build là **Unsigned IPA** (ứng dụng chưa được ký bằng chứng chỉ Apple Developer cá nhân vì trên môi trường CI chưa cấu hình Provisioning Profile / Certificate).
- **Yêu cầu cài đặt lên iPhone thực tế**:
  Để cài đặt file IPA chưa ký lên iPhone (chạy iOS 15 đến iOS 18+), bạn có thể sử dụng một trong các công cụ sideload phổ biến sau:
  - **Sideloadly (Khuyên dùng - Windows & Mac)**: Cài phần mềm Sideloadly trên máy tính, kết nối iPhone qua cáp USB, kéo thả file `game-ios-ipa.ipa` vào, nhập tài khoản Apple ID miễn phí của bạn để phần mềm tự động ký và cài thẳng vào iPhone.
  - **AltStore**: Cài đặt AltServer trên máy tính và cài ứng dụng qua AltStore.
  - **TrollStore**: Nếu thiết bị hỗ trợ TrollStore, có thể cài đặt và mở trực tiếp mà không cần ký lại mỗi 7 ngày.
  - **Xcode**: Nếu dùng máy Mac, mở Xcode -> *Window* -> *Devices and Simulators*, chọn iPhone của bạn và bấm dấu `+` ở mục *Installed Apps* để chọn file `.ipa` đã ký.
