@echo off
REM Setup de neni_learn -- je t'ai prepare tout ca, Neniri. Double-clique ou lance : setup.bat
setlocal
cd /d "%~dp0"

echo =================================================
echo    Salut Neniri ! On verifie que tout est en place.
echo =================================================
echo.

REM ----- Rust (dossier src\) -----
echo ## Rust  (dossier src\)
where cargo >nul 2>nul
if %errorlevel%==0 (
  echo   [OK]      Rust est la.
  echo            -^> pour lancer les lecons :  cargo run
) else (
  echo   [A FAIRE] Rust n'est pas installe.
  echo            Installe-le ici : https://rustup.rs
  echo            ^(Windows : telecharge rustup-init.exe^)
)
echo.

REM ----- Java (dossier java\) -----
echo ## Java  (dossier java\)
where java >nul 2>nul
if %errorlevel%==0 (
  echo   [OK]      Java est la.
  echo            -^> pour lancer les lecons :  cd java ^&^& java src\Main.java
  echo            ^(il te faut Java 22 minimum ; idealement 25, comme pour Hytale^)
) else (
  echo   [A FAIRE] Java ^(le JDK^) n'est pas installe.
  echo            Telecharge un JDK 25 ici : https://adoptium.net
)
echo.

REM ----- Mod Hytale (dossier hytale\template\) -----
echo ## Hytale  (dossier hytale\template\)
if exist "hytale\template" (
  echo   [OK]      Le template est la.
  if not exist "hytale\template\server" mkdir "hytale\template\server"
  if exist "hytale\template\libs\HytaleServer.jar" if not exist "hytale\template\server\HytaleServer.jar" (
    copy "hytale\template\libs\HytaleServer.jar" "hytale\template\server\HytaleServer.jar" >nul
    echo   [OK]      HytaleServer.jar copie dans server\
  )
  if not exist "hytale\template\server\Assets.zip" (
    echo   [A FAIRE] Il manque server\Assets.zip ^(utile seulement pour TESTER en jeu^).
    echo            Copie le Assets.zip de ton installation Hytale dans :
    echo              hytale\template\server\
  )
  echo            -^> pour compiler ton mod :  cd hytale\template ^&^& gradlew.bat shadowJar
) else (
  echo   [A FAIRE] Le dossier hytale\template est introuvable.
)
echo.

echo =================================================
echo    Voila, c'est bon. Par quoi commencer :
echo      - Rust   : dossier src\  ^(lance : cargo run^)
echo      - Java   : ouvre  java\GUIDE.md
echo      - Hytale : ouvre  hytale\GUIDE.md
echo      - Bonus  : dossier notions\ ^(Big O, collections, optimisations^)
echo.
echo    Si un truc coince, demande-moi. -- anna
echo =================================================
echo.
pause
