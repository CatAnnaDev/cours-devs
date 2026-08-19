# Le terminal

Un shell n'est pas un langage de plus à apprendre par cœur : c'est une machine à composer des
programmes, et elle repose sur cinq choses seulement. Le reste se cherche dans `man` sans honte.

## Cinq prises, et rien d'autre

- **ses arguments**, le tableau `argv` : `grep -n motif fichier.txt` en reçoit trois ;
- **une entrée**, `stdin`, descripteur 0 : ce qui lui arrive par `<` ou par un tube ;
- **une sortie**, `stdout`, descripteur 1 : son résultat, et rien d'autre ;
- **une sortie d'erreur**, `stderr`, descripteur 2 : ses plaintes et son avancement ;
- **un code de sortie**, un entier de 0 à 255, rendu en mourant.

Tout le reste est de la plomberie autour de ces cinq prises, et c'est ce qui laisse s'emboîter des
outils écrits par des inconnus. Deux sorties et pas une, parce que le tube ne porte que `stdout`.

## Le code de sortie

`0` veut dire **réussite**, tout le reste **échec**. C'est cette convention qui fait marcher `&&`,
`||`, `if`, les scripts et la CI ; voir `erreurs.md` pour ce qu'elle vaut face aux exceptions.

| Code | Signification |
|---|---|
| `1` | échec générique : `false`, `grep` qui ne trouve rien |
| `126` | le fichier existe mais n'est pas exécutable |
| `127` | commande introuvable |
| `128 + n` | tué par le signal `n` : `130` pour Ctrl-C, `143` pour SIGTERM |

```sh
$ grep -q zzz f.txt; echo $?      # 1 : rien trouvé. Ce n'est pas un bug.
$ ./s.sh; echo $?                 # 126 : sh: ./s.sh: Permission denied
$ commandequinexistepas; echo $?  # 127 : command not found
$ make && ./programme             # ne lance rien si la compilation a raté
```

Le piège le plus courant est `grep` : ne rien trouver rend `1`, donc sous `set -e` ou en CI une
simple vérification fait échouer l'étape. Écris `grep motif f || true` si l'absence est acceptable.

## Rediriger et tuyauter

| Écriture | Effet |
|---|---|
| `> f` | stdout dans `f`, **fichier vidé au préalable** ; `>> f` ajoute à la fin |
| `< f` | `f` devient stdin |
| `2> f` | stderr dans `f` |
| `2>&1` | stderr part **là où stdout part à cet instant** |
| `a \| b` | stdout de `a` devient stdin de `b` |

`2>&1` se lit « branche 2 sur la destination actuelle de 1 » : elle est **positionnelle**, et doit
venir **après** la redirection de stdout.

```sh
$ ls f.txt absent > o.txt 2>&1     # o.txt contient l'erreur ET f.txt
$ ls f.txt absent 2>&1 > o.txt     # l'erreur reste à l'écran, o.txt n'a que f.txt
```

Dans le second cas, quand `2>&1` s'exécute, `1` pointe encore sur le terminal : `2` y reste
branché, et le `> o.txt` qui suit ne déplace que `1`. Autre piège : le shell ouvre les redirections
**avant** de lancer la commande, donc `sort tri.txt > tri.txt` vide le fichier. Enfin un tube rend
le code de son **dernier** maillon : `false | true` rend `0`, d'où `set -o pipefail`.

## De petits programmes qui se composent

La puissance ne vient pas des outils mais du **format commun** : du texte, une ligne par
enregistrement. Chaque programme fait une chose, lit des lignes, écrit des lignes ; personne n'a
prévu ta question, tu la fabriques. *Quels fichiers ont été le plus modifiés ?* :

```sh
$ git log --pretty=format: --name-only | grep -v '^$' | sort | uniq -c | sort -rn | head -5
  18 lua/polish.lua
  13 lua/plugins/astrocore.lua
```

Six maillons, cinq idées : produire des lignes, jeter les vides, regrouper, classer par fréquence,
s'arrêter. La forme `sort | uniq -c | sort -rn | head` répond à la moitié des questions
« qu'est-ce qui revient le plus » d'une vie de dev : IP d'un log, codes HTTP, extensions, mots.

## Les outils du quotidien

| Question | Outil | L'usage à 90 % |
|---|---|---|
| où est ce fichier | `find` | `find . -name '*.rs'` |
| qui contient ce mot | `grep` | `grep -rn "motif" .` |
| découper, calculer en colonnes | `cut`, `awk` | `cut -d: -f1,3`, `awk -F: '{s+=$2} END {print s}'` |
| ranger, compter | `sort`, `uniq -c`, `wc -l` | `sort -n`, `sort -u`, `sort \| uniq -c`, `wc -l < f` |
| remplacer | `sed` | `sed -E 's/vieux/neuf/g'` |

Trois pièges qui coûtent une heure à tout le monde. **`sort` trie du texte par défaut** : il rend
`10 100 2 9`, et c'est `sort -n` pour des nombres. **`uniq` ne supprime que les doublons
adjacents** : sans `sort` avant, `uniq -c` sur `b a b a` rend quatre lignes à 1. **Le motif de
`find -name` va entre guillemets** : sinon `find . -name *.txt` rend `unknown primary or operator`.

## Les espaces dans les noms de fichiers

La source numéro un des scripts cassés. Après avoir remplacé une variable, le shell **redécoupe le
résultat en mots**, sur les espaces, les tabulations et les retours à la ligne :

```sh
f="mon rapport.txt"
rm $f      # rm: mon: No such file or directory / rm: rapport.txt: No such file or directory
rm "$f"    # correct
```

Règle sans exception : **toute expansion de variable va entre guillemets doubles**. `"$f"`, `"$1"`,
`"$@"`, `"$(pwd)"` — et `"$@"` transmet les arguments tels quels là où `$@` les recolle puis les
redécoupe. Même problème entre `find` et `xargs`, qui se parlent par lignes : un nom à espace y
arrive en deux morceaux. Le couple correct passe par l'octet nul, qui est avec `/` le seul
caractère qu'un nom de fichier ne peut pas contenir : `find . -name '*.txt' -print0 | xargs -0 rm`,
ou `-exec rm {} +` qui évite l'intermédiaire.

## Environnement, PATH, et le shell qui n'est pas le tien

Une variable de shell n'est pas une variable d'environnement : `X=local` n'existe que dans le shell
courant, `export X` la met dans l'environnement, et **seuls les enfants lancés ensuite** la voient.
Un enfant ne modifie jamais l'environnement de son parent, d'où `source script.sh` et non
`./script.sh` quand des variables doivent rester. `PATH` est une liste de dossiers séparés par `:`,
parcourue **dans l'ordre** : tout « command not found » tient là, et `command -v` dit qui gagne.

Et le piège quotidien : **un script n'est pas ton terminal.** Un shell interactif lit tes fichiers
de configuration et connaît tes alias ; un script **bash** ne lit pas `~/.bashrc` et **n'expanse
aucun alias**, même définis dans le script — un `ll` qui marche chez toi rend
`ll: command not found`, code `127`. `shopt -s expand_aliases` le rétablit, et zsh, lui, les
expanse tout seul : encore une raison de ne pas compter dessus. Écris des fonctions, et passe en
argument ce que ton `.zshrc` fournissait.

## Les permissions

| Droit | Sur un fichier | Sur un dossier |
|---|---|---|
| `r` = 4 | lire le contenu | lister les noms |
| `w` = 2 | modifier | créer et supprimer dedans |
| `x` = 1 | **exécuter** | traverser, ouvrir ce qu'il contient |

Trois droits pour trois publics — propriétaire, groupe, autres — qu'on additionne : `rwx` = 7,
`rw-` = 6, `r-x` = 5, d'où `chmod 644` pour un fichier, `755` pour un programme ou un dossier,
`600` pour une clé privée ; les défauts viennent du `umask`, `022` en général. Le bit qui manque
toujours est `x` : un script neuf rend `Permission denied` et le code `126`, que `chmod +x` règle.

## Écrire un script qui ne ment pas

Un script silencieux qui échoue est pire qu'un script bruyant : il continue, et exécute la suite
**sur un état faux**. L'exemple canonique, qui rend `0` et se croit réussi :

```sh
cd /dossier/inexistant
rm -rf ./build          # supprime le build du dossier où tu étais, pas celui visé
```

D'où l'en-tête standard, `#!/usr/bin/env bash` puis `set -euo pipefail` : `-e` arrête dès qu'une
commande rend non-zéro ; `-u` arrête sur variable non définie, sans quoi `rm -rf "$RACINE/build"`
avec `RACINE` vide devient `rm -rf /build` ; `pipefail` évite qu'un échec de tube soit avalé.

Connais ses limites, sinon lui aussi te mentira : `set -e` **ne déclenche pas** dans une condition
de `if`, ni à gauche de `&&` ou `||`, ni dans un tube sans `pipefail`. Là où ça compte, teste
explicitement, et écris l'erreur sur `stderr` : `if ! ./etape; then echo "echec" >&2; exit 1; fi`.

## macOS n'est pas Linux

macOS embarque les outils **BSD**, Linux les outils **GNU** : mêmes noms, options différentes.

| Commande | macOS (BSD) | Linux (GNU) |
|---|---|---|
| édition en place | `sed -i '' 's/a/b/' f` | `sed -i 's/a/b/' f` |
| entrée vide | `xargs` ne lance rien | `xargs` lance une fois, `-r` pour l'éviter |
| date relative | `date -v+1d` | `date -d '+1 day'` |
| chemin absolu | `readlink -f` échoue si la cible n'existe pas | `readlink -f` réussit |

`sed -i` est le plus vicieux : sur macOS il prend `s/a/b/` pour un suffixe de sauvegarde, puis lit
le nom du fichier comme un script et rend `sed: 1: "f.txt": invalid command code f` — un message
qui change avec le nom du fichier. Sur GNU il cherche un fichier nommé `s/a/b/`. Pas de
forme commune : `sed ... f > tmp && mv tmp f`, ou les outils GNU (`brew install gnu-sed`).

## À retenir

1. Cinq prises : arguments, `stdin`, `stdout`, `stderr`, code de sortie. Le reste est plomberie.
2. `0` est la réussite, tout le reste un échec : `126` pas de bit `x`, `127` introuvable.
3. `> f 2>&1`, jamais l'inverse. Et `cmd f > f` vide `f` avant de le lire.
4. Le tube ne transporte que `stdout` et rend le code du dernier maillon : `set -o pipefail`.
5. `sort | uniq -c | sort -rn | head` répond à la moitié des questions de comptage.
6. Guillemets autour de chaque variable, et `find -print0 | xargs -0` dès qu'il y a des fichiers.
7. Un script n'a ni tes alias ni ton `.bashrc` : des fonctions, `set -euo pipefail`, erreurs `>&2`.
8. macOS c'est BSD, Linux c'est GNU. Vérifie `sed -i`, `xargs`, `date`, `readlink` avant de copier.
