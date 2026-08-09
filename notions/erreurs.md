# Gérer les erreurs

Trois familles de mécanismes, et la question qui décide entre eux : **est-ce que l'appelant peut
faire quelque chose ?**

## Trois familles

| Mécanisme | Langages | Visible dans la signature | Coût quand tout va bien |
|---|---|---|---|
| **code de retour** | C, Go | oui (par convention) | nul |
| **exception** | C++, Java, C#, Python | non (sauf `checked` en Java) | nul, jusqu'à ce que ça lève |
| **type somme** | Rust, Haskell, C++23 | **oui, imposé** | nul |

```c
int lire(const char *chemin, char **sortie);        // 0 = ok
```

```cpp
std::string lire(const std::string &chemin);        // lève en cas d'échec
```

```rust
fn lire(chemin: &str) -> Result<String, Erreur>
```

Le troisième est le seul où **le compilateur t'oblige** à traiter le cas d'échec. C'est son intérêt
principal, plus que la performance.

## Ce que chacun coûte vraiment

**Code de retour** : rien, mais chaque appel se transforme en `if`. Le code utile finit noyé, et un
retour ignoré ne produit **aucun** avertissement par défaut. C'est le défaut du C, et c'est réel :
des générations de bugs viennent d'un `return` non testé.

**Exception** : rien du tout tant que rien ne lève — les compilateurs modernes utilisent des tables
consultées seulement au moment du déroulement. Mais lever coûte **cher**, des centaines de fois plus
qu'un `return`. D'où la règle : **une exception est pour l'exceptionnel**, pas pour un flux de
contrôle normal.

Et le prix caché : le code doit être **sûr face aux exceptions**. Toute ligne peut être un point de
sortie. C'est la raison d'être de RAII en C++ : sans destructeurs automatiques, écrire du code
correct face aux exceptions est presque impossible.

**Type somme** : rien, et l'échec est dans le type. Le prix est syntaxique — il faut propager
explicitement, d'où l'opérateur `?` de Rust.

## La question qui décide

> **L'appelant peut-il faire quelque chose de l'erreur ?**

| Situation | Réponse |
|---|---|
| fichier absent, réseau coupé, entrée invalide | **oui** → erreur récupérable, à renvoyer |
| indice hors bornes, pointeur nul, invariant cassé | **non** → c'est un **bug**, arrête-toi |
| mémoire épuisée | ça dépend : un serveur peut refuser la requête, un outil peut mourir |

La distinction **erreur** / **bug** est la plus importante du sujet, et elle est souvent ratée.

Une erreur fait partie du fonctionnement normal : un fichier peut ne pas exister, c'est prévu, et
le programme doit continuer.

Un bug est une violation d'invariant : le programme est dans un état qu'il ne devrait pas
atteindre. Continuer, c'est propager la corruption. **Arrête-toi bruyamment** — `assert`, `panic`,
`abort`. Rust a d'ailleurs les deux mécanismes séparés, et c'est une bonne idée de conception :
`Result` pour les erreurs, `panic!` pour les bugs.

## Les fautes classiques

**Avaler.**

```java
try { risque(); } catch (Exception e) { }
```

L'erreur disparaît, le programme continue dans un état faux, et le bug se manifeste ailleurs. Si tu
ne peux vraiment rien faire, **journalise au minimum**.

**Attraper trop large.** `catch (Exception)` attrape aussi les bugs de programmation que tu voulais
voir. Attrape le type précis que tu sais traiter.

**Ignorer un code de retour.** En C, marque les fonctions dont le retour compte :

```c
__attribute__((warn_unused_result)) int ecrire(...);
```

Rust le fait par défaut avec `#[must_use]` sur `Result`.

**Utiliser une exception comme flux normal.** Lever une exception à chaque ligne d'un fichier de
dix mille lignes coûte des secondes.

**Perdre le contexte.** « Erreur d'ouverture du fichier » ne sert à rien. « Impossible d'ouvrir
`/etc/config.toml` : permission refusée » se corrige tout de suite. Le contexte doit **s'accumuler**
en remontant.

## Le nettoyage

Le vrai problème d'une erreur au milieu d'une fonction : il faut libérer ce qui a été acquis avant.

**C** — le `goto` de nettoyage, qui est l'usage légitime le plus reconnu du `goto` :

```c
int traiter(void) {
    char *tampon = malloc(1024);
    if (!tampon) return -1;

    FILE *fichier = fopen("donnees", "rb");
    if (!fichier) goto erreur_fichier;

    fclose(fichier);
    free(tampon);
    return 0;

erreur_fichier:
    free(tampon);
    return -1;
}
```

**C++, Rust** — RAII : les destructeurs passent sur tous les chemins, il n'y a rien à écrire.

**Java, C#** — `try-with-resources` et `using`, qui sont du RAII limité aux blocs.

**Go** — `defer`, exécuté à la sortie de la fonction.

Tous résolvent le même problème, et le C est le seul à te laisser le faire à la main.

## Valider à la frontière

La bonne architecture concentre la validation **à l'entrée** :

```
entrée brute  →  [validation]  →  types garantis  →  logique métier
```

Une fois la frontière franchie, les données sont valides **par construction**, et le code métier
n'a plus à en douter. C'est ce qui évite les `if (x == null)` répétés à tous les étages.

Le corollaire : **fais porter les garanties par les types**. Une fonction qui prend un
`AdresseEmail` plutôt qu'un `String` ne peut pas recevoir n'importe quoi, et la vérification a lieu
une seule fois, au moment de la construction.

## Ce qu'on ne rattrape pas

Certaines erreurs ne doivent pas être attrapées :

- **débordement de pile** — l'état est déjà douteux ;
- **corruption de la mémoire** — trop tard ;
- **échec d'assertion** — c'est un bug, par définition.

Un serveur qui attrape tout et redémarre le traitement à chaque exception finit par tourner dans un
état incohérent. Redémarrer le **processus** est souvent plus sûr que de continuer — c'est le
principe du *let it crash* d'Erlang, et il a fait ses preuves.

## À retenir

1. La question est : l'appelant peut-il faire quelque chose ?
2. Erreur récupérable ≠ bug. Un bug s'arrête bruyamment.
3. Une exception coûte cher à lever : réserve-la à l'exceptionnel.
4. N'avale jamais une erreur ; n'attrape jamais plus large que ce que tu sais traiter.
5. Accumule le contexte en remontant.
6. Nettoyage : `goto` en C, RAII partout ailleurs.
7. Valide à la frontière, puis fais porter les garanties par les types.
