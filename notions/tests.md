# Les tests

Un test ne prouve pas qu'un programme est correct. Il prouve qu'un cas précis donne le bon
résultat, et rien de plus : tester montre la présence de bugs, jamais leur absence.

Alors à quoi sert vraiment une suite de tests ? À **pouvoir changer le code sans peur**. C'est la
seule thèse de cette page : un test qui ne te rend pas plus libre de refactoriser ne rapporte rien,
et il coûte deux fois — à écrire, puis à réparer à chaque changement. Le critère est toujours le
même : *me laisse-t-il réécrire l'intérieur sans casser le comportement qu'il surveille ?*

## Anatomie d'un bon test

Trois temps, toujours les mêmes : **préparer, agir, vérifier**.

```python
def test_la_remise_est_plafonnee_a_cinquante_pour_cent():
    prix, pourcentage = 100.0, 90           # préparer
    resultat = remise(prix, pourcentage)    # agir
    assert resultat == 50.0                 # vérifier
```

**Une seule chose vérifiée.** Un test qui en vérifie cinq échoue sur la première et cache les
quatre autres ; cinq tests échouent cinq fois et te disent lesquels.

**Un nom qui dit ce qui est attendu.** `test_remise` ne t'apprend rien quand il devient rouge six
mois plus tard ; le nom ci-dessus dit ce qui est cassé et ce qui était voulu. C'est la seule
documentation qui ne peut pas mentir : si elle ment, elle est rouge.

**Un échec lisible sans ouvrir le code.** `assertTrue(a == b)` affiche `False is not true`, ce qui
ne sert à rien ; `assertEqual(a, b)` affiche `AssertionError: 12.0 != 12.1`. pytest réécrit
l'expression et montre ses sous-expressions (`assert 3 == 2`, puis `where 3 = mediane([1, 3])`),
Rust affiche `left: 3` et `right: 2` — et un dernier argument à `assert_eq!` y ajoute ton contexte.

## Les trois niveaux

| Niveau | Ce qu'il couvre | Coût d'écriture | Vitesse | Diagnostic | Fragilité |
|---|---|---|---|---|---|
| **unitaire** | une fonction, une classe | faible | ~0,2 ms | la ligne fautive | faible |
| **intégration** | deux ou trois composants réels | moyen | 10 à 500 ms | le composant fautif | moyenne |
| **bout en bout** | le système entier, vu de l'utilisateur | élevé | 1 à 60 s | « c'est cassé » | **forte** |

Les deux premières vitesses sont mesurées, sur un Mac arm64 et démarrage du lanceur déduit (0,05 s)
: 1000 tests unitaires en mémoire passent en 0,26 s, soit **0,2 ms** chacun ; 20 tests qui lancent
un sous-processus prennent 0,44 s, soit **19 ms** chacun. L'essentiel de l'écart n'est pas la
frontière de processus — un `/usr/bin/true` coûte 1,5 ms — mais le **démarrage d'un interpréteur**,
18 ms à lui seul. Les deux dernières lignes du tableau sont des estimations, et une CI partagée est
plus lente que ta machine. D'où la **pyramide** : beaucoup d'unitaires, quelques tests
d'intégration, très peu de bout en bout. Non par dogme mais par économie, puisque le coût monte
pendant que la précision du diagnostic baisse. Elle se renverse quand ton code est surtout de la
colle entre services, ou quand le vrai risque **est** l'intégration : *où mon risque est-il
concentré ?*

## Ce qui mérite un test

Dans cet ordre, du plus rentable au moins rentable :

1. **Ce qui est compliqué.** Une règle métier à cinq cas, un parseur, un calcul d'index, une
   machine à états. Là où tu hésites en relisant, un test paie immédiatement.
2. **Ce qui a déjà cassé.** Un bug arrivé une fois est un bug qui peut revenir. Voir plus bas.
3. **Les bords.** Zéro, un, vide, la valeur maximale, le doublon, le négatif, le dépassement : la
   quasi-totalité des bugs vivent aux bords, pas au milieu.
4. **Ce qui coûte cher quand ça casse.** Paiement, suppression de données, droits d'accès : le
   test se justifie par le prix de la panne, même si le code est simple.

Ce qui n'en mérite pas : **les accesseurs triviaux** — vérifier qu'un `get` rend ce qu'un `set` a
mis teste le langage, pas ton code. Et **les détails d'implémentation** : « la méthode appelle bien
`normaliser` avant `valider` » interdit le refactoring, l'inverse exact du but.

## Le test de régression

Le réflexe qui change tout tient en une phrase : **un bug trouvé devient un test avant d'être
corrigé.**

L'ordre est l'essentiel. Tu écris d'abord le test, tu le lances, il échoue — et cet échec prouve
deux choses d'un coup : que tu as bien reproduit le bug, et que le test est capable de le détecter.
Ensuite seulement tu corriges, et tu le regardes passer au vert. Fais l'inverse et tu ne sauras
jamais si ton test aurait attrapé quoi que ce soit : beaucoup de tests écrits après la correction
passeraient aussi bien sur le code bugué. C'est aussi la façon la moins chère de faire grandir une
suite sur du code existant qui n'en a pas : tu couvres exactement les zones à risque avéré, sans
avoir à décider à l'avance quoi couvrir.

## Les tests fragiles

Un test fragile échoue sans qu'aucun bug soit apparu. C'est la raison numéro un pour laquelle une
équipe finit par ignorer sa suite : dès qu'un rouge sur deux est un faux rouge, plus personne ne
lit les rouges. **Un test intermittent est pire que pas de test.**

| Cause | Symptôme | Remède |
|---|---|---|
| l'horloge | passe le matin, échoue à minuit ou en juin | injecter l'instant en paramètre, voir `temps.md` |
| l'ordre | passe seul, échoue dans la suite | chaque test crée et détruit son propre état |
| l'état partagé | échoue une fois sur dix | rien de global : ni variable, ni fichier fixe, ni base commune |
| le réseau | échoue quand le wifi tousse | une doublure ; le vrai réseau dans un job séparé |
| le parallélisme | échoue selon la machine | port, dossier et identifiants uniques par test |
| l'ordre d'itération | change selon la version ou la graine | comparer des ensembles, ou trier avant de comparer |
| la mise en forme | échoue quand on ajoute une virgule | vérifier les données, pas la chaîne rendue |

Deux de ces lignes se vérifient en trente secondes. En Python, l'ordre d'itération d'un `set` de
chaînes change à chaque exécution parce que le hachage des chaînes est salé au démarrage : le même
ensemble de cinq lettres sort dans un ordre différent à chaque `PYTHONHASHSEED`. Et `cargo test`
exécute les tests **en parallèle par défaut**, autant de front qu'il y a de cœurs logiques : deux
tests qui posent la même
variable d'environnement se marchent dessus et l'un échoue au hasard, quand les mêmes passent tous
les deux sous `cargo test -- --test-threads=1`. Un test qui ne passe que seul n'est pas un test,
c'est **un état partagé**.

## Les doublures

Trois objets différents, souvent confondus sous le mot « mock ».

| Nom | Ce que c'est | Usage typique |
|---|---|---|
| **bouchon** (`stub`) | rend une réponse figée | forcer un cas : erreur 500, liste vide |
| **simulacre** (`mock`) | enregistre les appels et permet de les vérifier | vérifier qu'un mail part bien |
| **faux** (`fake`) | une vraie implémentation, simplifiée | base en mémoire, système de fichiers virtuel |

Le faux est presque toujours le meilleur des trois : il a un comportement réel, donc le test reste
vrai quand l'implémentation bouge. Le piège est de trop simuler — un test bourré de simulacres
vérifie **l'implémentation** au lieu du comportement : il affirme que telle méthode a été appelée
avec tels arguments, ce qui devient faux dès que tu réorganises le code à comportement identique.
Tu obtiens un test rouge à chaque refactoring et vert quand la fonctionnalité casse. Et une
doublure trop permissive ne teste rien : en Python, `Mock(spec=Banque)` laisse passer un appel à
cinq arguments sur une méthode qui en prend trois, là où `create_autospec(Banque)` lève
`TypeError: too many positional arguments`. Si la vraie signature change, la première version
reste verte pendant que la production casse — prends la forme qui vérifie la signature.

## La couverture

La couverture mesure **quelles lignes ont été exécutées** pendant les tests. C'est tout. Elle ne
mesure pas que le résultat ait été vérifié.

```python
def test_remise_ne_plante_pas():
    remise(100, 10)
    remise(100, 90)              # zéro assertion, et l'outil annonce 100 %
```

Ce test passerait aussi si `remise` rendait toujours zéro. **100 % ne veut rien dire** : c'est un
chiffre qu'on atteint en écrivant des tests sans assertion, et une équipe à qui on impose un seuil
finit par le faire. Mais **une couverture basse veut dire quelque chose** : 20 % sur un module de
facturation est solide, personne n'a jamais exécuté les 80 % restants hors production. Utilise-la
comme un détecteur de trous, jamais comme une note, et active la couverture de **branches** : sur
un `if` dont seul le cas vrai est testé, `coverage` annonce 100 % en lignes mais 83 % en branches,
avec un `4->exit` qui désigne la sortie jamais empruntée.

## Les tests qui rapportent le plus

**Le test par propriétés.** Au lieu d'un exemple, tu affirmes une règle vraie pour toute entrée et
la bibliothèque cherche un contre-exemple. Les propriétés qui paient : l'aller-retour, l'invariant
(`min <= mediane <= max`), l'équivalence avec une implémentation de référence lente mais évidente.

```python
@given(st.lists(st.text(), min_size=1))
def test_aller_retour(champs):
    assert decoder(encoder(champs)) == champs
```

Avec `encoder = ",".join` et un `decoder` qui découpe sur les virgules, le contre-exemple tombe en
moins d'une seconde, et surtout il est **réduit** au plus petit possible : `champs=[',']`. Un
exemple minimal est un bug à moitié compris, et les échecs sont rejoués en premier aux exécutions
suivantes.

**Le test par exemples générés.** Même idée sans bibliothèque : tu tires des entrées au hasard et
tu compares à un oracle. Sur une recherche dichotomique comparée à `cible in liste`, vingt mille
tirages trouvent instantanément le bug d'un `<=` devenu `<`, mais sans réduire : le tirage brut
rend `([1, 1, 1, 3, 7, 8, ...], 8)` là où la bibliothèque rend `([0], 0)`. Fixe la graine, sinon
l'échec n'est pas reproductible et tu viens d'écrire un test fragile.

**Le test de non-régression sur un bug réel.** Le moins spectaculaire et le plus rentable des
trois : court, portant sur un cas dont on sait qu'il casse, et déjà prouvé capable d'échouer. Aucun
test écrit à l'avance n'offre cette garantie.

## À retenir

1. Un test ne prouve pas que le code marche : il te permet de le changer sans peur.
2. Préparer, agir, vérifier. Une chose par test, un nom qui dit l'attendu, un échec lisible seul.
3. Un test hors du processus coûte cent fois un test en mémoire, et diagnostique bien moins.
4. Teste le compliqué, ce qui a déjà cassé, les bords, ce qui coûte cher. Pas les accesseurs.
5. Un bug devient un test **avant** d'être corrigé : l'échec prouve que le test peut échouer.
6. Un test intermittent est pire que pas de test. S'il ne passe que seul, c'est un état partagé.
7. Trop de simulacres et tu testes l'implémentation : rouge au refactoring, vert quand ça casse.
8. 100 % de couverture ne veut rien dire ; une couverture basse, si.
