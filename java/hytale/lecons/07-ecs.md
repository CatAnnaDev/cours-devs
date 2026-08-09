# Leçon 7 — Comprendre l'ECS (Entity Component System) — AVANCÉ

**Prérequis :** Leçons 1 à 6, et la leçon Java sur l'héritage/interfaces (Lecon12).
**Objectif :** comprendre l'**architecture** que Hytale utilise pour tout ce qui « existe »
dans le jeu (joueuses, créatures, objets...). Pas besoin de tout maîtriser : l'idée est que
tu reconnaisses les mots `Store`, `Ref`, `getComponent` quand tu les croises (tu les as déjà
vus en Leçon 6, dans l'UI).

C'est une « mini » leçon : juste le mental model.

---

## L'idée en trois mots

L'**ECS** range le jeu en trois briques :

- **Entité** (*Entity*) : une « chose » du jeu. En vrai, ce n'est presque rien — juste un
  identifiant. Une joueuse, un mob, un coffre... c'est chacun une entité.
- **Composant** (*Component*) : une **donnée attachée** à une entité. Par exemple : une
  position, des points de vie, un composant `Player`... Un composant, c'est juste des données.
- **Système** (*System*) : la **logique** qui agit sur les entités qui possèdent certains
  composants (ex. « pour toutes les entités qui ont une position ET une vitesse, déplace-les »).

En résumé : **une entité POSSÈDE des composants ; les systèmes font tourner la logique.**

---

## ECS vs héritage classique

En POO « classique » (Leçon 12), on aurait tendance à faire des arbres d'héritage :
`Player extends Creature extends Entity...` — et ça devient vite rigide.

L'ECS fait le contraire : la **composition**. Au lieu de dire « un Player EST une sorte de
Creature », on dit « cette entité A un composant Player, A un composant Position, A un
composant Vie ». On **assemble** des composants comme des briques. C'est plus souple (on
ajoute/retire une capacité en ajoutant/retirant un composant) et plus rapide pour le moteur.

> Tu te souviens de la note « côté Rust » en Lecon12 : Rust préfère lui aussi la composition
> à l'héritage. L'ECS, c'est cette même philosophie poussée à fond, pour les jeux.

---

## Comment ça se traduit dans le template

Dans `UISubCommand` (Leçon 6), tu as vu ces lignes :

```java
Player player = store.getComponent(ref, Player.getComponentType());
player.getPageManager().openCustomPage(ref, store, dashboardPage);
```

Traduisons avec les mots de l'ECS :

- **`Ref<EntityStore> ref`** = « QUELLE entité » (un repère vers une entité précise — ici,
  celle de la joueuse qui a tapé la commande).
- **`Store<EntityStore> store`** = « LE REGISTRE » où sont rangées les entités et leurs
  composants (le stockage du monde).
- **`store.getComponent(ref, Player.getComponentType())`** = « donne-moi le composant
  **Player** de cette entité ». Une fois le composant en main, tu lis/agis dessus
  (ici, ouvrir une page d'interface).

Autrement dit : pour faire quelque chose à une entité, tu demandes au `store` le **composant**
qui t'intéresse, à partir de sa **`ref`**.

---

## Ce que tu dois retenir

1. **Entité** = une chose (juste un identifiant) ; **Composant** = des données attachées ;
   **Système** = la logique.
2. L'ECS, c'est de la **composition** (« A un... ») plutôt que de l'héritage (« EST un... »).
3. Dans Hytale : **`Ref`** = quelle entité, **`Store`** = le registre, **`getComponent(...)`**
   = récupère tel composant de cette entité.
4. Tu ne construis pas l'ECS : Hytale te le fournit, tu l'**utilises**.

Quand tu voudras vraiment manipuler des entités (créer un mob, lire/modifier la vie d'une
joueuse, etc.), dis-le-moi et on fera une leçon dédiée avec des exemples concrets.
