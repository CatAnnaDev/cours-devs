# Mesurer

Comment savoir si c'est vraiment plus rapide — et pourquoi la moitié des mesures publiées ne
mesurent rien.

## Les trois règles

**1. Mesure avant d'optimiser.** L'intuition sur les performances est mauvaise, chez tout le
monde, y compris chez les gens expérimentés. Le goulot est presque jamais là où on le croit.

**2. Mesure la bonne chose.** Un profileur te dit où le temps passe. Un chronomètre te dit combien
de temps une chose prend. Ce ne sont pas les mêmes questions.

**3. Optimise ce qui compte.** Diviser par deux une fonction qui représente 2 % du temps total
fait gagner 1 %. C'est la loi d'Amdahl, et elle est impitoyable.

## Les pièges d'un micro-benchmark

### Le compilateur supprime ton code

```c
double debut = maintenant();
for (int i = 0; i < 1000000; i++) {
    calculer(i);              // résultat inutilisé
}
double fin = maintenant();
```

Le résultat n'est pas utilisé, donc l'appel peut être supprimé — et il l'est, en `-O2`. Tu mesures
une boucle vide.

La parade : accumuler le résultat et l'utiliser après la mesure, ou passer par une fonction que le
compilateur ne peut pas voir (`DoNotOptimize` de Google Benchmark, `std::hint::black_box` en Rust).

### La première itération n'est pas représentative

Le cache est froid, les branches ne sont pas prédites, le code n'est pas encore chargé, et sur une
machine virtuelle (JVM, .NET) le code n'est même pas encore compilé en natif. **Fais tourner à vide
avant de mesurer**, puis mesure plusieurs fois.

En Java et C#, le compilateur JIT peut mettre des milliers d'itérations avant d'optimiser une
boucle. Un benchmark JVM sans échauffement mesure l'interpréteur.

### Une seule mesure ne veut rien dire

La machine fait autre chose : un autre processus, la fréquence qui varie, un thread migré d'un cœur
à l'autre. Prends **beaucoup** de mesures et regarde la **médiane** — pas la moyenne, qui est
détruite par un seul pic.

Et regarde aussi la dispersion : si le minimum et le maximum diffèrent d'un facteur trois, la
mesure ne vaut rien, quelle que soit sa médiane.

### Les données de test ne sont pas les vraies

Trier un tableau déjà trié, chercher dans une table qui tient en cache, parser un fichier de trois
lignes : tout ça donne des chiffres qui ne prédisent rien. **Mesure sur des données de la taille et
de la forme réelles.**

Le cas classique : un algorithme qui bat tous les autres jusqu'à ce que les données dépassent le
cache L2, et qui s'écroule après.

## Ce qu'un profileur t'apprend

| Type | Comment | Ce qu'il donne |
|---|---|---|
| **échantillonnage** | interrompt le programme N fois par seconde | où le temps passe, faible surcoût |
| **instrumentation** | compte chaque appel | des comptes exacts, gros surcoût, résultat déformé |
| **compteurs matériels** | lit les compteurs du processeur | ratés de cache, branches mal prédites |

Commence **toujours** par l'échantillonnage : peu invasif, et il répond à la seule question qui
compte au début — *où passe le temps ?*

| Plateforme | Outil |
|---|---|
| Linux | `perf record` / `perf report` |
| macOS | Instruments (Time Profiler), `sample` |
| Windows | Visual Studio Profiler, VTune |
| Java | async-profiler, JFR |
| C# | dotnet-trace, Visual Studio |
| Godot / Unity / Unreal | leur profileur intégré, plus RenderDoc pour le GPU |

## Calcul ou mémoire ?

La question la plus utile après « où passe le temps ». Deux façons d'y répondre :

**Divise les données par deux.** Si le temps est divisé par deux, tu es limité par la mémoire ou
par le volume. S'il ne bouge presque pas, tu es limité par autre chose.

**Regarde les compteurs de cache.** `perf stat` donne les ratés de cache et les branches mal
prédites. Un taux de ratés élevé sur une boucle simple pointe vers `cache.md`.

Sur le GPU, la même question se pose en termes d'ALU contre bande passante : réduis la résolution
de moitié — si le temps chute proportionnellement, tu es limité par les pixels.

## Comparer honnêtement

**Change une seule chose à la fois.** Deux modifications, un gain : tu ne sais pas laquelle.

**Même machine, même conditions.** Un portable sur batterie réduit sa fréquence. Un autre programme
en fond change tout.

**Vérifie que les deux versions font la même chose.** L'optimisation la plus efficace de
l'histoire est celle qui supprime un calcul par erreur.

**Donne le contexte.** « 2× plus rapide » ne veut rien dire sans la taille des données, la machine,
le compilateur et son niveau d'optimisation.

## Quand ne pas optimiser

**Si ça n'est pas mesurable par l'utilisateur.** Une fonction appelée trois fois au démarrage n'a
aucune importance, quelle que soit sa complexité.

**Si ça détruit la lisibilité pour 3 %.** Le code sera lu cent fois et exécuté sur des machines qui
doublent de vitesse tous les quelques ans.

**Avant que ça marche.** Un code faux et rapide n'a aucune valeur.

Le contre-exemple, à connaître aussi : **certaines décisions ne se rattrapent pas après coup.** La
disposition des données en mémoire, le choix d'un conteneur au cœur d'une architecture, un pas de
temps variable dans une physique — ça se décide au début. « On optimisera plus tard » ne marche
que pour les optimisations locales.

## À retenir

1. Mesure avant. L'intuition se trompe, toujours.
2. Utilise le résultat, sinon le compilateur supprime ton benchmark.
3. Échauffe, répète, prends la médiane, regarde la dispersion.
4. Mesure sur des données réelles, à la taille réelle.
5. Échantillonnage d'abord ; les compteurs matériels ensuite.
6. Une modification à la fois, même machine, même sortie.
7. Ce qui ne se rattrape pas : la disposition des données et l'architecture.
