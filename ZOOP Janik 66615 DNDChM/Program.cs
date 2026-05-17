using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DnDCharacterManager
{
    public interface IUsable
    {
        string Use(Character target);
    }

    public interface IDescribable
    {
        string GetDescription();
    }

    public abstract class Item : IDescribable
    {
        public string Name { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }

        protected Item(string name, int weight, int value)
        {
            Name = name;
            Weight = weight;
            Value = value;
        }

        public abstract string GetDescription();

        public override string ToString()
        {
            return $"{Name} (waga: {Weight}, wartość: {Value} sz)";
        }
    }

    public class Weapon : Item, IUsable
    {
        public int Damage { get; set; }
        public string DamageType { get; set; }

        public Weapon(string name, int weight, int value, int damage, string damageType)
            : base(name, weight, value)
        {
            Damage = damage;
            DamageType = damageType;
        }

        public string Use(Character target)
        {
            int rolled = DiceRoller.Roll(1, 20);
            if (rolled == 20)
            {
                int dmg = DiceRoller.Roll(2, Damage) + DiceRoller.Roll(2, Damage);
                target.TakeDamage(dmg);
                return $"TRAFIENIE KRYTYCZNE! {Name} zadaje {dmg} obrazen ({DamageType}) celowi {target.Name}!";
            }
            else if (rolled >= 10)
            {
                int dmg = DiceRoller.Roll(1, Damage) + DiceRoller.Roll(1, 6);
                target.TakeDamage(dmg);
                return $"Trafienie! {Name} zadaje {dmg} obrazen ({DamageType}) celowi {target.Name}.";
            }
            else
            {
                return $"Pudlo! Atak bronia {Name} nie trafil w cel.";
            }
        }

        public override string GetDescription()
        {
            return $"Bron: {Name}, obrazenia: k{Damage} {DamageType}, waga: {Weight}, wartosc: {Value} sz";
        }
    }

    public class Armor : Item, IUsable
    {
        public int ArmorClass { get; set; }

        public Armor(string name, int weight, int value, int armorClass)
            : base(name, weight, value)
        {
            ArmorClass = armorClass;
        }

        public string Use(Character target)
        {
            target.ArmorBonus = ArmorClass;
            return $"{target.Name} zaklada {Name} (KP: +{ArmorClass}).";
        }

        public override string GetDescription()
        {
            return $"Zbroja: {Name}, KP: +{ArmorClass}, waga: {Weight}, wartosc: {Value} sz";
        }
    }

    public class Potion : Item, IUsable
    {
        public int HealAmount { get; set; }

        public Potion(string name, int weight, int value, int healAmount)
            : base(name, weight, value)
        {
            HealAmount = healAmount;
        }

        public string Use(Character target)
        {
            int before = target.CurrentHp;
            target.Heal(HealAmount);
            int actual = target.CurrentHp - before;
            return $"{target.Name} wypija {Name} i odzyskuje {actual} PZ.";
        }

        public override string GetDescription()
        {
            return $"Mikstura: {Name}, leczenie: {HealAmount} PZ, waga: {Weight}, wartosc: {Value} sz";
        }
    }

    public class Inventory<T> where T : Item
    {
        private List<T> _items = new List<T>();

        public int Count => _items.Count;

        public T this[string name]
        {
            get
            {
                return _items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public T this[int index]
        {
            get => _items[index];
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public bool Remove(T item)
        {
            return _items.Remove(item);
        }

        public IEnumerable<T> GetAll()
        {
            return _items.AsReadOnly();
        }

        public IEnumerable<T> FilterByMaxWeight(int maxWeight)
        {
            return _items.Where(i => i.Weight <= maxWeight).OrderBy(i => i.Name);
        }

        public IEnumerable<T> FilterByMaxValue(int maxValue)
        {
            return _items.Where(i => i.Value <= maxValue).OrderBy(i => i.Value);
        }

        public int TotalWeight => _items.Sum(i => i.Weight);
        public int TotalValue => _items.Sum(i => i.Value);
    }

    public delegate void CharacterDiedHandler(Character character);
    public delegate void HpChangedHandler(Character character, int oldHp, int newHp);

    public abstract class Character
    {
        public string Name { get; set; }
        public string Race { get; set; }
        public int Level { get; set; }
        public int MaxHp { get; protected set; }

        private int _currentHp;
        public int CurrentHp
        {
            get => _currentHp;
            private set
            {
                int old = _currentHp;
                _currentHp = Math.Max(0, Math.Min(value, MaxHp));
                OnHpChanged(old, _currentHp);
                if (_currentHp == 0)
                    OnCharacterDied();
            }
        }

        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        public int ArmorBonus { get; set; }
        public bool IsAlive => CurrentHp > 0;

        public Inventory<Item> Backpack { get; private set; } = new Inventory<Item>();

        public event CharacterDiedHandler OnDeath;
        public event HpChangedHandler OnHpChange;

        protected Character(string name, string race, int level)
        {
            Name = name;
            Race = race;
            Level = level;
            ArmorBonus = 0;
        }

        protected abstract int RollHitDie();

        public virtual void Initialize()
        {
            MaxHp = RollHitDie() + DiceRoller.GetModifier(Constitution);
            MaxHp = Math.Max(1, MaxHp);
            _currentHp = MaxHp;
        }

        public void TakeDamage(int amount)
        {
            CurrentHp -= amount;
        }

        public void Heal(int amount)
        {
            CurrentHp += amount;
        }

        public virtual string GetClassInfo()
        {
            return "Brak klasy";
        }

        public void PrintStats()
        {
            Console.WriteLine($"--- {Name} [{Race}] - Poziom {Level} ---");
            Console.WriteLine($"Klasa: {GetClassInfo()}");
            Console.WriteLine($"PZ: {CurrentHp}/{MaxHp}   KP: {10 + ArmorBonus + DiceRoller.GetModifier(Dexterity)}");
            Console.WriteLine($"SIL:{Strength,3} ({DiceRoller.GetModifier(Strength):+#;-#;0})  ZRE:{Dexterity,3} ({DiceRoller.GetModifier(Dexterity):+#;-#;0})  ZDR:{Constitution,3} ({DiceRoller.GetModifier(Constitution):+#;-#;0})");
            Console.WriteLine($"INT:{Intelligence,3} ({DiceRoller.GetModifier(Intelligence):+#;-#;0})  MAD:{Wisdom,3} ({DiceRoller.GetModifier(Wisdom):+#;-#;0})  CHA:{Charisma,3} ({DiceRoller.GetModifier(Charisma):+#;-#;0})");
            Console.WriteLine($"Ekwipunek: {Backpack.Count} przedmiotow (waga: {Backpack.TotalWeight}, wartosc: {Backpack.TotalValue} sz)");
        }

        private void OnHpChanged(int oldHp, int newHp)
        {
            OnHpChange?.Invoke(this, oldHp, newHp);
        }

        private void OnCharacterDied()
        {
            OnDeath?.Invoke(this);
        }

        public static Character operator +(Character character, Item item)
        {
            character.Backpack.Add(item);
            return character;
        }
    }

    public class Fighter : Character
    {
        public string FightingStyle { get; set; }
        public int ActionSurge { get; private set; }

        public Fighter(string name, string race, int level, string fightingStyle = "Obronny")
            : base(name, race, level)
        {
            FightingStyle = fightingStyle;
            ActionSurge = level >= 2 ? 1 : 0;
        }

        protected override int RollHitDie()
        {
            return DiceRoller.Roll(1, 10);
        }

        public override string GetClassInfo()
        {
            return $"Wojownik (Styl: {FightingStyle}, Atak szczytowy: {ActionSurge})";
        }
    }

    public class Wizard : Character
    {
        public string ArcaneSchool { get; set; }
        public int SpellSlots { get; private set; }

        public Wizard(string name, string race, int level, string arcaneSchool = "Ogolna")
            : base(name, race, level)
        {
            ArcaneSchool = arcaneSchool;
            SpellSlots = level * 2;
        }

        protected override int RollHitDie()
        {
            return DiceRoller.Roll(1, 6);
        }

        public override string GetClassInfo()
        {
            return $"Czarodziej (Szkola: {ArcaneSchool}, Miejsca czarow: {SpellSlots})";
        }
    }

    public class Rogue : Character
    {
        public int SneakAttackDice { get; private set; }

        public Rogue(string name, string race, int level)
            : base(name, race, level)
        {
            SneakAttackDice = (level + 1) / 2;
        }

        protected override int RollHitDie()
        {
            return DiceRoller.Roll(1, 8);
        }

        public override string GetClassInfo()
        {
            return $"Lotrzyk (Atak z zaskoczenia: {SneakAttackDice}k6)";
        }
    }

    public static class DiceRoller
    {
        private static readonly Random _rng = new Random();

        public static int Roll(int count, int sides)
        {
            if (sides <= 0 || count <= 0) return 0;
            int total = 0;
            for (int i = 0; i < count; i++)
                total += _rng.Next(1, sides + 1);
            return total;
        }

        public static int[] RollStats()
        {
            int[] stats = new int[6];
            for (int i = 0; i < 6; i++)
            {
                int[] rolls = { Roll(1, 6), Roll(1, 6), Roll(1, 6), Roll(1, 6) };
                Array.Sort(rolls);
                stats[i] = rolls[1] + rolls[2] + rolls[3];
            }
            return stats;
        }

        public static int GetModifier(int score)
        {
            return (int)Math.Floor((score - 10) / 2.0);
        }
    }

    public static class Reflector
    {
        public static void PrintProperties(object obj)
        {
            Console.WriteLine($"\n[REFLEKSJA] Typ: {obj.GetType().Name}");
            Console.WriteLine("Wlasciwosci:");
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                try
                {
                    if (prop.GetIndexParameters().Length > 0) continue;
                    var val = prop.GetValue(obj);
                    Console.WriteLine($"  {prop.Name} [{prop.PropertyType.Name}] = {val}");
                }
                catch
                {
                    Console.WriteLine($"  {prop.Name} [{prop.PropertyType.Name}] = <nie mozna odczytac>");
                }
            }
        }
    }

    [Serializable]
    public class CharacterSaveData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("race")]
        public string Race { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("class")]
        public string Class { get; set; }

        [JsonPropertyName("current_hp")]
        public int CurrentHp { get; set; }

        [JsonPropertyName("max_hp")]
        public int MaxHp { get; set; }

        [JsonPropertyName("strength")]
        public int Strength { get; set; }

        [JsonPropertyName("dexterity")]
        public int Dexterity { get; set; }

        [JsonPropertyName("constitution")]
        public int Constitution { get; set; }

        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }

        [JsonPropertyName("wisdom")]
        public int Wisdom { get; set; }

        [JsonPropertyName("charisma")]
        public int Charisma { get; set; }

        [JsonPropertyName("armor_bonus")]
        public int ArmorBonus { get; set; }

        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new List<string>();

        [JsonPropertyName("saved_at")]
        public string SavedAt { get; set; }
    }

    public static class SaveSystem
    {
        public static async Task SaveCharacterAsync(Character character, string filePath)
        {
            var data = new CharacterSaveData
            {
                Name = character.Name,
                Race = character.Race,
                Level = character.Level,
                Class = character.GetClassInfo(),
                CurrentHp = character.CurrentHp,
                MaxHp = character.MaxHp,
                Strength = character.Strength,
                Dexterity = character.Dexterity,
                Constitution = character.Constitution,
                Intelligence = character.Intelligence,
                Wisdom = character.Wisdom,
                Charisma = character.Charisma,
                ArmorBonus = character.ArmorBonus,
                Items = character.Backpack.GetAll().Select(i => i.GetDescription()).ToList(),
                SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task<CharacterSaveData> LoadCharacterAsync(string filePath)
        {
            string json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<CharacterSaveData>(json);
        }
    }

    class Program
    {
        static Character _activeCharacter = null;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            PrintHeader();

            bool running = true;
            while (running)
            {
                PrintMainMenu();
                string input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        CreateCharacter();
                        break;
                    case "2":
                        ShowCharacterStats();
                        break;
                    case "3":
                        RollDiceMenu();
                        break;
                    case "4":
                        AddItemMenu();
                        break;
                    case "5":
                        ShowInventoryMenu();
                        break;
                    case "6":
                        UseItemMenu();
                        break;
                    case "7":
                        await SaveCharacterMenu();
                        break;
                    case "8":
                        await LoadCharacterMenu();
                        break;
                    case "9":
                        ReflectionDebugMenu();
                        break;
                    case "0":
                        running = false;
                        Console.WriteLine("\nDo zobaczenia przy nastepnej sesji.");
                        break;
                    default:
                        Console.WriteLine("Nieznana opcja. Sprobuj ponownie.");
                        break;
                }
            }
        }

        static void PrintHeader()
        {
            Console.Clear();
            Console.WriteLine("=========================================");
            Console.WriteLine("       D&D CHARACTER MANAGER v1.0        ");
            Console.WriteLine("   Zaawansowane Programowanie Obiektowe  ");
            Console.WriteLine("=========================================");
            Console.WriteLine();
        }

        static void PrintMainMenu()
        {
            Console.WriteLine("\n--- MENU GLOWNE ---");
            if (_activeCharacter != null)
                Console.WriteLine($"  Aktywna postac: {_activeCharacter.Name} (PZ: {_activeCharacter.CurrentHp}/{_activeCharacter.MaxHp})");
            else
                Console.WriteLine("  Brak aktywnej postaci");
            Console.WriteLine();
            Console.WriteLine("  1. Stworz postac");
            Console.WriteLine("  2. Wyswietl statystyki");
            Console.WriteLine("  3. Rzut koscia");
            Console.WriteLine("  4. Dodaj przedmiot do ekwipunku");
            Console.WriteLine("  5. Wyswietl ekwipunek / LINQ");
            Console.WriteLine("  6. Uzyj przedmiotu");
            Console.WriteLine("  7. Zapisz postac do pliku");
            Console.WriteLine("  8. Wczytaj postac z pliku");
            Console.WriteLine("  9. Debug refleksja");
            Console.WriteLine("  0. Wyjdz");
            Console.Write("\nWybor: ");
        }

        static void CreateCharacter()
        {
            Console.WriteLine("\n--- TWORZENIE POSTACI ---");
            Console.Write("Podaj imie postaci: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) { Console.WriteLine("Imie nie moze byc puste."); return; }

            Console.WriteLine("Wybierz rase:");
            Console.WriteLine("  1. Czlowiek  2. Elf  3. Krasnolud  4. Polork  5. Gnom");
            Console.Write("Wybor: ");
            string raceInput = Console.ReadLine()?.Trim();
            string race = raceInput switch
            {
                "1" => "Czlowiek",
                "2" => "Elf",
                "3" => "Krasnolud",
                "4" => "Polork",
                "5" => "Gnom",
                _ => "Czlowiek"
            };

            Console.WriteLine("Wybierz klase:");
            Console.WriteLine("  1. Wojownik  2. Czarodziej  3. Lotrzyk");
            Console.Write("Wybor: ");
            string classInput = Console.ReadLine()?.Trim();

            Console.Write("Podaj poziom (1-20): ");
            if (!int.TryParse(Console.ReadLine(), out int level) || level < 1 || level > 20)
                level = 1;

            Console.WriteLine("\nRzucam statystyki (4k6, odrzucam najnizszy)...");
            int[] stats = DiceRoller.RollStats();
            Console.WriteLine($"Wyniki: SIL={stats[0]}, ZRE={stats[1]}, ZDR={stats[2]}, INT={stats[3]}, MAD={stats[4]}, CHA={stats[5]}");

            Character ch = classInput switch
            {
                "1" => new Fighter(name, race, level),
                "2" => new Wizard(name, race, level),
                "3" => new Rogue(name, race, level),
                _ => new Fighter(name, race, level)
            };

            ch.Strength = stats[0];
            ch.Dexterity = stats[1];
            ch.Constitution = stats[2];
            ch.Intelligence = stats[3];
            ch.Wisdom = stats[4];
            ch.Charisma = stats[5];

            ch.Initialize();

            ch.OnDeath += (dead) =>
            {
                Console.WriteLine($"\n!!! {dead.Name} padl na polu chwaly! PZ spadly do zera !!!");
            };

            ch.OnHpChange += (c, oldHp, newHp) =>
            {
                if (newHp < oldHp)
                    Console.WriteLine($"  [{c.Name} otrzymal obrazenia: {oldHp} -> {newHp} PZ]");
                else if (newHp > oldHp)
                    Console.WriteLine($"  [{c.Name} zostal uleczony: {oldHp} -> {newHp} PZ]");
            };

            _activeCharacter = ch;
            Console.WriteLine($"\nPostac {name} zostala stworzona!");
            _activeCharacter.PrintStats();
        }

        static void ShowCharacterStats()
        {
            if (_activeCharacter == null) { Console.WriteLine("Brak aktywnej postaci."); return; }
            Console.WriteLine();
            _activeCharacter.PrintStats();
        }

        static void RollDiceMenu()
        {
            Console.WriteLine("\n--- RZUT KOSCIA ---");
            Console.WriteLine("Format: XkY (np. 1k20, 2k6, 4k4)");
            Console.Write("Podaj rzut: ");
            string input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input) || !input.Contains('k'))
            {
                Console.WriteLine("Nieprawidlowy format.");
                return;
            }

            var parts = input.Split('k');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int count) || !int.TryParse(parts[1], out int sides))
            {
                Console.WriteLine("Nieprawidlowy format.");
                return;
            }

            if (count < 1 || count > 100 || sides < 2 || sides > 1000)
            {
                Console.WriteLine("Wartosci poza zakresem (kostki 2-1000, ilosc 1-100).");
                return;
            }

            int result = DiceRoller.Roll(count, sides);
            Console.WriteLine($"\nRzucasz {count}k{sides}... Wynik: {result}");

            if (count == 1 && sides == 20)
            {
                if (result == 20) Console.WriteLine("TRAFIENIE KRYTYCZNE! Naturalny 20!");
                else if (result == 1) Console.WriteLine("KATASTROFALNE PUDLO! Naturalny 1.");
            }
        }

        static void AddItemMenu()
        {
            if (_activeCharacter == null) { Console.WriteLine("Brak aktywnej postaci."); return; }

            Console.WriteLine("\n--- DODAWANIE PRZEDMIOTU ---");
            Console.WriteLine("Wybierz typ przedmiotu:");
            Console.WriteLine("  1. Bron  2. Zbroja  3. Mikstura  4. Gotowe zestawy startowe");
            Console.Write("Wybor: ");
            string choice = Console.ReadLine()?.Trim();

            Item newItem = null;

            switch (choice)
            {
                case "1":
                    newItem = CreateWeaponInteractive();
                    break;
                case "2":
                    newItem = CreateArmorInteractive();
                    break;
                case "3":
                    newItem = CreatePotionInteractive();
                    break;
                case "4":
                    AddStarterPack();
                    return;
                default:
                    Console.WriteLine("Nieznana opcja.");
                    return;
            }

            if (newItem != null)
            {
                _activeCharacter += newItem;
                Console.WriteLine($"Dodano '{newItem.Name}' do plecaka {_activeCharacter.Name}.");
            }
        }

        static Weapon CreateWeaponInteractive()
        {
            Console.Write("Nazwa broni: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) return null;

            Console.Write("Kostka obrazen (np. 6 dla k6): ");
            int.TryParse(Console.ReadLine(), out int dmg);
            if (dmg <= 0) dmg = 6;

            Console.Write("Typ obrazen (np. sieczne, przebijajace): ");
            string dmgType = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(dmgType)) dmgType = "fizyczne";

            Console.Write("Waga (sztuki): ");
            int.TryParse(Console.ReadLine(), out int weight);

            Console.Write("Wartosc (sztuki zlota): ");
            int.TryParse(Console.ReadLine(), out int value);

            return new Weapon(name, weight, value, dmg, dmgType);
        }

        static Armor CreateArmorInteractive()
        {
            Console.Write("Nazwa zbroi: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) return null;

            Console.Write("Bonus do KP: ");
            int.TryParse(Console.ReadLine(), out int ac);

            Console.Write("Waga: ");
            int.TryParse(Console.ReadLine(), out int weight);

            Console.Write("Wartosc: ");
            int.TryParse(Console.ReadLine(), out int value);

            return new Armor(name, weight, value, ac);
        }

        static Potion CreatePotionInteractive()
        {
            Console.Write("Nazwa mikstury: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) return null;

            Console.Write("Ilosc leczenia (PZ): ");
            int.TryParse(Console.ReadLine(), out int heal);
            if (heal <= 0) heal = 8;

            Console.Write("Waga: ");
            int.TryParse(Console.ReadLine(), out int weight);

            Console.Write("Wartosc: ");
            int.TryParse(Console.ReadLine(), out int value);

            return new Potion(name, weight, value, heal);
        }

        static void AddStarterPack()
        {
            _activeCharacter += new Weapon("Miecz dlugi", 3, 15, 8, "sieczne");
            _activeCharacter += new Armor("Skorzana zbroja", 10, 10, 2);
            _activeCharacter += new Potion("Mikstura leczenia", 1, 50, 8);
            _activeCharacter += new Potion("Wielka mikstura leczenia", 1, 100, 20);
            _activeCharacter += new Weapon("Sztylet", 1, 2, 4, "przebijajace");
            Console.WriteLine("Dodano zestaw startowy (miecz, zbroja, 2x mikstura, sztylet).");
        }

        static void ShowInventoryMenu()
        {
            if (_activeCharacter == null) { Console.WriteLine("Brak aktywnej postaci."); return; }

            Console.WriteLine("\n--- EKWIPUNEK ---");
            Console.WriteLine("  1. Wyswietl wszystkie przedmioty");
            Console.WriteLine("  2. Filtruj LINQ: przedmioty ponizej maksymalnej wagi");
            Console.WriteLine("  3. Filtruj LINQ: przedmioty ponizej maksymalnej wartosci");
            Console.WriteLine("  4. Wyszukaj przedmiot po nazwie (indeksator)");
            Console.Write("Wybor: ");
            string choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    PrintAllItems(_activeCharacter.Backpack.GetAll());
                    Console.WriteLine($"\nLaczna waga: {_activeCharacter.Backpack.TotalWeight}  Laczna wartosc: {_activeCharacter.Backpack.TotalValue} sz");
                    break;
                case "2":
                    Console.Write("Maksymalna waga przedmiotu: ");
                    if (int.TryParse(Console.ReadLine(), out int maxW))
                    {
                        var filtered = _activeCharacter.Backpack.FilterByMaxWeight(maxW);
                        Console.WriteLine($"\nPrzedmioty o wadze <= {maxW} (LINQ, posortowane alfabetycznie):");
                        PrintAllItems(filtered);
                    }
                    break;
                case "3":
                    Console.Write("Maksymalna wartosc przedmiotu: ");
                    if (int.TryParse(Console.ReadLine(), out int maxV))
                    {
                        var filtered = _activeCharacter.Backpack.FilterByMaxValue(maxV);
                        Console.WriteLine($"\nPrzedmioty o wartosci <= {maxV} sz (LINQ, posortowane po wartosci):");
                        PrintAllItems(filtered);
                    }
                    break;
                case "4":
                    Console.Write("Podaj nazwe przedmiotu: ");
                    string iname = Console.ReadLine()?.Trim();
                    var found = _activeCharacter.Backpack[iname];
                    if (found != null)
                        Console.WriteLine($"Znaleziono: {found.GetDescription()}");
                    else
                        Console.WriteLine("Nie znaleziono przedmiotu o tej nazwie.");
                    break;
                default:
                    Console.WriteLine("Nieznana opcja.");
                    break;
            }
        }

        static void PrintAllItems(IEnumerable<Item> items)
        {
            var list = items.ToList();
            if (list.Count == 0) { Console.WriteLine("  (brak przedmiotow)"); return; }
            for (int i = 0; i < list.Count; i++)
                Console.WriteLine($"  {i + 1}. {list[i].GetDescription()}");
        }

        static void UseItemMenu()
        {
            if (_activeCharacter == null) { Console.WriteLine("Brak aktywnej postaci."); return; }

            var usable = _activeCharacter.Backpack.GetAll()
                .Where(i => i is IUsable)
                .ToList();

            if (usable.Count == 0)
            {
                Console.WriteLine("Brak uzytecznych przedmiotow w ekwipunku.");
                return;
            }

            Console.WriteLine("\n--- UZYCIE PRZEDMIOTU ---");
            for (int i = 0; i < usable.Count; i++)
                Console.WriteLine($"  {i + 1}. {usable[i].Name}");

            Console.Write("Wybierz przedmiot (numer): ");
            if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > usable.Count)
            {
                Console.WriteLine("Nieprawidlowy wybor.");
                return;
            }

            var item = usable[idx - 1] as IUsable;
            string result = item.Use(_activeCharacter);
            Console.WriteLine($"\n{result}");
        }

        static async Task SaveCharacterMenu()
        {
            if (_activeCharacter == null) { Console.WriteLine("Brak aktywnej postaci."); return; }

            Console.Write("\nPodaj nazwe pliku (bez rozszerzenia): ");
            string fileName = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(fileName)) fileName = _activeCharacter.Name.ToLower().Replace(" ", "_");

            string path = $"{fileName}.json";

            Console.Write($"Zapisuje do '{path}'...");
            try
            {
                await SaveSystem.SaveCharacterAsync(_activeCharacter, path);
                Console.WriteLine(" Zapisano.");
                Console.WriteLine($"Pelna sciezka: {Path.GetFullPath(path)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Blad! {ex.Message}");
            }
        }

        static async Task LoadCharacterMenu()
        {
            Console.Write("\nPodaj nazwe pliku (bez rozszerzenia): ");
            string fileName = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(fileName)) { Console.WriteLine("Anulowano."); return; }

            string path = $"{fileName}.json";

            if (!File.Exists(path))
            {
                Console.WriteLine($"Plik '{path}' nie istnieje.");
                return;
            }

            try
            {
                Console.Write("Wczytuje...");
                var data = await SaveSystem.LoadCharacterAsync(path);
                Console.WriteLine(" Wczytano.");
                Console.WriteLine("\n--- DANE Z PLIKU ---");
                Console.WriteLine($"Postac: {data.Name} [{data.Race}] - Poziom {data.Level}");
                Console.WriteLine($"Klasa: {data.Class}");
                Console.WriteLine($"PZ: {data.CurrentHp}/{data.MaxHp}");
                Console.WriteLine($"SIL:{data.Strength} ZRE:{data.Dexterity} ZDR:{data.Constitution} INT:{data.Intelligence} MAD:{data.Wisdom} CHA:{data.Charisma}");
                Console.WriteLine($"Zapisano: {data.SavedAt}");
                if (data.Items.Count > 0)
                {
                    Console.WriteLine("Przedmioty:");
                    foreach (var item in data.Items)
                        Console.WriteLine($"  - {item}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Blad wczytywania! {ex.Message}");
            }
        }

        static void ReflectionDebugMenu()
        {
            if (_activeCharacter == null) { Console.WriteLine("Brak aktywnej postaci."); return; }

            Console.WriteLine("\n--- DEBUG: REFLEKSJA ---");
            Console.WriteLine("Wybierz obiekt do inspekcji:");
            Console.WriteLine("  1. Aktywna postac");
            Console.WriteLine("  2. Kostka losujaca (typ statyczny)");
            Console.WriteLine("  3. Pierwszy przedmiot w ekwipunku");
            Console.Write("Wybor: ");
            string choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    Reflector.PrintProperties(_activeCharacter);
                    break;
                case "2":
                    Console.WriteLine("\n[REFLEKSJA] Typ: DiceRoller (statyczny)");
                    var methods = typeof(DiceRoller).GetMethods(BindingFlags.Public | BindingFlags.Static);
                    Console.WriteLine("Metody publiczne:");
                    foreach (var m in methods)
                    {
                        var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({pars})");
                    }
                    break;
                case "3":
                    var first = _activeCharacter.Backpack[0];
                    if (first != null)
                        Reflector.PrintProperties(first);
                    else
                        Console.WriteLine("Brak przedmiotow w plecaku.");
                    break;
                default:
                    Console.WriteLine("Nieznana opcja.");
                    break;
            }
        }
    }
}
