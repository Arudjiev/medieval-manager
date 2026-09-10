using System;
using System.Collections.Generic;

namespace Medieval_Manager_Clean_Code
{
    public enum ResourceType { Population, Gold, Food, Steel, Horse }

    public class Resources
    {
        private Dictionary<ResourceType, int> _resources = new();

        public Resources()
        {
            _resources[ResourceType.Population] = Balance.StartPopulation;
            _resources[ResourceType.Gold] = Balance.StartGold;
            _resources[ResourceType.Food] = Balance.StartFood;
            _resources[ResourceType.Steel] = Balance.StartSteel;
            _resources[ResourceType.Horse] = Balance.StartHorse;
        }

        public int Get(ResourceType type)
        {
            return _resources[type];
        }

        public bool Spend(ResourceType type, int amount)
        {
            if (_resources[type] >= amount)
            {
                _resources[type] -= amount;
                return true;
            }
            else return false;
        }

        public void Add(ResourceType type, int amount)
        {
            _resources[type] += amount;
        }

        // Списывает столько, сколько есть, не уходя ниже нуля (для голода/частичных списаний).
        public void Reduce(ResourceType type, int amount)
        {
            _resources[type] = Math.Max(0, _resources[type] - amount);
        }
    }

    public class Building
    {
        public string Name { get; init; }
        public ResourceType ProdResource { get; init; }
        public float Efficiency { get; init; }
        public int MaxSeats { get; init; }
        public int OccupiedSeats { get; private set; } = 0;

        public Building(string name, ResourceType prodResource, float efficiency, int maxSeats)
        {
            Name = name;
            ProdResource = prodResource;
            Efficiency = efficiency;
            MaxSeats = maxSeats;
        }

        public void ChangeSeats(int seats)
        {
            OccupiedSeats += seats;
            OccupiedSeats = Math.Clamp(OccupiedSeats, 0, MaxSeats);
        }

        public int Produce()
        {
            int result = (int)(Efficiency * ((float)OccupiedSeats / MaxSeats) * OccupiedSeats);
            return result;
        }
    }

    interface IRecruitable
    {
        Dictionary<ResourceType, int> RecruitmentCost { get; }
    }

    // Маркер: юнит способен к манёвру и окружению (конница). Используется для резерва и фланга.
    interface IMobile
    {
    }

    interface ISustainable
    {
        public int ExtraMorale { get; }
    }

    public class Unit
    {
        public string Name { get; init; }
        public int MaxAmount { get; private set; }
        public int Amount { get; private set; }
        public int Power { get; private set; }
        public int MaxMorale { get; private set; }
        public int Morale { get; private set; }
        public int MoraleThreshold { get; init; }
        public int Mobility { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsReserved { get; private set; }

        public void SetActivity(bool value)
        {
            IsActive = value;
        }

        public void SetReserved(bool value)
        {
            IsReserved = value;
        }

        public void RestoreMorale()
        {
            Morale = MaxMorale;
        }

        public void Restore()
        {
            RestoreMorale();
            IsReserved = false;
            IsActive = true;
        }

        public void ReduceMorale(int amount)
        {
            if (amount > Morale) Morale = 0;
            else Morale -= amount;
        }

        public bool IsRouted()
        {
            return MoraleThreshold > Morale || Amount == 0;
        }

        public void TakeCasualties(int amount)
        {
            if (amount > Amount) Amount = 0;
            else Amount -= amount;
        }

        public void RecruitSoldiers(int amount)
        {
            if (amount + Amount > MaxAmount) Amount = MaxAmount;
            else Amount += amount;
        }

        public Unit(string name, int maxAmount, int power, int maxMorale, int moraleThreshold, int mobility)
        {
            Name = name;
            MaxAmount = maxAmount;
            Amount = MaxAmount;
            Power = power;
            MaxMorale = maxMorale;
            Morale = MaxMorale;
            MoraleThreshold = moraleThreshold;
            Mobility = mobility;
            IsActive = true;
            IsReserved = false;
        }
    }

    public class Militia : Unit, IRecruitable
    {
        public Militia() : base(
            "ополчение",
            Balance.MilitiaMaxAmount,
            Balance.MilitiaPower,
            Balance.MilitiaMorale,
            Balance.MilitiaMoraleThreshold,
            Balance.MilitiaMobility) { }

        public Dictionary<ResourceType, int> RecruitmentCost => Balance.MilitiaCost;
    }

    public class Cavalry : Unit, IMobile, IRecruitable
    {
        public Cavalry() : base(
            "кавалерия",
            Balance.CavalryMaxAmount,
            Balance.CavalryPower,
            Balance.CavalryMorale,
            Balance.CavalryMoraleThreshold,
            Balance.CavalryMobility) { }

        public Dictionary<ResourceType, int> RecruitmentCost => Balance.CavalryCost;
    }

    public class LightInfantry : Unit, IRecruitable
    {
        public LightInfantry() : base(
            "пехота",
            Balance.LightInfantryMaxAmount,
            Balance.LightInfantryPower,
            Balance.LightInfantryMorale,
            Balance.LightInfantryMoraleThreshold,
            Balance.LightInfantryMobility) { }

        public Dictionary<ResourceType, int> RecruitmentCost => Balance.LightInfantryCost;
    }

    public class Army
    {
        List<Unit> FullArmy = new();

        public void Add(Unit unit)
        {
            FullArmy.Add(unit);
        }

        public void Remove(Unit unit)
        {
            FullArmy.Remove(unit);
        }

        public List<Unit> ActiveArmy()
        {
            var result = FullArmy.Where(a => !a.IsReserved && a.IsActive).ToList();
            return result;
        }

        public List<Unit> AllRemainingUnits()
        {
            var result = FullArmy.Where(a => a.IsActive).ToList();
            return result;
        }

        public List<Unit> ReservedArmy()
        {
            var result = FullArmy.Where(a => a.IsReserved).ToList();
            return result;
        }

        public int ActiveArmyPower()
        {
            int sum = 0;
            List<Unit> activeUnits = ActiveArmy();
            foreach(var activeUnit in activeUnits)
            {
                sum += activeUnit.Power * activeUnit.Amount;
            }
            return sum;
        }

        public int ReservedArmyPower()
        {
            int sum = 0;
            List<Unit> activeUnits = ReservedArmy();
            foreach (var activeUnit in activeUnits)
            {
                sum += activeUnit.Power * activeUnit.Amount;
            }
            return sum;
        }

        // Манёвренная мощь активной конницы (Mobility*Amount по IMobile) — основа окружения.
        public int CavalryManeuver()
        {
            int sum = 0;
            foreach (var unit in ActiveArmy())
                if (unit is IMobile)
                    sum += unit.Mobility * unit.Amount;
            return sum;
        }

        public void Recover()
        {
            FullArmy.RemoveAll(u => u.Amount == 0);
            foreach (var unit in FullArmy) unit.Restore();
        }

        // Все отряды независимо от IsActive/IsReserved (нужен для крестового похода и Replenish).
        public List<Unit> Roster()
        {
            return FullArmy.ToList();
        }

        public List<Unit> RoutedUnits()
        {
            var result = FullArmy.Where(a => a.IsRouted()).ToList();
            return result;
        }

        public double FullScaleArmy()
        {
            var result = FullArmy.Sum(a => a.Amount);
            return result;
        }

        public void DeployReserves()
        {
            foreach (var unit in FullArmy)
            {
                if(unit.IsReserved)
                    unit.SetReserved(false);
            }
        }

        public void Reserve(List<Unit> candidates)
        {
            int aliveCount = ActiveArmy().Count;
            int leftOnBattlefield = Math.Max(1, aliveCount - candidates.Count);
            int toReserve = aliveCount - leftOnBattlefield;
            for(int i = 0; i < toReserve; i++)
            {
                candidates[i].SetReserved(true);
            }
        }
    }

    public class Settlement
    {
        public string Name { get; init; }

        public Resources Resources { get; }

        private List<Building> _buildings = new();
        public IReadOnlyList<Building> Buildings => _buildings;
        public Army Army { get; }

        public int Day { get; private set; } = 1;
        public int RaidsDone { get; private set; } = 0;
        public bool IsAlive { get; private set; } = true;


        public void NewDay()
        {
            Day++;
        }

        public void RegisterRaid()
        {
            RaidsDone++;
        }

        public void Death()
        {
            IsAlive = false;
        }

        public void AddBuilding(Building building)
        {
            _buildings.Add(building);
        }

        public void RemoveBuilding(Building building)
        {
            _buildings.Remove(building);
        }

        public int FreePopulation()
        {
            int occupied = 0;
            foreach (var b in Buildings)
                occupied += b.OccupiedSeats;
            int mobilized = (int)Army.FullScaleArmy();
            return Resources.Get(ResourceType.Population) - occupied - mobilized;
        }

        public Settlement(string name, Resources resources, Army army)
        {
            Name = name;
            this.Resources = resources;
            this.Army = army;
        }

        public string Summary()
        {
            return $"^^^^^^^^  День: {Day} ^^^^^^^^ \nЗолото: {Resources.Get(ResourceType.Gold)}, Еда: {Resources.Get(ResourceType.Food)}, " +
                   $"Сталь: {Resources.Get(ResourceType.Steel)}, Лошади: {Resources.Get(ResourceType.Horse)}, " +
                   $"Население: {Resources.Get(ResourceType.Population)}, В армии: {(int)Army.FullScaleArmy()}, Свободное население: {FreePopulation()}";
        }
    }

    // Сюжетный контроллер: дни событий рандомны в окнах (реиграбельность), порядок гарантирован.
    public class Campaign
    {
        private readonly IRandom _rng;

        public int InvasionDay { get; }
        public int LordDemandDay { get; }
        public int CrusadeDay { get; }
        public int LordTroopsRequired { get; } = Balance.LordTroopsRequired;
        public int NextCaravanDay { get; private set; }

        public Campaign(IRandom rng)
        {
            _rng = rng;
            InvasionDay = rng.Next(Balance.InvasionDayMin, Balance.InvasionDayMax + 1);
            LordDemandDay = InvasionDay + rng.Next(Balance.LordDemandGapMin, Balance.LordDemandGapMax + 1);
            CrusadeDay = LordDemandDay + rng.Next(Balance.CrusadeGapMin, Balance.CrusadeGapMax + 1);
            NextCaravanDay = rng.Next(Balance.CaravanIntervalMin, Balance.CaravanIntervalMax + 1);
        }

        // Лорд объявил сбор и идёт обратный отсчёт до похода (для подсказки в меню деревни).
        public bool MusterAnnounced(int day) => day >= LordDemandDay && day < CrusadeDay;

        // Вызывается в конце дня. Отправитель (VillageDayState) сам решает переход.
        public IGameState? CheckStoryTrigger(int day, GameContext ctx)
        {
            // Караван: приходит раз в несколько дней и восстанавливает рыночные цены.
            if (day >= NextCaravanDay)
            {
                ctx.TradeSystem.ResetModifiers();
                ctx.View.Show("Прибыл торговый караван — рыночные цены восстановлены.");
                NextCaravanDay = day + _rng.Next(Balance.CaravanIntervalMin, Balance.CaravanIntervalMax + 1);
            }

            if (day == InvasionDay)
                return new InvasionState(ctx);

            if (day == LordDemandDay)
            {
                ctx.View.Show($"\n*** Лорд собирает войско для крестового похода! ***\n" +
                              $"Соберите минимум {LordTroopsRequired} бойцов к дню {CrusadeDay} — иначе поход без вас, и это поражение.");
                return null;
            }

            // В назначенный день уходим всем войском. Хватает бойцов — поход, иначе поражение.
            if (day == CrusadeDay)
            {
                if ((int)ctx.Settlement.Army.FullScaleArmy() >= LordTroopsRequired)
                    return new CrusadeState(ctx);

                ctx.View.Show($"Вы не собрали войско к походу (нужно было {LordTroopsRequired}).");
                return new GameOverState(ctx);
            }

            return null;
        }
    }

    public static class UnitFactory
    {
        public static bool TryCreate(string key, out Unit? unit)
        {
            unit = key switch
            {
                "ополчение" => new Militia(),
                "кавалерия" => new Cavalry(),
                "пехота" => new LightInfantry(),
                _ => null
            };
            return unit != null;
        }
    }

    public static class EnemyArmyFactory
    {
        public static Army Build(Dictionary<string, int> composition)
        {
            var army = new Army();
            foreach (var pair in composition)
                for (int i = 0; i < pair.Value; i++)
                    if (UnitFactory.TryCreate(pair.Key, out Unit? unit) && unit != null)
                        army.Add(unit);
            return army;
        }

        public static Army CreateNomadHorde(IRandom rng)
        {
            var composition = new Dictionary<string, int>
            {
                { "кавалерия", rng.Next(4,7) }
            };
            return Build(composition);
        }

        // Рейд: число отрядов растёт с difficulty; типы берутся из Balance.RaidUnitTypes (раздаём по кругу).
        public static Army CreateRaid(IRandom rng, int difficulty)
        {
            int squads = Balance.RaidBaseSquads + difficulty * Balance.RaidSquadsPerDifficulty + rng.Next(0, Balance.EnemySquadVariance);
            var types = Balance.RaidUnitTypes;
            var comp = new Dictionary<string, int>();
            for (int i = 0; i < squads; i++)
            {
                string type = types[i % types.Length];
                comp[type] = comp.GetValueOrDefault(type) + 1;
            }
            return Build(comp);
        }

        // Нашествие кочевников (оборона) — крупнее рейда, смешанное.
        public static Army CreateInvasion(IRandom rng)
        {
            var comp = new Dictionary<string, int>
            {
                { "кавалерия", Balance.InvasionCavalry + rng.Next(0, Balance.EnemySquadVariance) },
                { "пехота", Balance.InvasionInfantry },
                { "ополчение", Balance.InvasionMilitia }
            };
            return Build(comp);
        }

        // Финальный враг крестового похода.
        public static Army CreateCrusadeEnemy(IRandom rng)
        {
            var comp = new Dictionary<string, int>
            {
                { "кавалерия", Balance.CrusadeEnemyCavalry },
                { "пехота", Balance.CrusadeEnemyInfantry },
                { "ополчение", Balance.CrusadeEnemyMilitia + rng.Next(0, Balance.EnemySquadVariance) }
            };
            return Build(comp);
        }

        // Контингент лорда (основная сила похода): растёт с merit = RaidsDone.
        public static Army CreateLordHost(IRandom rng, int merit)
        {
            int bonus = merit * Balance.LordHostPerMerit;
            var comp = new Dictionary<string, int>
            {
                { "пехота", Balance.LordHostInfantry + bonus },
                { "кавалерия", Balance.LordHostCavalry + bonus / 2 },
                { "ополчение", Balance.LordHostMilitia }
            };
            return Build(comp);
        }
    }

    public static class BehaviorFactory
    {
        public static IBehavior CreateRandom(IRandom rng)
        {
            return rng.Next(0, 3) switch
            {
                0 => new AggressiveBehavior(),
                1 => new CautiousBehavior(rng),
                _ => new ModerateBehavior(rng)
            };
        }
    }

    public static class BuildingFactory
    {
        public static bool TryCreate(string name, out Building? building)
        {
            building = name switch
            {
                "поместье" => new Building("Поместье", ResourceType.Food, Balance.ManorEfficiency, Balance.ManorSeats),
                "кузница" => new Building("Кузница", ResourceType.Steel, Balance.SmithyEfficiency, Balance.SmithySeats),
                "конюшня" => new Building("Конюшня", ResourceType.Horse, Balance.StableEfficiency, Balance.StableSeats),
                _ => null
            };
            return building != null;
        }
    }

    // Итог обработки дня — для обратной связи игроку (UI решает, что показать).
    public readonly struct DayReport
    {
        public int Immigrants { get; init; }
        public int Starved { get; init; }
    }

    public class EconomySystem
    {
        private readonly IRandom _rng;

        public EconomySystem(IRandom rng)
        {
            _rng = rng;
        }

        public DayReport ProcessDay(Settlement settlement)
        {
            if (!settlement.IsAlive) return default;

            foreach(var building in settlement.Buildings)
            {
                int amount = building.Produce();
                ResourceType type = building.ProdResource;
                settlement.Resources.Add(type, amount);
            }

            int population = settlement.Resources.Get(ResourceType.Population);
            float rate = Balance.FoodConsumptionBase + (float)_rng.NextDouble() * Balance.FoodConsumptionVariance;
            int consumption = _rng.RoundStochastic(population * rate);

            int immigrants = 0;
            int starved = 0;
            int food = settlement.Resources.Get(ResourceType.Food);
            if (food >= consumption)
            {
                settlement.Resources.Spend(ResourceType.Food, consumption);
                // Еды хватило — избыток сверх порога привлекает иммигрантов.
                immigrants = Immigrate(settlement);
            }
            else
            {
                // Голод: съедаем всё что есть, остаток дефицита бьёт по населению постепенно.
                settlement.Resources.Reduce(ResourceType.Food, food);
                int deficit = consumption - food;
                starved = Math.Max(1, (int)(deficit * Balance.StarvationRate));
                settlement.Resources.Reduce(ResourceType.Population, starved);
                if (settlement.Resources.Get(ResourceType.Population) <= 0)
                    settlement.Death();
            }
            settlement.NewDay();
            return new DayReport { Immigrants = immigrants, Starved = starved };
        }

        // Приток населения: тем больше, чем больше запас еды сверх порога.
        private int Immigrate(Settlement settlement)
        {
            int surplus = settlement.Resources.Get(ResourceType.Food) - Balance.ImmigrationFoodThreshold;
            if (surplus <= 0) return 0;

            int immigrants = Math.Min(surplus / Balance.ImmigrationFoodPerPerson, Balance.ImmigrationMaxPerDay);
            if (immigrants > 0)
                settlement.Resources.Add(ResourceType.Population, immigrants);
            return immigrants;
        }
    }


    public enum BattleResult { PlayerWon, EnemyWon, Ongoing, Draw }
    public enum FlankResult { PlayerFlanked, EnemyFlanked, None }
    public class BattleSystem
    {
        public event Action<Army>? OnBattleStarted;
        public event Action<Army, Army>? OnRoundEnded;

        private readonly IRandom _rng;

        public BattleSystem(IRandom rng)
        {
            _rng = rng;
        }

        public BattleResult Fight(Army playerArmy, Army enemyArmy)
        {
            BattleResult battleResult = BattleResult.Ongoing;
            int round = 0;

            OnBattleStarted?.Invoke(enemyArmy);
            while (battleResult == BattleResult.Ongoing && round < Balance.MaxBattleRounds)
            {
                AmountLoses(playerArmy, enemyArmy);
                MoraleLoses(playerArmy, enemyArmy);

                // Окружение проверяется каждый час заново.
                FlankResult flank = TryFlank(playerArmy, enemyArmy);
                if (flank != FlankResult.None)
                {
                    Army surrounded = SurroundedArmy(flank, playerArmy, enemyArmy);
                    if (surrounded.ReservedArmy().Count > 0)
                        surrounded.DeployReserves();        // окружение вынуждает ввести резерв
                    else
                        ApplyFlankPenalties(surrounded);    // нет резерва — несёшь потери от окружения
                }

                CheckRouted(playerArmy, enemyArmy);
                battleResult = CheckResult(playerArmy, enemyArmy);
                OnRoundEnded?.Invoke(playerArmy, enemyArmy);
                round++;
            }

            // Лимит раундов исчерпан без решения — тай-брейк по остаточной силе.
            if (battleResult == BattleResult.Ongoing)
                battleResult = ResolveTiebreak(playerArmy, enemyArmy);

            return battleResult;
        }

        public void AmountLoses(Army playerArmy, Army enemyArmy)
        {
            var playerActive = playerArmy.ActiveArmy();
            var enemyActive = enemyArmy.ActiveArmy();
            var ratios = GetPowerRatios(playerArmy, enemyArmy);

            foreach(var unit in playerActive)
            {
                double raw = unit.Amount * Balance.CasualtyFactor * ratios.enemyRatio
                    * _rng.Range(Balance.CasualtyVarianceMin, Balance.CasualtyVarianceMax);
                unit.TakeCasualties(_rng.RoundStochastic(raw));
            }

            foreach (var unit in enemyActive)
            {
                double raw = unit.Amount * Balance.CasualtyFactor * ratios.playerRatio
                    * _rng.Range(Balance.CasualtyVarianceMin, Balance.CasualtyVarianceMax);
                unit.TakeCasualties(_rng.RoundStochastic(raw));
            }
        }

        public void MoraleLoses(Army playerArmy, Army enemyArmy)
        {
            var playerActive = playerArmy.ActiveArmy();
            var enemyActive = enemyArmy.ActiveArmy();
            var scaleRatio = GetScaleRatios(playerArmy, enemyArmy);
            var powerRatio = GetPowerRatios(playerArmy, enemyArmy);

            foreach (var unit in playerActive)
            {
                int moraleLoses = (int)(_rng.Range(Balance.MoralePowerMin, Balance.MoralePowerMax) * powerRatio.enemyRatio
                    + _rng.Range(Balance.MoraleScaleMin, Balance.MoraleScaleMax) * scaleRatio.enemyRatio);
                unit.ReduceMorale(moraleLoses);
            }

            foreach (var unit in enemyActive)
            {
                int moraleLoses = (int)(_rng.Range(Balance.MoralePowerMin, Balance.MoralePowerMax) * powerRatio.playerRatio
                    + _rng.Range(Balance.MoraleScaleMin, Balance.MoraleScaleMax) * scaleRatio.playerRatio);
                unit.ReduceMorale(moraleLoses);
            }
        }

        // Окружает та сторона, чья активная конница заметно превосходит вражескую; шанс за раунд растёт
        // с разрывом, но ограничен потолком. Если у защищающегося резерв — он вынужденно вступит (см. Fight).
        // Поэтому держать конницу в резерве рискованно: пустой фланг провоцирует окружение и авто-выпуск.
        public FlankResult TryFlank(Army playerArmy, Army enemyArmy)
        {
            int playerCav = playerArmy.CavalryManeuver();
            int enemyCav = enemyArmy.CavalryManeuver();

            if (playerCav > enemyCav * Balance.FlankAdvantageRatio && FlankRoll(playerCav, enemyCav))
                return FlankResult.PlayerFlanked;
            if (enemyCav > playerCav * Balance.FlankAdvantageRatio && FlankRoll(enemyCav, playerCav))
                return FlankResult.EnemyFlanked;
            return FlankResult.None;
        }

        // Шанс окружения за раунд ~ доля превосходства конницы (1 − защита/атака), но не выше потолка.
        private bool FlankRoll(int attackerCav, int defenderCav)
        {
            double advantage = 1.0 - (double)defenderCav / attackerCav;
            double chance = Math.Min(advantage, Balance.FlankChancePerRound);
            return _rng.NextDouble() < chance;
        }

        public void ApplyFlankPenalties(Army surroundedArmy)
        {
            foreach(var unit in surroundedArmy.ActiveArmy())
            {
                unit.ReduceMorale(_rng.Next(Balance.FlankMoraleMin, Balance.FlankMoraleMax));
                int casualties = 1 + (int)(unit.Amount * Balance.FlankCasualtyFactor);
                unit.TakeCasualties(casualties);
            }
        }

        public void CheckRouted(Army playerArmy, Army enemyArmy)
        {
            foreach(var unit in playerArmy.ActiveArmy())
            {
                if (unit.IsRouted())
                {
                    unit.SetActivity(false);
                    PsychologicalDamage(playerArmy);
                }
            }

            foreach (var unit in enemyArmy.ActiveArmy())
            {
                if (unit.IsRouted())
                {
                    unit.SetActivity(false);
                    PsychologicalDamage(enemyArmy);
                }
            }
        }

        public void PsychologicalDamage(Army armyWithRoutedUnit)
        {
            foreach(var unit in armyWithRoutedUnit.AllRemainingUnits())
            {
                unit.ReduceMorale(Balance.PsychologicalMoraleDamage);
            }
        }

        public BattleResult CheckResult(Army playerArmy, Army enemyArmy)
        {
            bool isPlayerArmyRouted = playerArmy.ActiveArmy().Count == 0;
            bool isEnemyArmyRouted = enemyArmy.ActiveArmy().Count == 0;

            if (isEnemyArmyRouted && isPlayerArmyRouted) return BattleResult.Draw;
            else if (isPlayerArmyRouted) return BattleResult.EnemyWon;
            else if (isEnemyArmyRouted) return BattleResult.PlayerWon;
            else return BattleResult.Ongoing;
        }

        public BattleResult ResolveTiebreak(Army playerArmy, Army enemyArmy)
        {
            int playerPower = playerArmy.ActiveArmyPower();
            int enemyPower = enemyArmy.ActiveArmyPower();

            if (playerPower > enemyPower) return BattleResult.PlayerWon;
            else if (enemyPower > playerPower) return BattleResult.EnemyWon;
            else return BattleResult.Draw;
        }

        public Army SurroundedArmy(FlankResult flankResult, Army playerArmy, Army enemyArmy)
        {
            Army surroundedArmy = flankResult == FlankResult.PlayerFlanked ? enemyArmy : playerArmy;
            return surroundedArmy;
        }

        // playerRatio — относительная сила игрока (бьёт по потерям ВРАГА),
        // enemyRatio — относительная сила врага (бьёт по потерям ИГРОКА).
        private (double playerRatio, double enemyRatio) GetPowerRatios(Army player, Army enemy)
        {
            int playerPower = player.ActiveArmyPower();
            if (playerPower == 0) playerPower = 1;
            int enemyPower = enemy.ActiveArmyPower();
            if (enemyPower == 0) enemyPower = 1;
            return ((double)playerPower / enemyPower, (double)enemyPower / playerPower);
        }

        private (double playerRatio, double enemyRatio) GetScaleRatios(Army player, Army enemy)
        {
            double playerScale = player.FullScaleArmy();
            if (playerScale == 0) playerScale = 1;
            double enemyScale = enemy.FullScaleArmy();
            if (enemyScale == 0) enemyScale = 1;
            return (playerScale / enemyScale, enemyScale / playerScale);
        }

    }

    public class RecruitmentSystem
    {
        public bool RecruitNew(Settlement settlement, Unit unit)
        {
            if(unit is IRecruitable recruitable)
            {
                if (unit.MaxAmount > settlement.FreePopulation()) return false;

                foreach(var item in recruitable.RecruitmentCost)
                {
                    if (settlement.Resources.Get(item.Key) < item.Value)
                    {
                        return false;
                    }
                }
                foreach (var item in recruitable.RecruitmentCost)
                {
                    settlement.Resources.Spend(item.Key, item.Value);
                }
                settlement.Army.Add(unit);
                return true;
            }
            return false;
        }

        // Смета восполнения: сколько бойцов добрать и во что обойдётся (армейское округление —
        // копим цену в double, Math.Ceiling один раз над суммой, без спишения).
        public ReplenishQuote QuoteReplenish(Settlement settlement)
        {
            Dictionary<ResourceType, double> priceAccumulator = new();
            int bodiesNeeded = 0;

            foreach(var unit in settlement.Army.Roster())
            {
                if (unit is not IRecruitable recruitable) continue;
                int lost = unit.MaxAmount - unit.Amount;
                if (lost == 0) continue;
                bodiesNeeded += lost;
                double ratio = (double)lost / unit.MaxAmount;
                foreach(var cost in recruitable.RecruitmentCost)
                    priceAccumulator[cost.Key] = priceAccumulator.GetValueOrDefault(cost.Key) + cost.Value * ratio;
            }

            Dictionary<ResourceType, int> price = new();
            foreach (var kv in priceAccumulator)
                price[kv.Key] = (int)Math.Ceiling(kv.Value);

            return new ReplenishQuote(price, bodiesNeeded);
        }

        public bool Replenish(Settlement settlement)
        {
            ReplenishQuote quote = QuoteReplenish(settlement);
            if (quote.BodiesNeeded == 0) return false;
            if (quote.BodiesNeeded > settlement.FreePopulation()) return false;
            foreach (var kv in quote.Price)
                if (settlement.Resources.Get(kv.Key) < kv.Value) return false;

            foreach (var kv in quote.Price)
                settlement.Resources.Spend(kv.Key, kv.Value);

            foreach (var unit in settlement.Army.Roster())
            {
                int lost = unit.MaxAmount - unit.Amount;
                if (lost > 0) unit.RecruitSoldiers(lost);
            }
            return true;
        }
    }

    // Смета восполнения: сколько бойцов нужно добрать и во что это обойдётся по ресурсам.
    public class ReplenishQuote
    {
        public Dictionary<ResourceType, int> Price { get; }
        public int BodiesNeeded { get; }
        public ReplenishQuote(Dictionary<ResourceType, int> price, int bodiesNeeded)
        {
            Price = price;
            BodiesNeeded = bodiesNeeded;
        }
    }

    public class BuildingSystem
    {
        public bool Build(Settlement settlement, Building building)
        {
            if (!settlement.Resources.Spend(ResourceType.Gold, Balance.BuildingCost)) return false;
            settlement.AddBuilding(building);
            return true;
        }
    }

    public class TradeSystem
    {
        public Dictionary<ResourceType, double> Modifier { get; private set; } = new()
        {
            {ResourceType.Food, Balance.ModifierStart },
            {ResourceType.Steel, Balance.ModifierStart },
            {ResourceType.Horse, Balance.ModifierStart },
        };

        public bool Sell(Settlement settlement, ResourceType resourceType)
        {
            MarketEntry entry = Balance.MarketPrices[resourceType];
            if (!settlement.Resources.Spend(resourceType, entry.BatchSize)) return false;
            int earnedGold = CurrentSellPrice(resourceType);
            settlement.Resources.Add(ResourceType.Gold, earnedGold);
            Modifier[resourceType] *= Balance.SellModifierDecay;
            return true;
        }

        public int CurrentSellPrice(ResourceType type)
        {
            MarketEntry entry = Balance.MarketPrices[type];
            return (int)(entry.BasePrice * Modifier[type]);
        }

        public bool Buy(Settlement settlement, ResourceType resourceType)
        {
            MarketEntry entry = Balance.MarketPrices[resourceType];
            if (!settlement.Resources.Spend(ResourceType.Gold, entry.BasePrice)) return false;
            settlement.Resources.Add(resourceType, entry.BatchSize);
            return true;
        }

        public void ResetModifiers()
        {
            foreach (var key in Modifier.Keys.ToList())
            {
                Modifier[key] = Balance.ModifierStart;
            }
        }
    }

    public interface IBehavior
    {
        void DecideReserve(Army army);
    }

    public interface IRoundUpdatable
    {
        public void OnRoundUpdate(Army playerArmy, Army enemyArmy);
    }

    public abstract class BehaviorBase : IBehavior
    {
        protected readonly IRandom _rng;
        protected readonly double _reserveThreshold;

        protected BehaviorBase(IRandom rng, double reserveThreshold)
        {
            _rng = rng;
            _reserveThreshold = reserveThreshold;
        }

        public void DecideReserve(Army army)
        {
            if (_rng.NextDouble() > _reserveThreshold)
            {
                army.Reserve(army.ActiveArmy().Where(a => a is IMobile).ToList());
            }
        }
    }

    public class AggressiveBehavior : IBehavior
    {
        public void DecideReserve(Army army) { }
    }

    public class CautiousBehavior : BehaviorBase
    {
        public CautiousBehavior(IRandom rng) : base(rng, Balance.CautiousReserveThreshold) { }
        
    }

    public class ModerateBehavior : BehaviorBase, IRoundUpdatable
    {
        private double _patience = Balance.ModerateStartPatience;
        private readonly double _aggressionMultiplier;

        public ModerateBehavior(IRandom rng) : base(rng, Balance.ModerateReserveThreshold)
        {
            _aggressionMultiplier = rng.Range(Balance.ModerateAggressionMin, Balance.ModerateAggressionMax);
        }

        public void OnRoundUpdate(Army playerArmy, Army enemyArmy)
        {
            if (enemyArmy.ReservedArmy().Count <= 0) return;
            double lossRatio = enemyArmy.ActiveArmyPower() != 0 ? (double)playerArmy.ActiveArmyPower() / enemyArmy.ActiveArmyPower() : Balance.ModeratePatienceFallback;
            _patience -= Balance.ModeratePatienceStep + lossRatio * _aggressionMultiplier;
            if (_patience < Balance.ModeratePatienceThreshold)
                enemyArmy.DeployReserves();
        }
    }

    public class EnemyAI
    {
        IBehavior behavior;

        public void Subscribe(BattleSystem battleSystem)
        {
            battleSystem.OnBattleStarted += behavior.DecideReserve;
            if(behavior is IRoundUpdatable updatable)
                battleSystem.OnRoundEnded += updatable.OnRoundUpdate;
        }

        public void Unsubscribe(BattleSystem battleSystem)
        {
            battleSystem.OnBattleStarted -= behavior.DecideReserve;
            if (behavior is IRoundUpdatable updatable)
                battleSystem.OnRoundEnded -= updatable.OnRoundUpdate;
        }

        public EnemyAI(IBehavior behavior)
        {
            this.behavior = behavior;
        }
    }

    public struct MarketEntry
    {
        public int BasePrice;
        public int BatchSize;
    }

    public class GameContext
    {
        public Settlement Settlement { get; }
        public BattleSystem BattleSystem { get; }
        public EconomySystem EconomySystem { get; }
        public RecruitmentSystem RecruitmentSystem { get; }
        public BuildingSystem BuildingSystem { get; }
        public TradeSystem TradeSystem { get; }
        public Campaign Campaign { get; }
        public IGameView View { get; }
        public IRandom Rng { get; }

        public GameContext(Settlement settlement, BattleSystem battleSystem, EconomySystem economySystem, RecruitmentSystem recruitmentSystem, 
            BuildingSystem buildingSystem, TradeSystem tradeSystem, Campaign campaign, IGameView view, IRandom rng)
        {
            Settlement = settlement;
            BattleSystem = battleSystem;
            EconomySystem = economySystem;
            RecruitmentSystem = recruitmentSystem;
            BuildingSystem = buildingSystem;
            TradeSystem = tradeSystem;
            Campaign = campaign;
            View = view;
            Rng = rng;
        }
    }

    public interface IGameState
    {
        IGameState? Update();
    }

    public class GameStateMachine
    {
        private IGameState? _current;

        public GameStateMachine(IGameState initial)
        {
            _current = initial;
        }

        public void Run()
        {
            while (_current != null)
            {
                _current = _current.Update();
            }
        }
    }

    public class VictoryState : IGameState
    {
        readonly GameContext _context;
        public VictoryState(GameContext context) => _context = context;
        public IGameState? Update()
        {
            Console.WriteLine("Игра окончена! Вы победили!");
            return null;
        }
    }

    public class GameOverState : IGameState
    {
        readonly GameContext _context;
        public GameOverState(GameContext context) => _context = context;
        public IGameState? Update()
        {
            Console.WriteLine("Игра окончена! Вы проиграли!");
            return null;
        }
    }

    // Общий каркас боевого состояния: выбор ИИ, снимок потерь, подписка/отписка в finally.
    // Интерактивный бой: каждый час показывает обе стороны по типам войск (численность + мораль),
    // общую сводку войны, даёт резерв конницы, ручной/авто ввод и опции пауза/скип.
    public class InteractiveBattle
    {
        private readonly BattleSystem _battle;
        private readonly IGameView _view;
        private readonly IRandom _rng;

        private int _hour;
        private bool _hold;            // держать резерв до окружения — не спрашивать
        private bool _skip;            // скип до конца боя — не показывать и не спрашивать
        private bool _playerDeployed;  // игрок ввёл резерв сам (чтобы не путать с авто-выпуском)
        private int _prevReserve;
        private readonly HashSet<Unit> _reportedLost = new(); // отряды, о выбытии которых уже сообщили

        public InteractiveBattle(BattleSystem battle, IGameView view, IRandom rng)
        {
            _battle = battle;
            _view = view;
            _rng = rng;
        }

        public BattleResult Fight(Army player, Army enemy, bool askReserveCavalry)
        {
            _view.Show("\n========== БИТВА ==========");
            ShowField(player, enemy);

            if (askReserveCavalry)
                AskReserveCavalry(player);

            _prevReserve = player.ReservedArmy().Count;

            var ai = new EnemyAI(BehaviorFactory.CreateRandom(_rng));
            ai.Subscribe(_battle);
            _battle.OnRoundEnded += OnHour;
            try
            {
                return _battle.Fight(player, enemy);
            }
            finally
            {
                ai.Unsubscribe(_battle);
                _battle.OnRoundEnded -= OnHour;
            }
        }

        private void AskReserveCavalry(Army player)
        {
            var cavalry = player.ActiveArmy().Where(u => u is IMobile).ToList();
            if (cavalry.Count == 0) return;

            _view.Show($"\nПоставить конницу в резерв? ({cavalry.Count} отр.)\n1 - да\n2 - нет");
            if (_view.ReadChoice(1, 2) == 1)
            {
                player.Reserve(cavalry);
                _view.Show("Конница в резерве — введёте её сами или она выйдет при окружении.");
            }
        }

        private void OnHour(Army player, Army enemy)
        {
            _hour++;
            if (!_skip)
            {
                _view.Show($"\n----- {_hour} час -----");
                ShowField(player, enemy);
                ReportLosses(player, enemy);
                // Авто-выпуск при окружении: резерв был, исчез, а игрок его не вводил.
                if (_prevReserve > 0 && player.ReservedArmy().Count == 0 && !_playerDeployed)
                    _view.Show(">>> Вас окружили — резерв вступил в бой автоматически!");
            }
            _prevReserve = player.ReservedArmy().Count;
            HourPrompt(player, enemy);
        }

        // Сообщает, какие отряды выбыли с прошлого показа и почему: бегство (мораль) или уничтожение.
        private void ReportLosses(Army player, Army enemy)
        {
            foreach (var u in player.Roster())
                if (!u.IsActive && _reportedLost.Add(u))
                    _view.Show(LossLine("ваш отряд", u));
            foreach (var u in enemy.Roster())
                if (!u.IsActive && _reportedLost.Add(u))
                    _view.Show(LossLine("вражеский отряд", u));
        }

        private static string LossLine(string who, Unit u) =>
            u.Amount == 0
                ? $"  >> {who} {u.Name} уничтожен"
                : $"  >> {who} {u.Name} обращён в бегство (мораль {u.Morale} < порога {u.MoraleThreshold})";

        // Обе стороны по типам войск + общая сводка войны в конце.
        private void ShowField(Army player, Army enemy)
        {
            ShowSide("Ваши войска", player, includeReserve: true);
            ShowSide("Враг", enemy, includeReserve: false);
            int you = player.ActiveArmy().Sum(u => u.Amount);
            int foe = enemy.ActiveArmy().Sum(u => u.Amount);
            _view.Show($"== Война: ваши в строю {you} — враг {foe} ==");
        }

        private void ShowSide(string title, Army army, bool includeReserve)
        {
            _view.Show(title + ":");
            ShowGroups(army.ActiveArmy(), "  ");
            if (includeReserve)
            {
                var reserve = army.ReservedArmy().Where(u => u.Amount > 0).ToList();
                if (reserve.Count > 0)
                {
                    _view.Show("  Резерв:");
                    ShowGroups(reserve, "    ");
                }
            }
        }

        // Группировка по типу: суммарная численность и взвешенная мораль (каждый тип на своей строке).
        private void ShowGroups(List<Unit> units, string indent)
        {
            var groups = units.Where(u => u.Amount > 0)
                .GroupBy(u => u.Name)
                .OrderByDescending(g => g.Sum(u => u.Amount))
                .ToList();
            if (groups.Count == 0)
            {
                _view.Show($"{indent}(никого)");
                return;
            }
            foreach (var g in groups)
            {
                int amount = g.Sum(u => u.Amount);
                int max = g.Sum(u => u.MaxAmount);
                int morale = g.Sum(u => u.Morale * u.Amount) / amount;
                _view.Show($"{indent}{g.Key}: {amount}/{max}, мораль {morale}");
            }
        }

        // Единый почасовой промпт: следующий час / ввести резерв / держать / скип до конца.
        private void HourPrompt(Army player, Army enemy)
        {
            if (_skip) return;

            bool battleLive = player.ActiveArmy().Count > 0 && enemy.ActiveArmy().Count > 0;
            bool reserveReady = battleLive && !_hold && player.ReservedArmy().Count > 0;

            if (!reserveReady && !Balance.PauseEachHour) return; // нечего спрашивать и пауза выключена

            var lines = new List<string> { "1 - следующий час" };
            int n = 2;
            int deploy = 0, hold = 0;
            if (reserveReady)
            {
                deploy = n++; lines.Add($"{deploy} - ввести резерв ({player.ReservedArmy().Count} отр.)");
                hold = n++; lines.Add($"{hold} - держать резерв до окружения");
            }
            int skip = n++; lines.Add($"{skip} - до конца боя (скип)");

            _view.Show(string.Join("\n", lines));
            int choice = _view.ReadChoice(1, skip);
            if (reserveReady && choice == deploy)
            {
                player.DeployReserves();
                _playerDeployed = true;
                _view.Show("Резерв вступает в бой!");
            }
            else if (reserveReady && choice == hold)
            {
                _hold = true;
                _view.Show("Резерв ждёт окружения.");
            }
            else if (choice == skip)
            {
                _skip = true;
            }
            // choice == 1 → просто следующий час
        }
    }

    public abstract class BattleStateBase : IGameState
    {
        protected readonly GameContext _context;
        protected BattleStateBase(GameContext context) => _context = context;

        public abstract IGameState? Update();

        // Проводит интерактивный бой и возвращает исход + число погибших бойцов СВОЕЙ стороны.
        // askReserveCavalry: спросить перед боем, ставить ли конницу в резерв (рейд/оборона).
        protected (BattleResult result, int playerDead) RunBattle(Army player, Army enemy, bool askReserveCavalry)
        {
            int before = (int)player.FullScaleArmy();
            var battle = new InteractiveBattle(_context.BattleSystem, _context.View, _context.Rng);
            BattleResult result = battle.Fight(player, enemy, askReserveCavalry);
            int after = (int)player.FullScaleArmy();
            return (result, before - after);
        }
    }

    public class RaidState : BattleStateBase
    {
        public RaidState(GameContext context) : base(context) { }

        public override IGameState? Update()
        {
            var settlement = _context.Settlement;
            var enemy = EnemyArmyFactory.CreateRaid(_context.Rng, settlement.RaidsDone);
            int enemyBodies = (int)enemy.FullScaleArmy();
            _context.View.Show($"\n=== РЕЙД #{settlement.RaidsDone + 1} (с каждым рейдом враг сильнее) ===");
            _context.View.Show($"Противник: {enemy.Roster().Count} отрядов, {enemyBodies} бойцов. Ваша армия: {(int)settlement.Army.FullScaleArmy()} бойцов.");

            var (result, dead) = RunBattle(settlement.Army, enemy, askReserveCavalry: true);
            settlement.Resources.Reduce(ResourceType.Population, dead);
            _context.View.Show($"Исход: {result}. Погибло ваших бойцов: {dead}.");
            ApplyReward(result);

            settlement.Army.Recover();
            settlement.RegisterRaid();

            if (settlement.Resources.Get(ResourceType.Population) <= 0)
                return new GameOverState(_context);
            return new VillageDayState(_context);
        }

        private void ApplyReward(BattleResult result)
        {
            var res = _context.Settlement.Resources;
            int difficulty = _context.Settlement.RaidsDone;

            if (result == BattleResult.PlayerWon)
            {
                int gold = Balance.RaidLootGoldBase + difficulty * Balance.RaidLootGoldPerDifficulty;
                res.Add(ResourceType.Gold, gold);
                res.Add(ResourceType.Steel, Balance.RaidLootSteel);
                res.Add(ResourceType.Horse, Balance.RaidLootHorse);
                _context.View.Show($"Добыча: золото +{gold}, сталь +{Balance.RaidLootSteel}, лошади +{Balance.RaidLootHorse}.");
            }
            else if (result == BattleResult.EnemyWon)
            {
                res.Reduce(ResourceType.Gold, Balance.RaidPillageGold);
                res.Reduce(ResourceType.Food, Balance.RaidPillageFood);
                _context.View.Show($"Разграблено: золото -{Balance.RaidPillageGold}, еда -{Balance.RaidPillageFood}.");
            }
            else
            {
                _context.View.Show("Ничья — без добычи.");
            }
        }
    }

    public class InvasionState : BattleStateBase
    {
        public InvasionState(GameContext context) : base(context) { }

        public override IGameState? Update()
        {
            var settlement = _context.Settlement;
            var enemy = EnemyArmyFactory.CreateInvasion(_context.Rng);
            _context.View.Show($"\n*** НАШЕСТВИЕ КОЧЕВНИКОВ! Оборона деревни! ***");

            var (result, dead) = RunBattle(settlement.Army, enemy, askReserveCavalry: true);
            settlement.Resources.Reduce(ResourceType.Population, dead);
            _context.View.Show($"Исход обороны: {result}. Погибло: {dead}.");

            settlement.Army.Recover();

            if (result == BattleResult.EnemyWon || settlement.Resources.Get(ResourceType.Population) <= 0)
                return new GameOverState(_context);
            _context.View.Show("Деревня выстояла!");
            return new VillageDayState(_context);
        }
    }

    public class CrusadeState : BattleStateBase
    {
        public CrusadeState(GameContext context) : base(context) { }

        public override IGameState? Update()
        {
            var settlement = _context.Settlement;

            // Армия похода: контингент лорда бьётся (active), твои отряды — в резерве.
            var crusade = new Army();
            foreach (var unit in EnemyArmyFactory.CreateLordHost(_context.Rng, settlement.RaidsDone).Roster())
                crusade.Add(unit);
            foreach (var unit in settlement.Army.Roster())
            {
                unit.SetReserved(true);
                crusade.Add(unit);
            }

            var enemy = EnemyArmyFactory.CreateCrusadeEnemy(_context.Rng);
            _context.View.Show($"\n*** КРЕСТОВЫЙ ПОХОД! *** Лорд ведёт {crusade.ActiveArmy().Count} отрядов, " +
                               $"ваш резерв — {crusade.ReservedArmy().Count}.");

            // Резерв уже сконфигурирован (твои отряды), поэтому коннице вопрос не задаём.
            var (result, _) = RunBattle(crusade, enemy, askReserveCavalry: false);

            if (result == BattleResult.PlayerWon)
                return new VictoryState(_context);
            return new GameOverState(_context);
        }
    }

    public class VillageDayState : IGameState
    {
        readonly GameContext _context;
        public VillageDayState(GameContext context) => _context = context;
        public IGameState? Update()
        {
            while (true)
            {
                _context.View.Show(_context.Settlement.Summary());
                if (_context.Campaign.MusterAnnounced(_context.Settlement.Day))
                    _context.View.Show($">>> Сбор в крестовый поход: нужно {_context.Campaign.LordTroopsRequired} бойцов к дню " +
                                       $"{_context.Campaign.CrusadeDay} (в армии сейчас {(int)_context.Settlement.Army.FullScaleArmy()}).");
                _context.View.Show("Опции: \n1 - Построить здание\n2 - Рабочие\n3 - Торговля\n4 - Найм отрядов\n5 - Рейд\n6 - Закончить день");
                int choice = _context.View.ReadChoice(1, 6);
                switch (choice)
                {
                    case 1: OpenBuildMenu(); break;
                    case 2: OpenWorkerMenu(); break;
                    case 3: OpenTradeMenu(); break;
                    case 4: OpenRecruitMenu(); break;
                    case 5: return new RaidState(_context);
                    case 6: return EndDay();
                }
            }
        }

        private IGameState EndDay()
        {
            DayReport report = _context.EconomySystem.ProcessDay(_context.Settlement);

            if (report.Starved > 0)
                _context.View.Show($"От голода погибло: {report.Starved}.");
            if (report.Immigrants > 0)
                _context.View.Show($"В поселение прибыло иммигрантов: {report.Immigrants}.");

            if (!_context.Settlement.IsAlive)
                return new GameOverState(_context);
            return _context.Campaign.CheckStoryTrigger(_context.Settlement.Day, _context) ?? new VillageDayState(_context);
        }

        private void OpenBuildMenu()
        {
            while (true)
            {
                _context.View.Show("\nПостроенные здания:");
                ShowBuildings();
                _context.View.Show("\nПостроить:\n1 - Поместье\n2 - Кузница\n3 - Конюшня\n4 - Назад");
                int choice = _context.View.ReadChoice(1, 4);
                switch (choice)
                {
                    case 1: BuildSelected("поместье"); break;
                    case 2: BuildSelected("кузница"); break;
                    case 3: BuildSelected("конюшня"); break;
                    case 4: return;
                }
            }
        }

        private void OpenWorkerMenu()
        {
            ShowBuildings();
            _context.View.Show("1 - заполнить всё / 2 - вручную по зданию / 0 - назад");
            int input = _context.View.ReadChoice(0, 2);
            if (input == 0) return;
            else if (input == 1) FillAllBuildings();
            else ManualWorkerMenu();
        }

        private void FillAllBuildings()
        {
            int assigned = 0;
            foreach(var building in _context.Settlement.Buildings)
            {
                int free = _context.Settlement.FreePopulation();
                int add = Math.Min(free, building.MaxSeats - building.OccupiedSeats);
                building.ChangeSeats(add);
                assigned += add;
            }

            if (_context.Settlement.FreePopulation() == 0)
                _context.View.Show($"Назначено рабочих: {assigned}. Свободное население кончилось — часть мест осталась пустой " +
                                   $"(люди заняты в армии: {(int)_context.Settlement.Army.FullScaleArmy()}).");
            else
                _context.View.Show($"Назначено рабочих: {assigned}. Все доступные места заполнены.");
        }

        private void ManualWorkerMenu()
        {
            while (true)
            {
                _context.View.Show("\nВ какое здание назначить или убрать рабочих?(0 для выхода)\n");
                int input = _context.View.ReadChoice(0, _context.Settlement.Buildings.Count);
                if (input == 0) return;
                Building building = _context.Settlement.Buildings[input - 1];
                _context.View.Show("Сколько рабочих назначить(или убрать, тогда нужно начать с -): ");
                input = _context.View.ReadChoice(-(building.OccupiedSeats), Math.Min(_context.Settlement.FreePopulation(), building.MaxSeats - building.OccupiedSeats));
                building.ChangeSeats(input);
                _context.View.Show($"Готово, теперь в здание {building.OccupiedSeats} занятых мест из {building.MaxSeats}");
            }
        }

        private void OpenTradeMenu()
        {
            while (true)
            {
                _context.View.Show(_context.Settlement.Summary());
                _context.View.Show("\nТорговать:\n1 - Еда\n2 - Сталь\n3 - Лошади\n4 - Назад");
                int choice = _context.View.ReadChoice(1, 4);
                if (choice == 4) return;

                ResourceType resource = choice switch
                {
                    1 => ResourceType.Food,
                    2 => ResourceType.Steel,
                    _ => ResourceType.Horse
                };
                TradeResource(resource);
            }
        }

        private void OpenRecruitMenu()
        {
            while (true)
            {
                _context.View.Show($"\nСвободного населения: {_context.Settlement.FreePopulation()}");
                var units = _context.Settlement.Army.AllRemainingUnits();
                foreach(var unit in units)
                {
                    _context.View.Show($"Имя: {unit.Name} - Количество {unit.Amount}/{unit.MaxAmount}");
                }
                _context.View.Show($"\nНанять:\n1 - Ополчение ({FormatCost(Balance.MilitiaCost)})" +
                    $"\n2 - Кавалерия ({FormatCost(Balance.CavalryCost)})" +
                    $"\n3 - Пехота ({FormatCost(Balance.LightInfantryCost)})" +
                    $"\n4 - Восполнить потери" +
                    $"\n5 - Назад");
                int choice = _context.View.ReadChoice(1, 5);
                switch (choice)
                {
                    case 1: RecruitSelected("ополчение"); break;
                    case 2: RecruitSelected("кавалерия"); break;
                    case 3: RecruitSelected("пехота"); break;
                    case 4: ReplenishArmy(); break;
                    case 5: return;
                }
            }
        }

        private void ReplenishArmy()
        {
            var settlement = _context.Settlement;
            ReplenishQuote quote = _context.RecruitmentSystem.QuoteReplenish(settlement);

            if (quote.BodiesNeeded == 0)
            {
                _context.View.Show("Восполнять нечего - армия в полном составе.");
                return;
            }

            string priceStr = string.Join(", ", quote.Price.Select(kv => $"{Balance.ResourceNames[kv.Key]} {kv.Value}"));
            _context.View.Show($"Восполнение: {quote.BodiesNeeded} бойцов; людей нужно {quote.BodiesNeeded}; ресурсы: {priceStr}.");

            if (_context.RecruitmentSystem.Replenish(settlement))
            {
                _context.View.Show("Потери восполнены.");
                return;
            }

            // Точно показываем, чего и сколько не хватает.
            _context.View.Show("Не хватает:");
            int free = settlement.FreePopulation();
            if (quote.BodiesNeeded > free)
                _context.View.Show($"  свободных людей: нужно {quote.BodiesNeeded}, есть {free} (освободите рабочих в меню \"Рабочие\")");
            foreach (var kv in quote.Price)
            {
                int have = settlement.Resources.Get(kv.Key);
                if (have < kv.Value)
                    _context.View.Show($"  {Balance.ResourceNames[kv.Key]}: нужно {kv.Value}, есть {have}");
            }
        }

        private static string FormatCost(IReadOnlyDictionary<ResourceType, int> cost)
        {
            return string.Join(", ", cost.Select(kv => $"{Balance.ResourceNames[kv.Key]}: {kv.Value}"));
        }

        private void RecruitSelected(string key)
        {
            if (UnitFactory.TryCreate(key, out Unit? unit) && unit != null)
            {
                if (_context.RecruitmentSystem.RecruitNew(_context.Settlement, unit))
                    _context.View.Show("Нанято!");
                else
                    _context.View.Show("Не хватает ресурсов или людей.");
            }
        }

        private void BuildSelected(string key)
        {
            if (BuildingFactory.TryCreate(key, out Building? building) && building != null)
            {
                if (_context.BuildingSystem.Build(_context.Settlement, building))
                    _context.View.Show("Построено!");
                else
                    _context.View.Show("Не хватает золота.");
            }
        }

        private void ShowBuildings()
        {
            var buildings = _context.Settlement.Buildings;   
            if (buildings.Count == 0)
            {
                _context.View.Show("Зданий пока нет.");
                return;
            }
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                _context.View.Show($"{i + 1} - {b.Name} (рабочих: {b.OccupiedSeats}/{b.MaxSeats})");
            }
        }

        private void TradeResource(ResourceType resource)
        {
            MarketEntry entry = Balance.MarketPrices[resource];
            _context.View.Show($"\n{Balance.ResourceNames[resource]}: продать {entry.BatchSize} шт за {_context.TradeSystem.CurrentSellPrice(resource)} золота / купить {entry.BatchSize} шт за {entry.BasePrice} золота");
            _context.View.Show("1 - Продать\n2 - Купить\n3 - Назад");
            int choice = _context.View.ReadChoice(1, 3);
            switch (choice)
            {
                case 1:
                    if (_context.TradeSystem.Sell(_context.Settlement, resource))
                        _context.View.Show("Продано!");
                    else
                        _context.View.Show("Нечего продавать.");
                    break;
                case 2:
                    if (_context.TradeSystem.Buy(_context.Settlement, resource))
                        _context.View.Show("Куплено!");
                    else
                        _context.View.Show("Не хватает золота.");
                    break;
                case 3: return;
            }
        }
    }

    public class ConsoleView : IGameView
    {
        public void Show(string message)
        {
            Console.WriteLine(message);
        }

        public int ReadChoice(int min, int max)
        {
            while (true)
            {
                string? line = Console.ReadLine();
                if (line == null)
                {
                    // Конец ввода (stdin закрыт) — выходим, чтобы не зациклиться на null.
                    Console.WriteLine("Ввод завершён. Выход из игры.");
                    Environment.Exit(0);
                }
                if (!int.TryParse(line, out int result))
                {
                    Console.WriteLine("Введите число.");
                    continue;
                }
                if (result < min || result > max)
                {
                    Console.WriteLine($"Введите число от {min} до {max}.");
                    continue;
                }
                return result;
            }
        }
    }

    public interface IGameView
    {
        void Show(string message);
        int ReadChoice(int min, int max);
    }

    public interface IRandom
    {
        int Next(int min, int max);
        double NextDouble();
        double Range(double min, double max);
        // Округляет вероятностно: дробная часть f даёт +1 с вероятностью f.
        // Матожидание сохраняется, мелкие значения не теряются в (int)-усечении.
        int RoundStochastic(double value);
    }

    public class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom() => _random = new Random();
        public SystemRandom(int seed) => _random = new Random(seed);

        public int Next(int min, int max) => _random.Next(min, max);

        public double NextDouble() => _random.NextDouble();

        public double Range(double min, double max) => (max - min) * _random.NextDouble() + min;

        public int RoundStochastic(double value)
        {
            int floor = (int)Math.Floor(value);
            double frac = value - floor;
            return _random.NextDouble() < frac ? floor + 1 : floor;
        }
    }

    // Единая точка гейм-дизайна: все балансные числа собраны здесь.
    public static class Balance
    {
        // --- Стартовые ресурсы ---
        public const int StartPopulation = 200;
        public const int StartGold = 10;
        public const int StartFood = 100;
        public const int StartSteel = 50;
        public const int StartHorse = 10;

        // --- Экономика ---
        public const float FoodConsumptionBase = 0.6f;
        public const float FoodConsumptionVariance = 0.3f;
        public const double StarvationRate = 0.1; // доля дневного дефицита еды, теряемая населением
        public const int BuildingCost = 1;

        // --- Иммиграция (приток при достатке еды, если не было голода) ---
        public const int ImmigrationFoodThreshold = 60; // нужен запас еды выше порога
        public const int ImmigrationFoodPerPerson = 15;  // 1 иммигрант на каждые N еды сверх порога
        public const int ImmigrationMaxPerDay = 50;      // потолок притока за день

        // --- Бой ---
        public const int MaxBattleRounds = 100;
        public const double CasualtyFactor = 0.1;
        public const double CasualtyVarianceMin = 0.75;
        public const double CasualtyVarianceMax = 1.25;
        public const int EnemySquadVariance = 2; // rng.Next(0, X): разброс отрядов врага (±(X-1))
        public const bool PauseEachHour = true; // true: каждый час пауза «след. час / скип» (наблюдение по часам)

        // --- Мораль ---
        public const double MoralePowerMin = 1.5;
        public const double MoralePowerMax = 2.5;
        public const double MoraleScaleMin = 1.2;
        public const double MoraleScaleMax = 1.8;
        public const int PsychologicalMoraleDamage = 2;

        // --- Фланг / окружение ---
        public const int FlankMoraleMin = 5;
        public const int FlankMoraleMax = 10;
        public const double FlankCasualtyFactor = 0.2;
        public const double FlankAdvantageRatio = 1.5;  // во сколько раз нужно больше конницы, чтобы окружить
        public const double FlankChancePerRound = 0.2;  // потолок шанса окружения за раунд

        // --- Поведение ИИ ---
        public const double CautiousReserveThreshold = 0.2;
        public const double ModerateReserveThreshold = 0.5;
        public const double ModerateStartPatience = 1.0;
        public const double ModerateAggressionMin = 0.2;
        public const double ModerateAggressionMax = 0.6;
        public const double ModeratePatienceStep = 0.1;
        public const double ModeratePatienceThreshold = 0.1;
        public const double ModeratePatienceFallback = 3.0; // когда у игрока сила есть, а у врага нет

        // --- Торговля ---
        public const double SellModifierDecay = 0.75;
        public const double ModifierStart = 1.0;

        // --- Статы юнитов: maxAmount, power, morale, moraleThreshold, mobility ---
        public const int MilitiaMaxAmount = 50;
        public const int MilitiaPower = 10;
        public const int MilitiaMorale = 80;
        public const int MilitiaMoraleThreshold = 40;
        public const int MilitiaMobility = 50;

        public const int CavalryMaxAmount = 25;
        public const int CavalryPower = 30;
        public const int CavalryMorale = 90;
        public const int CavalryMoraleThreshold = 20;
        public const int CavalryMobility = 90;

        public const int LightInfantryMaxAmount = 40;
        public const int LightInfantryPower = 18;
        public const int LightInfantryMorale = 80;
        public const int LightInfantryMoraleThreshold = 30;
        public const int LightInfantryMobility = 60;

        // --- Стоимость найма ---
        public static readonly Dictionary<ResourceType, int> MilitiaCost = new()
        {
            { ResourceType.Gold, 4 },
            { ResourceType.Steel, 25 }
        };

        public static readonly Dictionary<ResourceType, int> CavalryCost = new()
        {
            { ResourceType.Gold, 10 },
            { ResourceType.Steel, 40 },
            { ResourceType.Horse, 20 }
        };

        public static readonly Dictionary<ResourceType, int> LightInfantryCost = new()
        {
            { ResourceType.Gold, 10 },
            { ResourceType.Steel, 40 }
        };

        // --- Рыночные цены ---
        public static readonly Dictionary<ResourceType, MarketEntry> MarketPrices = new()
        {
            { ResourceType.Food, new MarketEntry { BasePrice = 10, BatchSize = 50 } },
            { ResourceType.Steel, new MarketEntry { BasePrice = 10, BatchSize = 20 } },
            { ResourceType.Horse, new MarketEntry { BasePrice = 20, BatchSize = 10 } }
        };

        // --- Рейды (масштаб от difficulty = RaidsDone) ---
        public const int RaidBaseSquads = 2;
        public const int RaidSquadsPerDifficulty = 1;
        public const int RaidLootGoldBase = 15;
        public const int RaidLootGoldPerDifficulty = 10;
        public const int RaidLootSteel = 25;
        public const int RaidLootHorse = 10;
        public const int RaidPillageGold = 10;
        public const int RaidPillageFood = 30;
        // Типы войск в рейде (ключи UnitFactory). Меняй этот список, чтобы менять состав рейдов.
        // Отряды раздаются по кругу: {"кавалерия"} = только конница; {"пехота","ополчение"} = пополам и т.д.
        public static readonly string[] RaidUnitTypes = { "кавалерия", "пехота", "ополчение" };

        // --- Сюжет (окна дней, рандом в диапазоне для реиграбельности) ---
        public const int InvasionDayMin = 10;
        public const int InvasionDayMax = 14;
        public const int LordDemandGapMin = 3;   // после нашествия — лорд объявляет сбор
        public const int LordDemandGapMax = 6;
        public const int CrusadeGapMin = 6;      // от объявления до похода — время собрать войско
        public const int CrusadeGapMax = 10;
        public const int LordTroopsRequired = 500; // минимум бойцов, чтобы уйти в поход (иначе поражение)
        public const int CaravanIntervalMin = 3;  // караван приходит раз в 3–5 дней
        public const int CaravanIntervalMax = 5;

        // --- Составы: нашествие / финальный враг / контингент лорда ---
        public const int InvasionCavalry = 2;
        public const int InvasionInfantry = 2;
        public const int InvasionMilitia = 1;
        // Крестовый поход — масштабное сражение на тысячи бойцов. У врага перевес над лордом;
        // твоё войско — значимое меньшинство, способное склонить чашу, но не решающее в одиночку.
        public const int CrusadeEnemyCavalry = 18;   // ~450
        public const int CrusadeEnemyInfantry = 30;  // ~1200
        public const int CrusadeEnemyMilitia = 8;    // ~400   → враг ~2050
        public const int LordHostCavalry = 15;       // ~375
        public const int LordHostInfantry = 25;      // ~1000
        public const int LordHostMilitia = 4;        // ~200   → лорд ~1375
        public const int LordHostPerMerit = 1;       // +отряды за каждый RaidsDone

        // --- Здания (эффективность, мест) ---
        public const float ManorEfficiency = 1.5f;
        public const int ManorSeats = 100;
        public const float SmithyEfficiency = 0.4f;
        public const int SmithySeats = 50;
        public const float StableEfficiency = 0.15f;
        public const int StableSeats = 20;

        // --- Русские названия ресурсов (для UI) ---
        public static readonly Dictionary<ResourceType, string> ResourceNames = new()
        {
            { ResourceType.Population, "Население" },
            { ResourceType.Gold, "Золото" },
            { ResourceType.Food, "Еда" },
            { ResourceType.Steel, "Сталь" },
            { ResourceType.Horse, "Лошади" }
        };
    }
}
