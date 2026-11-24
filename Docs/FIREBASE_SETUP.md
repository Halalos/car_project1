# Firebase beállítás – Cars Velocity

## Állapot és támogatás

A Firebase-projekt azonosítója `cars-velocity-game`, az Android-csomagnév `com.carsstudentteam.carsvelocity`. A felhőmentés be van kapcsolva, és az Android-konfiguráció az `Assets/google-services.json` fájlban található. A felhasználó által futtatott élő REST-teszt igazolta az anonim belépést, a Firestore-írást és -olvasást, valamint az idegen hozzáférés elutasítását. A Unity futásidejű próba még hátravan.

A hivatalos Unity SDK támogat asztali fejlesztői munkafolyamatot Authentication és Firestore termékekkel. Ez korlátozott desktop támogatás, nem általános garancia minden Firebase termék végleges Windows terjesztésére. A beadáshoz a tényleges Windows builden is ellenőrizni kell.

Források:
- https://firebase.google.com/docs/unity/setup
- https://firebase.google.com/docs/auth/unity/anonymous-auth
- https://firebase.google.com/docs/firestore/manage-data/add-data
- https://firebase.google.com/docs/firestore/security/rules-conditions
- https://firebase.google.com/support/release-notes/unity

## Felhasználó által elvégzendő külső konfiguráció

1. A Firebase Console-ban nyisd meg a meglévő `cars-velocity-game` projektet.
2. Regisztráld az alkalmazást a Unity beállítási folyamatában. A desktop konfigurációhoz az SDK a valódi mobilalkalmazás-konfigurációból készít `google-services-desktop.json` fájlt; kövesd az aktuális hivatalos desktop lépéseket.
3. Az Anonymous szolgáltató a `firebase deploy --only auth --project cars-velocity-game` paranccsal vagy a konzol Authentication → Sign-in method menüjében engedélyezhető.
4. Hozz létre Cloud Firestore adatbázist megfelelő régióban, production szabályokkal. A projekt gyökerében található `firebase.json` a `Docs/firestore.rules` fájlra mutat. A szabályokat a Console-ban vagy a `firebase deploy --only firestore:rules --project <projektazonosító>` paranccsal publikálhatod.
5. A saját Firebase-projektből letöltött `google-services.json` fájlt helyezd az Assets gyökerébe. Ha iOS-konfigurációt használsz, a hivatalos útmutató szerint helyezd el a `GoogleService-Info.plist` fájlt. Ezek nem helyettesíthetők mintaértékekkel.
6. Ha az SDK nincs importálva, a hivatalos https://firebase.google.com/download/unity csomagból importáld a FirebaseAuth és FirebaseFirestore unitypackage fájlokat minden függőséggel együtt. Unity 2022.3 alatt ellenőrizd az aktuális SDK minimumkövetelményeit és a DLL importbeállításait.
7. A `CARS_FIREBASE` szimbólum Android és Standalone célra be van állítva. Androidon a minimum API-szint 24.
8. Az `Enable Firebase` mező az `Assets/Cars/Generated/GameConfig.asset` fájlban be van kapcsolva. A generátor a meglévő konfigurációt megtartja.
9. Indítsd el a játékot. A helyi profil betöltése után a Firebase inicializálása és szükség esetén az anonim belépés a háttérben indul. Az állapotsorban várd meg a `Cloud sign-in ready` jelzést. A Console Authentication listájában ellenőrizd az UID-t.
10. Teljesíts futamot, majd ellenőrizd a Console-ban a saját `players/{uid}` dokumentumot és a `results/{runId}` aldokumentumot. Indítsd újra a játékot, használd a RESTORE CLOUD funkciót, és ellenőrizd a pénzt, fejlesztéseket és rekordokat.

## Adatmodell és működés

- `players/{uid}`: playerId, coins, upgrades (speed, acceleration, handling), profileJson, updatedAt.
- A profileJson tartalmazza a három fejlesztési szintet, a mód/pálya/szint szerinti rekordokat és a már jutalmazott futamazonosítókat. A feltöltésre váró futamokat a helyi profil őrzi.
- `players/{uid}/results/{runId}`: játékos, pont, UTC kliensidő, szerveridőbélyeg, mód, környezet, szint, távolság, idő, helyezés, győzelem és pénzjutalom.
- A mentés aszinkron. A szolgáltatás szerverről olvassa vissza az adatot, mielőtt ellenőrzött felhőmentést jelezne. A puszta SDK-import vagy gyorsítótárazott írás nem bizonyít kapcsolatot.
- A futamazonosító azonos dokumentumra irányítja az ismételt feltöltést. A helyi jutalom egy futamra egyszer alkalmazható.
- A futamok és vásárlások először helyben mentődnek. Bekapcsolt Firebase esetén ezután automatikus feltöltés indul. Hiba után a SYNC CLOUD újrapróbálja a feltöltést.
- A RESTORE CLOUD külön, kétszeri kattintással megerősített művelet. Felülírja a helyi profilt; a még függő helyi futamok megmaradnak. Több eszköz egyidejű gazdasági módosításainak automatikus összeolvasztása nincs megvalósítva.
- Az anonim hozzáférés az adott Firebase auth munkamenethez tartozik. Az azonosító elvesztése után nincs automatikus fiók-helyreállítás; ahhoz később fiók-összekapcsolás szükséges.

## Kötelező valódi ellenőrzések

Rögzíts dátumot, SDK-verziót, tesztplatformot és eredményt a REQUIREMENTS_CHECKLIST.md fájlban:

- SDK inicializálás, anonim UID létrejötte.
- Mindhárom mód eredményének feltöltése és szerveroldali időbélyege.
- Profil és megvásárolt fejlesztések visszaállítása újraindítás után.
- Hálózat nélkül helyi mentés; hálózat visszaállítása után sikeres újrapróbálás.
- Szabályteszt: kijelentkezett felhasználó és másik UID ne olvashassa/írhassa az első játékos dokumentumait.
- Hibás típusú és negatív pénzösszegű írás elutasítása.
- Ugyanazon futam ismételt feltöltése ne hozzon létre új eredményt és új pénzjutalmat.

A szabályok saját profilra korlátozzák a hozzáférést; a játék kliensoldali pontozása nem csalásbiztos. Nyilvános, versenyszerű ranglistához szerveroldali eredményellenőrzés szükséges. A JSON-profil tartalmát a szabályok nem tudják mezőnként elemezni.

## Ebben a projektben már elvégzett SDK-lépések

A hivatalos SDK 14.1.0 App, Auth és Firestore csomagjai importálva vannak. A játékkód Roslynnal lefordult az importált DLL-ek ellenében. Az Android Unity-tesztfuttatás érvényes Editor-licenc hiányában leállt. Az eredeti SDK-licenc a FIREBASE_SDK_LICENSE.txt fájlban található.
