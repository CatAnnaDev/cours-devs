#!/usr/bin/env bash
# Setup de neni_learn — je t'ai préparé tout ça, Neniri. Lance simplement : ./setup.sh
set -u

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

ok()   { printf "  [OK]      %s\n" "$1"; }
todo() { printf "  [À FAIRE] %s\n" "$1"; }

echo "================================================="
echo "   Salut Neniri ! On vérifie que tout est en place."
echo "================================================="
echo

# ----- Rust (dossier src/) -----
echo "## Rust  (dossier src/)"
if command -v cargo >/dev/null 2>&1; then
  ok "Rust est là  ($(cargo --version))"
  echo "     -> pour lancer les leçons :  cargo run"
else
  todo "Rust n'est pas installé."
  echo "     Installe-le ici : https://rustup.rs"
  echo "     (Linux/Mac : copie la commande affichée sur le site)"
fi
echo

# ----- Java (dossier java/) -----
echo "## Java  (dossier java/)"
if command -v java >/dev/null 2>&1 && command -v javac >/dev/null 2>&1; then
  ok "Java est là  ($(java -version 2>&1 | head -n1))"
  echo "     -> pour lancer les leçons :  cd java && java src/Main.java"
  echo "     (il te faut Java 22 minimum ; idéalement 25, comme pour Hytale)"
else
  todo "Java (le JDK) n'est pas installé."
  echo "     Télécharge un JDK 25 ici : https://adoptium.net"
fi
echo

# ----- Mod Hytale (dossier hytale/template/) -----
echo "## Hytale  (dossier hytale/template/)"
TPL="$ROOT/hytale/template"
if [ -d "$TPL" ]; then
  ok "Le template est là."
  chmod +x "$TPL/gradlew" 2>/dev/null && ok "gradlew rendu exécutable."
  mkdir -p "$TPL/server"
  if [ -f "$TPL/libs/HytaleServer.jar" ] && [ ! -f "$TPL/server/HytaleServer.jar" ]; then
    cp "$TPL/libs/HytaleServer.jar" "$TPL/server/HytaleServer.jar" && ok "HytaleServer.jar copié dans server/"
  fi
  if [ ! -f "$TPL/server/Assets.zip" ]; then
    todo "Il manque server/Assets.zip (utile seulement pour TESTER en jeu)."
    echo "     Copie le Assets.zip de ton installation Hytale dans :"
    echo "       hytale/template/server/"
  fi
  echo "     -> pour compiler ton mod :  cd hytale/template && ./gradlew shadowJar"
else
  todo "Le dossier hytale/template est introuvable."
fi
echo

echo "================================================="
echo "   Voilà, c'est bon. Par quoi commencer :"
echo "     - Rust   : dossier src/  (lance : cargo run)"
echo "     - Java   : ouvre  java/GUIDE.md"
echo "     - Hytale : ouvre  hytale/GUIDE.md"
echo "     - Bonus  : dossier notions/ (Big O, collections, optimisations)"
echo
echo "   Si un truc coince, demande-moi. — anna"
echo "================================================="
