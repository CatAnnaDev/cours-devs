# 04 — Les règles de zéro, trois et cinq

## Les cinq opérations

```cpp
Chose(const Chose &);                  // 1. constructeur de copie
Chose &operator=(const Chose &);       // 2. affectation par copie
Chose(Chose &&) noexcept;              // 3. constructeur de déplacement
Chose &operator=(Chose &&) noexcept;   // 4. affectation par déplacement
~Chose();                              // 5. destructeur
```

Le compilateur les génère pour toi, **sous conditions**. Et ces conditions sont la source de la
plupart des mauvaises surprises de performance du C++.

## La règle de zéro

> Si tes membres savent déjà se copier, se déplacer et se détruire, n'écris **aucune** des cinq.

```cpp
struct Personnage {
    std::string nom;
    std::vector<int> inventaire;
    std::unique_ptr<Arme> arme;
};
```

Cette classe se copie correctement (enfin, elle ne se copie pas, à cause du `unique_ptr`), se
déplace correctement, se détruit correctement, et **tu n'as rien écrit**. C'est la meilleure classe
possible : celle qu'on ne maintient pas.

C'est aussi la raison d'être de `unique_ptr` et des conteneurs standard : ils encapsulent la
gestion de ressource **une fois**, pour que tes classes n'aient plus jamais à le faire.

## La règle de trois

> Si tu écris l'une des trois — destructeur, constructeur de copie, affectation par copie — tu
> dois probablement écrire les trois.

Elle vient du C++98 et reste vraie. La logique : si ta classe a besoin d'un destructeur, c'est
qu'elle possède quelque chose ; si elle possède quelque chose, la copie par défaut — qui recopie
les membres un à un — va dupliquer un **pointeur**, pas la ressource.

```cpp
class Tampon {
    int *donnees_;
public:
    explicit Tampon(std::size_t n) : donnees_(new int[n]()) {}
    ~Tampon() { delete[] donnees_; }
};

Tampon a(4);
Tampon b = a;      // copie superficielle : b.donnees_ == a.donnees_
```

Les deux objets libèrent le même bloc à la destruction. C'est un `double-free`, et ASan l'attrape
immédiatement.

Trois issues, selon ce que la classe doit être :

**Copie profonde** — allouer et recopier :

```cpp
Tampon(const Tampon &autre) : donnees_(new int[autre.taille_]), taille_(autre.taille_) {
    std::copy(autre.donnees_, autre.donnees_ + autre.taille_, donnees_);
}
```

**Copie interdite** — souvent le bon choix :

```cpp
Tampon(const Tampon &) = delete;
Tampon &operator=(const Tampon &) = delete;
```

**Ne pas posséder de ressource brute** — remplacer `int *` par `std::vector<int>`, et retomber sur
la règle de zéro. C'est presque toujours la meilleure réponse.

## La règle de cinq, et le piège silencieux

C++11 ajoute le déplacement, et avec lui une règle qui coûte cher en silence :

> **Déclarer un destructeur — même vide, même `= default` — supprime la génération implicite des
> opérations de déplacement.**

```cpp
struct Tampon {
    std::vector<Sonde> donnees;
    ~Tampon() = default;          // cette ligne suffit
};

Tampon destination = std::move(source);   // COPIE, pas déplacement
```

Le déplacement n'existe pas, donc la résolution de surcharge retombe sur la copie, qui accepte les
rvalues via `const &`. Le programme est **correct**, juste beaucoup plus lent. Aucun avertissement,
aucun sanitizer.

Le tableau complet, à garder sous la main :

| Ce que tu déclares | Copie générée | Déplacement généré |
|---|---|---|
| rien | oui | oui |
| un destructeur | oui (déprécié) | **non** |
| un constructeur de copie | oui | **non** |
| un constructeur de déplacement | **non** (supprimée) | oui |

La règle de cinq en découle : **si tu en déclares une, déclare les cinq**, quitte à les mettre à
`= default`.

```cpp
~Tampon() = default;
Tampon(const Tampon &) = default;
Tampon &operator=(const Tampon &) = default;
Tampon(Tampon &&) noexcept = default;
Tampon &operator=(Tampon &&) noexcept = default;
```

C'est verbeux, et c'est justement pourquoi la règle de zéro est préférable : **la meilleure façon
de respecter la règle de cinq est de n'en avoir pas besoin.**

## Écrire un déplacement à la main

Quand la classe possède vraiment une ressource brute :

```cpp
Tampon(Tampon &&autre) noexcept : donnees_(autre.donnees_), taille_(autre.taille_) {
    autre.donnees_ = nullptr;
    autre.taille_ = 0;
}
```

Deux gestes, et les deux sont obligatoires :

**1. Voler** les membres de la source.
**2. Laisser la source dans un état valide et destructible.** Ici `nullptr`, parce que
`delete nullptr` ne fait rien. Sans cette ligne, les deux objets libèrent le même bloc.

**Le `noexcept` n'est pas décoratif.** `std::vector` ne déplace ses éléments lors d'une
réallocation que si leur déplacement est `noexcept` ; sinon il les **copie**, pour pouvoir revenir
en arrière si une exception survient au milieu. Un constructeur de déplacement non marqué
`noexcept` fait donc silencieusement copier tout un vecteur à chaque croissance.

## `= delete` et `= default`

```cpp
Ressource(const Ressource &) = delete;              // interdit, à la compilation
Ressource(Ressource &&) noexcept = default;         // généré, explicitement
```

`= delete` est un vrai outil de conception : il rend un usage **impossible** au lieu de le
décourager par un commentaire. Un verrou, un fichier, une connexion : les copier n'a aucun sens, et
l'interdire évite d'y penser.

`= default` dit « je veux la version du compilateur, et je le déclare exprès ». C'est ce qui
permet de respecter la règle de cinq sans écrire de code.

## Quand écrire quoi, en une table

| Ta classe | Ce que tu écris |
|---|---|
| des membres standard uniquement | **rien** |
| une ressource unique, non copiable | `= delete` sur la copie, `= default` sur le déplacement |
| une ressource copiable | les cinq, dont copie profonde |
| une base polymorphe | destructeur `virtual`, et les cinq (ou copie interdite) |

Le dernier cas mérite un mot : **une classe destinée à l'héritage doit avoir un destructeur
virtuel**, sinon détruire par un pointeur de base n'appelle pas le destructeur dérivé. Et déclarer
ce destructeur supprime les déplacements — la règle de cinq s'applique, une fois de plus.

## À retenir

1. Le mieux est de n'écrire **aucune** des cinq opérations.
2. Déclarer un destructeur supprime les déplacements — sans avertissement.
3. Une copie par défaut sur une ressource brute est une copie superficielle, donc un `double-free`.
4. Un constructeur de déplacement doit laisser la source **valide**, pas seulement vidée.
5. `noexcept` sur le déplacement, sinon `vector` copie.
6. `= delete` est de la conception, pas de la restriction.

**Exercices : `04_regle_zero`.**
