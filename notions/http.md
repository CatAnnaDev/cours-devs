# HTTP et les API

Le protocole que tu utilises tous les jours sans jamais le lire. Presque toute la complexité d'une
application web vient de sa première propriété : **HTTP ne se souvient de rien.**

## Un client demande, un serveur répond, et personne ne se souvient

Un client ouvre une connexion, envoie une **requête**, reçoit une **réponse**. Fin. Le serveur ne
garde aucune trace : la requête suivante repart de zéro, même sur la même connexion. C'est ce qui
le rend passable à l'échelle, et ce qui coûte cher : tout ce que tu appelles « session » est
reconstruit par-dessus, un identifiant renvoyé à chaque requête et un état retrouvé à partir de
lui. Dès que cet état vit en mémoire côté serveur, tu ne peux plus ajouter de second serveur.

## Une requête, une réponse, en texte

HTTP/1.1 est du texte. Un vrai échange à la main avec `nc example.com 80`, en-têtes abrégés :

```
GET / HTTP/1.1                          HTTP/1.1 200 OK
Host: example.com                       Date: Wed, 19 Aug 2026 14:55:37 GMT
Accept: text/html                       Content-Type: text/html
Connection: close                       Transfer-Encoding: chunked

                                        22f
                                        <!doctype html><html lang="en">...
```

Requête : **méthode**, **chemin**, version, **en-têtes**, ligne vide, corps éventuel. Réponse :
version, **code de statut**, en-têtes, ligne vide, corps. Les lignes finissent par CRLF, pas LF.
`Host` est obligatoire : retire-le et example.com répond `400 Bad Request`. Un corps annonce sa
longueur en **octets** (un accent UTF-8 en vaut deux, voir `unicode.md`) via `Content-Length`, ou
par morceaux via `Transfer-Encoding: chunked`.

## Les méthodes se jugent sur deux propriétés

Le nom ne dit rien d'utile. **Sûre** : la méthode ne modifie rien, on peut la lancer sans
conséquence, la précharger, la répéter. **Idempotente** : la répéter laisse le même état final.

| Méthode | Sûre | Idempotente | Corps | Usage |
|---|---|---|---|---|
| `GET` | oui | oui | non | lire |
| `HEAD` | oui | oui | non | les en-têtes seuls, sans le corps |
| `PUT` | non | **oui** | oui | remplacer entièrement |
| `DELETE` | non | **oui** | non | supprimer |
| `PATCH` | non | non | oui | modifier partiellement |
| `POST` | non | **non** | oui | tout le reste |

**L'idempotence est ce qui autorise le réessai.** Un réseau perd des réponses, pas que des
requêtes : ton client ignore si le serveur a agi. Sur un `PUT`, rejoue sans réfléchir. Sur un
`POST`, tu viens peut-être de facturer deux fois. Mesuré : trois `POST` identiques créent **trois**
ressources ; avec un même `Idempotency-Key`, **une**. Cette clé se tire **côté client**.

## Les codes de statut

| Famille | Sens | Ceux qui comptent |
|---|---|---|
| **1xx** | information, ce n'est pas fini | `103` early hints |
| **2xx** | c'est bon | `200` ok, `201` créé (avec `Location`), `204` rien à renvoyer |
| **3xx** | va voir ailleurs | `301`/`308` définitif, `302`/`307` temporaire, `304` inchangé |
| **4xx** | **ta** faute, ne réessaie pas tel quel | `400` `401` `403` `404` `409` `422` `429` |
| **5xx** | **sa** faute, réessaie plus tard | `500` `502` `503` `504` |

La frontière 4xx / 5xx est la seule que ton code doit vraiment comprendre : elle décide s'il faut
**réessayer**. Un 4xx rejoué à l'identique redonnera un 4xx ; un 5xx ou un 429 peut passer. La
paire qu'on confond : `401` « je ne sais pas qui tu es » contre `403` « je sais, et non ».

**Ne renvoie jamais `200` avec une erreur dans le corps.** Tout ce qui est en aval lit le statut,
pas ton JSON : caches, répartiteurs, supervision, réessais. Vérifié : un serveur qui rend `200`
avec `{"status":"error"}`, interrogé par `curl --fail --retry 3`, sort avec le code **0**. Aucun
réessai, aucune alerte. Le même contenu en `500` sort avec 22.

## Les en-têtes du quotidien

| En-tête | Côté | Ce qu'il fait |
|---|---|---|
| `Content-Type` | les deux | le type du corps : `application/json`, `text/html; charset=utf-8` |
| `Authorization` | requête | `Bearer <jeton>`, la plupart du temps |
| `Accept` | requête | ce que le client sait lire ; `Accept-Encoding` pour la compression |
| `Cache-Control` | les deux | la politique de cache |
| `ETag` / `If-None-Match` | réponse / requête | la revalidation par étiquette d'entité |

`Accept-Encoding` est gratuit et énorme : la page d'accueil de cloudflare.com pèse 1 315 709 octets
brute et 295 061 en gzip lors de la mesure. Facteur 4,5 pour un en-tête — la page étant dynamique,
refais-la, l'ordre de grandeur tient.

**L'étiquette d'entité** (`ETag`) est une empreinte du contenu : le serveur la renvoie, le client
la repropose dans `If-None-Match`, et si rien n'a bougé la réponse est `304 Not Modified` **sans
corps**. Sur l'API GitHub, `GET /repos/torvalds/linux` renvoie 5 487 octets, la même requête
conditionnelle `304` et 0 octet. Nuance qui compte : non authentifié, ce `304` consomme quand même
une unité de quota ; **authentifié**, GitHub ne le décompte pas. Le cache économise toujours la
bande passante, et parfois le droit d'appeler.

## Le cache HTTP

**La durée de vie** : `Cache-Control: max-age=60` veut dire « valable 60 secondes », et aucune
requête n'est émise. **La revalidation** : la durée écoulée, le client redemande *avec condition*
et le serveur répond `304` sans corps. On paie un aller-retour, pas le contenu.

| Ressource observée | En-tête | Ce que ça veut dire |
|---|---|---|
| page d'accueil | `public, max-age=10, s-maxage=10` | 10 s pour tous, 10 s pour les caches partagés |
| `lodash@4.17.21` sur `cdn.jsdelivr.net` | `public, max-age=31536000, immutable` | un an, et ne revalide même pas |

Le second est le motif à connaître : **mets la version dans l'URL** et le contenu devient immuable
— pour publier autre chose, tu changes l'URL. C'est l'optimisation la moins chère du web : un
en-tête, zéro code, zéro requête. Rien à voir avec `cache.md`, le cache du processeur.

## Concevoir une API qui ne se retourne pas contre toi

**Des ressources, pas des verbes.** `POST /articles/42/publier` devient vite ingérable : il faudra
`/depublier`, `/archiver`, `/restaurer`. Nomme des choses, les méthodes sont les verbes.

**Pagine dès le premier jour.** Sans pagination, une collection marche jusqu'à ce que quelqu'un ait
50 000 lignes — trop tard, les clients attendent tout. Le décalage (`?page=3`) est simple mais
saute et duplique dès que les données bougent ; le curseur (`?after=<opaque>`) est stable mais
interdit d'aller droit à la page 47. Publie la navigation dans un en-tête `Link`, comme GitHub.

**Des erreurs lisibles par une machine et par un humain** : un statut juste, plus un corps stable
du genre `{"code": "solde_insuffisant", "message": "...", "trace_id": "01J8Z"}`. Le `code` sert au
`switch` du client et **ne change jamais**, le `message` sert à l'humain, le `trace_id` retrouve la
requête dans tes journaux. Voir `erreurs.md`, erreur contre bug.

**Versionne.** `/v1/` dans le chemin est laid, et marche. La vraie règle est ailleurs : **ajouter
un champ n'est pas cassant, en retirer un ou changer son type l'est.** Un client qui ignore les
champs inconnus (voir `json.md`) te laisse évoluer des années sans jamais sortir de `/v1/`.

**Limite le débit, et annonce-le.** Côté serveur, un quota et un `429` avec `Retry-After` ; côté
client, lis ces en-têtes plutôt que de foncer dans le mur. GitHub renvoie `x-ratelimit-limit: 60`,
`x-ratelimit-remaining: 53` et un `x-ratelimit-reset` en epoch (voir `temps.md`).

## Ce qui casse en vrai

**Pas de temps d'attente, pas de service.** Un appel sans délai maximal bloque un fil d'exécution
pour toujours ; assez d'appels bloqués et ton service tombe alors que c'est le voisin qui est en
panne. Fixe-en deux, séparés : un pour la connexion (court, 1 à 3 s), un pour la réponse complète.

**Les réessais sans recul exponentiel aggravent la panne.** Un service qui vacille reçoit trois
fois plus de trafic, tous rejouant aussitôt. Le recul doit doubler : face à un `503`,
`curl --retry 4` réessaie à 0, 1, 3, 7 et 15 s. Ajoute du **bruit aléatoire** (voir
`aleatoire.md`) : sinon mille clients tombés ensemble rejouent ensemble. Et si le serveur dit
`Retry-After`, obéis.

**Ne rejoue jamais une opération non idempotente** sans clé d'idempotence : c'est le bug le plus
cher de la liste. **Et méfie-toi des redirections, qui changent la méthode** : vérifié, un `POST`
suivi par `curl -L` à travers un `301` ou un `302` repart en `GET`, corps perdu, alors qu'un `307`
ou un `308` le laisse en `POST`. Écris tes API en `307`/`308`.

**Une connexion refusée n'est pas une réponse lente**, et ça ne se traite pas pareil :

| Symptôme | Sortie de curl | Ce que ça dit |
|---|---|---|
| nom introuvable | 6 | DNS : le service n'existe pas, ou ton résolveur est mort |
| connexion refusée | 7, immédiat | rien n'écoute sur ce port. Réessaie tout de suite |
| délai dépassé | 28, après le délai | ça écoute et n'arrive pas à suivre. **Ralentis** |
| réponse `500` | 0 sans `--fail` | il a répondu : à ton client de lire le statut |

## HTTPS, HTTP/2, HTTP/3

TLS chiffre et authentifie le **contenu** : chemin, en-têtes, cookies, corps, et il interdit sa
modification. Il ne cache pas **à qui** tu parles : DNS, adresse IP et nom du serveur (SNI) sont en
clair. Vérifiable — `openssl s_client -connect example.com:443 -servername example.com -debug`
montre `example.com` en clair dans le `ClientHello`, à cheval sur deux lignes du vidage
hexadécimal. Un observateur voit *que* tu visites example.com, pas *quelle page*.

HTTP/2 et HTTP/3 ne changent ni méthodes, ni statuts, ni en-têtes : ton code est le même. Ce qui
change, c'est le **multiplexage** — six requêtes parallèles vers le même hôte font 6 connexions TCP
en HTTP/1.1 et **une seule** en HTTP/2, ce qui rend inutile de regrouper les fichiers ou d'éclater
sur des sous-domaines — et le **transport**, HTTP/3 passant sur QUIC au-dessus d'UDP, où la perte
d'un paquet ne bloque plus les autres flux. La négociation est automatique (ALPN dans la poignée
TLS) ; vérifie ton outil : si `HTTP3` n'apparaît pas dans la ligne `Features:` de `curl -V`, ton
curl ne fait pas HTTP/3 — c'est le cas de celui livré avec macOS.

## À retenir

1. HTTP est sans état : la session est reconstruite par-dessus, et c'est là qu'est la complexité.
2. Idempotent veut dire rejouable. `POST` ne l'est pas : clé d'idempotence tirée côté client.
3. 4xx c'est ta faute, ne rejoue pas. 5xx et 429, rejoue avec un recul qui double, plus du bruit.
4. Un `200` portant une erreur rend ta panne invisible à toute la chaîne en aval.
5. `ETag` et `If-None-Match` : `304`, zéro octet. Version dans l'URL et `immutable` : zéro requête.
6. Pagination et versionnement le premier jour, jamais après.
7. Deux temps d'attente, toujours : un pour la connexion, un pour la réponse.
8. Connexion refusée, rejoue tout de suite. Délai dépassé, ralentis.
