# Déboguer

On passe bien plus de temps à lire et à comprendre du code cassé qu'à en écrire du neuf. Déboguer
n'est pas un à-côté du métier : c'est la moitié du métier, et la seule qu'on n'apprend nulle part.

C'est une **méthode**, pas un talent. Ceux qui trouvent vite ne devinent pas mieux que toi : ils
éliminent plus vite, parce qu'ils suivent une procédure même quand ils croient déjà savoir.

## La méthode en cinq temps

1. **Reproduire** de façon fiable. Tant que tu ne sais pas déclencher le bug à volonté, tu ne
   sauras pas non plus qu'il est corrigé.
2. **Réduire** au plus petit cas qui le déclenche encore.
3. **Former une hypothèse falsifiable** : une affirmation précise, qui prédit une observation
   capable de la tuer.
4. **La tester**, une seule chose à la fois.
5. **Corriger, puis verrouiller par un test** qui échoue avant la correction et passe après.

Le point 3 est celui qu'on rate. « Il y a un problème dans le parsing » n'est pas une hypothèse :
rien ne peut la contredire, donc rien ne peut la confirmer. « `lire_entete` renvoie une taille
nulle quand le fichier fait moins de 16 octets » en est une : un fichier de 8 octets tranche.
**Une hypothèse qu'on ne peut pas réfuter ne fait pas avancer, elle rassure.**

Corollaire du point 4 : une modification à la fois, sinon un résultat différent ne dit pas
laquelle a agi. Et sans le point 5, rien ne prouve que tu as traité la cause et non le symptôme.

## Reproduire

Un bug « non reproductible » est un bug dont tu n'as pas identifié l'entrée cachée. Cinq familles.

| Entrée cachée | À quoi ça ressemble |
|---|---|
| les données | un fichier sur dix mille contient une séquence que personne n'avait prévue |
| l'ordre | ça ne casse que si on supprime avant d'insérer |
| le temps | ça ne casse qu'à minuit, au changement d'heure, ou après 25 jours d'uptime |
| la concurrence | ça ne casse qu'une fois sur mille, et jamais sous le débogueur |
| l'environnement | ça ne casse que chez le client : autre version, autre locale, autre système |

Rendre le bug reproductible **est** le travail. Fige tout ce qui varie : graine du générateur
écrite dans le journal (`aleatoire.md`), horloge injectée au lieu d'être lue (`temps.md`), données
conservées, versions épinglées. Un bug qui tombe une fois sur mille devient traitable dès qu'une
boucle le déclenche vingt fois, et la version exacte où tu l'observes rend la bissection possible.

## Réduire : la bissection

Même idée que la recherche dichotomique, et de loin la technique la plus rentable du métier :
chaque essai divise l'espace par deux, donc mille candidats se traitent en dix essais et un
million en vingt. **Au code**, coupe la moitié du programme, garde celle qui casse encore,
recommence.

**Aux données.** Un fichier de 100 000 lignes fait planter le lecteur ? Garde la première moitié ;
si ça plante encore, recoupe, sinon prends l'autre. Dix-sept coupes suffisent, même à la main.

**À l'historique.** Git le fait seul. Sur un dépôt de 1000 commits où la régression est au 743e :

```bash
git bisect start HEAD <un-commit-connu-bon>
git bisect run ./test.sh   # 0 = bon, 1..127 sauf 125 = mauvais, 125 = intestable
```

`git bisect run` a lancé le test **10 fois** et désigné le commit exact. Le code 125 sert aux
commits qui ne compilent pas : `make || exit 125` les saute au lieu de fausser le verdict. Et si
tu sais quoi chercher, `git blame -L 12,12 f.c` et `git log -S "motif"` répondent directement.

## Le débogueur

Le modèle mental tient en cinq objets : le **point d'arrêt** (« arrête-toi ici », éventuellement
sous condition), l'**exécution pas à pas** (une ligne, entrer dans un appel, en ressortir), la
**pile d'appels** (qui a appelé qui pour arriver là), l'**inspection** (lire les variables et
évaluer des expressions dans le contexte arrêté), le **point d'observation** (« arrête-toi quand
cette donnée change », sans savoir qui la modifie).

Tout ce qui suit suppose une compilation avec `-g` (et `-O0` : à `-O2`, la moitié des variables a
disparu). Sans `-g`, le point d'arrêt reste `pending` sans message clair. Sur macOS, garde aussi
les `.o` ou lance `dsymutil` : le débogueur y lit les symboles depuis les objets, et les supprimer
suffit à tout perdre.

| Intention | lldb (macOS) | gdb (Linux) |
|---|---|---|
| point d'arrêt, conditionnel | `b f.c:7`, `br s -f f.c -l 7 -c "i == 4"` | `break f.c:7`, `break f.c:7 if i == 4` |
| ligne suivante, entrer, sortir | `n`, `s`, `fin` | `next`, `step`, `finish` |
| locales, expression | `v`, `p t[i]` | `info locals`, `print t[i]` |
| pile d'appels, continuer | `bt`, `c` | `backtrace`, `continue` |
| observer une donnée | `watchpoint set variable etat` | `watch etat` |

Le débogueur bat l'affichage dès que tu cherches **un état** plutôt qu'un flux. Sur une somme qui
lit un élément de trop, un point d'arrêt conditionnel donne la scène complète d'un coup :

```
frame #0: somme(t=0x10068ca80, n=4) at bug.c:7
(int) s = 6      (int) i = 4      (int) t[i] = -321120576
```

`s = 6` est juste, `i = 4` est hors du tableau : diagnostic en une ligne, et
`fin` donne la valeur de retour. Le point d'observation, lui, résout le cas le plus pénible, « qui
écrit cette variable ? » : lldb affiche `old value` / `new value`, et un `bt` nomme le coupable.

## Quand l'affichage reste le bon outil

**Les boucles chaudes.** Un point d'arrêt dans une boucle de trois cents millions de tours est
inutilisable ; un compteur qui n'affiche qu'à la millionième itération se lit très bien.

**La concurrence.** Un point d'arrêt gèle **tout le processus**, puis le relance : il détruit
l'ordonnancement, donc le bug ; un journal horodaté par thread reste la seule vue de
l'entrelacement. **L'embarqué, le noyau, le shader** n'ont souvent aucun débogueur attachable :
une UART, une LED, un tampon mémoire relu après coup.

Deux précautions. D'abord, **l'affichage se perd** : redirige la sortie standard vers un fichier,
plante juste après un `printf`, et le fichier est **vide** — cette sortie est tamponnée par blocs
dès qu'elle n'est pas un terminal. Écris sur la sortie d'erreur, ou force un `fflush`.

Ensuite, **l'affichage change le bug**. Deux threads incrémentent un compteur 200 000 fois chacun
sans verrou. En `-O0` le programme affiche environ 201 000 au lieu de 400 000 ; avec un `fprintf`
dans la boucle, 399 996 ; compilé en `-O1`, 400 000 pile. La course de données est pourtant
toujours là, et le sanitizer la dénonce dans les trois cas. C'est l'effet observateur : **un bug
qui disparaît quand tu l'observes n'est pas corrigé.**

## Les outils qui trouvent à ta place

| Outil | Ce qu'il attrape | Prix |
|---|---|---|
| `-fsanitize=address` | débordement, usage après libération, double libération | ~2x le temps, ~3x la mémoire |
| `-fsanitize=undefined` | débordement signé, décalage invalide, désalignement | quelques pourcents |
| `-fsanitize=thread` | courses de données | 5 à 15x, mesuré à 10x |
| Valgrind (Linux) | mémoire, sans recompiler | 20 à 50x |
| `clang --analyze` | chemins fautifs, sans exécuter | compilation seulement |
| `-Wall -Wextra -Werror` | tout ce que le compilateur sait déjà | zéro |

Ces outils ne devinent pas : ils **observent** ce que le programme fait vraiment. Une boucle qui
lit un élément de trop rend une somme fausse et sort avec le code 0 ; la même compilée avec
AddressSanitizer s'arrête net sur `heap-buffer-overflow bug.c:5 in somme`, avec la pile de la
lecture et celle de l'allocation. Trois détails : UndefinedBehaviorSanitizer **signale et
continue** par défaut, code de sortie 0, donc ajoute `-fno-sanitize-recover=all` en intégration
continue ; ASan et TSan sont incompatibles, deux constructions séparées ; l'analyse statique voit
ce que les avertissements ratent, un usage après libération muet en `-Wall -Wextra` étant signalé
par `clang --analyze` en une ligne. Le plus rentable reste gratuit : traiter les avertissements
comme des erreurs (`compilation.md`), puis assertionner, ce qui change une corruption silencieuse
en arrêt immédiat au bon endroit — en sachant que `-DNDEBUG` supprime toutes les assertions.

## Les pièges de raisonnement

| Piège | La phrase qui le tue |
|---|---|
| le lampadaire | tu cherches où c'est confortable ; classe les suspects par probabilité |
| « ça ne peut pas être ça » | c'est souvent ça, et la tester coûte cinq minutes |
| corriger le symptôme | une garde de trop change un plantage franc en corruption (`erreurs.md`) |
| le biais de confirmation | écris ce que tu t'attends à voir avant de regarder |

## Le bug est dans ton code

La probabilité que la faute soit dans le compilateur, la bibliothèque standard ou le processeur
n'est pas nulle — ces bugs existent et sont publiés — mais elle est minuscule devant celle que la
faute soit dans ce que tu as écrit cette semaine. L'ordre de suspicion :

1. le code que tu viens de modifier ;
2. le code que tu as écrit il y a longtemps et que tu crois connaître ;
3. ta configuration : options de compilation, versions mélangées, cache de build pas invalidé ;
4. une bibliothèque peu utilisée, ou utilisée d'une façon inhabituelle ;
5. une bibliothèque très utilisée, le compilateur, le système ;
6. le matériel.

Le piège classique se déguise en bug de compilateur : un programme qui marche en `-O0` et casse en
`-O2`. Cette boucle affiche « 31 tours » sans optimisation ; en `-O2`, clang compile tout `main` en
**une seule instruction de trap**, et le programme meurt instantanément.

```c
for (int i = 1; i > 0; i += i) n++;
```

Le compilateur a raison : un débordement d'entier signé est un comportement indéfini, donc il peut
supposer que `i > 0` reste vrai pour toujours. Un `-fsanitize=undefined` nomme la ligne en une
seconde. **Quand `-O2` casse ce que `-O0` fait marcher, soupçonne d'abord ton propre comportement
indéfini.** Et si tu crois tenir un bug d'outil, réduis-le à dix lignes autonomes : si tu y
arrives, tu as un bon rapport de bug ; sinon, c'est qu'il était chez toi.

## À retenir

1. Déboguer est une méthode, pas un talent : reproduire, réduire, hypothèse, test, verrou.
2. Une hypothèse qu'aucune observation ne peut réfuter ne sert à rien.
3. Un bug non reproductible se rend reproductible d'abord : fige graine, horloge, données, version.
4. Bissection partout — code, données, historique. Mille candidats en dix essais.
5. Le débogueur gagne dès que tu cherches un état ; le point d'observation dit qui écrit.
6. Un bug qui disparaît quand tu ajoutes un affichage n'est pas corrigé.
7. Sanitizers, analyse statique et `-Werror` trouvent gratis ce que tu chercherais des heures.
8. Le bug est dans ton code. `-O0` qui marche et `-O2` qui casse, c'est du comportement indéfini.
