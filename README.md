Unity Editor tool for testing and balancing character teams against various enemy configurations.

### Features

Import character data from CSV
Select up to 5 characters to form a team
Configure enemy setups with customizable parameters
Run batch simulations to calculate win rates
View color-coded results matrix
Export results to CSV for further analysis

### Usage

Access the tool from Unity's menu: Tools > Battle Balance Tool
Load character data from a CSV file with the following columns:

ID, Enemy?, Name, Type, Role, Faction, Troop, Troop Count, ATK, HP, Range, ATKTime, CritRating, Accuracy, Dodge, DPS, Movement Speed, Level, Star, Power


Select characters to form your team
Configure enemy setups with different parameters
Run the simulation to calculate win rates
Analyze the results and export to CSV if needed

### Installation

Create an "Editor" folder in your Unity project if one doesn't exist
Add the BattleBalanceTool.cs script to this folder
Restart Unity or refresh the Asset Database

### Requirements

Unity 2019.4 or newer
Character data in CSV format

### Notes
The battle simulation currently uses a weighted random approach. You can customize the SimulateTeamBattle() method to implement your game's actual combat mechanics.
