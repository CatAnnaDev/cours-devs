# Tutoriel — construire `blap` depuis zéro

Un tutoriel pas à pas pour écrire une calculatrice RPN complète en Rust, sans
aucune dépendance externe, en partant d'un dossier vide.

## Ce qu'on va toucher au passage

Les types de base et `f64` · `Vec` et les tranches (`slice`) · `String` et `&str` ·
l'emprunt et la possession · `Option` et `Result` · le `match` et ses gardes ·
les `enum` avec données · les `struct` et les `impl` · les traits (`Display`,
`Default`, `Error`) · les `derive` · les modules et la visibilité · les closures
passées en paramètre · les itérateurs (`map`, `fold`, `collect`) · les entrées /
sorties · les arguments de ligne de commande · les codes de sortie · les tests
unitaires · `cargo fmt` et `cargo clippy`.

## Les chapitres

| # | Chapitre | Ce qu'on y apprend |
|---|----------|--------------------|
| 01 | [Avant de commencer](01-avant-de-commencer.md) | Installer Rust, `cargo`, le premier programme |
| 02 | [La notation RPN](02-la-notation-rpn.md) | Le concept, à la main, avant de coder |
| 03 | [La pile en Rust](03-la-pile-en-rust.md) | `Vec<f64>`, `push`, `pop`, possession |
| 04 | [Lire une ligne, la découper](04-lire-une-ligne-et-la-decouper.md) | `&str`, `split_whitespace`, `parse`, `Option` |
| 05 | [Des erreurs propres](05-des-erreurs-propres.md) | `enum`, `Result`, `?`, `Display` |
| 06 | [Des modules et une struct](06-des-modules-et-une-struct.md) | `mod`, `pub`, `struct`, `impl` |
| 07 | [Le dictionnaire d'opérateurs](07-le-dictionnaire-d-operateurs.md) | Gros `match`, closures en paramètre |
| 08 | [Manipuler la pile](08-manipuler-la-pile.md) | Tranches, `fold`, `rotate_left` |
| 09 | [Tout ou rien](09-tout-ou-rien.md) | Évaluation atomique, restauration |
| 10 | [La ligne de commande](10-la-ligne-de-commande.md) | `args`, REPL, `stdin`, `flush`, code de sortie |
| 11 | [Le mode trace](11-le-mode-trace.md) | Enregistrer et expliquer chaque étape |
| 12 | [Des couleurs](12-des-couleurs.md) | ANSI, `IsTerminal`, `NO_COLOR` |
| 13 | [Les tests](13-les-tests.md) | `#[cfg(test)]`, `assert_eq!`, `cargo test` |
| 14 | [Finitions](14-finitions.md) | Aide, `fmt`, `clippy`, `--release`, git |
| 15 | [Pour aller plus loin](15-pour-aller-plus-loin.md) | Exercices, pistes, ressources |

**Le tutoriel se termine au chapitre 15.** À ce stade tu as le programme complet,
celui de [`src/`](../src) à la racine du dépôt.

## Bonus — la seconde couche

Deux chapitres facultatifs, à lire seulement quand le reste est digéré. Ils
reprennent le programme fini et le réécrivent pour la vitesse, en mesurant tout.

| # | Chapitre | Ce qu'on y apprend |
|---|----------|--------------------|
| 16 | [Mesurer avant d'optimiser](16-mesurer-avant-d-optimiser.md) | Banc d'essai, `black_box`, compteur d'allocations |
| 17 | [La grosse passe d'optimisation](17-la-grosse-passe-d-optimisation.md) | Durées de vie, arène indexée, `Cow`, `const` générique, profil de compilation |

Le résultat est un projet séparé et complet : [`opti/`](opti/). Le code de `src/`
à la racine ne bouge pas — les deux versions coexistent pour être comparées.

## Annexes

À consulter au besoin, dans n'importe quel ordre.

| Annexe | Contenu |
|--------|---------|
| [A — Décoder les erreurs du compilateur](annexe-a-erreurs-du-compilateur.md) | Les douze erreurs que tu vas vraiment rencontrer, et quoi faire |
| [B — Aide-mémoire](annexe-b-aide-memoire.md) | Toute la syntaxe et les méthodes vues, sur une page |

## Comment suivre

Tape le code toi-même, ne copie-colle pas. Le compilateur Rust est le meilleur
prof du monde : ses messages d'erreur disent presque toujours quoi corriger, et
tu ne les liras jamais si tout marche du premier coup.

À chaque chapitre, lance :

```bash
cargo run
cargo test
```

Si ça ne compile pas, lis le message en entier — vraiment en entier, du haut
jusqu'au `help:` en bas. C'est là qu'est la réponse. Et si le message reste
opaque, l'[annexe A](annexe-a-erreurs-du-compilateur.md) traduit les plus fréquents.

Le code final se trouve dans `src/` à la racine du dépôt : si tu bloques, compare.
