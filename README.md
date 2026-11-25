# Realm of Champions (3D Action MOBA — Unity 6)

Game 3D Action-MOBA berbasis **Unity 6 (`6000.6.8f1`)** dengan **Universal Render Pipeline (URP)**.

## Fitur Utama
- **3D Lobby Dashboard (`MainMenu`):** Menampilkan Hero 3D *Maria – The Dawnblade*, status skill, target boss, serta modal *About & Story* (3 tab).
- **Arena 5v5 Land of Dawn (`BattleArena`):** Map simetris `160m × 160m` dengan 3 *lanes*, sungai beranimasi, 3 jembatan, *jungle*, *Boss Pit*, *Spawn Fountain*, serta 12 *Turret* & *Crystal Core*.
- **Sistem Respawn & Evolusi Boss:**
  - Pemain (*Maria*) respawn otomatis di Fountain setelah `5` detik.
  - Boss musuh (*Mutant – The Void Reaver*) respawn setelah `6` detik dengan evolusi stat (`+600 HP`, `+18 Attack`, `+40 Skill`, `+0.45 Speed`, ukuran membesar).
- **Objective & Storyline Dinamis:** Kemenangan diraih setelah menghancurkan seluruh **6 Turret Void** musuh, disertai banner narasi progresif per babak.
- **Procedural Audio & Combat VFX:** Efek suara dan partikel jurus dibangkitkan langsung dari script C#.

## Kontrol
- `W A S D`: Bergerak
- `Shift + WASD`: Lari (*Sprint*)
- `Spasi`: Lompat
- `F` / `Klik Kiri`: Basic Attack
- `1`: Skill 1 — *Radiant Dash*
- `2`: Skill 2 — *Solar Flare* (AOE + Shield)
- `3`: Ultimate — *Celestial Judgement*
- `Klik Kanan (tahan)` + `Scroll`: Putar & Zoom Kamera

## Cara Menjalankan & Build `.exe` di Unity
1. Buka proyek ini di **Unity 6 (`6000.6.8f1`)**.
2. (Opsional jika ingin generate ulang scene) Klik menu **`MOBA Game → Generate Complete Game`**.
3. Untuk menjadikan file `.exe` Windows: klik menu **`MOBA Game → Build Windows (.exe)`** — hasil build akan tersimpan di `Builds/RealmOfChampions.exe`.
