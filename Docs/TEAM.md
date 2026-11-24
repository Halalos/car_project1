# Háromfős csapat – javasolt felelősségek

Ez felelősségi felosztás, nem állítás arról, hogy a munkát már három ember végezte.

1. **Játékmenet és AI:** ArcadeCarController, RivalDriver, TrafficActor, GameSession; vezetési érzés, kiegyensúlyozás, valódi győzelem/vereség.
2. **Környezet és grafika:** TrackSystem, TrackSection, ChaseCamera, CompleteGameBuilder; két környezet, low-poly modellek, URP, pálya-újrahasznosítás, vizuális visszajelzés.
3. **UI, gazdaság és adatok:** CarsUI, GameConfig, SaveService; menük, HUD, fejlesztések, helyi és Firebase-mentés, dokumentáció.

Közös integráció: mindenki ellenőrizze a másik alrendszerének bemenetét/kimenetét, együtt fussák végig a TEST_PLAN.md forgatókönyveit. A Firebase konzol tulajdonosa végezze el a külső konfigurációt és a hozzáférési szabályok tesztjét. Beadás előtt közös Windows játékpróba és a REQUIREMENTS_CHECKLIST.md frissítése szükséges.
