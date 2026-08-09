# 15 — Pour aller plus loin

Tu as un programme complet d'environ 580 lignes de code et 170 de tests, sans dépendance, qui fait
quelque chose d'utile. Ce chapitre rassemble ce qu'on a volontairement laissé de
côté, et de quoi continuer.

## Les raccourcis assumés du tutoriel

À chaque étape, on a choisi la version simple plutôt que la version rapide. Ce
n'était pas de la paresse : c'était le bon arbitrage pour un outil qu'on lance à
la main. Voici le récapitulatif de ce qu'on a laissé sur la table.

| Raccourci | Où | Ce qu'aurait donné la version rapide |
|---|---|---|
| `String` dans `EvalError` | ch. 05 | `EvalError<'a>` avec des `&'a str` : zéro allocation sur le chemin d'erreur |
| `self.stack.clone()` par ligne | ch. 09 | un tampon `scratch` réutilisé dans l'`Engine` |
| `Vec<Step>` avec deux `Vec<f64>` par pas | ch. 11 | trois tampons plats indexés, réutilisés d'une ligne à l'autre |
| `trace: bool` testé à chaque jeton | ch. 11 | un paramètre `const TRACE: bool`, résolu à la compilation |
| `fmt_num(n) -> String` | ch. 10 | un `struct Num(f64)` qui implémente `Display` |
| `text.to_string()` sans couleur | ch. 12 | `Cow<str>`, qui emprunte au lieu de copier |
| un `println!` par ligne d'aide | ch. 14 | la sortie verrouillée une fois et tamponnée |

Ces sept points ne sont pas laissés en exercice : **les chapitres 16 et 17 les
appliquent pour de vrai, un par un, avec les mesures avant et après.** Le résultat
complet est dans `tuto/opti/`.

Va les lire quand tu seras à l'aise avec tout ce qui précède — pas avant. Ils
supposent que la version simple est parfaitement claire pour toi, puisqu'ils
passent leur temps à s'y comparer.

## Des exercices, par difficulté

### Pour se chauffer

1. **Ajoute `asin`, `acos`, `atan`.** Une ligne chacun dans `ops.rs`. Pense au
   domaine de `asin` et `acos` : `[-1, 1]`.
2. **Ajoute `deg` et `rad`** pour convertir les angles. `f64` a déjà
   `to_degrees()` et `to_radians()`.
3. **Ajoute `depth`** qui empile le nombre d'éléments de la pile.
4. **Ajoute `pick`** : `[a b c] 2 pick` recopie le 2ᵉ en partant du sommet.
   Attention à l'erreur si l'index dépasse.

### Pour comprendre la structure

5. **Une commande `stack`** qui affiche toute la pile, joliment, sans la modifier.
6. **Un mot `undo`.** Garde un `Vec<Vec<f64>>` des états précédents dans
   l'`Engine`, et limite-le à une vingtaine d'entrées. Question à trancher :
   `undo` doit-il annuler une ligne ou un jeton ?
7. **Des variables nommées.** `3 4 + x sto` range le résultat, `x rcl` le
   rappelle. Il faudra une `HashMap<String, f64>` dans l'`Engine` — et là, la
   `HashMap` est le bon outil, parce que les clés sont créées à l'exécution
   (relis la fin du chapitre 07).
8. **Lire un fichier de script.** `blap -f calcul.rpn` évalue chaque ligne.
   Regarde `std::fs::read_to_string` et `lines()`. Décide ce qui se passe si la
   ligne 12 échoue : on s'arrête, ou on continue ?

### Pour aller plus loin en Rust

9. **Transforme le projet en bibliothèque + binaire.** Renomme la logique en
   `src/lib.rs`, garde `src/main.rs` comme simple interface. Tu découvriras la
   différence entre ce qui doit être `pub` et ce qui ne doit pas l'être, et tu
   pourras écrire de vrais tests d'intégration dans `tests/`.
10. **Passe `EvalError` aux durées de vie** : remplace les `String` par des
    `&'a str`. Fais-le en une fois, laisse le compilateur te guider signature par
    signature. C'est un excellent exercice, il n'est dangereux nulle part, et le
    chapitre 17 donne la correction.
11. **Historique et édition de ligne.** Flèche haut pour rappeler la ligne
    précédente, `Ctrl-A`, etc. C'est le moment d'ajouter ta première dépendance :
    `rustyline`. Regarde comme `Cargo.toml` et `cargo add` rendent ça indolore.
12. **Une couverture de tests mesurée.** `cargo install cargo-llvm-cov`, puis
    `cargo llvm-cov`. Vise les branches non couvertes, et demande-toi pour
    chacune si elle mérite un test ou si elle est morte.
13. **Compile pour le web.** `cargo build --target wasm32-unknown-unknown`. Il
    faudra remplacer l'interface terminal, mais **tout `eval.rs` et `ops.rs`
    fonctionnent tels quels** — c'est la récompense d'avoir séparé le moteur de
    l'affichage au chapitre 06.

### Le gros morceau

14. **Un mode infixe.** Accepter `(2 + 3) * 4` en plus de la RPN. Cherche
    l'*algorithme de la gare de triage* (*shunting-yard*) de Dijkstra : il
    transforme une expression infixe en RPN, avec une pile d'opérateurs. Une fois
    la conversion faite, **ton évaluateur actuel n'a pas besoin d'être touché** —
    tu ajoutes un module `infix.rs` en amont.

    C'est le prolongement naturel de ce projet, et c'est là qu'on comprend
    vraiment pourquoi la RPN existe.

## Où continuer à apprendre le Rust

- **[Le Book](https://doc.rust-lang.org/book/)** — le livre officiel, gratuit,
  traduit en français. La référence pour reprendre les fondations dans l'ordre.
- **[Rust by Example](https://doc.rust-lang.org/rust-by-example/)** — le même
  contenu en exemples exécutables. Bon complément si tu apprends par la pratique.
- **[Rustlings](https://github.com/rust-lang/rustlings)** — de petits exercices
  où tu répares du code qui ne compile pas. Redoutablement efficace pour
  apprivoiser le vérificateur d'emprunts.
- **[La doc de `std`](https://doc.rust-lang.org/std/)** — à lire pour de vrai. La
  moitié de ce qu'on a écrit dans ce tutoriel existe déjà là-dedans. `cargo doc
  --open` ouvre la même chose hors ligne, augmentée de tes propres modules.
- **[Rust for Rustaceans](https://nostarch.com/rust-rustaceans)** — quand les
  bases seront acquises et que tu voudras comprendre ce qui se passe en dessous.

## Le mot de la fin

Ce qui rend ce projet intéressant n'est pas la calculatrice. C'est que tu as
rencontré, sur un problème minuscule, à peu près tout ce qui fait le quotidien du
développement : choisir une représentation de données, décider où une erreur doit
être traitée, séparer le calcul de l'affichage, arbitrer entre simplicité et
performance, et écrire les tests qui figent les décisions prises.

Ces arbitrages sont les mêmes sur un projet de cent mille lignes. Ils sont juste
plus faciles à voir sur six cents.

Le tutoriel s'arrête ici. Si tu veux une seconde couche — comment on mesure du
code Rust, et comment on le rend trois fois plus rapide sans jamais deviner —
elle commence au chapitre 16.

---

**Bonus, facultatif :** [16 — Mesurer avant d'optimiser](16-mesurer-avant-d-optimiser.md)

**Retour au [sommaire](README.md).**
