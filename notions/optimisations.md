# Les petites optimisations (valables partout)

Neniri, ces conseils marchent quel que soit le langage (Rust, Java...) et quel que soit le but
du projet (un mod Hytale, un outil, un script). Mais d'abord, **la règle d'or** :

> **D'abord ça marche et c'est lisible. Ensuite seulement on optimise — et uniquement ce qui
> est vraiment lent, après l'avoir mesuré.**

Optimiser au hasard, c'est perdre du temps et rendre le code illisible pour gagner des
microsecondes que personne ne remarquera. On dit que « l'optimisation prématurée est la
racine de bien des maux » — et c'est vrai.

---

## Le plus gros levier, de loin : le bon algorithme / la bonne structure

Avant toute micro-astuce : ton meilleur gain vient de la **complexité** (`big-o.md`) et du
**choix de collection** (`collections.md`). Passer un O(n²) en O(n) avec un `HashSet`, ça vaut
mille micro-optimisations. **Commence toujours par là.**

---

## Les optimisations générales (tous langages)

1. **Choisis la bonne structure de données.**
   `HashMap`/`HashSet` pour retrouver/tester vite ; `Vec`/`ArrayList` pour l'accès par index.
   (Voir `collections.md`.)

2. **Sors le travail répété des boucles.**
   Si une valeur ne change pas dans la boucle, calcule-la **une seule fois avant** :
   ```text
   // ❌ recalcule liste.length à chaque tour
   pour i de 0 à liste.length: ...
   // ✅ calcule une fois
   n = liste.length ; pour i de 0 à n: ...
   ```

3. **Réserve la capacité quand tu connais la taille.**
   `Vec::with_capacity(n)` (Rust) / `new ArrayList<>(n)` (Java) → évite les recopies (voir
   `collections.md`).

4. **Évite les copies et allocations inutiles.**
   - Rust : **emprunte** (`&`) au lieu de cloner ; un `.clone()` sur une grosse donnée coûte
     cher. N'appelle `.clone()` que si tu en as vraiment besoin.
   - Java : ne crée pas d'objets à l'intérieur d'une boucle « chaude » si tu peux les
     réutiliser. Pour construire du texte dans une boucle, utilise **`StringBuilder`** au lieu
     de `+=` sur une `String` (sinon tu recrées toute la chaîne à chaque tour : O(n²) !).

5. **Sors tôt (`return` / `break`).**
   Dès que tu as la réponse, arrête. Inutile de continuer à parcourir une liste après avoir
   trouvé ce que tu cherchais.

6. **Mets en cache (mémoïse) un résultat coûteux réutilisé.**
   Si tu recalcules sans cesse la même chose chère, calcule-la une fois et **range le
   résultat** (dans une variable, un `HashMap`...).

7. **Regroupe les entrées/sorties (I/O).**
   Lire/écrire un fichier, le réseau, une base de données... c'est lent. Fais-le **par lots**
   plutôt que mille petites fois dans une boucle.

---

## Mesurer, ne pas deviner

Tu ne sais pas *où* ton programme passe son temps tant que tu ne l'as pas mesuré. Avant
d'optimiser :

- entoure le bout suspect d'un petit chrono (`Instant::now()` en Rust, `System.nanoTime()` en
  Java — exemples dans `big-o.md`) ;
- compare **avant / après** ton changement.

Souvent, le vrai point lent n'est **pas** là où on l'imaginait. Mesure d'abord.

---

## Selon la destination du projet

**Un mod Hytale, un jeu, un serveur** (du code qui tourne en boucle, à chaque *tick* ou
chaque image) :
- Le code exécuté **à chaque tick/frame** est critique : la moindre lourdeur y est multipliée
  des dizaines de fois par seconde.
- **Évite d'allouer à chaque tick** (créer des listes/objets en continu fait travailler le
  ramasse-miettes et provoque des saccades). Prépare/réutilise tes structures.
- **Ne bloque pas le thread principal / du monde** avec du gros travail (I/O, gros calculs,
  attente réseau) : ça fige le serveur pour tout le monde.
- Utilise des structures **O(1)** (`HashMap`) pour tout ce que tu consultes souvent (les
  données des joueuses, par exemple).

**Un outil ou un script ponctuel** (qui tourne une fois, à la main) :
- La **lisibilité** prime. N'optimise quasiment rien : si ça met 2 secondes une fois par jour,
  c'est très bien.

---

## Mini-checklist avant d'optimiser

1. Est-ce que ça **marche** et que c'est **lisible** ? (sinon, finis ça d'abord)
2. Est-ce **vraiment** lent, mesuré ? (sinon, ne touche à rien)
3. Si oui : ai-je le bon **algorithme** et la bonne **structure de données** ? (le gros gain)
4. Ensuite seulement : les petites astuces ci-dessus.

---

## À retenir

- **Lisible et juste d'abord ; optimiser ensuite, et seulement le prouvé-lent.**
- Le **gros** gain = bon algo + bonne structure (`big-o.md`, `collections.md`).
- Les **petits** gains = sortir le travail des boucles, réserver la capacité, éviter les copies,
  sortir tôt, mettre en cache, grouper les I/O.
- **Mesure** au lieu de deviner.
- Adapte l'effort à la destination : un mod/jeu en boucle ≠ un script lancé une fois.

Quand tu veux, dis-le-moi et je t'ajoute un vrai exercice chronométré pour t'entraîner à
repérer et corriger un point lent.
