# Le binaire

Ranger des données dans des octets, et les relire ailleurs sans se tromper.

## Un entier est un paquet de bits

```
  0b0000_1010  = 10
```

Les opérations à connaître, et les trois gestes qu'on refait sans arrêt :

```c
int lire      = (valeur >> position) & 1u;
unsigned mis  = valeur | (1u << position);
unsigned ote  = valeur & ~(1u << position);
```

Trois règles de sécurité, valables dans tous les langages :

**Travaille en non signé.** Le décalage à droite d'un négatif dépend de l'implémentation, et le
décalage à gauche qui déborde est un comportement indéfini en C et C++.

**Ne décale jamais de plus que la largeur du type.** `1u << 32` sur 32 bits est indéfini, pas
« zéro ».

**Parenthèses.** `&` et `|` sont **moins** prioritaires que `==`. `a & 1 == 0` se lit `a & (1 == 0)`.

## Empaqueter

Quand la place compte — un réseau, un format de fichier, un sommet GPU — on range plusieurs valeurs
dans un seul entier.

```c
uint32_t empaqueter(uint8_t r, uint8_t v, uint8_t b, uint8_t a) {
    return ((uint32_t)r << 24) | ((uint32_t)v << 16) | ((uint32_t)b << 8) | a;
}

uint8_t rouge(uint32_t couleur) { return (couleur >> 24) & 0xFF; }
```

La règle qui évite les bugs : **convertis avant de décaler**. `r << 24` sur un `uint8_t` promu en
`int` peut déborder ; `(uint32_t)r << 24` non.

L'autre usage courant est la **quantification** : ranger un flottant de `[0,1]` dans un octet.

```c
uint8_t quantifier(float valeur) {
    return (uint8_t)(fminf(fmaxf(valeur, 0.0f), 1.0f) * 255.0f + 0.5f);
}
```

Le `+ 0.5f` arrondit au lieu de tronquer. Sans lui, `1.0` peut ressortir à `254`.

C'est exactement ce que fait une texture 8 bits, et pourquoi les normal maps encodent `-1..1` en
`0..1` avant de le stocker.

## L'ordre des octets

Un entier de 4 octets peut être rangé dans deux ordres :

| Ordre | `0x12345678` en mémoire | Qui |
|---|---|---|
| **petit-boutiste** (little-endian) | `78 56 34 12` | x86, ARM en pratique |
| **gros-boutiste** (big-endian) | `12 34 56 78` | réseau, quelques anciennes machines |

Tant que les données restent dans un processus, l'ordre n'a aucune importance. Il devient critique
dès que les octets **sortent** : fichier, réseau, mémoire partagée entre machines.

La règle : **choisis un ordre pour ton format, écris-le dans la spécification, et convertis
explicitement.** L'ordre réseau est traditionnellement le gros-boutiste (`htonl`, `ntohl`), mais
beaucoup de formats modernes choisissent le petit-boutiste puisque c'est celui de presque toutes
les machines.

Ne dépends **jamais** de `memcpy` d'une structure vers un fichier : tu dépendrais à la fois de
l'ordre des octets, du remplissage et de l'alignement (voir `memoire.md`). Écris champ par champ.

## Les entiers de taille variable

Pour un format compact, la plupart des entiers sont petits. Le **varint** ne dépense que ce qu'il
faut : sept bits utiles par octet, le huitième dit « il y a une suite ».

```c
size_t ecrire_varint(uint8_t *sortie, uint64_t valeur) {
    size_t n = 0;
    while (valeur >= 0x80) {
        sortie[n++] = (uint8_t)(valeur | 0x80);
        valeur >>= 7;
    }
    sortie[n++] = (uint8_t)valeur;
    return n;
}
```

Un octet jusqu'à 127, deux jusqu'à 16 383. C'est ce qu'utilisent Protobuf, LEB128 et les formats de
sauvegarde compacts.

Le problème : un nombre **négatif** en complément à deux a tous ses bits de poids fort à 1, donc
`-1` occupe dix octets. D'où le **zigzag**, qui entrelace positifs et négatifs :

```c
uint64_t zigzag(int64_t valeur) { return (valeur << 1) ^ (valeur >> 63); }
```

`0 → 0`, `-1 → 1`, `1 → 2`, `-2 → 3`. Les petits négatifs redeviennent de petits entiers.

## Un format de fichier qui tient

Cinq choses à mettre dans l'en-tête, dans cet ordre :

1. **Un nombre magique** — quatre octets reconnaissables. Ça évite de lire un fichier qui n'est pas
   le tien, et `file` peut l'identifier.
2. **Une version** — un octet. Le jour où le format change, tu peux encore lire les anciens.
3. **La taille ou le nombre d'éléments** — pour allouer d'un coup et détecter la troncature.
4. **Les données.**
5. **Une somme de contrôle** — CRC32 suffit pour détecter la corruption (pas pour détecter une
   modification volontaire : ce n'est pas de la cryptographie).

Et la règle la plus importante : **ne fais jamais confiance à ce que tu lis.** Un champ de taille
lu dans un fichier peut valoir quatre milliards. Vérifie-le contre la taille réelle du fichier
avant d'allouer quoi que ce soit. C'est le mécanisme d'une bonne moitié des failles dans les
lecteurs de formats.

## Écrire sans se faire couper

Un fichier écrit directement est corrompu si le programme meurt au milieu. L'écriture **atomique**
règle ça :

1. écrire dans `sauvegarde.tmp` ;
2. forcer l'écriture sur le disque (`fsync`, `FlushFileBuffers`) ;
3. renommer `sauvegarde.tmp` en `sauvegarde.dat`.

Le renommage est atomique sur les systèmes de fichiers courants : le fichier final est soit
l'ancien, soit le nouveau, jamais un mélange. C'est trois lignes de plus, et ça évite les
sauvegardes perdues.

## Binaire ou texte

| | Binaire | Texte (JSON, TOML) |
|---|---|---|
| taille | petite | 3 à 10× plus grosse |
| vitesse de lecture | rapide | lente (parsing) |
| lisible à l'œil | non | oui |
| modifiable à la main | non | oui |
| diff, git | inutilisable | parfait |
| évolutivité | à prévoir | naturelle |

**Texte pour ce que des humains touchent** : configuration, données de jeu éditées à la main,
formats d'échange. **Binaire pour ce que seule la machine touche** : sauvegardes, réseau, caches,
assets.

Beaucoup de projets font les deux : texte en développement, binaire cuit à la compilation.

## À retenir

1. Non signé, parenthèses, jamais décaler de plus que la largeur.
2. Convertis avant de décaler, arrondis en quantifiant.
3. L'ordre des octets ne compte que quand les données sortent du processus.
4. Jamais de `memcpy` d'une structure vers un fichier.
5. Varint plus zigzag pour des entiers compacts.
6. Magie, version, taille, données, somme de contrôle.
7. Ne fais jamais confiance à une taille lue dans un fichier.
8. Écris dans un temporaire, puis renomme.
