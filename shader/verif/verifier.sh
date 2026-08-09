#!/bin/bash
set -u

RACINE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LECONS="$RACINE/lecons"
TRAVAIL="$(mktemp -d)"
CODE_SORTIE=0

trouver_godot() {
    if [ -n "${GODOT:-}" ]; then echo "$GODOT"; return; fi
    if command -v godot >/dev/null 2>&1; then command -v godot; return; fi
    for candidat in /Applications/Godot*.app/Contents/MacOS/Godot; do
        [ -x "$candidat" ] && echo "$candidat" && return
    done
}

trouver_unity() {
    if [ -n "${UNITY:-}" ]; then echo "$UNITY"; return; fi
    local dernier
    dernier="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort | tail -1)"
    [ -x "$dernier" ] && echo "$dernier"
}

verifier_godot() {
    local binaire="$1"
    local projet="$TRAVAIL/godot"

    echo "== Godot : $binaire"
    mkdir -p "$projet/shaders"
    cp "$RACINE/verif/godot/project.godot" "$RACINE/verif/godot/verif.gd" "$RACINE/verif/godot/verif.tscn" "$projet/"

    local nombre=0
    while IFS= read -r fichier; do
        cp "$fichier" "$projet/shaders/$(echo "${fichier#"$LECONS/"}" | tr '/' '_')"
        nombre=$((nombre + 1))
    done < <(find "$LECONS" -name '*.gdshader' | sort)
    echo "   $nombre shaders copies"

    local journal="$TRAVAIL/godot.log"
    "$binaire" --headless --path "$projet" >"$journal" 2>&1

    if grep -qE 'SHADER ERROR|chargement impossible' "$journal"; then
        echo "   ECHEC"
        grep -B4 -A2 -E 'SHADER ERROR' "$journal"
        CODE_SORTIE=1
    else
        echo "   OK : $nombre shaders compilent"
    fi
}

verifier_unity() {
    local binaire="$1"
    local projet="$TRAVAIL/unity"

    echo "== Unity : $binaire"
    local editeur
    editeur="$(cd "$(dirname "$binaire")/../../../.." && pwd)"
    local version_urp
    version_urp="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1]))['version'])" \
        "$binaire/../../Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.universal/package.json" 2>/dev/null)"
    if [ -z "$version_urp" ]; then
        version_urp="$(python3 -c "import json,glob,sys;c=glob.glob(sys.argv[1]);print(json.load(open(c[0]))['version'] if c else '')" \
            "$editeur/*/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.universal/package.json" 2>/dev/null)"
    fi
    if [ -z "$version_urp" ]; then
        echo "   URP introuvable dans cet editeur, Unity ignore"
        return
    fi
    echo "   URP $version_urp"

    "$binaire" -batchmode -quit -nographics -createProject "$projet" -logFile "$TRAVAIL/unity_creation.log"

    python3 - "$projet/Packages/manifest.json" "$version_urp" <<'PYTHON'
import json, sys
chemin, version = sys.argv[1], sys.argv[2]
donnees = json.load(open(chemin))
donnees["dependencies"]["com.unity.render-pipelines.universal"] = version
json.dump(donnees, open(chemin, "w"), indent=2)
PYTHON

    mkdir -p "$projet/Assets/Shaders" "$projet/Assets/Editor"
    cp "$RACINE/verif/unity/VerifShaders.cs" "$projet/Assets/Editor/"

    local nombre=0
    while IFS= read -r fichier; do
        cp "$fichier" "$projet/Assets/Shaders/$(echo "${fichier#"$LECONS/"}" | tr '/' '_')"
        nombre=$((nombre + 1))
    done < <(find "$LECONS" -name '*.shader' | sort)
    echo "   $nombre shaders copies"

    local journal="$TRAVAIL/unity.log"
    "$binaire" -batchmode -nographics -projectPath "$projet" -executeMethod VerifShaders.Tout -logFile "$journal"
    local resultat=$?

    grep -E 'VERIF \|' "$journal" | sed 's/^/   /'
    if [ $resultat -ne 0 ]; then
        echo "   ECHEC"
        CODE_SORTIE=1
    fi
}

CIBLE="${1:-tout}"

GODOT_BIN="$(trouver_godot)"
UNITY_BIN="$(trouver_unity)"

if [ "$CIBLE" = "tout" ] || [ "$CIBLE" = "godot" ]; then
    if [ -n "$GODOT_BIN" ]; then verifier_godot "$GODOT_BIN"; else echo "== Godot introuvable, ignore"; fi
fi

if [ "$CIBLE" = "tout" ] || [ "$CIBLE" = "unity" ]; then
    if [ -n "$UNITY_BIN" ]; then verifier_unity "$UNITY_BIN"; else echo "== Unity introuvable, ignore"; fi
fi

echo "== travail : $TRAVAIL"
exit $CODE_SORTIE
