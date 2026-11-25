# REALM OF CHAMPIONS: DAWN OF CELESTIA
### Game Mini 1v1 MOBA (Ala Mobile Legends) Berbasis Unity

Selamat datang di project **Realm of Champions: Dawn of Celestia**. Project ini telah dirancang khusus untuk Unity (kompatibel dengan Unity 6 dan Unity 2022/2023 LTS) dengan arsitektur MOBA 1v1 lengkap.

---

## 1. Latar Belakang Cerita (Lore & Story)
Di benua kuno **Land of Celestia**, berdiri sebuah Altar Suci yang menjaga keseimbangan dunia dengan memancarkan energi dari **Kristal Cahaya Abadi** (*Aethel Crystal*). Altar ini dijaga oleh ordo suci bernama **Dawn Order**.

Selama ribuan tahun, dimensi kegelapan **Void Syndicate** terkunci di balik gerbang kehampaan. Namun sebuah anomali magis membuka celah dimensi, dan muncullah **Malakar - The Shadow Revenant**, panglima bayangan terkuat yang haus akan energi kristal suci untuk menenggelamkan dunia ke dalam malam abadi.

Untuk mempertahankan altar dan menyelamatkan Celestia, terpilihlah kesatria pelindung matahari: **Aurelius - The Dawn Guardian**. Dengan armor emas bersinar dan Pedang Mentari (*Sunlight Claymore*), Aurelius berdiri di satu-satunya jalur masuk (*lane*) untuk menghadapi Malakar dalam duel hidup dan mati.

---

## 2. Fitur & Gameplay (1 Player vs 1 Enemy)
Game ini mengusung format duel 1v1 ala Mobile Legends di dalam lane arena lengkap dengan menara pertahanan (*Turret*):

### Karakter:
- **Player: Aurelius (The Dawn Guardian)**
  - HP: 1000 | Mana: 400
  - **Basic Attack [Spasi / Tombol Serang]**: Tebasan pedang jarak dekat (Damage: 55).
  - **Skill 1: Radiant Dash [Tombol Q]**: Menerjang cepat ke depan sejauh 6 meter menembus musuh (Damage: 130, Cooldown: 5 detik).
  - **Skill 2: Solar Flare [Tombol W / E]**: Ledakan cahaya area (AOE) di sekeliling hero (Damage: 200) + Memberikan Shield proteksi HP tambahan (Cooldown: 8 detik).
  - **Skill 3 / Ultimate: Celestial Judgement [Tombol R]**: Memanggil pedang suci raksasa dari langit yang menghantam musuh (Damage masif: 420 burst damage + efek knockback, Cooldown: 15 detik).

- **Enemy AI: Malakar (The Shadow Revenant)**
  - HP: 1200
  - Menggunakan **AI State Machine**:
    - **Patrol**: Menjaga jalur dan berpatroli di sekitar altar tengah.
    - **Chase**: Mendeteksi keberadaan Aurelius saat berada di dalam radius 11 meter dan mengejar.
    - **Attack**: Melancarkan serangan cakar bayangan saat jarak dekat.
    - **Shadow Skill**: Melepaskan ledakan sihir bayangan gelap secara berkala.
    - **Retreat to Base**: Jika HP Malakar sekarat (di bawah 25%), AI akan secara taktis mundur ke menara pertahanannya untuk memulihkan diri!

- **Turret Pertahanan (Towers)**:
  - Turret Player (Biru) & Turret Musuh (Merah) memiliki sensor otomatis yang menembak target yang mendekat.
  - Memiliki HP 2000 dan dapat dihancurkan.

---

## 3. Menu & Dashboard
1. **Play Game**: Langsung memuat scene arena pertempuran (`BattleArena`).
2. **About**: Membuka pop-up modal interaktif yang memiliki 3 tab:
   - Tab Cerita (Lore Lengkap)
   - Tab Cara Bermain (Kontrol & Skill)
   - Tab Sumber Asset
3. **Quit**: Menutup aplikasi game / menghentikan mode Play di Unity Editor.
4. **In-Game HUD**:
   - Indikator HP & Mana Bar Player di kiri bawah.
   - Boss HP Bar Malakar di atas tengah.
   - Tombol virtual Skill ala Mobile Legends di kanan bawah (lengkap dengan overlay cooldown & hitung mundur angka).
   - Timer pertandingan & Score KDA di atas.
   - Tombol Pause & Panel Game Over (Victory / Defeat).

---

## 4. Cara Bermain (Controls & Objective)
- **Gerakan Hero**:
  - Keyboard: `W`, `A`, `S`, `D` atau `Tombol Panah`.
  - Mouse: `Klik Kanan` di area lantai tanah (*Click-to-Move* khas MOBA).
- **Serangan & Skill**:
  - `Spasi` / Klik Tombol Attack = Basic Attack.
  - `Q` / Klik Tombol Skill 1 = Radiant Dash.
  - `W` atau `E` / Klik Tombol Skill 2 = Solar Shield AOE.
  - `R` / Klik Tombol Ult = Celestial Judgement.
- **Kondisi Menang**: Kalahkan Malakar atau hancurkan Turret musuh!
- **Kondisi Kalah**: Jika Aurelius kehabisan darah (HP 0).

---

## 5. Sumber Asset Lengkap & Legal (Bebas Royalti)
Semua aset yang direkomendasikan dan disiapkan untuk game ini berasal dari platform resmi, legal, dan gratis:

1. **Adobe Mixamo (`https://www.mixamo.com`)**:
   - Model karakter 3D (Humanoid Paladin/Knight untuk Aurelius dan Demon/Vampire untuk Malakar).
   - Ratusan animasi gratis: *Idle, Walk, Run, Great Sword Slash, Spell Casting, Death*.
   - Format: FBX for Unity (100% kompatibel langsung dengan Unity Mecanim Humanoid Avatar).

2. **Kenney.nl (`https://kenney.nl`)**:
   - Aset 3D prototype kit, texture lantai, dan icon UI bertema fantasy.
   - Lisensi: **CC0 1.0 Universal (Public Domain)**, bebas digunakan untuk tugas, portofolio, hingga komersial tanpa royalti.

3. **Unity Asset Store (`https://assetstore.unity.com`)**:
   - *Particle Pack* by Unity Technologies (efek visual ledakan api, petir, sihir cahaya).
   - *RPG Monster Polyart* (aset monster gratis untuk variasi musuh).

4. **OpenGameArt.org (`https://opengameart.org`)**:
   - Efek suara pedang mengayun (*sword whoosh*), suara tebasan (*hit impact*), dan sihir.

5. **Freesound.org (`https://freesound.org`)**:
   - Musik latar bertema orkestra fantasy perang (*Fantasy Battle BGM*) dan suara klik tombol UI.

---

## 6. Panduan Membuka & Menjalankan Game di Unity
Ikuti langkah-langkah mudah berikut:

1. **Buka Unity Hub**:
   - Buka aplikasi **Unity Hub** di komputer Anda.
2. **Tambahkan Project**:
   - Klik tombol **Add** (atau tanda panah di samping tombol Add -> **Add project from disk**).
   - Cari dan pilih folder: `D:\tugas pa ali`.
   - Unity Hub akan mendeteksi project dengan versi Unity (Unity 6 / 6000.x atau versi LTS lainnya).
3. **Buka Project**:
   - Klik nama project `tugas pa ali` di daftar project Unity Hub.
   - Tunggu beberapa saat saat Unity mengimpor script dan paket.
4. **Generate Scene Otomatis (1 Klik Selesai)**:
   - Setelah Unity Editor terbuka, lihat menu bar di bagian paling atas:
   - Klik menu **`MOBA Game`** -> pilih **`Generate Complete Game (Scenes & Assets)`**.
   - Dalam 1 detik, script akan otomatis membangun scene `MainMenu` dan `BattleArena`, membuat material warna, menata kamera, player, musuh, turret, dan memasang seluruh Canvas UI secara presisi!
5. **Mainkan Game**:
   - Buka folder `Assets/Scenes/` di jendela Project panel bawah.
   - Dobel klik file scene **`MainMenu`**.
   - Tekan tombol **Play (▶️)** di bagian tengah atas Unity Editor.
   - Selamat bermain! Klik **Play Game** untuk masuk ke arena, atau buka **About** untuk membaca ceritanya!
