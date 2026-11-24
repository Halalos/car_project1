# Ellenőrzési terv – hat 10 pontos kategória

Minden kézi futáshoz rögzíts dátumot, Unity/build verziót, gépet, elvárt és tényleges eredményt. Az automata XML eredmények a Logs mappába kerülnek. A nem futtatott teszt nem tekinthető sikeresnek.

## 1. Játékélmény és dinamika

- Nyomj rövid A/D impulzusokat: az elmozdulás a következő képkockában induljon; elengedés után ne maradjon tartós sodródás.
- Tartsd a kormányt 30 másodpercig mindkét szélen: az autó maradjon az úton, az út kanyarjaiban is.
- Próbálj 30/60/120 FPS-t: azonos 30 másodperces futam közel azonos távolságot tegyen meg.
- Ütközz forgalomba és korlátként szolgáló akadályba: sebességvesztés, effekt, állapotvesztés és rövid ismételt-találat elleni védelem legyen; ne essen darabokra a karosszéria.
- A boost fogyjon, majd töltődjön vissza. A HUD km/h egyezzen a Speed × 3,6 értékkel.

Automata: SteeringRespondsWithinOneFrameAndRemainsOnRoad, MovementIsConsistentAcrossFrameRates, CollisionPenalizesSpeedAndHasBriefImmunity, BoostIsTemporaryAndRecharges, SpeedDisplayUsesMetresPerSecond.

## 2. Környezet

- Mindkét környezetben, mindhárom szinten ellenőrizd a három sávot, kanyarokat, korlátokat, táblákat, épületeket és növényzetet.
- Endless módban legalább 15 percig vizsgáld a pályát: ne keletkezzen rés, duplázott szakasz vagy folyamatos objektumszám-növekedés.
- A koordináta-áthelyezéskor ne ugorjon a kamera az autóhoz képest; az aktív útszakaszok száma maradjon állandó.
- A forgalom mozogjon és váltson sávot; az akadályok egymástól elkülönülve jelenjenek meg, maradjon kerülési lehetőség.

Automata: RoadPoolReusesSectionsAndRebasesWithoutGaps; PlayMode közös runtime teszt.

## 3. Grafika

- Ellenőrizd mindkét környezetet és minden autót: nincs pink/missing anyag; felismerhető kerekek, ablakok, fényszórók és hátsó lámpák.
- Legyenek árnyékok, jól látható útjelzések, kontrasztos akadályok és egységes színpaletta.
- Gyorsuláskor változzon a látószög, boostkor legyen részecskeeffekt, ütközéskor rövid kamera- és HUD-visszajelzés.
- Ellenőrizd 16:9 és 16:10 felbontáson, majd Windows buildben. Külön vizsgáld a távoli pályaszakaszok megjelenését és teljesítményét.

Bizonyíték: képernyőképek és kézi grafikai ellenőrzés; a fordítás önmagában nem igazolja a látványt.

## 4. UI és fejlesztések

- Járd végig: Main Menu → mód/környezet/szint → Start → Pause → Resume → Restart → Result → Main Menu.
- A sebesség, távolság, pont, idő és helyezés helyesen frissüljön; nem versenymódban a helyezés nem alkalmazható jelölést kapjon.
- Futam után egyszer járjon pénz. Újraindítás ne adja újra ugyanazt a jutalmat.
- Vásárolj mindhárom fejlesztésből; mérd az új végsebességet, gyorsulási időt és oldalirányú sebességet.
- Pénzhiány és maximum szint esetén ne történjen vásárlás. Újraindítás után a pénz és szintek maradjanak meg.
- Sérült fő mentésnél a biztonsági másolat töltsön vissza; hiba jelenjen meg az állapotsorban.

Automata: AllUpgradesChangeDrivingPropertiesAndRespectBudgetAndCap, RunRewardIsIdempotentAndBestRecordsRemainIndependent, LocalSaveRoundTripAndCorruptionRecovery, PlayMode menü/szünet/újrakezdés.

## 5. Ellenfelek

- Race módban legyen öt AI, köztük óvatos és agresszív. Figyeld a sávváltást, lassítást és akadálykerülést.
- Tiszta, boostot használó futammal lehessen nyerni; több hibával lehessen veszíteni. Mindhárom nehézséget külön próbáljátok ki.
- Előzéskor változzon a helyezés; célba érés után maradjon rögzített. Közeli célba érésnél az interpolált áthaladási idő döntsön.
- Time Attack: időben célba érés győzelem, időtúllépés vereség, checkpoint +5 másodperc. Endless: járműállapot elfogyásakor eredményképernyő.

Automata: RaceAndTimeAttackCanBothBeWonAndLost, RivalsAvoidBlockedLaneAndBehavioursHaveDifferentPace, RivalFinishTimeIsInterpolatedAndFrozen; PlayMode valódi GameSession eredménytesztek.

## 6. Firebase

A FIREBASE_SETUP.md külső lépései után:

- Konfiguráció nélküli indulás ne omoljon össze; helyi profil és játék működjön.
- Anonim UID, mindhárom mód eredménye, dátum, mód/pálya/szint, rekordok, pénz és fejlesztések ténylegesen kerüljenek a saját Firestore dokumentumokba.
- SYNC CLOUD után szerverről visszaolvasott sikerjelzés legyen. RESTORE CLOUD állítsa vissza a profilt.
- Kapcsold le az internetet: a futam maradjon helyben; visszakapcsolás és SYNC után töltődjön fel. Ugyanaz a runId ne duplázzon.
- A Rules Playground vagy Emulator Suite segítségével ellenőrizd a saját UID engedélyezését, az idegen UID és névtelen/kijelentkezett kérés tiltását, valamint a hibás adattípusok elutasítását.

Ezekhez tényleges projekt és hálózat szükséges. SDK-fordítás, helyi teszt vagy dokumentáció nem helyettesíti a valódi mentés/betöltés vizsgálatát.

## After Dark kiegészítő tesztek

- Motorhang: változzon a sebességgel; boost alatt külön szél/gyorsítási effekt legyen. Szünetben és eredményképernyőn ne szóljon tovább a motor/boost.
- Ütközés: egy elfogadott találat egy effektet indítson; a sérthetetlenség alatt ne szóljon újra minden képkockában.
- Menü, rajt, checkpoint, elhaladás, vásárlás/elutasítás és győzelem/vereség külön hangot adjon.
- SOUND gomb: a főmenüből és szünetmenüből is némítson; a mentett beállítás maradjon meg újraindítás után.
- Quit: friss pénzegyenleggel kattints a gombra; a mentés után Windowsban záródjon be az alkalmazás, Unityben álljon le a Play mód. Szünetből is működjön. Sikertelen helyi mentésről adjon visszajelzést.
- Neon megjelenés: a renderer postProcessData hivatkozása és a Volume ne legyen hiányzó; az emissziós anyagok import után is tartsák meg az _EMISSION kulcsszót. Az út/autók maradjanak jól felismerhetők éjszakai fényben.
- Automata Windows kilépési próba: a -cars-quit-smoke argumentum külön ideiglenes profillal indít, majd a tényleges főmenü Quit gombját hívja. Elvárt: CARS_PLAYER_QUIT_SMOKE_UI_CLICK és CARS_QUIT_READY naplójel, majd 0 kilépési kód. A normál játék nem indít ilyen próbát.
- A WAV-adatok és bekötések automatikusan ellenőrzöttek; a hangerő és hangkarakter végső minősítéséhez hangszórós/fejhallgatós próba is szükséges.
