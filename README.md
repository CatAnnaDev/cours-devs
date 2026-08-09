# cours devs

Des cours de programmation que j'écris, en français, avec une idée fixe : **on n'apprend pas en
lisant, on apprend en réparant.**

Chaque cours suit la même forme — des leçons à lire, puis du code cassé à faire marcher, avec un
programme qui compile, lance, et dit exactement ce qui ne va pas.

## Ce qu'il y a dedans

| Dossier | Cours | État |
|---|---|---|
| `shader/` | Écrire des shaders pour Godot, Unity et Unreal | 20 leçons sur 32 |
| `c/` | Le C par la mémoire — clings | 46 exercices, 8 chapitres |
| `cpp/` | Le C++ moderne, et ce que ça coûte — cpplings | 36 exercices, 7 chapitres |
| `csharp/` | Le C# pour le jeu vidéo — csharplings | 196 exercices |
| `java/` | Java depuis zéro, jusqu'au mod Hytale | leçons + exercices + notions |
| `rust/` | rpn — une calculatrice en Rust, construite pas à pas | 17 chapitres |
| `gamedev/` | Écrire un moteur, pas utiliser un moteur | à venir |
| `notions/` | La culture transverse : complexité, cache, mémoire, flottants, texte, concurrence | 16 pages |

## Le principe

**Un programme qui te suit.** Chaque cours à exercices a son runner : il s'arrête au premier
exercice non terminé, affiche la consigne, compile, lance, montre l'erreur. Tu corriges, tu
sauvegardes, il relance tout seul.

**Écrit dans le langage visé.** Le runner de `clings` est en C. Celui de `cpplings` est en C++.
Personne ne devrait avoir à installer un autre langage pour apprendre celui qu'il vise.

**Les erreurs sont le cours.** Les exercices C et C++ sont compilés avec AddressSanitizer et
UndefinedBehaviorSanitizer : au lieu d'un plantage sans explication, tu obtiens le fichier, la
ligne, la nature de la faute, et l'endroit où le bloc fautif avait été alloué. Apprendre à lire
ces rapports fait partie du programme.

**Ce que ça coûte, pas seulement ce que ça fait.** Un shader qui marche mais rame n'est pas fini.
Une classe C++ qui copie un mégaoctet à chaque appel non plus. Chaque leçon dit le prix.

**Rien n'est publié sans avoir été compilé.** `./clings verify`, `./cpplings verify`,
`shader/verif/verifier.sh` : les solutions sont vérifiées par les vrais outils — Godot 4.7,
Unity 6 avec URP, clang. Pas relues : compilées.

## Démarrer

```bash
cd c/clings   && make && ./clings        # le C
cd cpp/cpplings && make && ./cpplings    # le C++
cd csharp/csharplings-gamedev/csharplings && dotnet run
```

Pour `shader/`, il n'y a rien à lancer : chaque leçon est un fichier à déposer dans ton moteur.
Commence par `shader/00-bases/`.

Et `notions/` se lit sans rien lancer du tout, dans l'ordre que tu veux.
