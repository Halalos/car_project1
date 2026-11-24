# Cars Velocity

Unity **2022.3.62f3**, URP **14.0.12**, Windows x64. Angol felület, magyar fejlesztői dokumentáció. A grafikai modelleket és anyagokat a projekt saját generátora állítja elő; nincs átvett játékasset.

## Indítás

1. Nyisd meg a projektet a fenti Unity-verzióval, és várd meg a csomagok importját.
2. Válaszd a **Tools → Cars → Build Complete Game** menüpontot. Újrafuttatható: a generált fájlokat azonos helyen frissíti, a GameConfig beállításait megőrzi. A generált jelenetek kézi módosításai újraépítéskor felülíródnak; saját tartalmat a Generated mappán kívül tárolj.
3. Nyisd meg az `Assets/Cars/Generated/Scenes/MainMenu.unity` jelenetet és nyomj Play-t. A Bootstrap a főmenüre irányít; közvetlen Gameplay indításkor is elérhető a menü.
4. Windows build: **Tools → Cars → Build Windows Player**. Kimenet: `Builds/Windows/CarsVelocity.exe`. Az egész Windows mappát együtt add át.

Irányítás: **A/D vagy bal/jobb nyíl** kormányzás, **Space** újratöltődő boost, **Esc** szünet. Az autó automatikusan gyorsít.

## Módok

- **Endless Survival:** folyamatosan újrahasznosított út, forgalom és útakadályok. Távolságért és elhaladásért jár pont. A járműállapot elfogyása zárja a futamot.
- **Race:** öt valóban haladó AI, interpolált célba érési idő, élő helyezés. Első hely győzelem; későbbi hely vagy végzetes ütközés vereség.
- **Time Attack:** célba érés határidőn belül; 500 méterenként +5 másodperces checkpoint a cél előtt. Időtúllépés vagy járművesztés vereség.

Mindhárom mód közös autót, pályát, HUD-ot, pénztárcát, fejlesztéseket és mentést használ. Két környezet és három nehézségi szint választható. A garázsban öt fokozatú sebesség-, gyorsulás- és kezelhetőség-fejlesztés vásárolható.

## Beállítások és mentés

A `GameConfig.asset` tartalmazza az alapértékeket, szinteket, forgalomsűrűséget, árakat és a Firebase kapcsolót. A HUD km/h értéke a mozgásmodell m/s értékéből számolódik. A pályagörbületet a TrackSystem adja; a kijelzett távolság a mozgásmodell pálya menti haladása.

Helyi profil: Unity `Application.persistentDataPath` alatt `cars-profile.json`, `.bak` biztonsági másolattal. Windows alatt a Cars Student Team/Cars Velocity alkalmazásadat-mappában található. Felhőbeállítás: [Docs/FIREBASE_SETUP.md](Docs/FIREBASE_SETUP.md). Valódi konfiguráció nélkül a felhőmentés nem állítható működőnek.

## Ellenőrzés és csapatmunka

- Automata tesztek: Window → General → Test Runner → EditMode és PlayMode.
- Generált tartalom ellenőrzése: Tools → Cars → Validate Generated Game.
- Részletes kézi tesztterv: [Docs/TEST_PLAN.md](Docs/TEST_PLAN.md).
- Követelményállapot: [REQUIREMENTS_CHECKLIST.md](REQUIREMENTS_CHECKLIST.md).
- Háromfős feladatosztás: [Docs/TEAM.md](Docs/TEAM.md).

A Library, Temp, Logs és Builds mappák nem forráskódok. A projektet Assets, Packages és ProjectSettings mappákkal együtt kell átadni. A Firebase SDK külső, saját licencű függőség; a hozzá tartozó licencfájlokat meg kell tartani.

## After Dark frissítés – neon grafika, hangok és kilépés

A teljes generált látvány neon/synthwave stílusú: éjszakai égbolt csíkozott nappal, cián–magenta útjelzések, világító korlátok és épületek, bloom, sportosabb autók és hozzá illő menük. A környezetek új megjelenített neve Neon Valley és Midnight Desert; a meglévő profilok pályaazonosítói megmaradtak.

12 saját, programból generált WAV: motor, szél, boost, menükattintás, rajt, ütközés, checkpoint, elhaladás, vásárlás, elutasított vásárlás, győzelem és vereség. A sebesség módosítja a motor hangmagasságát; szünetben a folyamatos játékmenethangok leállnak. A főmenü és a szünetmenü SOUND gombja kapcsolja a hangot; a beállítás megmarad újraindítás után is.

A QUIT a helyi mentés befejezése után lép ki. Unity Editorban leállítja a Play módot, a Windows programot bezárja; futó felhőszinkron nem teszi a gombot némán hatástalanná. Helyi mentési hiba esetén hibaüzenet jelenik meg, és a játék nyitva marad az újrapróbáláshoz. A szünetmenüből is elérhető.

Új kód: GameAudio, SoundSynth, AudioAssetBuilder, NeonSceneStyle, SynthwaveSky.shader. A Build Complete Game a hangfájlokat és a post-processing beállításokat is újragenerálja. A hangokhoz nincs szükség letöltött, jogvédett játékfelvételre.
