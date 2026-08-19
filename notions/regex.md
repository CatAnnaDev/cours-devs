# Les expressions régulières

Une expression régulière ne cherche pas un texte : elle **décrit une forme**, un ensemble de
chaînes. `[0-9]{4}-[0-9]{2}` ne désigne pas `2026-08` mais le million de chaînes de cette forme.
On n'écrit pas ce qu'on veut trouver : on écrit la règle à laquelle tout ce qu'on veut trouver
obéit, et à laquelle tout le reste doit désobéir.

## Les six briques

| Brique | Exemple | Trouve dans | Ne trouve rien dans | Autres écritures |
|---|---|---|---|---|
| **littéral** | `chat` | `chaton` | `Chat` | — |
| **classe** | `[aeiou]` | `bal` | `xyz` | `[a-z]`, `[^0-9]`, `.` |
| **quantificateur** | `a{2,4}` | `baaad` | `bad` | `a*`, `a+`, `a?` |
| **ancre** | `\bchat\b` | `le chat` | `chateau` | `^`, `$` |
| **alternative** | `gris\|noir` | `chat noir` | `chat bleu` | — |
| **groupe** | `(ab){2}` | `ababX` | `abX` | — |

Une colonne « ne trouve rien » ne vaut que pour la forme exacte de la colonne « exemple » : `a*`
accepte la chaîne vide, donc il trouve quelque chose partout, et `\b` seul trouve les bords de
`chateau`. C'est déjà la gourmandise qui pointe.

Il n'y en a pas d'autres. `.` est une classe, « tout caractère **sauf** le saut de ligne ».
`[a-z]` est un intervalle de points de code : il ne contient pas `é`, dans aucun moteur. Et `\b`
n'est pas un caractère mais une position — une ancre ne consomme rien.

## La gourmandise, le piège numéro un

Un quantificateur est **gourmand** : `.*` avale jusqu'à la fin de la ligne, puis rend un caractère
à la fois jusqu'à ce que la suite du motif passe.

```
texte      <b>gras</b> et <i>italique</i>
<.*>       ['<b>gras</b> et <i>italique</i>']    une seule correspondance : tout
<.*?>      ['<b>', '</b>', '<i>', '</i>']        quantificateur paresseux
<[^>]*>    ['<b>', '</b>', '<i>', '</i>']        classe négative
```

Deux remèdes, qui ne se valent pas. Le **paresseux** `*?` prend le minimum et n'en reprend que si
nécessaire : le moteur essaie et défait toujours, et il s'arrête au premier délimiteur, pas au
bon — `<div>.*?</div>` sur `<div><div>a</div></div>` renvoie `<div><div>a</div>`, déséquilibré.
La **classe négative** `[^>]*` ne peut pas franchir le délimiteur : plus rien à défaire. Sur 2000
blocs de 200 caractères en Python 3.14, `<div>.*?</div>` prend 1,79 ms et `<div>[^<]*</div>`
0,23 ms. Quand tu peux nommer ce que le motif n'a **pas** le droit de traverser, écris-le.

## Groupes et substitution

| Forme | Rôle |
|---|---|
| `(…)` | capturant, numéroté par l'ordre de sa parenthèse **ouvrante** |
| `(?:…)` | non capturant : quantifier ou alterner sans réserver un numéro |
| `(?<nom>…)` | nommé — Python veut `(?P<nom>…)` |

Le non capturant n'est pas de la coquetterie : chaque capture coûte une paire d'index et décale la
numérotation de toutes les suivantes. Dès trois captures, nomme-les.

`\1` a **deux sens**. Dans le motif, c'est une référence arrière : « exactement ce qu'a capturé le
groupe 1 ». Dans le remplacement, c'est le texte capturé, et la syntaxe change selon l'outil :
`\1` en sed et Python, `$1` en Perl, JavaScript et .NET, `\g<jour>` pour un nommé en Python.

```sh
printf 'abab\nabcd\n' | grep '\(ab\)\1'      # ne garde que abab
echo 2026-08-19 | sed -E 's/([0-9]{4})-([0-9]{2})-([0-9]{2})/\3\/\2\/\1/'
19/08/2026
```

## Les dialectes

| | BRE (POSIX) | ERE (POSIX) | PCRE et langages |
|---|---|---|---|
| groupe, répétition bornée | `\(…\)` et `a\{2,4\}` | `(…)` et `a{2,4}` | comme ERE |
| `+` et `?` | **littéraux** | opérateurs | opérateurs |
| alternative | absente (`\\|` en extension GNU) | `\|` | `\|` |
| `\d` `\w` `\s` | non définis | non définis | oui |
| non capturant, paresseux | non | non | `(?:…)`, `.*?` |
| groupe nommé | non | non | `(?<n>…)`, Python `(?P<n>…)` |

BRE, c'est `grep` et `sed` nus, où tout ce qui est puissant s'échappe. ERE, c'est `grep -E`,
`sed -E`, `awk`. PCRE, c'est `grep -P` — GNU seulement, macOS 27 répond `invalid option -- P` —
Perl, PHP, et l'esprit de Python, Java, .NET, JavaScript, Rust et Go.

Méfie-toi des extensions : sur macOS 27, `/usr/bin/grep -E` accepte `\d`, `(?:…)`, `(?i)` et
`.*?`, dont POSIX ne définit aucun. Ça marche chez toi et pas sur la machine d'à côté ; dans un
script portable, `[[:digit:]]` plutôt que `\d`.

La différence la plus vicieuse n'est pas syntaxique. Sur `chateau`, `chat|chateau` donne `chat`
avec un moteur à retour arrière — Python, Perl, JavaScript, la crate Rust `regex` : la première
alternative qui passe gagne. Il donne `chateau` avec `grep -oE`, qui suit POSIX : la plus longue
gagne. Range tes alternatives de la plus longue à la plus courte, le problème disparaît partout.

## Les drapeaux

| Drapeau | En ligne | Effet |
|---|---|---|
| `i` | `(?i)` | insensible à la casse |
| `m` | `(?m)` | `^` et `$` valent à chaque ligne, pas seulement aux bouts du texte |
| `s` | `(?s)` | `.` matche aussi le saut de ligne |
| `x` | `(?x)` | espaces ignorés, `#` ouvre un commentaire |

Sur `"Ligne1\nligne2"` en Python, `^ligne\d` avec `i` seul trouve `Ligne1` ; avec `i` **et** `m`,
les deux. Sans `s`, `e1.l` ne trouve rien ; avec `s`, il trouve `e1\nl`. Le mode verbeux est le
seul moyen honnête d'écrire un motif long — mais une espace littérale y devient `\ ` ou `[ ]` :

```python
DATE = re.compile(r"""
    (?P<annee>\d{4}) -   # annee sur quatre chiffres
    (?P<mois>\d{2})  - (?P<jour>\d{2})
""", re.X)
```

## Le retour arrière catastrophique

`(a+)+b` appliqué à une suite de `a` sans `b` final. Pour conclure à l'échec, le moteur doit
essayer **toutes** les façons de répartir les `a` entre le groupe intérieur et le groupe
extérieur : `2^(n-1)` découpes. Mesuré avec `re.search` en Python 3.14 :

| Nombre de `a` | 16 | 20 | 24 | 26 | 28 |
|---|---|---|---|---|---|
| Temps | 0,003 s | 0,045 s | 0,70 s | 2,8 s | **11,3 s** |

Chaque `a` ajouté double le temps ; Node 24 donne 6,9 s sur les mêmes 28 caractères, et à quarante
`a` on dépasse les dix heures pour une entrée de quarante octets. C'est une faille de déni de
service (CWE-1333, « ReDoS ») qui n'exige pas que l'expression vienne de l'utilisateur : il suffit
qu'un motif ambigu soit appliqué à une **chaîne** qu'il contrôle. Un champ de formulaire suffit.

1. **Lever l'ambiguïté.** `(a+)+b` s'écrit `a+b`, qui répond en 0,00005 s à 28 `a`. La cause est
   toujours la même : un quantificateur dans un quantificateur, aux portées qui se recouvrent.
2. **Interdire le retour arrière** — groupe atomique `(?>a+)+b`, quantificateur possessif `a++b` :
   0,000003 s. En Perl, PCRE, Java, et en Python depuis 3.11.
3. **Changer de moteur.** RE2 (Go, C++) et la crate Rust `regex` compilent vers un automate :
   `rg '(a+)+b'` répond en 10 ms sur quarante `a`, mais sans référence arrière ni lookaround.
4. **Un délai maximum** si le motif n'est pas modifiable : `Regex` en .NET prend un `matchTimeout`.

## Quand ne pas en écrire

Une expression régulière décrit un langage *régulier* : reconnu par un automate à nombre **fini**
d'états. Compter une imbrication demande une mémoire non bornée, donc aucune expression ne dit si
des parenthèses sont équilibrées à profondeur quelconque. Vérifié : `\(([^()]|\([^()]*\))*\)`
accepte `(a(b)c)` en entier et refuse `(a(b(c)d)e)` en entier — attention, ce n'est vrai qu'avec
une correspondance **totale** : en recherche de sous-chaîne il y trouve encore `(b(c)d)`. On ajoute
toujours un niveau à la main, jamais tous.

- **HTML, XML** : parseur. Même sans imbrication le motif casse — `<[^>]*>` sur `<a
  href="a>b">lien</a>` renvoie `<a href="a>`, car `>` est légal dans un attribut.
- **JSON** : parseur, voir `json.md`. Idem pour tout format imbriqué : YAML, S-expressions, code.
- **Une adresse de courriel complète** : RFC 5322 admet des commentaires imbriqués et des parties
  entre guillemets. Le motif naïf `[^@\s]+@[^@\s]+\.[a-z]{2,}`, en correspondance totale, rejette
  `"a b"@exemple.fr` et `anna@localhost`, tous deux valides — et en simple recherche il accepte la
  première par un morceau, `b"@exemple.fr`, ce qui est pire. La seule validation qui prouve quelque
  chose est l'envoi d'un message de confirmation.

PCRE sait récurser — Perl accepte `^(\((?:[^()]|(?1))*\))$` et équilibre correctement — mais ce
n'est plus une expression régulière : c'est un parseur écrit dans le pire langage pour ça. Reste
légitime : extraire un morceau d'un fragment **déjà** isolé par un vrai parseur.

## L'unicode dans une expression

`\w`, `\b` et `[[:alpha:]]` dépendent de ce que le moteur appelle « lettre », et ce n'est pas la
même chose partout. Sur `café` :

| Outil | `\w+` donne |
|---|---|
| Python 3 sur `str` | `café` |
| Python 3 sur `bytes`, ou drapeau `re.A` | `caf` |
| JavaScript | `caf` — `\w` y vaut toujours `[A-Za-z0-9_]` |
| Perl avec `-CSD -Mutf8` | `café` |
| `/usr/bin/grep -oE` | `café` en locale UTF-8, `caf` avec `LC_ALL=C` |

La même commande, deux résultats selon une variable d'environnement. Et `[a-zà-ÿ]` est une
rustine : elle rate `Œ`, attrape `÷` qui traîne dans l'intervalle, et dépend de l'ordre des points
de code. La bonne réponse, ce sont les **propriétés Unicode** : `\p{L}` toute lettre, `\p{Lu}` les
majuscules, `\p{N}` les nombres, `\p{Script=Greek}` une écriture. Disponibles en Perl, PCRE, Java,
.NET, Rust, et en JavaScript avec le drapeau `u` ; le module `re` de Python ne les a pas et répond
`bad escape \p`, il faut le module tiers `regex`. Dernier rappel : `.` consomme un point de code,
pas un caractère perçu — `é` en NFD vaut deux `.`. Voir `unicode.md`.

## À retenir

1. Une expression décrit un ensemble de chaînes, pas une chaîne : on écrit une forme.
2. `.*` avale tout puis recule. Quand tu peux nommer l'interdit, `[^x]*` bat `.*?`.
3. `(?:…)` pour grouper sans capturer ; au troisième groupe, nomme-les.
4. Entre dialectes bougent les échappements, `\d`, `.*?`, les groupes nommés, et l'alternance.
5. `(a+)+b` sur 28 `a` : 11 secondes. Un quantificateur dans un quantificateur est une faille.
6. Parades : lever l'ambiguïté, groupe atomique, ou moteur à automate (RE2, Rust).
7. Une grammaire imbriquée n'est pas régulière : HTML, JSON, courriel, c'est un parseur.
8. `\w` et `\b` dépendent de la locale et du moteur ; `\p{L}` est la réponse portable.
