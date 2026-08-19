# 11 — Les comportements indéfinis

Le chapitre 00 en donnait la définition en cinq lignes. Celui-ci la déplie : ce que dit la norme,
ce que le compilateur en fait, ce que les sanitizers en voient, et ce qu'ils n'en voient pas. Tout
ce qui suit a été mesuré sur la plateforme de référence du cours, **arm64 macOS avec Apple clang
21** ; quand une autre machine répond autre chose, c'est dit.

## Trois voisins qu'on confond

**Comportement indéfini** (*undefined behavior*) : la norme n'impose **aucune exigence**. Ni sur le
résultat de l'opération, ni sur la suite du programme, ni sur ce qui l'a précédée. Une seule
construction indéfinie exécutée prive le programme entier de sens : l'indéfini n'est pas localisé.

**Comportement non spécifié** (*unspecified*) : la norme donne plusieurs possibilités, laisse
choisir, et n'oblige à documenter aucun choix. Le programme reste valide.

**Défini par l'implémentation** (*implementation-defined*) : même chose, mais le compilateur **doit
documenter** son choix. Le résultat est prévisible dès qu'on a fixé le compilateur et la machine.

| Catégorie | Exemple | Ce qui est garanti |
|---|---|---|
| **indéfini** | `INT_MAX + 1` | rien, y compris pour le reste du programme |
| **non spécifié** | l'ordre d'évaluation de `f() + g()` | les deux sont appelés, dans un ordre non dit |
| **défini par l'implémentation** | `sizeof(int)`, le signe de `char` | une valeur documentée, stable sur une plateforme |

Vérifié au compilateur : `sizeof(int)` vaut 4 sur arm64 macOS, sur x86-64 Linux et sur arm64
Linux, où `char` est en revanche **non signé** alors qu'il est signé sur les deux premières
(`CHAR_MIN` y vaut -128). Un `char` qui vaut -1 chez toi vaut 255 chez ton collègue, et c'est
conforme.

Le piège du milieu se joue à un mot près. L'ordre d'évaluation de `f() + g()` est **non spécifié**,
et il le reste même si `f` et `g` modifient la même variable : les deux corps ne s'entrelacent
jamais, l'un s'exécute entièrement avant l'autre, on ne sait simplement pas lequel. Pour basculer
dans l'indéfini il faut des effets de bord **non séquencés** sur le même objet, ce qui demande
qu'ils soient dans la même expression sans appel de fonction pour les séparer : `i = i++ + 1`,
`t[i] = i++`, `f(i++) + g(i++)`.

## Pourquoi la norme en a

Parce qu'elle décrit une **machine abstraite** et que les machines réelles ne sont pas d'accord.
Voici ce que devient `return (int)d;` sous Apple clang 21, en changeant seulement la cible.

```
arm64    :  fcvtzs    w0, d0
x86-64   :  cvttsd2si %xmm0, %eax
```

Deux instructions, deux réponses. `fcvtzs` **sature** : `(int)1e18` donne 2147483647, `(int)-1e18`
donne -2147483648, `(int)NaN` donne 0 — mesuré sur arm64. `cvttsd2si` renvoie la « valeur entière
indéfinie » 0x80000000, soit `INT_MIN`, pour tout ce qui sort du domaine.

Si la norme tranchait, elle condamnerait une des deux familles à un test et une correction à
**chaque conversion**. Même raisonnement pour le débordement signé, le décalage plus large que le
type, l'accès mal aligné : la case vide permet au C d'être rapide **partout**.

## Ce que le compilateur en fait vraiment

Le compilateur ne choisit pas « un comportement raisonnable » pour les cas indéfinis : il **suppose
qu'ils n'arrivent pas**, et s'en sert comme d'un théorème pour optimiser autour.

### Le test de nullité qui disparaît

```c
int lire(int *p) {
    int valeur = *p;            // si p etait nul, on est deja dans l'indefini
    if (p == NULL) { return -1; }
    return valeur;
}
```

Compilé avec `cc -O2 -S`, la fonction entière tient en deux instructions — `ldr w0, [x0]` puis
`ret` — et le `if` a disparu. Le raisonnement : « déréférencer un pointeur nul est indéfini, donc
`p` n'est pas nul à la ligne suivante, donc ce test est toujours faux, donc je le supprime. » À
`-O0`, la même fonction fait dix-sept instructions et contient bien le `cbnz` qui teste `p`. **Le
code de protection existe en débogage et disparaît en production.**

### La boucle qui devient infinie

```c
void afficher(int depart) {
    for (int i = depart; i > 0; i++) { putchar('.'); }
}
```

À `-O2`, le corps de boucle devient `mov w0, #46` / `bl _putchar` / `b LBB0_1` : plus de compteur,
plus d'incrémentation, plus de comparaison, juste un saut inconditionnel en arrière. Le
raisonnement : « `i` part d'une valeur positive, il ne fait qu'augmenter, et le débordement signé
est indéfini donc impossible ; donc `i > 0` est toujours vrai. » Avec `-fwrapv`, la même
compilation garde le compteur (`adds w19, w19, #1` puis `b.lo`) et sort correctement.

Un cran plus loin : `for (int i = 2147483640; i > 0; i++) compteur++;` suivi d'un `printf` affiche
`8` à `-O0`, et à `-O2` compile `main` en **une seule instruction**, `brk #0x1` — mort immédiate
sur `SIGTRAP`, code 133, sans rien afficher. Un comportement indéfini ne produit pas « une valeur
bizarre », il produit un **programme différent**.

## Le catalogue des fautes classiques

Diagnostics obtenus à `-O0` avec `-fsanitize=address,undefined -Wall -Wextra`, et
`ASAN_OPTIONS=detect_stack_use_after_return=1` à l'exécution — c'est ce que pose `clings`, et sans
cette option-là le pointeur pendouillant passe inaperçu.

| Faute | Exemple | Diagnostic obtenu |
|---|---|---|
| débordement signé | `INT_MAX + 1` | UBSan `signed integer overflow: 2147483647 + 1 cannot be represented in type 'int'` |
| décalage trop grand | `1 << 32` | UBSan `shift exponent 32 is too large for 32-bit type 'int'` |
| décalage négatif | `1 << -1` | UBSan `shift exponent -1 is negative` |
| décalage qui déborde | `32 << 30` | UBSan `left shift of 32 by 30 places cannot be represented in type 'int'` |
| `INT_MIN / -1` | quotient non représentable | UBSan `division of -2147483648 by -1 cannot be represented in type 'int'` |
| division ou modulo par zéro | `5 % 0` | UBSan `division by zero` |
| flottant hors domaine | `(int)1e18` | UBSan `1e+18 is outside the range of representable values of type 'int'` |
| accès hors bornes | `t[4]` sur `int t[4]` | UBSan `index 4 out of bounds for type 'int[4]'`, puis ASan `stack-buffer-overflow` |
| lecture non initialisée | `int x; printf("%d", x);` | **rien** à l'exécution ; seulement `-Wuninitialized` |
| pointeur pendouillant | renvoyer `&local` | ASan `stack-use-after-return`, et `-Wreturn-stack-address` |
| alias strict | écrire par `float*`, relire par `int*` | **rien** |
| adresse mal alignée | lire un `int` à l'offset 1 | UBSan `load of misaligned address ... requires 4 byte alignment` |
| non-`void` sans `return` | sortir par le bas | **rien** à l'exécution ; seulement `-Wreturn-type` (et ce n'est indéfini que si l'appelant utilise la valeur) |
| `_Bool` invalide | y écrire 7 par `memcpy` | UBSan `load of value 7, which is not a valid value for type '_Bool'` |
| `enum` avec une valeur hors liste | y écrire 9 par `memcpy` | **rien, et c'est normal** : en C la valeur tient dans le type entier compatible, il n'y a pas d'indéfini. C'est en C++ que la règle contraint la plage |
| modification multiple | `i = i++ + 1;` | **rien** à l'exécution ; seulement `-Wunsequenced` |
| pointeur de fonction faux | appeler `int(int)` comme `void(void)` | **rien**, même avec `-fsanitize=function` |

**L'alias strict** mérite son code. La norme n'autorise l'accès à un objet qu'à travers un pointeur
d'un type incompatible — les types caractère, `char`, `signed char` et `unsigned char`, étant les
exceptions — et le compilateur s'en sert :

```c
int reinterprete(int *entier, float *reel) {
    *entier = 1;
    *reel = 2.0f;
    return *entier;             // relit-il vraiment ?
}
```

À `-O2`, la valeur renvoyée est la constante `1` : le compilateur a décidé que `*reel` ne pouvait
pas toucher `*entier`. Avec `-fno-strict-aliasing`, il émet un vrai `ldr` et relit. Aucun sanitizer
ne dit rien dans les deux cas ; le seul symptôme est un résultat faux à `-O2`.

Trois autres lignes méritent un mot, et c'est mesuré. Le contrôle `return` d'UBSan ne vaut
qu'en C++ : la fonction sans `return` a rendu ce qui traînait dans le registre de retour.
`int x; printf("%d", x);` a affiché une valeur de pile arbitraire — refais la mesure, tu en auras
une autre — et un
bloc `malloc` non initialisé a donné `-1094795586`, soit `0xBEBEBEBE`, le motif de remplissage
d'ASan. Enfin, l'appel de fonction mal typé n'a fait broncher ni `-fsanitize=undefined` ni
`-fsanitize=function`.

## Ce que chaque sanitizer attrape, et ce qu'il rate

| Détecteur | Domaine | Disponible ici |
|---|---|---|
| **ASan** (`-fsanitize=address`) | bornes, `use-after-free`, double `free`, `use-after-return` | oui |
| **UBSan** (`-fsanitize=undefined`) | arithmétique, décalages, alignement, pointeur nul, bornes statiques | oui |
| **MSan** (`-fsanitize=memory`) | lectures non initialisées | **non** |
| **LSan** (`-fsanitize=leak`) | blocs jamais libérés | **non** |

Les deux dernières lignes ne sont pas une approximation. Apple clang 21 répond `clang: error:
unsupported option '-fsanitize=memory' for target 'arm64-apple-darwin27.0.0'`, idem pour `leak` ;
`ASAN_OPTIONS=detect_leaks=1` répond `detect_leaks is not supported on this platform.` D'où
`suivi_malloc` et `VERIFIE_PAS_DE_FUITE()` dans `verif.h`.

`cc -fsanitize=undefined -###` montre les dix-sept contrôles activés : `alignment`, `array-bounds`,
`bool`, `builtin`, `enum`, `float-cast-overflow`, `integer-divide-by-zero`, `nonnull-attribute`,
`null`, `pointer-overflow`, `return`, `returns-nonnull-attribute`, `shift-base`, `shift-exponent`,
`signed-integer-overflow`, `unreachable`, `vla-bound`.

Deux absences. `float-divide-by-zero` n'y est pas : `1.0 / 0.0` produit `inf` sans un mot, alors
que C17 6.5.5 la classe comme indéfinie tant que l'annexe F n'est pas en vigueur, et qu'Apple clang
ne définit pas `__STDC_IEC_559__` en `-std=c17`. Et `unsigned-integer-overflow` non plus, parce que
le débordement **non signé** n'est pas indéfini : `4294967295u + 1u` vaut 0, c'est dans la norme.

### Passer les sanitizers ne prouve rien

- **Ils n'inspectent pas le code, ils surveillent une exécution.** Une branche jamais prise n'est
  jamais vérifiée : un programme qui passe cent tests peut casser au cent-unième.
- **Ils dépendent des données.** Le débordement de la section suivante n'apparaît qu'avec des
  mesures assez grandes ; avec des petites, tout est vert.
- **Cinq fautes du catalogue ne sont surveillées par aucun sanitizer ici** : alias strict, lecture
  non initialisée, modification multiple, `return` manquant, pointeur de fonction. Et une sixième,
  le pointeur pendouillant, ne l'est qu'à condition de poser `ASAN_OPTIONS`.
- **Ils coûtent cher.** Sur un micro-test arithmétique à `-O0`, quatre mesures : 0,83 s sans rien,
  1,99 s avec ASan seul, 5,44 s avec UBSan seul, 5,99 s avec les deux. Le facteur dépend beaucoup
  du code : sur un autre micro-test de la même machine il tombe à 1,0 et 3,0. Mesure le tien.

## Lire un rapport UBSan ligne par ligne

Un cas réaliste plutôt qu'un cas d'école : une moyenne dont la somme déborde.

```c
static int moyenne(const int *mesures, int nombre) {
    int total = 0;
    for (int i = 0; i < nombre; i++) {
        total += mesures[i];                    // ligne 6
    }
    return total / nombre;
}

int main(void) {
    int mesures[4] = { 2000000000, 2000000000, 1, 1 };
    printf("%d\n", moyenne(mesures, 4));        // ligne 13
    return 0;
}
```

Compilé comme le fait `clings`, il produit exactement ceci :

```
moyenne.c:6:15: runtime error: signed integer overflow: 2000000000 + 2000000000
cannot be represented in type 'int'
SUMMARY: UndefinedBehaviorSanitizer: undefined-behavior moyenne.c:6:15
```

| Morceau | Ce qu'il dit |
|---|---|
| `moyenne.c:6:15` | fichier, **ligne**, **colonne** — la colonne 15 est le `+=` lui-même |
| `runtime error` | diagnostic d'exécution, pas de compilation |
| `signed integer overflow` | **quel** contrôle a déclenché, ici `signed-integer-overflow` |
| `2000000000 + 2000000000` | les **opérandes réelles** au moment de la faute |
| `cannot be represented in type 'int'` | pourquoi c'est une faute, et dans quel type |
| `SUMMARY:` | la ligne récapitulative, faite pour être grepée |

Le programme s'arrête là, code de retour 134 (`SIGABRT`), parce que `clings` compile avec
`-fno-sanitize-recover=undefined`. Sans cette option, UBSan aurait imprimé le même message **et
continué**, en affichant une moyenne fausse dans l'indifférence générale.

UBSan ne donne pas de trace de pile par défaut, contrairement à ASan. Lancé avec
`UBSAN_OPTIONS=print_stacktrace=1`, il intercale la pile d'appels entre l'erreur et le `SUMMARY` :

```
    #0 0x000100494c14 in moyenne moyenne.c:6
    #1 0x000100494990 in main moyenne.c:13
    #2 0x000186edbe84 in start+0x1a1c (dyld:arm64e+0x31e84)
```

On lit maintenant **qui a appelé quoi** ; sur une fonction utilitaire appelée de trente endroits,
c'est la seule information qui compte. La correction n'est pas de mettre `long` partout : c'est de
choisir un accumulateur assez large, ici `long long total = 0;`.

## Les options qui comptent

| Option | Effet |
|---|---|
| `-fsanitize=undefined` | les dix-sept contrôles listés plus haut |
| `-fsanitize=address` | la mémoire : bornes, `use-after-free`, `use-after-return` |
| `-fno-sanitize-recover=undefined` | arrêter au **premier** diagnostic au lieu de continuer |
| `-fwrapv` | l'addition, la soustraction et la multiplication signées bouclent en complément à deux : plus d'indéfini là ; la division `INT_MIN / -1` reste à ta charge |
| `-ftrapv` | l'addition, la soustraction et la multiplication signées **piègent** à l'exécution ; la division `INT_MIN / -1` n'est pas couverte |
| `-fno-strict-aliasing` | le compilateur cesse de déduire des choses des types de pointeurs |
| `-Wall -Wextra` | ce qu'aucun sanitizer ne voit ici |

**`-fwrapv` et `-ftrapv` ne font pas la même chose.** Le premier définit le débordement : avec lui,
`INT_MAX + 1` a donné -2147483648 et le code de retour 0. Le second l'attrape : le même programme
est mort sur `SIGTRAP`, que le shell rapporte en code 133. Chez GCC, `-ftrapv` passe par `abort()`
et donne `SIGABRT`. Jamais les deux options ensemble.

**Combien coûtent `-fwrapv` et `-fno-strict-aliasing` ?** Sur les deux micro-tests mesurés ici, un
filtre flottant et une boucle entière, **rien de mesurable** : 0,035 s et 0,129 s dans toutes les
combinaisons. Le coût est réel, de l'ordre de quelques pourcents, mais il dépend entièrement du
code. Mesure sur **ton** programme avant de payer ou de refuser de payer.

**Les avertissements ne sont pas un supplément.** Quatre fautes du catalogue ne sont attrapées que
par eux : `-Wreturn-type`, `-Wuninitialized`, `-Wunsequenced`, `-Wreturn-stack-address`. Et
`-Warray-bounds` attrape `t[7]` sur un `int t[4]` mais **pas** `bloc[9]` sur un bloc de quatre
`int` alloué par `malloc` : seul le tableau statique a sa taille dans son type.

## Comment ne pas en écrire

**Calcule les indices et les tailles en `size_t`.** Il est non signé, son débordement est donc
défini, et c'est le type que rendent `sizeof` et `strlen`.

**Teste avant de diviser, les deux cas.** `if (n != 0)` ne suffit pas en signé, il reste
`INT_MIN / -1` : le test complet est `if (n != 0 && !(a == INT_MIN && n == -1))`.

**Ne décale jamais d'une valeur non bornée.** `x << n` exige `0 <= n < 32` pour un `int` de 32
bits, et que le résultat rentre dans le type. Pour les bits, travaille en `unsigned`.

**Initialise à la déclaration.** `int x = 0;` plutôt que `int x;` suivi d'un `if` qui remplit
parfois : le compilateur supprime l'affectation morte, et la faute muette disparaît.

**`memcpy` plutôt que le jeu de mots sur les types.** Lire les bits d'un flottant par
`memcpy(&motif, &reel, sizeof motif)` est légal, portable, et compile en une seule instruction à
`-O2` : `fmov w0, s0`.

**Compile avec les sanitizers pendant tout le développement**, pas seulement quand ça casse : un
sanitizer allumé en permanence transforme « ça plante parfois » en « ligne 6, colonne 15 ».

**Mets les tests instrumentés dans l'intégration continue.** C'est là que les chemins rares
finissent par être parcourus, et le seul endroit où le surcoût des sanitizers n'a aucune
importance.

## À retenir

1. Indéfini, non spécifié, défini par l'implémentation : trois choses distinctes, une seule grave.
2. Indéfini ne veut pas dire « valeur imprévisible » : le compilateur suppose que le cas n'arrive
   pas et réécrit le code autour de cette supposition.
3. Vérifié à `-O2` : un test de nullité disparaît, une boucle devient un saut inconditionnel, un
   `main` entier devient `brk #0x1`.
4. Ces cases sont vides parce que les machines diffèrent : `fcvtzs` sature là où `cvttsd2si`
   renvoie `INT_MIN`.
5. UBSan couvre l'arithmétique, ASan la mémoire ; MSan et LSan n'existent pas sur macOS ARM.
6. Alias strict, lecture non initialisée, modification multiple, `return` manquant : rien ne les
   voit à l'exécution ici, seuls les avertissements les attrapent.
7. Passer les sanitizers n'est pas une preuve : ils ne voient que les chemins réellement exécutés,
   avec les données réellement fournies.

**Exercices : `11_ub`.**
