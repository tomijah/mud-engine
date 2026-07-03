namespace Mud.Core.Game
{
    public static class DefaultWorld
    {
        public static World Create()
        {
            var world = new World();

            var townSquare = new Room
            {
                Name = "Town Square",
                Description = "A bustling square at the heart of town. A weathered fountain gurgles in the center.",
            };

            var northRoad = new Room
            {
                Name = "North Road",
                Description = "A dusty road leading out of town. Wagon tracks cut deep into the dirt.",
            };

            var tavern = new Room
            {
                Name = "The Rusty Tankard",
                Description = "A dim, smoky tavern. The smell of stale ale hangs in the air.",
            };

            var market = new Room
            {
                Name = "Market Street",
                Description = "Stalls line both sides of the street, their keepers hawking wares of every kind.",
            };

            townSquare.Exits["north"] = northRoad.Id;
            townSquare.Exits["east"] = tavern.Id;
            townSquare.Exits["west"] = market.Id;
            northRoad.Exits["south"] = townSquare.Id;
            tavern.Exits["west"] = townSquare.Id;
            market.Exits["east"] = townSquare.Id;

            world.AddRoom(townSquare, isStartingRoom: true);
            world.AddRoom(northRoad);
            world.AddRoom(tavern);
            world.AddRoom(market);

            return world;
        }
    }
}
