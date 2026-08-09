# 00 — Avant de commencer

## Le C++ n'est pas « le C avec des classes »

Il l'a été, en 1983. Aujourd'hui c'est un langage différent, avec une idée directrice que le C n'a
pas : **les types savent gérer leurs propres ressources**.

En C, tu écris `malloc` puis `free`, et tu es responsable de ce qui se passe entre les deux. En
C++, tu écris un `std::vector` et le langage garantit qu'il se libérera — même si tu sors par une
exception, même si tu sors par un `return` au milieu, même si tu l'oublies.

Ce que tu paies pour ça : **le code n'a plus l'air de ce qu'il fait**. Une affectation peut
allouer et copier un mégaoctet. Une boucle innocente peut construire et détruire un objet par tour.
Un `shared_ptr` passé par valeur incrémente un compteur atomique.

C'est tout le sujet de ce cours : **retrouver, sous la syntaxe, ce que la machine fait
vraiment**.

## Compiler à la main, une fois

```cpp
#include <print>

int main() {
    std::println("salut");
    return 0;
}
```

```bash
c++ -std=c++23 essai.cpp -o essai
./essai
```

Les options qu'on utilisera :

| Option | Effet |
|---|---|
| `-std=c++20` / `-std=c++23` | la version du langage — elle change beaucoup de choses |
| `-Wall -Wextra` | les avertissements utiles |
| `-g` | les numéros de ligne |
| `-fsanitize=address,undefined` | les deux détecteurs |

Le choix de la norme n'est pas cosmétique en C++ : les concepts et les ranges sont C++20,
`std::expected` et `std::print` sont C++23. Le runner l'indique pour chaque exercice.

## Lire un mur d'erreurs de template

C'est **la** difficulté d'entrée du C++, et elle décourage plus de gens que les pointeurs.

```
In file included from exercices/00_intro/intro3.cpp:1:
/usr/include/c++/v1/__algorithm/sort.h:642:17: error: no matching function for call to object
of type '(lambda at intro3.cpp:12:44)'
  642 |         if (__comp(*__j, *__i))
      |             ^~~~~~
/usr/include/c++/v1/__algorithm/sort.h:1000:5: note: in instantiation of function template
specialization 'std::__sort4[abi:ne200100]<...>' requested here
...
exercices/00_intro/intro3.cpp:12:5: note: in instantiation of function template specialization
'std::sort<...>' requested here
   12 |     std::sort(noms.begin(), noms.end(), [](int a, int b) {
      |     ^
```

Trente lignes, dont vingt-huit parlent de fichiers que tu n'as jamais ouverts. La méthode, en
trois gestes :

**1. Cherche la première ligne `error:`.** Tout ce qui vient après est une conséquence. Ici :
« pas de fonction correspondante pour un objet de type lambda ».

**2. Cherche `in instantiation of ... requested here` en partant du BAS.** La dernière occurrence
pointe **ta** ligne. Ici : `intro3.cpp:12`, ton appel à `std::sort`.

**3. Recolle les deux.** Le tri appelle ton comparateur avec les éléments du conteneur — des
`std::string` — et ta lambda prend des `int`. C'est tout.

Le reste du mur est la **pile d'instanciation** : `sort` a appelé `__sort4`, qui a appelé
`__comp`. C'est utile deux fois par an ; le reste du temps, ignore-le.

**Les concepts (C++20) réduisent beaucoup ce problème** en vérifiant les contraintes à l'entrée
plutôt qu'au fond de l'implémentation. Quand tu écris tes propres templates, contrains-les :
c'est autant pour le message d'erreur que pour la correction.

## Les sanitizers

Comme en C, et pour les mêmes raisons : on instrumente le programme, il s'arrête à la faute
exacte, il raconte tout.

Ce qui change, c'est **ce qu'ils attrapent**. En C++ les fautes classiques sont différentes :

**`heap-use-after-free` après un `push_back`.** Un `std::vector` garde ses éléments dans un bloc
continu ; quand il n'a plus de place, il en alloue un autre, déplace tout, et libère l'ancien.
Tout pointeur, référence ou itérateur vers l'ancien bloc devient pendant. C'est **la** faute
numéro un du C++ moderne, parce qu'elle n'a l'air de rien.

**`container-overflow`.** libc++ annote ses conteneurs : ASan connaît la différence entre `size()`
et `capacity()`. Lire `v[3]` sur un vecteur de trois éléments dont la capacité est huit est
attrapé — alors qu'en C pur, personne ne l'aurait vu.

**`stack-use-after-return` sur une lambda.** Une lambda qui capture par référence garde une
référence, pas une copie. Si elle survit à la portée capturée, elle pointe sur du vide.

**`double-free` par copie superficielle.** Une classe qui possède un `new` et laisse le
compilateur générer sa copie : les deux objets pointent sur le même bloc et le libèrent tous les
deux.

Les quatre arrivent dans les exercices de ce cours, avec un rapport lisible à chaque fois.

## Ce que les sanitizers ne voient pas

**Les copies inutiles.** Elles sont correctes, juste coûteuses. C'est pour ça que le cours fournit
`verif::Sonde`, qui les compte.

**Les fuites**, sur macOS ARM : LeakSanitizer n'y existe pas. Le cours compare donc
`constructions` et `destructions`, ce qui revient au même et marche partout.

**Les exceptions non gérées, les invariants cassés, la logique fausse.** Un programme peut passer
tous les sanitizers et être entièrement faux.

## Compter avec la Sonde

```cpp
verif::Compteur::remettre_a_zero();

std::vector<verif::Sonde> sondes;
sondes.reserve(3);
sondes.emplace_back(1);

VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie");
```

`verif::Sonde` incrémente un compteur dans chacune de ses cinq opérations. Ça transforme des
affirmations vagues en vérifications :

| Compteur | Ce qu'il révèle |
|---|---|
| `constructions` | combien d'objets ont été créés de zéro |
| `copies` | combien ont été **dupliqués** — c'est ce qu'on veut à zéro |
| `deplacements` | combien ont été pillés — bon marché, mais pas gratuit |
| `destructions` | doit égaler constructions + copies + déplacements |

Prends l'habitude, dans ton propre code, de mettre un compteur temporaire dans un constructeur de
copie quand tu doutes. C'est cinq minutes, et ça remplace une heure de lecture.

## Les cinq opérations

Chaque type C++ a cinq opérations que le compilateur peut générer. Il faut les connaître de nom
dès maintenant, on y revient au chapitre 04 :

```cpp
struct Chose {
    Chose(const Chose &);              // constructeur de copie
    Chose &operator=(const Chose &);   // affectation par copie
    Chose(Chose &&) noexcept;          // constructeur de déplacement
    Chose &operator=(Chose &&) noexcept; // affectation par déplacement
    ~Chose();                          // destructeur
};
```

**Copier** duplique la ressource. **Déplacer** la vole en laissant la source vide mais valide.
Un `std::string` de dix mégaoctets se copie en dix mégaoctets et se déplace en trois pointeurs.

Toute la différence de performance entre du C++ écrit correctement et du C++ écrit naïvement tient
dans cette phrase.

## En route

```bash
cd cpplings
make
./cpplings
```

Le premier exercice ne demande rien. Le deuxième ne compile pas, exprès. Le troisième te sort un
mur de templates, exprès — et tu viens de lire comment le lire.
