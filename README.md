# DnD Character Manager

Konsolowa aplikacja w języku C# (.NET 8) do zarządzania postaciami w systemie RPG Dungeons & Dragons 5. edycji. Projekt realizowany w ramach zaliczenia przedmiotu **Zaawansowane Programowanie Obiektowe** (semestr 5.).

Aplikacja umożliwia tworzenie postaci trzech klas (Wojownik, Czarodziej, Lotrzyk), losowanie statystyk, zarządzanie ekwipunkiem, symulację walki oraz asynchroniczny zapis stanu postaci do pliku JSON.

---

## Instrukcja uruchomienia

**Wymagania:** .NET SDK w wersji 6, 7 lub 8 (zalecana 8).

```bash
# 1. Sklonuj repozytorium
git clone https://github.com/slavomirus/ZOOP-Janik-66615-DND-Character-Manager.git
cd DnDCharacterManager

# 2. Zbuduj projekt
dotnet build

# 3. Uruchom aplikację
dotnet run
```

Aplikacja jest w całości interaktywna — wszystkie operacje wykonuje się przez menu numeryczne w konsoli.

---

## Zastosowane mechanizmy OOP (Wymagania Zaliczeniowe)

### 1. Klasy

Projekt definiuje hierarchię klas modelującą domenę gry: `Item` (bazowa klasa przedmiotów), `Weapon`, `Armor`, `Potion` (konkretne przedmioty), `Character` (bazowa klasa postaci) oraz `Fighter`, `Wizard`, `Rogue` (klasy postaci). Pomocnicze klasy `DiceRoller`, `SaveSystem` i `Reflector` enkapsulują logikę narzędziową.

### 2. Konstruktory

Każda klasa posiada własny konstruktor inicjalizujący pola specyficzne dla danego typu — na przykład `Weapon(string name, int weight, int value, int damage, string damageType)` ustawia wszystkie atrybuty broni, a konstruktory klas pochodnych (`Fighter`, `Wizard`, `Rogue`) wywołują konstruktor bazowy `Character` przez słowo kluczowe `base(...)` i doinicjalizowują właściwości specyficzne dla klasy (np. `SpellSlots` w `Wizard`).

### 3. Właściwości oraz Indeksatory

Klasa `Character` udostępnia właściwości publiczne (`Name`, `CurrentHp`, `MaxHp`, `ArmorBonus`) z kontrolowanym dostępem — `CurrentHp` posiada prywatny setter implementujący logikę ucinania wartości do zakresu `[0, MaxHp]` i wywołujący zdarzenia. Klasa generyczna `Inventory<T>` definiuje **dwa indeksatory**: `this[string name]` (wyszukiwanie przedmiotu po nazwie, ignorując wielkość liter) oraz `this[int index]` (dostęp pozycyjny), co demonstruje przeciążanie indeksatora.

### 4. Elementy statyczne

Klasa `DiceRoller` jest w całości statyczna i zawiera wspólną, prywatną instancję `Random` (pole statyczne `_rng`). Udostępnia statyczne metody `Roll(int count, int sides)`, `RollStats()` i `GetModifier(int score)`, zapewniając jedyny, globalny punkt dostępu do losowości w aplikacji. Klasa `Reflector` jest analogicznie statyczna.

### 5. Dziedziczenie

Klasa abstrakcyjna `Character` jest bazą dla `Fighter`, `Wizard` i `Rogue`. Klasa abstrakcyjna `Item` jest bazą dla `Weapon`, `Armor` i `Potion`. Dziedziczenie umożliwia współdzielenie logiki (obsługa HP, ekwipunek, zdarzenia) przy jednoczesnej specjalizacji zachowań w klasach pochodnych. Klasy pochodne `Character` są tworzone i przechowywane jako referencja do typu bazowego (`Character _activeCharacter`).

### 6. Polimorfizm

Metoda abstrakcyjna `RollHitDie()` w `Character` jest nadpisana (`override`) w każdej z trzech klas postaci, rzucając odpowiednią kostką hitpointów (k10 dla Wojownika, k6 dla Czarodzieja, k8 dla Łotrzyka). Metoda wirtualna `GetClassInfo()` jest nadpisywana w celu zwrócenia opisu specyficznego dla klasy. Dzięki polimorfizmowi metoda `PrintStats()` wywołuje `GetClassInfo()` bez znajomości konkretnego typu postaci.

### 7. Interfejsy i Klasy Abstrakcyjne

Interfejs `IUsable` wymusza implementację metody `Use(Character target)` w klasach `Weapon`, `Armor` i `Potion`, zapewniając jednolity kontrakt użycia przedmiotu niezależnie od jego typu. Interfejs `IDescribable` wymusza metodę `GetDescription()`. Klasy `Item` i `Character` są **abstrakcyjne** — nie można ich instancjonować bezpośrednio; wymuszają na klasach pochodnych implementację metod abstrakcyjnych (`RollHitDie()`, `GetDescription()`).

### 8. Typy ogólne / Kolekcje

Klasa `Inventory<T> where T : Item` jest **generycznym kontenerem** ograniczonym ograniczeniem typu do `Item`. Używa wewnętrznie `List<T>` i udostępnia metody typowane (`Add(T)`, `GetAll()`, indeksatory zwracające `T`). W kodzie aplikacji używana jest jako `Inventory<Item>` — pole `Backpack` klasy `Character`.

### 9. Delegacje i Zdarzenia

Zdefiniowano dwa typy delegatów: `CharacterDiedHandler(Character)` i `HpChangedHandler(Character, int, int)`. Klasa `Character` deklaruje zdarzenia (`event`) `OnDeath` i `HpChange`, subskrybowane w metodzie `CreateCharacter()` w `Program.cs` za pomocą wyrażeń lambda. Zdarzenie `OnDeath` wypisuje komunikat o śmierci postaci; `HpChange` informuje o każdej zmianie punktów życia.

### 10. Przeciążanie operatorów

Operator `+` jest przeciążony w klasie `Character`: wyrażenie `_activeCharacter += newItem` jest równoważne wywołaniu `_activeCharacter.Backpack.Add(newItem)` i zwraca referencję do postaci. Pozwala to na czytelny, idiomatyczny zapis dodawania przedmiotów do ekwipunku postaci.

### 11. Programowanie asynchroniczne

Klasa `SaveSystem` implementuje metody `SaveCharacterAsync` i `LoadCharacterAsync` z użyciem słów kluczowych `async`/`await`. Zapis i odczyt pliku JSON odbywa się przez `File.WriteAllTextAsync` i `File.ReadAllTextAsync` — nieblokujące operacje I/O. Metody menu (`SaveCharacterMenu`, `LoadCharacterMenu`) w `Program.cs` są oznaczone `async Task` i `await`ują operacje I/O.

### 12. Refleksja

Klasa statyczna `Reflector` używa przestrzeni nazw `System.Reflection`. Metoda `PrintProperties(object obj)` pobiera wszystkie publiczne właściwości instancyjne przekazanego obiektu przez `GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)`, a następnie iteruje po nich, odczytując nazwy, typy i wartości. Opcja debugowania w menu inspektuje aktywną postać, pierwszy przedmiot w plecaku lub metody statycznej klasy `DiceRoller`.

---

## Wyjście poza materiał zajęciowy (Ocena BDB)

### Zapytania LINQ

Klasa `Inventory<T>` udostępnia metody `FilterByMaxWeight` i `FilterByMaxValue`, które używają wyrażeń LINQ (`Where`, `OrderBy`) do filtrowania i sortowania kolekcji przedmiotów. W `UseItemMenu()` w `Program.cs` LINQ filtruje ekwipunek do przedmiotów implementujących `IUsable` (`Where(i => i is IUsable)`). Wszystkie operacje LINQ działają na `IEnumerable<T>`, korzystając z mechanizmu odroczonego wykonania.

### Serializacja JSON (System.Text.Json)

Do zapisu stanu postaci zastosowano wbudowaną bibliotekę `System.Text.Json` (dostępną od .NET Core 3.0, bez zewnętrznych zależności). Klasa `CharacterSaveData` jest opatrzona atrybutami `[JsonPropertyName]` definiującymi nazwy kluczy w formacie snake\_case. Serializacja jest konfigurowana przez `JsonSerializerOptions` z włączonym `WriteIndented = true`, generując czytelny, sformatowany plik JSON. Operacja zapisu i odczytu jest w całości asynchroniczna.

---

## Struktura projektu

```
DnDCharacterManager/
├── DnDCharacterManager.csproj   # Definicja projektu .NET 8
└── Program.cs                   # Cały kod źródłowy aplikacji
```

Cały kod zawarty jest w jednym pliku `Program.cs`, pogrupowanym w przestrzeni nazw `DnDCharacterManager`. Plik dzieli się logicznie na następujące sekcje:

| Sekcja | Zawartość |
|---|---|
| Interfejsy | `IUsable`, `IDescribable` |
| Hierarchia przedmiotów | `Item` (abstrakt), `Weapon`, `Armor`, `Potion` |
| Kolekcja generyczna | `Inventory<T>` |
| Delegaty | `CharacterDiedHandler`, `HpChangedHandler` |
| Hierarchia postaci | `Character` (abstrakt), `Fighter`, `Wizard`, `Rogue` |
| Narzędzia | `DiceRoller` (statyczny), `Reflector` (statyczny) |
| Zapis stanu | `CharacterSaveData`, `SaveSystem` (async) |
| Punkt wejścia | `Program` — menu `Main()` oparte na `switch/case` |

---

## Autor
Krzysztof Janik 
66615
README file prepared by Claude Sonnet 4.6

Projekt zaliczeniowy — Zaawansowane Programowanie Obiektowe, semestr 5.
