# Rapport de Projet — DrawMe
## Application de dessin vectoriel en C# / WPF
**Module :** Programmation des Interfaces Interactives Avancées (PIIA)  
**Niveau :** Licence 3 Informatique  
**Date :** Avril 2026  

---

## 1. Introduction

Dans le cadre du module PIIA, ce projet consiste à concevoir et développer une application de dessin vectoriel destinée à des chefs de projets souhaitant créer des schémas intégrables dans Word ou PowerPoint. L'objectif est de proposer un outil professionnel, intuitif et complet, reposant sur des technologies modernes et des principes ergonomiques reconnus.

---

## 2. Description de l'Application

**DrawMe** est une application de bureau développée en C# avec WPF (.NET 8). Elle permet de créer, éditer et sauvegarder des dessins vectoriels composés de lignes, rectangles et ellipses. Les dessins sont persistés au format JSON (extension `.drawme`) et restent entièrement éditables après rechargement.

L'interface se structure en trois zones :
- **Barre d'outils gauche** : accès rapide aux outils de dessin, édition (Undo/Redo, suppression) et gestion de fichiers.
- **Barre de propriétés supérieure** : couleur de remplissage, couleur de contour, épaisseur du trait, contrôles de zoom, application de couleur à la sélection.
- **Canvas central** : zone de dessin interactive avec ScrollViewer.

---

## 3. Fonctionnalités Implémentées

### Fonctionnalités minimales

| Fonctionnalité | État | Détails |
|---|---|---|
| Ligne | ✅ | Tracé par drag, hit-test précis (distance au segment) |
| Rectangle | ✅ | Tracé normalisé, hit-test contour + remplissage |
| Ellipse | ✅ | Hit-test par équation canonique de l'ellipse |
| Sélection | ✅ | Clic sur forme, bordure pointillée bleue |
| Déplacement (drag & drop) | ✅ | Delta souris, MoveShapeCommand pour Undo |
| Redimensionnement (8 poignées) | ✅ | Thumb WPF, ResizeShapeCommand |
| Couleur avant dessin | ✅ | FillColor + StrokeColor dans la barre de propriétés |
| Modifier couleur sélection | ✅ | ChangeColorCommand + bouton "Appliquer à la sélection" |
| Z-Index (premier plan / arrière-plan) | ✅ | ChangeZIndexCommand |

### Fonctionnalités bonus

| Fonctionnalité | État | Détails |
|---|---|---|
| Sauvegarde / Chargement JSON | ✅ | Format `.drawme` (JSON), formes restent éditables |
| Zoom | ✅ | Molette souris + boutons, ScaleTransform |
| Undo / Redo | ✅ | Command Pattern complet, Ctrl+Z / Ctrl+Y |
| Épaisseur du trait | ✅ | Slider de 0,5 à 30 px |
| Raccourcis clavier | ✅ | Voir tableau ci-dessous |

### Raccourcis clavier

| Raccourci | Action |
|---|---|
| `V` | Outil Pointeur |
| `L` | Outil Ligne |
| `R` | Outil Rectangle |
| `E` | Outil Ellipse |
| `Suppr` | Supprimer la sélection |
| `A` | Appliquer couleur à la sélection |
| `Ctrl+Z` | Annuler |
| `Ctrl+Y` | Rétablir |
| `Ctrl+S` | Sauvegarder |
| `Ctrl+O` | Ouvrir |
| `Ctrl+N` | Nouveau dessin |
| `Ctrl++` / `Ctrl+-` | Zoom + / - |
| `Ctrl+0` | Zoom 100% |
| `Ctrl+]` / `Ctrl+[` | Avant-plan / Arrière-plan |
| `Ctrl+Molette` | Zoom à la molette |

---

## 4. Choix Techniques

### Langage et Framework

**C# avec WPF (.NET 8)** a été choisi pour les raisons suivantes :
- WPF est le framework de référence pour les applications de bureau riches sous Windows.
- Le système XAML permet une séparation nette entre la vue et la logique.
- Les primitives graphiques natives (`Canvas`, `Line`, `Rectangle`, `Ellipse`, `Thumb`) éliminent le besoin de bibliothèques tierces.
- Les liaisons de données (`Binding`) et `INotifyPropertyChanged` facilitent l'implémentation du pattern MVVM.

### Format de sauvegarde

**JSON** via `System.Text.Json` (inclus dans .NET 8). Le polymorphisme est géré par `[JsonPolymorphic]` et `[JsonDerivedType]` : chaque forme est sérialisée avec un discriminant `$type` ("line", "rectangle", "ellipse"), ce qui garantit une désérialisation correcte et des formes éditables après rechargement.

---

## 5. Architecture Logicielle

L'application suit le pattern **MVVM (Model-View-ViewModel)** enrichi du **Command Pattern** (GoF) pour Undo/Redo.

```
DrawMe/
├── Models/             → Données pures (DrawingShapeBase, DrawingLine, DrawingRectangle,
│                         DrawingEllipse, DrawingDocument)
├── Commands/           → Command Pattern (IDrawingCommand, DrawingCommandManager,
│                         AddShape, DeleteShape, Move, Resize, ChangeColor, ChangeZIndex)
├── ViewModels/         → MainViewModel (INotifyPropertyChanged, ObservableCollection)
├── Views/              → MainWindow.xaml + .cs, DrawingCanvas.cs
└── Helpers/            → GeometryHelper, JsonDocumentHelper
```

### Diagramme des responsabilités

```
MainWindow.xaml  ────bindings────►  MainViewModel
      │                                   │
      │ injecte                     Shapes, SelectedShape, Tool...
      ▼                                   │
DrawingCanvas ◄──────────────────── CommandManager (Undo/Redo)
      │
      ├── MouseEvents → DrawingShapeBase instances
      ├── Hit-test     → HitTest() abstrait (polymorphisme)
      └── Thumb drag   → ApplyResizeDelta() + PushResizeCommand()
```

### Principes SOLID respectés

| Principe | Application |
|---|---|
| **S**ingle Responsibility | Chaque classe a une responsabilité unique (GeometryHelper ≠ DrawingCanvas ≠ JsonHelper) |
| **O**pen/Closed | `DrawingShapeBase` est extensible sans modification (ajout futur de `DrawingPolygon`) |
| **L**iskov | `DrawingLine`, `DrawingRectangle`, `DrawingEllipse` sont substituables à `DrawingShapeBase` |
| **I**nterface Segregation | `IDrawingCommand` est minimal (Execute, Undo, Description) |
| **D**ependency Inversion | `DrawingCanvas` reçoit le ViewModel par injection |

---

## 6. Justification Ergonomique (UX)

### Loi de Hick-Hyman

> *Le temps de décision augmente logarithmiquement avec le nombre d'options.*

L'interface limite volontairement les outils visibles : 4 outils de dessin regroupés en haut de la barre latérale, les actions d'édition en dessous, les actions de fichier tout en bas. L'utilisateur ne voit jamais plus de 3-4 choix par groupe. Les outils rarement utilisés (Z-index) sont regroupés avec une étiquette de section.

### Loi de Fitts

> *Le temps pour atteindre une cible est fonction de sa taille et de sa distance.*

- Les boutons d'outils font **44×44 px**, bien au-delà du minimum recommandé (44 px sur mobile).
- La barre d'outils est placée **à gauche** : accès rapide depuis le canvas sans traverser toute la fenêtre.
- Les **8 poignées de redimensionnement** ont une taille de 10×10 px et leurs zones de clic sont generously sized.
- La barre de propriétés est fixée en **haut**, accessible sans déplacer la souris loin du canvas.

### Heuristiques de Nielsen

| Heuristique | Application |
|---|---|
| **Visibilité de l'état du système** | Barre de statut (bas) : outil actif, action en cours, formes sauvegardées |
| **Correspondance système/monde réel** | Icônes universelles : crayon, rectangle, ellipse, corbeille, disquette |
| **Contrôle et liberté de l'utilisateur** | Undo/Redo complet jusqu'à 100 niveaux + raccourcis Ctrl+Z/Y |
| **Cohérence et standards** | Raccourcis standards (Ctrl+S, Ctrl+O, Ctrl+Z), curseur contextuellement adapté |
| **Prévention des erreurs** | Taille minimale de forme (4 px) évite les créations accidentelles ; confirmation avant "Nouveau" |
| **Reconnaissance plutôt que mémorisation** | Tooltips sur tous les boutons, bouton actif visuellement distinct (bordure rouge) |
| **Flexibilité et efficacité** | Raccourcis clavier complets (V, L, R, E, Suppr, A...) pour les utilisateurs avancés |
| **Esthétique et design minimaliste** | Palette sombre, séparateurs clairs, pas de fonctionnalités superflues dans la toolbar |
| **Aide à la reconnaissance des erreurs** | Messages clairs en cas d'erreur de chargement/sauvegarde (MessageBox) |

### Théorie de Norman (Design Centré Utilisateur)

- **Affordances** : les boutons ont un aspect cliquable (coins arrondis, effet hover bleu), les poignées de redimensionnement sont des carrés distincts (rouge sur fond blanc) signalant leur draggabilité.
- **Feedback** : la sélection d'une forme produit immédiatement une bordure pointillée bleue + 8 poignées rouges. La barre de statut confirme chaque action.
- **Modèle conceptuel** : le canvas ressemble à une feuille blanche sur un bureau gris, cohérent avec le modèle mental des logiciels de dessin (Visio, Inkscape).
- **Contraintes** : on ne peut pas créer une forme de taille < 4 px ; les formes ne peuvent pas avoir une taille négative lors du redimensionnement.
- **Manipulation directe** : drag & drop natif, redimensionnement en temps réel, feedback visuel pendant le tracé (forme fantôme en cours de dessin).

---

## 7. Difficultés Rencontrées

### 7.1 Sérialisation polymorphique JSON

La sérialisation de `List<DrawingShapeBase>` en JSON nécessite la préservation des types concrets. Avant .NET 7, cela exigeait `Newtonsoft.Json` avec `TypeNameHandling`. Depuis .NET 7, `System.Text.Json` supporte `[JsonPolymorphic]` et `[JsonDerivedType]`, ce qui a permis d'éviter une dépendance externe.

### 7.2 Redimensionnement via Thumb en temps réel

La difficulté principale est que le redimensionnement se produit en temps réel (au fil du drag), mais le Command Pattern nécessite un état "before" et un état "after" atomiques. La solution adoptée : capturer un `ShapeSnapshot` au `DragStarted` et l'enregistrer dans la pile Undo au `DragCompleted` via `PushResizeCommand()`, sans re-exécuter la commande.

### 7.3 Référence aux propriétés auto depuis ref

En C#, les propriétés auto-implémentées ne sont pas adressables par `ref`. Le refactoring en accès direct aux propriétés (read/write explicite) a été nécessaire pour la logique de redimensionnement.

### 7.4 Sélecteur de couleur WPF

WPF ne fournit pas de `ColorPicker` natif. La solution la plus simple et la plus compatible est d'utiliser `System.Windows.Forms.ColorDialog` via l'interop WinForms (`UseWindowsForms=true`). Une alternative serait un `ColorPicker` XAML custom ou une bibliothèque tierce (MaterialDesignInXamlToolkit).

---

## 8. Améliorations Possibles

- **Grille d'accrochage (Snap to Grid)** : aligner automatiquement les formes sur une grille configurable.
- **Polygone / Chemin libre** : ajouter un outil Freehand et Polygone.
- **Texte** : permettre l'insertion de `TextBlock` éditables dans le canvas.
- **Export PNG/SVG** : exporter vers des formats images ou vectoriels pour intégration dans Word/PowerPoint.
- **Duplication** : Ctrl+D pour dupliquer la sélection.
- **Sélection multiple** : sélectionner plusieurs formes par rectangle de sélection (rubber band).
- **ColorPicker XAML natif** : remplacer le ColorDialog WinForms par un sélecteur WPF pour une meilleure cohérence visuelle.
- **Palette de couleurs personnalisées** : mémoriser les couleurs récemment utilisées.
- **Connexion entre formes** (type Visio) : flèches avec points de connexion sur les formes.

---

## 9. Instructions d'Exécution

### Prérequis
- **Visual Studio 2022** (ou plus récent) avec la charge de travail "Développement .NET Desktop"
- **OU** .NET 8 SDK autonome (`dotnet run` en ligne de commande)
- Système d'exploitation : **Windows 10/11** (WPF est Windows uniquement)

### Compilation et lancement

**Via Visual Studio :**
1. Ouvrir `DrawMe.sln`
2. Sélectionner la configuration `Debug` ou `Release`
3. Appuyer sur `F5` (avec débogage) ou `Ctrl+F5` (sans débogage)

**Via ligne de commande :**
```cmd
cd C:\...\DrawMe
dotnet run
```

**Compilation seule :**
```cmd
dotnet build DrawMe.sln
```

### Structure des fichiers générés
```
DrawMe/
├── Models/               Formes (DrawingShapeBase, Line, Rectangle, Ellipse, Document)
├── Commands/             Undo/Redo (IDrawingCommand, DrawingCommandManager, DrawingCommands)
├── ViewModels/           MainViewModel
├── Views/                MainWindow.xaml + .cs, DrawingCanvas.cs
├── Helpers/              GeometryHelper.cs, JsonDocumentHelper.cs
├── App.xaml + .cs        Point d'entrée, styles globaux
├── DrawMe.csproj         Projet .NET 8 WPF
└── DrawMe.sln            Solution Visual Studio
```
