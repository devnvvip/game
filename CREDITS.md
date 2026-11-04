# CREDITS & ASSET LICENSES

Dự án game: **Một Ngày Của Idol Streamer** (A Day of an Idol Streamer)
Phát triển cho: iOS (iPhone) & Windows Standalone.

---

## 1. Textures & Materials (PBR)
Toàn bộ texture và vật liệu PBR được tạo lập theo chuẩn Creative Commons CC0 / Public Domain / Procedural Synthesis:

- **Wood Parquet / Herringbone Flooring (PBR)**:
  - Base: CC0 Procedural synthesis inspired by ambientCG (WoodFloor041)
  - Maps: Albedo, Normal (Sobel filter), Smoothness
  - License: Creative Commons 0 (Public Domain Dedication)

- **Modern Plaster Wall (PBR)**:
  - Base: CC0 Architectural Wall Material inspired by Poly Haven (plaster_grey)
  - Maps: Albedo, Normal, Roughness
  - License: Creative Commons 0 (Public Domain Dedication)

- **Studio Acoustic Slat Wall Panel (PBR)**:
  - Base: Modern sound-diffusing acoustic oak panel with black acoustic felt
  - Maps: Albedo, Normal, Height
  - License: CC0 / Public Domain

- **Carrara Italian Marble Kitchen Counter (PBR)**:
  - Base: CC0 Marble Stone inspired by ambientCG (Marble012)
  - Maps: Albedo, Normal, Polish Specular
  - License: Creative Commons 0 (Public Domain Dedication)

- **Linen Cotton Fabric (Bed & Cushions)**:
  - Base: CC0 Fabric Weave inspired by Poly Haven (fabric_pattern_07)
  - Maps: Albedo, Normal
  - License: Creative Commons 0 (Public Domain Dedication)

- **Monitor Stream Graphics & UI Displays**:
  - Tự thiết kế và dựng texture 2D dành riêng cho game Idol Streamer
  - License: MIT / CC0

---

## 2. Audio & Sound Design
- **Procedural Sound Synthesizer (SoundManager.cs)**:
  - Tự tạo procedural PCM waveform tại runtime (Sine wave tone, Harmonics chime, Shutter burst noise, Espresso brew stream, Fanfare arpeggio).
  - Không sử dụng file âm thanh có bản quyền thương mại của bên thứ ba.
  - License: MIT

---

## 3. Technology Stack & Engines
- **Unity 6 (6000.6.8f1)** - Universal Render Pipeline (URP).
- **Unity Input System** (`com.unity.inputsystem`).
- **Post-Processing Stack v3** (URP Volume Profile: ACES Tonemapping, Bloom, Vignette, Film Grain, Color Adjustments).
- **GameCI** - Unity Actions workflow for iOS Xcode build & IPA packaging.
