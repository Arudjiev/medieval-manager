using Medieval_Manager_Clean_Code;

// Composition root: собираем все системы и запускаем стейт-машину с деревни.
IRandom rng = new SystemRandom();

var resources = new Resources();
var army = new Army(); // игрок начинает без армии и набирает её сам

var settlement = new Settlement("Гринфорд", resources, army);

var context = new GameContext(
    settlement,
    new BattleSystem(rng),
    new EconomySystem(rng),
    new RecruitmentSystem(),
    new BuildingSystem(),
    new TradeSystem(),
    new CraftingSystem(),
    new Campaign(rng),
    new ConsoleView(),
    rng);

new GameStateMachine(new VillageWeekState(context)).Run();