# Les journaux

Ton programme tourne sur une machine à laquelle tu n'as pas accès, il a planté il y a six heures,
et personne ne sait le reproduire. Le journal est tout ce qui reste. Le reste en découle.

## Comprendre après coup, pas pendant

Un débogueur travaille pendant, avec la machine sous la main. Un journal, lui, s'écrit tout le
temps et se lit **plus tard**, par quelqu'un d'autre, sans toi. Ce n'est ni du débogage interactif,
ni une trace d'exécution — il raconte les **événements qui comptent**, pas chaque ligne parcourue —
ni de la mesure : un temps noté dedans est une indication, pas un chiffre de benchmark (voir
`mesurer.md`). D'où la question devant chaque appel : **qui lira cette ligne dans six mois ?**

## Les niveaux

Six niveaux, un ordre, une règle par niveau. Les noms varient — `syslog` en définit huit, de 0 pour
`emerg` à 7 pour `debug`, dans `sys/syslog.h` — mais le modèle est partout le même.

| Niveau | Ce qui le mérite | Qui le lit |
|---|---|---|
| **trace** | le détail interne, tour de boucle par tour de boucle | toi, quelques minutes |
| **debug** | une décision de branche, une valeur intermédiaire | toi, en développement |
| **info** | un événement métier réussi : démarrage, commande validée | tout le monde, plus tard |
| **avertissement** | ça marche mais mal : réessai, quota à 90 %, config obsolète | l'astreinte, un jour |
| **erreur** | une opération a échoué, le service continue | l'astreinte, ce jour-là |
| **critique** | le service ne peut pas continuer | quelqu'un à 3 h du matin |

Trois pièges. **Tout en `info`** : si tout est important, rien ne l'est, et un fichier dont 99 %
des lignes sont du bruit ne s'ouvre jamais. **Une erreur qui n'en est pas une** : une requête
client mal formée est un avertissement — le niveau erreur désigne un problème de ton côté (voir
`erreurs.md`). **Journaliser et relancer** : si tu propages l'erreur, ne la journalise pas ici.

Le test qui tranche : **si personne ne regarde jamais ce message, il ne devrait pas exister.**
Supprimer une ligne de journal est un acte légitime.

## Ce qu'on met dans une ligne

Quatre choses : **quand**, **quoi**, **sur quoi**, **dans quel contexte**. Il en manque presque
toujours une. `ERROR échec du traitement` est inutilisable — quel traitement, pour qui, échoué
comment, la combientième fois ? Le même événement, écrit correctement :

```
2026-08-19T14:03:11.482Z ERROR echec ecriture commande commande_id=91827 client_id=4412
  tentative=3/3 cause="connexion refusee 10.2.0.7:5432" duree_ms=1204
```

Horodatage absolu en UTC avec les millisecondes : une heure locale sans fuseau ne se compare pas
entre deux machines (voir `temps.md`). Un identifiant, sans quoi tu sais qu'un échec a eu lieu mais
pas lequel. Des valeurs plutôt que des adjectifs : « lent » ne vaut rien, `duree_ms=1204` se trie.
La cause exacte, non reformulée. Et **un événement par ligne** : pas une pile d'appels sur quinze.

## Le journal structuré

Une ligne n'est pas une phrase, c'est un **enregistrement avec des champs**. Le même événement, un
objet JSON complet par ligne — format JSONL, voir `json.md` :

```json
{"ts":"2026-08-19T14:03:11.482Z","level":"error","msg":"echec ecriture commande",
 "commande_id":91827,"client_id":4412,"tentative":3,"duree_ms":1204}
```

Ce que ça permet : chercher, agréger, alerter sans expression régulière. Compter les erreurs par
route sur 200 000 lignes prend 0,7 s et une commande, `jq -r 'select(.level=="error") | .route'`
suivi d'un `sort | uniq -c`. En texte libre, la même chose passe par des positions de champ et
casse dès qu'un message contient une espace : sur `ERROR echec paiement carte refusee id=91828`,
`awk '{print $4}'` renvoie `carte` et `$5` renvoie `refusee` ; et `grep -c ERROR` compte aussi la
ligne où un utilisateur
« a signalé une ERROR dans son formulaire ».

Le coût est réel mais modeste : 190 octets contre 155 sur cette ligne, soit **+23 %**, et une
lecture à l'œil nu moins agréable. Compressé, l'écart disparaît — du JSONL se comprime environ
**3 à 10 fois** avec `gzip -9`, selon l'entropie des champs. Règle pratique : **le champ `msg` est
une constante**, les valeurs vont
dans les champs, sinon compter un événement revient à regrouper des chaînes toutes différentes.

## La corrélation

Une requête traverse une passerelle, deux services et une base. Chacun écrit ses lignes, dans son
fichier, sur sa machine. Sans rien de commun, tu as trois journaux et aucune histoire. La solution
est un **identifiant unique généré à l'entrée** et propagé partout : en-tête HTTP, message de file,
appel interne. Le standard courant est `traceparent` (W3C Trace Context).

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
             version | trace_id, 32 hexa | span_id, 16 hexa | drapeaux
```

Le `trace_id` reste identique sur toute la requête, chaque étape crée son propre `span_id`, et
chaque ligne de journal le porte dans un champ. C'est **la** chose qui rend un journal réparti
exploitable : elle transforme « il y a des erreurs 500 » en « voici les 14 lignes, sur 4 services,
de la requête qui a échoué à 14 h 03 ». Deux oublis chers : il doit traverser les **tâches de
fond** et les files de messages, et **revenir au client** dans la réponse d'erreur.

## Ce qu'on ne met JAMAIS dans un journal

Mots de passe, jetons, clés d'API, cookies de session, numéros de carte, données personnelles, et
**le corps complet des requêtes et des réponses**. Un journal n'est pas un fichier privé sur une
machine : il est copié vers un agrégateur, dupliqué sur plusieurs nœuds, archivé des mois, lu par
l'astreinte et le support, souvent envoyé chez un hébergeur tiers. **Ce qui entre dans un journal
en ressort rarement** : tu ne sais plus où sont les copies. Les fuites sont involontaires :

| Fuite | Comment ça arrive |
|---|---|
| jeton, cookie | un journal d'accès enregistre l'URL complète, ou un vidage des en-têtes HTTP |
| mot de passe | un objet de formulaire journalisé « pour déboguer », avec tous ses champs |
| carte bancaire | le corps JSON d'une requête de paiement, journalisé en cas d'erreur |
| identité | un message d'exception qui contient l'objet client entier |

Les parades : **journaliser des identifiants, pas des contenus** (`client_id=4412`, jamais
l'adresse e-mail) ; donner aux types sensibles un affichage qui écrit `***` par défaut, pour que la
fuite demande un effort ; relire ses propres journaux en cherchant `password`, `token`, `Bearer`.
Et échapper les valeurs : un retour à la ligne dans du texte utilisateur fabrique une fausse ligne.

## Ce que coûte une ligne

Vraiment quelque chose. Mesure sur un Mac Apple Silicon, `clang -O2`, un million d'itérations :

| Opération | Coût |
|---|---|
| formater la ligne (`snprintf`, 90 octets) | 160 ns |
| fabriquer l'horodatage lisible (`localtime_r` + `strftime`) | 400 ns, contre 12 ns brut |
| écrire sur une sortie bufférisée (`fprintf`) | **230 ns** |
| écrire avec un `write()` par ligne | **1 400 ns** |
| `write()` + `fsync()` par ligne | **17 000 ns** |

**Compte 400 ns par ligne.** À mille lignes par seconde, c'est 0,04 % d'un cœur : invisible. Dans
une boucle chaude à 100 000 itérations par image, c'est 40 ms par image, soit **plus du double du
budget de 16,7 ms** à 60 images par seconde. Gratuit ou fatal selon l'endroit.

**N'évalue pas les arguments avant de savoir si le niveau est actif.** Si le test du niveau est
*dans* la fonction, les arguments sont déjà calculés : un appel désactivé `journaliser(DEBUG,
"etat=%s", decrire_etat(i))` coûte 39 ns par tour et appelle `decrire_etat` un million de fois ;
derrière une macro qui teste d'abord, 0 ns et zéro appel. D'où des **macros**, pas des fonctions.

**Le vrai piège est l'écriture synchrone.** Un `write()` par ligne coûte six fois un appel
bufférisé, et y ajouter `fsync()` douze fois plus encore : la latence du disque devient celle de
tes requêtes. Un journal passe par un tampon, et en production par un fil d'écriture séparé. Le
tampon est perdu si le processus meurt brutalement : on force le vidage pour les seuls critiques.

## La rotation et la rétention

Un journal grossit sans limite, et **un disque plein arrête le service**. Le calcul est immédiat :
200 octets par ligne, 1 000 lignes par seconde, cela fait **17 Go par jour**. La panne est brutale
et silencieuse : sur un volume rempli, `fprintf` renvoie un négatif et `errno` vaut 28, `No space
left on device` — et le code qui ne vérifie pas ses écritures continue comme si de rien n'était.

- **Rotation par taille et par âge** : `logrotate` sous Linux, `newsyslog` sur macOS et les BSD,
  dont le `/etc/newsyslog.conf` décrit un fichier par ligne (nom, mode, nombre d'archives, taille
  en Ko, quand, drapeaux).
- **Rétention bornée**, en archives *et* en jours : cinq archives de 100 Mo donnent un plafond de
  600 Mo — les cinq archives **plus** le fichier en cours —, et un plafond connu est la seule chose
  qui protège le disque. Compresse-les, mais compte un facteur 4 tant que tu ne l'as pas mesuré.
- **Réouverture après rotation.** Le rotateur renomme puis supprime, ton processus garde son
  descripteur et écrit dans un fichier **qui n'a plus de nom** : l'espace n'est rendu qu'à la
  fermeture, et entre-temps ton processus écrit **dans l'archive** : le nouveau fichier reste vide,
  et tes lignes disparaissent quand la rétention efface cette archive. Parade : `SIGHUP`, ou
  `copytruncate`.
- **Une partition à part**, ou une réserve : le journal ne doit pas remplir le disque des données.

Et surtout : **teste la rotation**. C'est le seul mécanisme d'un système qui, par construction, ne
se déclenche qu'au bout de plusieurs jours — donc jamais en développement.

## Ce qui n'est pas un journal

| | Ce que c'est | La question à laquelle ça répond |
|---|---|---|
| **métrique** | un nombre agrégé dans le temps : compteur, jauge, histogramme | « combien, en ce moment » |
| **trace** | l'arbre chronologique des étapes d'une seule requête | « où est passé le temps » |
| **journal** | un événement horodaté, avec son contexte | « que s'est-il passé à 14 h 03 » |

Une métrique répond à « le taux d'erreur monte-t-il ? » à coût constant quel que soit le trafic :
c'est elle qui déclenche l'alerte. Une trace répond à « quelle étape a pris 1,2 s ? », un journal à
« pourquoi celle-là a échoué ? ». Ne compte pas des événements en relisant des journaux : c'est
cher, c'est en retard, et ça casse au premier changement de format. Voir `mesurer.md`.

## À retenir

1. Un journal sert à comprendre après coup, sur une machine que tu n'as pas.
2. Une ligne utile dit quand, quoi, sur quoi : un identifiant et des valeurs chiffrées.
3. Des champs, pas une phrase. Le `msg` est une constante ; +25 % d'octets, annulés par gzip.
4. Un `trace_id` généré à l'entrée et propagé partout rend exploitable un journal réparti.
5. Jamais de mot de passe, de jeton, de carte ni de corps de requête : rien n'en ressort jamais.
6. 400 ns par ligne, 1,4 µs avec un `write()`, 17 µs avec `fsync()`. Jamais dans une boucle chaude.
7. Teste le niveau avant d'évaluer les arguments : c'est pour ça que ce sont des macros.
8. Rotation, rétention bornée, réouverture après rotation : un disque plein arrête le service.
