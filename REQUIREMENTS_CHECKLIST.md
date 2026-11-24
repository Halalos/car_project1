# Requirements checklist – Cars Velocity

Ellenőrzés: **2026-11-14**, Unity **2022.3.62f3**, URP **14.0.12**, Windows x64, Firebase Unity SDK **14.1.0**.

A cél hat darab 10 pontos kategória lefedése. Az alábbi lista funkció- és tesztállapotot rögzít. **Az élő Firebase REST-próba sikeres. A Unity-játékból indított élő próba még hátravan.**

## 1. Játékélmény és dinamika – cél: 10 pont

**Elkészült:** automatikus előrehaladás, A/D és nyílvezérlés, képkockasebességtől nagyrészt független simítás, közvetlen reakció, finom szélkorrekció és pályahatár. Újratöltődő boost. Ütközési sebesség- és állapotvesztés, rövid sérthetetlenség, részecske/HUD/kamera-visszajelzés; nincs deformáció vagy szétesés.

**C# fájlok:** `Assets/Cars/Scripts/ArcadeCarController.cs`, `GameSession.cs`, `ChaseCamera.cs`, `GameConfig.cs`.

**Tesztállapot:** PASS – automatikus reakció/úton maradás, 30 és 120 FPS összehasonlítás, ütközési büntetés és ismételt-találat elleni védelem, boost és visszatöltés; PlayMode automatikus haladás és szünet.

**Hiányosság / további ellenőrzés:** emberi irányításérzet- és egyensúlyteszt szükséges mindhárom nehézségen. Nincs állítás arról, hogy a szubjektív játékélmény már háromfős játékpróbán elfogadott.

## 2. Kidolgozott környezet – cél: 10 pont

**Elkészült:** háromsávos út enyhe kanyarokkal; újrahasznosított 16 szakaszos pályapool; koordináta-áthelyezés. Neon Valley geometrikus fákkal és lila dombokkal; Midnight Desert világító pylonokkal és neon tornyokkal. A belső pályaazonosítók a régi mentések kompatibilitása miatt megmaradtak. Mindkettőben épületek, ablakok/ajtók, korlátok, jelzések, mozgó és sávot váltó forgalom, útépítési akadályok.

**C# fájlok:** `Assets/Cars/Scripts/TrackSystem.cs`, `TrackSection.cs`, `TrafficActor.cs`; `Assets/Cars/Editor/CompleteGameBuilder.cs`.

**Tesztállapot:** PASS – szakaszok újrahasznosítása és hézagmentes számsorrend 100 km pályatávolságig; állandó poolméret; koordináta-áthelyezés. PlayMode mindhárom módot és mindkét környezetet elindította. Renderelt képek vizuálisan megvizsgálva.

**Hiányosság / további ellenőrzés:** a 100 km-es automatikus állapotugrás nem helyettesít 15 perces folyamatos emberi stressztesztet. A környezet szándékosan ismétlődő, generált szakaszokból áll.

## 3. Grafika – cél: 10 pont

**Elkészült:** URP Lit anyagok, saját készítésű low-poly karosszéria és üvegezett kabin; kerekek, felnik, tükrök, lámpák, lökhárítók, kipufogó és játékosspoiler. Neon/synthwave színpaletta, saját csíkozott napos éjszakai égbolt, HDR, működő bloom és tonemapping, emissziós útszegélyek és épületek; irányfény, fényszóró, árnyékok, köd; sebességfüggő FOV, ütközési kamerarázás, boost- és szikraeffekt. Nincsenek átvett jogvédett játékassetek.

**C# fájlok:** `Assets/Cars/Editor/CompleteGameBuilder.cs`; `Assets/Cars/Scripts/ChaseCamera.cs`, `ArcadeCarController.cs`.

**Tesztállapot:** PASS – URP-val fordul; Direct3D 11 PlayMode képernyőképek elkészültek és megtekintve: `Artifacts/Race.png`, `EndlessSurvival.png`, `TimeAttack.png`, `mode-selection.png`. Nincs látható pink/hiányzó anyag. Windows build sikeres, renderelést engedélyező indulási smoke teszt hibamentes.

**Hiányosság / további ellenőrzés:** hosszabb Windows játékmenet, több felbontás és teljesítménymérés még kézi ellenőrzést igényel. A screenshotok a Unity PlayMode renderelt képei; nem Windows teljes futamról készült képek.

## 4. Felhasználói felület és fejlesztések – cél: 10 pont

**Elkészült:** angol főmenü, mód-, környezet- és szintválasztás, garázs, szünet/folytatás, restart, eredményképernyő. Sebesség a mozgásmodellből (`Speed × 3,6`), pont, távolság, idő, módhoz illő helyezés. Egyszeri futamjutalom, pénztárca, öt fokozatú sebesség/gyorsulás/irányíthatóság-fejlesztés, tényleges tulajdonságmódosítással; mód/pálya/szint szerinti rekordok. Helyi aszinkron profilmentés, atomikus fájlcsere és biztonsági másolat.

**C# fájlok:** `Assets/Cars/Scripts/CarsUI.cs`, `GameSession.cs`, `GameConfig.cs`, `SaveService.cs`.

**Tesztállapot:** PASS – km/h számítás; fejlesztések ára, pénzhiány és szintkorlát; mért gyorsulás, oldalirányú sebesség és végsebesség; egyszeri jutalom és független rekordok. Helyi mentés/visszatöltés és sérült fő fájl helyreállítása. PlayMode menü, mindhárom mód, szünet/folytatás és restart.

**Hiányosság / további ellenőrzés:** minden menügomb emberi végigkattintása és több képarány tesztelése szükséges. A helyezés Endless/Time Attack módban „—”, mivel ott nincs versenymezőny.

## 5. Ellenfelek – cél: 10 pont

**Elkészült:** öt AI-versenyző; óvatos és agresszívebb viselkedés eltérő tempóval, döntési idővel és sávváltási sebességgel; forgalom és akadályok figyelése. Élő helyezés, célvonal, interpolált és rögzített célba érési idők. Race győzelem csak első helyen. Time Attack időkeret/checkpoint/cél; Endless állapotvesztés. Az eredmény tényleges haladásból és időkből számolódik.

**C# fájlok:** `Assets/Cars/Scripts/RivalDriver.cs`, `TrafficActor.cs`, `GameSession.cs`, `GameConfig.cs`.

**Tesztállapot:** PASS – akadály előtti sávváltás, két viselkedés eltérő tempója, célba érés interpolálása és eredményfagyasztás. PlayMode a valódi GameSession vezérlővel külön győzelmi és vereségi versenyeredményt, Time Attack sikert/időtúllépést, valamint Endless járművesztést ellenőrzött.

**Hiányosság / további ellenőrzés:** a célállapotokat izolált tesztszcenáriók állítják elő; ezek nem helyettesítik a teljes normál pályák emberi balansztesztjét. Az AI arcade pálya/sávmodell, nem teljes fizikai járműszimuláció.

## 6. Firebase-adatmentés – cél: 10 pont

**Elkészült:** hivatalos Firebase Unity SDK 14.1.0 Authentication + Firestore csomagok importálva. Külön `FirebaseService`, `ISaveService`, helyi szolgáltatás és koordinátor. Anonymous Authentication, aszinkron függőségellenőrzés/mentés/betöltés, 20 másodperces kéréskorlát és hibajelzés; szerverről történő visszaolvasás a sikerjelzés előtt. Egyedi runId, offline függő eredmények, külön felhő-visszaállítás. Játékos, pont, UTC dátum és szerveridő, környezet/szint, mód, rekordok, pénz és fejlesztések mentése. Tulajdonos UID-jére korlátozott Firestore szabályok és magyar beállítási útmutató.

**C# fájlok:** `Assets/Cars/Scripts/SaveService.cs` (`FirebaseService`, `LocalSaveService`, `SaveCoordinator`), `GameConfig.cs`, `GameSession.cs`, `CarsUI.cs`; `Assets/Cars/Editor/FirebaseSdkInstaller.cs`.

**Tesztállapot:** SDK-val és `CARS_FIREBASE` szimbólummal a teljes projekt és Windows build sikeresen fordult. A konfiguráció nélküli, kikapcsolt felhővel történő indulás PlayMode és Windows Direct3D startup tesztben nem omlott össze. A helyi mentés/visszatöltés PASS.

**2026-11-23, Firebase SDK 14.1.0:** a felhasználó Windows PowerShellből igazolta az élő anonim REST-bejelentkezést, az írást/visszaolvasást és a telepített hozzáférési szabályokat a `cars-velocity-game` projektben. A játékban a felhőmentés aktív, az anonim belépés automatikusan indul. Az új kód és a mentési EditMode-tesztek lefordultak. A Unity-tesztek futtatását Editor-licenchiba akadályozta. A játékból indított élő mentés, a hálózati megszakítás utáni újrapróbálás és a felhőből visszaállítás még ellenőrizendő.

**Külső lépések:** `Docs/FIREBASE_SETUP.md`, szabályok: `Docs/firestore.rules`. A profil kliensoldali, nem szerveresen hitelesített csalásvédett gazdaság; több eszköz egyidejű írásainak automatikus összeolvasztása nincs.

## Automatizálás, build és bizonyítékok

- `Tools → Cars → Build Complete Game`: Bootstrap, MainMenu, Gameplay, hét prefab, két környezet, anyagok, URP, kamera, UI és közös módrendszer. Többször sikeresen lefutott; a generált eszközök ugyanazon útvonalakon frissülnek, nem képződnek név szerinti másolatok. A saját GameConfig megmarad.
- `Tools → Cars → Validate Generated Game`: PASS.
- `Tools → Cars → Build Windows Player`: PASS; `Builds/Windows/CarsVelocity.exe`.
- **EditMode: 20/20 PASS**, `Logs/Cars-NeonEditMode.xml`.
- **PlayMode: 5/5 PASS**, `Logs/Cars-NeonPlayMode.xml`.
- SDK-val történő neon/hang generálás: `Logs/Cars-NeonGenerate.log`.
- Windows build: `Logs/Cars-NeonWindowsBuild.log`, `CARS_WINDOWS_BUILD_OK`.
- Windows Direct3D 11 indulás: `Logs/Cars-PlayerSmoke-D3D11.log`; 12 másodpercig futott, majd kizárólag a tesztpéldányt leállítottuk. Nincs indulási exception/shaderhiba. Ez startup smoke teszt, nem teljes emberi játékpróba.
- Egy korábbi `-nographics` próba Null GPU mellett várható shaderhibákat naplózott; grafikai ellenőrzésként nem használható. Ezért külön Direct3D 11 próba készült.
- Részletes fennmaradó kézi ellenőrzések: `Docs/TEST_PLAN.md`; háromfős felelősségi javaslat: `Docs/TEAM.md`.

## After Dark – hangok, neon grafika és javított Quit

**Elkészült:** 12 eredeti, generált mono WAV-hang. Sebességfüggő motorhang, szél/boost, menü, rajt, ütközés, checkpoint, elhaladás, vásárlás/elutasítás, győzelem/vereség. Szünetkor a motor/szél/boost leáll; SOUND kapcsoló a főmenüben és szünetben, tartós beállítással. Teljes neon látvány és menütéma; a generátor az Audio mappát, skyboxot, Volume-profilt és URP post-processing hivatkozást is elkészíti.

**Quit:** mentés után Windowsban Application.Quit, Unity Editorban Play mód leállítás. Futó felhőművelet nem blokkolja némán a kilépést; a helyi mentés megmarad. Helyi mentési hiba esetén visszajelzés és újrapróbálás. Szünetmenüből is elérhető.

**Kapcsolódó fájlok:** `Assets/Cars/Scripts/GameAudio.cs`, `SoundSynth.cs`, `StandaloneQuitSmoke.cs`; módosított `GameSession.cs`, `CarsUI.cs`, `ArcadeCarController.cs`, `SaveService.cs`. Generálás: `Assets/Cars/Editor/AudioAssetBuilder.cs`, `NeonSceneStyle.cs`, `CompleteGameBuilder.cs`; égbolt: `Assets/Cars/Shaders/SynthwaveSky.shader`.

**Ellenőrzés:** 20/20 EditMode és 5/5 PlayMode PASS. Hangminták végesek, nem némák és nem klippelnek; a motorloop határa folytonos. A 12 importált WAV mono és megfelelő hosszúságú. Emisszió import után is aktív, post-processing függőség ténylegesen bekötött. Runtime sebességfüggő hangmagasság, boost, ütközési esemény, szünet és némítás PASS. Quit gombbal kért kilépés előtt a módosított pénzegyenleg fájlba kerül: PASS.

**Kész Windows program Quit próbája:** `Logs/Cars-NeonQuitSmoke.log` tartalmazza a tényleges menügomb meghívását (`CARS_PLAYER_QUIT_SMOKE_UI_CLICK`) és a mentés utáni kilépést (`CARS_QUIT_READY`). Az alkalmazás saját maga **0 kilépési kóddal bezárult**; nem kellett kényszerleállítás. Direct3D 11, elkülönített ideiglenes tesztprofil, hiányzó Firebase-konfiguráció mellett is működő indulás. A logban nincs runtime exception vagy shaderhiba.

**További kézi ellenőrzés:** a végső hangkarakter és hangerő hangszórón/fejhallgatón, emberi balansz- és hosszú stresszteszt, valamint élő Firebase-próba a Unity-játékból.
