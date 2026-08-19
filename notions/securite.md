# La sécurité au quotidien

Pas la page du spécialiste : celle du développeur ordinaire, qui écrit du code qui marche et
laisse une porte ouverte sans le savoir. Les failles qui coûtent cher ne sont presque jamais des
exploits savants — ce sont cinq ou six erreurs que tout le monde commet.

## Aucune entrée n'est digne de confiance

Tout le reste en découle. Pas seulement l'entrée de l'utilisateur : ni celle du réseau, du fichier
de configuration, de la base, de l'autre service maison, ni le nom de fichier trouvé sur le disque
— « ça vient de chez nous » veut juste dire que quelqu'un s'est peut-être fait avoir avant toi.
**Valide au bord**, une fois, là où la donnée entre ; ensuite le code manipule des données réputées
propres. Et valide contre une **liste de ce qui est permis**, jamais contre une liste de ce qui est
interdit, parce qu'une liste noire se contourne toujours.

```python
noire = ["<script>", "javascript:"]
"<SCRIPT>alert(1)</SCRIPT>", "<script >", "<img src=x onerror=alert(1)>"   # les trois passent
autorise = re.compile(r"\A[a-z0-9_-]{1,32}\z")             # ceci, personne ne le contourne
```

Testé : `../../etc/passwd`, `<script>`, `anna' OR '1'='1`, une chaîne de 40 caractères, tout est
refusé sans que tu aies rien eu à prévoir. Ce que tu n'as pas décrit n'entre pas.

Note le `\z` minuscule, et ce n'est pas un détail : `$` comme `\Z` acceptent un saut de ligne
final, donc `"anna\n"` franchirait la liste blanche. Les ancres changent d'un moteur à l'autre — en
JavaScript `\A` et `\Z` n'existent pas et valent la lettre elle-même, la forme équivalente y est
`/^[a-z0-9_-]{1,32}$/`. Voir `regex.md`.

## Les injections : des données collées dans du code

SQL, commande shell, chemin de fichier, HTML, LDAP, gabarit : les noms changent, la faute est
unique — **une chaîne de données a été concaténée dans une chaîne que quelqu'un va interpréter
comme du code.** Avec `nom = "anna' OR '1'='1"`, la requête `"... WHERE nom = '%s'" % nom` rend
**toute la table** : deux lignes au lieu d'une, mesuré. Le remède est **structurel**, jamais un
échappement à la main : code et données voyagent séparément.

| Cible | La faute | Le remède |
|---|---|---|
| SQL | concaténer la valeur | requête paramétrée, `WHERE nom = ?` |
| shell | `system("convert " + f)` | un **tableau** d'arguments, sans shell, et `--` avant la valeur |
| chemin | `join(base, nom)` tel quel | canoniser, puis comparer par `commonpath`, pas par préfixe |
| HTML | insérer la valeur brute | échapper **à l'affichage**, selon le contexte |

Le piège des paramètres SQL : **ils portent des valeurs, jamais des identifiants.** `SELECT * FROM
?` est une erreur de syntaxe, et `ORDER BY ?` est pire — accepté, mais il trie par une constante,
donc il ne trie rien. Un nom de colonne se valide contre une liste blanche.

Le shell, mesuré : avec `arg = "fichier.txt; echo INJECTE"`, `run("echo " + arg, shell=True)`
exécute **deux** commandes, là où `run(["echo", arg])` affiche la chaîne entière, point-virgule
compris — le réflexe n'est pas de citer, c'est de **ne pas invoquer de shell**. Ça ne suffit pas
encore : un nom valant `--version` sera lu comme une **option**, et `run(["grep", "x", nom])`
affichera la version au lieu de chercher. On met `--` avant toute valeur venue de l'extérieur.

Pour un chemin, `realpath(join(base, nom))` doit rester sous `realpath(base)`, et `join(base,
"/etc/passwd")` vaut `/etc/passwd` : un absolu **écrase** la base. Compare avec `commonpath`,
jamais avec un `startswith` nu — `/srv/base-public/secret` commence bien par `/srv/base`. Et
revérifie après ouverture : un lien symbolique peut changer entre les deux appels. Pour le HTML, on
échappe **à l'écriture dans la page**, jamais à l'entrée — un attribut sans guillemets reste
injectable quoi qu'il arrive.

## Les mots de passe

**Jamais en clair**, et **jamais chiffrés non plus** : le chiffrement est réversible, donc il y a
une clé, et la clé finit sur le même serveur que la base. On n'a jamais besoin de relire un mot de
passe, juste de vérifier qu'on retrouve le même. C'est un hachage — mais pas un hachage rapide.
Mesuré avec `openssl speed sha256`, un cœur traite 216 Mo/s sur des blocs de 16 octets, soit
**13 millions de hachages par seconde** ; une carte graphique en fait des milliards, et une base
volée en SHA-256 nu se déroule en quelques heures.

Il faut une fonction **lente, salée, faite pour ça** : Argon2id de préférence, sinon scrypt, bcrypt
ou PBKDF2. Si c'est bcrypt, sache qu'il **coupe à 72 octets**, en silence dans beaucoup
d'implémentations : deux phrases secrètes qui ne diffèrent qu'après le 72e octet ouvrent le même
compte. Borne la longueur, ou pré-hache en SHA-256 et encode en base64 — jamais en octets bruts, un
octet nul terminerait la chaîne. Toutes prennent un **coût** réglable et un **sel** unique par mot
de passe, qui n'est pas secret : il empêche une table précalculée commune à tous les comptes. Vise
**50 à 250 ms par vérification** :

| Fonction | Paramètres | Durée mesurée |
|---|---|---|
| SHA-256 nu | — | 0,000074 ms |
| PBKDF2-SHA256 | 600 000 itérations | 49 ms |
| scrypt | N=16384, r=8, p=1 | 24 ms, 17 Mo de mémoire |
| scrypt | N=131072, r=8, p=1, `maxmem=2**28` | 203 ms, 134 Mo de mémoire |
| Argon2id | m=64 Mio, t=3, p=4 (défaut d'`argon2-cffi`) | 40 ms |

Près de **trois millions** de fois d'écart entre la première ligne et la troisième : c'est le but.
Ces durées sont celles d'une machine de bureau, et le défaut d'Argon2id passe déjà sous la cible :
monte le coût — `t` d'abord, puis `m` — jusqu'à retomber dans la fourchette **sur le serveur de
production**, pas sur le tien.
La colonne mémoire compte autant, une carte graphique ayant beaucoup de cœurs et peu de mémoire par
cœur. Et compare les empreintes en **temps constant** : un `==` s'arrête au premier octet
différent, donc il parle.

## Les secrets

Clé d'API, mot de passe de base, jeton, certificat privé. **Jamais dans le code, jamais dans un
dépôt, jamais dans un journal** — les journaux sont copiés, agrégés, envoyés à un tiers et lus par
des gens sans accès à la production. **Jamais dans une URL** : elle est enregistrée par le serveur,
le proxy, l'historique du navigateur, et selon la politique de referrer elle repart dans `Referer`.
Ni en argument de ligne de commande : `ps -axo command` montre celle de tous les processus de la
machine, à n'importe quel utilisateur. Variable d'environnement, coffre dès que ça devient sérieux,
fichier hors dépôt listé dans `.gitignore` **avant le premier commit** : `.gitignore` n'ignore pas
un fichier déjà suivi, et une ligne ajoutée après coup ne le sort pas du dépôt.

**Un secret commité une fois est compromis pour toujours.** Vérifié : on commite une clé, on la
retire au commit suivant, on supprime même le fichier, et `git cat-file -p HEAD~2:conf.env` la
ressort intacte. Réécrire l'historique ne suffit pas non plus — la copie est déjà chez tes
collègues, dans les clones, dans le cache du serveur (voir `git.md`). La seule réponse correcte est
de **révoquer** la clé ; nettoyer l'historique est du confort.

## Authentification et autorisation

Deux questions différentes. **Authentification** : *qui es-tu ?* — mot de passe, jeton, session.
**Autorisation** : *as-tu le droit de faire ça, sur cet objet-là ?* La faute la plus répandue du
web tient en une ligne : on vérifie la première et on oublie la seconde.

```python
@connexion_requise
def facture(id):
    return base.get("factures", id)     # connectée, oui. Mais est-ce SA facture ?
```

L'utilisateur 42 demande `/facture/97`, qui appartient à l'utilisateur 7, et l'obtient : il est
authentifié, donc le contrôle passe. **L'autorisation se vérifie sur l'objet, pas sur la route** :
le propriétaire entre dans la requête (`WHERE id = ? AND proprietaire = ?`) plutôt que dans un `if`
qu'on oubliera, et on refuse **par défaut**.

## Le chiffrement : n'écris pas le tien

Deux phrases. Un algorithme de chiffrement ne se teste pas : il produit du charabia qui a l'air
correct même quand il est cassé, donc tes tests passent tous. Et sa solidité ne vient pas du
secret de sa construction mais d'années d'attaques publiques ratées, ce que ton code n'a pas.

Donc la bibliothèque du système, et **un mode authentifié** : AES-GCM, ChaCha20-Poly1305. Sans
authentification, l'attaquant ne lit pas le message mais peut le **modifier**, et certains
protocoles lui répondent assez pour qu'il finisse par le déchiffrer. Et **jamais deux fois le même
vecteur d'initialisation avec la même clé** : vérifié en AES-GCM, à nonce égal le XOR des deux
chiffrés est **exactement** le XOR des deux clairs, donc connaître un message révèle l'autre en
entier, et la clé d'authentification fuite aussi. Nonce de 12 octets tiré du générateur
cryptographique ou compteur qui ne repart jamais de zéro, et une clé par usage.

## Les dépendances

La plus grosse surface d'attaque d'un projet moderne : un projet Node ou Rust ordinaire tire
quelques centaines de paquets transitifs, chacun s'exécutant avec tes droits, en production.
**Verrouille-les.** `package-lock.json`, `Cargo.lock`, `uv.lock` ou `poetry.lock` figent la version
**exacte** de chaque paquet transitif, souvent avec son empreinte. En Go la sélection vient de
`go.mod`, `go.sum` ne portant que les empreintes. Commite le verrou et installe depuis lui : `npm
ci` refuse de démarrer sans, *« The `npm ci` command can only install with an existing
package-lock.json with lockfileVersion >= 1. »*. Sans verrou, `^1.2.3` accepte n'importe quel `1.x`
publié depuis. **Mets à jour quand même** : le verrou protège de la surprise, pas des failles
connues — `npm audit`, `cargo audit`, `pip-audit`, en passe régulière plutôt qu'en panique
annuelle.

**Méfie-toi des noms proches.** Le typosquattage consiste à publier `reqeusts` au lieu de
`requests`, `crossenv` au lieu de `cross-env`, et à attendre la faute de frappe. Vérifie le nom
exact, les téléchargements, la date de publication, le dépôt source — et une dépendance de trois
lignes ne vaut jamais le risque.

## Le hasard

| | Pseudo-aléatoire | Cryptographique |
|---|---|---|
| exemples | `rand()`, Mersenne Twister, `random` | `getrandom`, `os.urandom`, `secrets` |
| propriété | reproductible depuis la graine | **imprédictible** |
| usage | jeux, simulation, tests | jetons, sels, clés, mots de passe |

Un générateur pseudo-aléatoire est déterministe : c'est sa raison d'être, pas son défaut. Vérifié
en trois lignes — un Mersenne Twister semé avec l'horodatage courant et un second semé de la même
façon produisent **exactement** la même suite, indéfiniment. Si tes jetons de session sortent de
là, il suffit de deviner la seconde où le serveur a démarré : deux générateurs partis de la même
graine donnent la suite entière, indéfiniment. Ce n'est pas le défaut du langage — un générateur
créé sans graine part d'`os.urandom` — c'est le `seed(time())` écrit à la main qui ouvre le trou.
Pour ce qui doit être imprédictible : le générateur du système, au moins 128 bits —
`secrets.token_hex(32)` en donne 256. Voir `aleatoire.md`.

## Ce qui ne protège pas

**La sécurité par l'obscurité.** Algorithme maison, port décalé, URL non documentée : ça ralentit
un curieux et n'arrête personne. Un système doit rester sûr même publié — Kerckhoffs, 1883.

**La validation côté client seule.** Le JavaScript qui vérifie le formulaire est un confort
d'ergonomie ; une requête `curl` l'ignore. Toute règle qui compte est revérifiée côté serveur.

**Un identifiant difficile à deviner.** Un UUID à la place de `id=7` ne remplace pas un contrôle
d'accès : les identifiants fuitent par une URL partagée, un `Referer`, un export.

**HTTPS.** Il protège le **transport**, rien d'autre : ni ce que fait le serveur, ni la validation
des entrées, ni le stockage au repos, et le nom du serveur reste visible dans le DNS et le SNI. Un
site en HTTPS avec une injection SQL est vulnérable, et correctement chiffré.

## À retenir

1. Aucune entrée n'est fiable, pas même celle du service d'à côté. Valide au bord, contre une
   liste de ce qui est permis.
2. Toute injection est la même faute : des données collées dans du code. Sépare, n'échappe pas.
3. Les paramètres SQL portent des valeurs, jamais des noms de table ou de colonne.
4. Argon2id ou scrypt, 50 à 250 ms. SHA-256 en fait 13 millions par seconde et par cœur.
5. Un secret commité une fois est compromis pour toujours : on révoque, on ne nettoie pas.
6. Authentifié ne veut pas dire autorisé. Vérifie le droit sur **cet objet-là**.
7. Mode authentifié, et jamais deux fois le même nonce avec la même clé.
8. Pseudo-aléatoire pour les jeux, générateur du système pour tout ce qui doit être imprédictible.
