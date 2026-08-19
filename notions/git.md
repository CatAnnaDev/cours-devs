# Git

Presque tout le monde apprend Git par les commandes et se retrouve bloqué au premier ennui. Le
modèle en dessous tient en un paragraphe, et une fois qu'on le tient tout le reste se déduit.

## Un graphe d'instantanés, pas une suite de diffs

Un commit n'est pas « ce que j'ai changé » : c'est un objet qui contient l'**arbre complet** du
projet à un instant, un ou plusieurs parents, deux identités datées, un message. Rien d'autre.

```
$ git cat-file -p HEAD
tree 248f605899bcdb06668fb66f1a196de46bf640fe
parent 7234e1914558889f14e04273447c88760ead5c63
author Anna <a@exemple.fr> 1787151329 +0200
committer Anna <a@exemple.fr> 1787151329 +0200

corrige le calcul du seuil
```

Les diffs de `git log -p` sont **calculés à la demande**, jamais stockés, et l'instantané complet
ne coûte presque rien : un objet est nommé par le SHA-1 de son type, de sa taille et de son contenu
(voir `hachage.md`), donc deux commits partageant un fichier inchangé pointent le **même** blob.
Deux dates et pas une : `author` ne bouge jamais, `committer` est réécrit à chaque rebase — c'est
pourquoi `git log` affiche encore de vieilles dates sur des commits rejoués hier. Une **branche**
est un identifiant de commit sur 40 caractères, dans un fichier de `.git/refs/heads/` tant qu'elle
est isolée, dans une ligne de `.git/packed-refs` après compactage. `HEAD` contient
`ref: refs/heads/main`, et commiter crée un commit ayant le courant pour parent avant d'avancer ce
pointeur.

## Les trois zones

| Zone | Ce que c'est | On la regarde avec |
|---|---|---|
| répertoire de travail | tes fichiers sur le disque | `git diff` |
| index (« staging ») | la photo du prochain commit | `git diff --staged` |
| dépôt | le graphe des commits, dans `.git` | `git log` |

`git add` ne « marque » pas un fichier : il **copie son contenu actuel dans l'index**. Modifie-le
encore ensuite et l'index garde l'ancienne version — `git status -s` affiche alors `MM a.txt`.
C'est ce que cette zone apporte : tu commites **une partie** de ton travail, `git add -p` te
proposant bloc par bloc. À l'inverse `git commit -a` ne prend que les fichiers **déjà suivis**.

## Le cycle de tous les jours

```sh
git status            # les trois zones d'un coup d'œil
git diff              # ce que j'ai écrit et qui n'est pas encore dans l'index
git add -p            # je choisis ce qui entre
git diff --staged     # je relis le commit que je m'apprête à faire
git commit
```

La ligne qui compte est `git diff --staged` : dernière occasion de voir la trace de débogage
oubliée, le `TODO`, la clé d'API, le fichier de 40 Mo. Ensuite, `git log --oneline --graph`.

## Écrire un message utile

Le diff dit *ce qui* a changé et il est déjà dans le commit : le message doit dire **pourquoi**. La
convention de git lui-même (`man git-commit`) suffit — un sujet de **50 caractères au plus** à
l'impératif, une ligne vide, puis un corps optionnel : la raison, ce qu'on a écarté, le ticket.

```
corrige la fuite de descripteurs du lecteur de config

Fermé seulement sur le chemin nominal : après 1024 rechargements, plus aucune connexion possible.
```

Ce qui ne sert à rien : « fix », « wip », « update ». Le test : est-ce que ce message m'aiderait si
`git bisect` venait de désigner ce commit ?

## Fusionner ou rebaser

`git merge` crée un commit à **deux parents** et conserve l'histoire réelle avec ses parallèles.
`git rebase main` rejoue tes commits un par un au sommet de `main` : le résultat est linéaire, mais
les commits sont **neufs** — même contenu, autres parents, donc autres identifiants.

```
*   11bc4fe Merge branch 'feat'     rebase :  * 6abd7d7 feat 1   (c'était 6915f25)
|\                                            * 151ed69 main A
| * 6915f25 feat 1                            * cb48ff3 base
* | 151ed69 main A
|/
* cb48ff3 base
```

Le choix entre les deux est affaire de goût, avec un défaut propre au rebase : il peut reposer les
mêmes conflits à chaque commit rejoué. Sauf sur un point : **ne récris jamais un historique déjà
partagé.** Après un `rebase` ou un `--amend` sur des commits poussés, `git push` refuse le résultat
en `non-fast-forward`, et forcer règle ton problème en créant celui des autres : le collègue qui
synchronise récupère le même travail deux fois, sous deux identités.

**`--force-with-lease` ne suffit pas seul.** Il compare la branche distante à ta copie de suivi, et
un simple `git fetch` — que ton éditeur lance peut-être tout seul — met cette copie à jour et
désarme le bail : le `push` passe alors et efface le commit du collègue. La forme réellement sûre
est `git push --force-with-lease --force-if-includes` (Git 2.30 et plus), qui exige en plus que tu
aies intégré ce que tu as rapatrié.

## Le conflit

Git fusionne seul tant que les deux versions touchent des zones différentes. Quand elles modifient
les mêmes lignes, il ne devine pas : il te pose la question.

```
<<<<<<< HEAD
valeur = 7
||||||| a47be53      ce bloc n'apparait qu'avec conflictStyle diff3 ou zdiff3
valeur = 1
=======
valeur = 42
>>>>>>> feat
```

Entre `<<<<<<<` et `=======`, la version de `HEAD` ; en dessous, celle qu'on intègre. `git status`
affiche `UU fichier` et `git merge` a rendu le code 1. La résolution : écrire la bonne version —
pas forcément l'une des deux —, supprimer les marqueurs, `git add` (c'est **lui** qui déclare le
conflit résolu), puis `git commit` ou `git rebase --continue`.

Fais `git config --global merge.conflictStyle zdiff3` une fois pour toutes (Git 2.35 et plus ;
`diff3` avant) : le bloc du milieu est l'**ancêtre commun**, et le voir dit qui a modifié quoi.
Deux pièges. **Pendant un rebase, `HEAD` est la branche d'accueil, pas la tienne**, le bloc du bas
est ton commit. Et rien ne vérifie que les marqueurs ont disparu. Au pire on renonce, et un conflit
n'engage à rien — mais avec la bonne commande : `git merge --abort` pendant une fusion, `git rebase
--abort` pendant un rebase. L'une pendant l'autre rend `fatal`.

## Réparer

| Situation | Commande | Danger |
|---|---|---|
| retirer de l'index | `git restore --staged f` | aucun |
| jeter la modification du fichier | `git restore f` | **perte définitive** |
| corriger le dernier commit | `git commit --amend` | récrit un commit |
| annuler un commit publié | `git revert <sha>` | aucun, sauf sur une fusion : `-m 1` |
| défaire le dernier commit | `git reset --soft/--mixed/--hard HEAD~1` | récrit, `--hard` détruit |

`git revert` ne supprime rien : il **ajoute** un commit appliquant le diff inverse, seule
annulation utilisable sur une branche partagée. Les trois `reset` ne diffèrent que par le nombre de
zones ramenées en arrière : `--soft` déplace la branche seule, `--mixed` (défaut) déplace aussi
l'index, `--hard` déplace aussi tes fichiers. Ce dernier, comme `restore`, **détruit du travail non
commité**, que le reflog n'enregistre pas — avec une nuance qui sauve : ce qui est passé par
`git add` survit comme objet orphelin, et `git fsck --lost-found` le ressort. Un commit, lui, se
retrouve tant que le reflog le garde : 90 jours, 30 pour ce qui est devenu inatteignable, et
**uniquement dans le dépôt où il a été fait** — le reflog est local et ne se clone pas.

```
$ git reflog
8bf8d33 HEAD@{1}: commit: c2 important       <- git reset --hard HEAD@{1} le ramène
```

Le reflog garde les positions successives de `HEAD` et de chaque branche, 90 jours par défaut, 30
pour ce qui n'est plus atteignable : avant de paniquer, `git reflog`. Quant à `git stash`, il range
les modifications en cours et `git stash pop` les remet — piège, il **ne prend pas les fichiers non
suivis**, `git stash -u` les inclut.

## Trouver

`git log -S "seuil"` — la « pioche » : les commits où le **nombre d'occurrences** de la chaîne
change, donc ceux qui l'ont introduite ou supprimée ; `git log -G` prend, lui, toute diff
mentionnant le motif. `git blame` donne pour chaque ligne le dernier commit qui l'a touchée ; `-w`
ignore les espaces, sans quoi une réindentation générale rend le blame inutile.

`git bisect` — une **recherche dichotomique dans l'historique**, l'idée exacte de la recherche
binaire de `big-o.md`, donc en O(log n) : au pire `log2 n` essais, 7 sur 100 commits, 10 sur 1000.
`debogage.md` détaille la méthode et les codes de sortie exacts qu'attend `git bisect run`.

## Les habitudes qui évitent les ennuis

**Des commits petits et cohérents.** Un commit, une idée : messages écrivables, `revert`
chirurgical, `bisect` utile. Un commit de 40 fichiers mêlant renommage, correction et reformatage
est irrécupérable.

**Un `.gitignore` dès le premier commit** : binaires, dossiers de build, `.env`, fichiers d'IDE.
Piège, il **n'ignore pas un fichier déjà suivi** ; il faut d'abord `git rm --cached f` — et ce
commit-là **supprime le fichier du disque de tous ceux qui tirent**. À réserver à ce qui est
réellement local (`.env`, cache d'éditeur), jamais à une configuration dont les autres se servent.

**Jamais de secret dans un commit.** Un mot de passe commité est compromis, et le rester : même
après avoir récrit l'historique et forcé, le commit reste atteignable par son identifiant côté
serveur jusqu'au ramasse-miettes, et sur une forge il survit dans les copies et les références de
demandes de fusion. **Révoque le secret, tout de suite** ; le nettoyage — `git filter-repo` — n'est
que du confort. Voir `securite.md`.

**`git pull --rebase`** — sinon un `pull` fabrique un commit « Merge branch 'main' of... » à chaque
synchronisation, pour rien. `git config --global pull.rebase true`, une fois — et
`git config --global rebase.autoStash true` dans la foulée, sans quoi le moindre fichier modifié
fait échouer le `pull` en `cannot pull with rebase`.

**Une branche par sujet**, même seul : un pointeur de 40 caractères, une piste abandonnable, une
branche principale toujours livrable.

## À retenir

1. Un commit est un instantané complet, plus des parents, un auteur, un committeur, un message.
2. Une branche est un simple pointeur : créer ou supprimer une branche ne coûte rien.
3. `add` photographie dans l'index ; c'est ce qui permet de commiter une partie du travail.
4. Relis `git diff --staged` avant chaque commit. C'est le filet le plus rentable de tous.
5. Le message dit le pourquoi : sujet impératif de 50 caractères, ligne vide, corps.
6. Merge conserve le graphe, rebase le linéarise en récrivant les identifiants.
7. Ne récris jamais un historique déjà partagé ; et `--force-with-lease` ne protège vraiment
   qu'accompagné de `--force-if-includes`.
8. `--hard` détruit du non-commité ; le commité se retrouve au reflog, 90 jours, et seulement ici.
9. Un secret commité est compromis pour de bon : on le révoque, on ne le nettoie pas.
