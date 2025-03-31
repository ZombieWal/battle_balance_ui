using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;
using System.Linq;

/// <summary>
/// Battle Balance Tool for running simulations and calculating win rates for hero combinations
/// </summary>
public class BattleBalanceTool : EditorWindow
{
    // Data structures
    private List<Character> allCharacters = new List<Character>();
    private List<Character> selectedCharacters = new List<Character>();
    private List<EnemySetup> enemySetups = new List<EnemySetup>();
    
    // UI elements
    private ScrollView heroSelectionView;
    private ScrollView enemySetupView;
    private ScrollView resultsView;
    
    // Configuration
    private string heroDataPath = "";
    private int numBattles = 1000;
    private int numEnemySetups = 3;
    private int enemyLevel = 1;
    private int enemySkillLevel = 1;
    private int enemyStars = 1;
    private bool isLoaded = false;

    // Simulation results
    private float[,] winRates;

    [MenuItem("Tools/Battle Balance Tool")]
    public static void ShowWindow()
    {
        BattleBalanceTool window = GetWindow<BattleBalanceTool>();
        window.titleContent = new GUIContent("Battle Balance Tool");
        window.minSize = new Vector2(800, 600);
    }

    private void CreateGUI()
    {
        // Root container
        VisualElement root = rootVisualElement;
        
        // Main layout
        var mainContainer = new VisualElement();
        mainContainer.style.flexDirection = FlexDirection.Column;
        mainContainer.style.flexGrow = 1;
        root.Add(mainContainer);
        
        // Create sections
        CreateConfigSection(mainContainer);
        CreateHeroSelectionSection(mainContainer);
        CreateEnemySetupSection(mainContainer);
        CreateSimulationSection(mainContainer);
        CreateResultsSection(mainContainer);
    }

    private void CreateConfigSection(VisualElement container)
    {
        var configSection = new Box();
        configSection.style.marginBottom = 10;
        container.Add(configSection);
        
        var titleLabel = new Label("Battle Balance Configuration");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 16;
        configSection.Add(titleLabel);
        
        // File selection
        var fileRow = new VisualElement();
        fileRow.style.flexDirection = FlexDirection.Row;
        fileRow.style.marginTop = 5;
        configSection.Add(fileRow);
        
        var filePathField = new TextField("Hero Data Path (CSV):");
        filePathField.style.flexGrow = 1;
        filePathField.value = heroDataPath;
        filePathField.RegisterValueChangedCallback(evt => heroDataPath = evt.newValue);
        fileRow.Add(filePathField);
        
        var browseButton = new Button(() => {
            string path = EditorUtility.OpenFilePanel("Select Hero Data CSV", "", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                heroDataPath = path;
                filePathField.value = path;
            }
        });
        browseButton.text = "Browse";
        fileRow.Add(browseButton);
        
        var loadButton = new Button(() => LoadHeroData());
        loadButton.text = "Load Heroes";
        fileRow.Add(loadButton);
        
        // Battle configuration
        var configRow = new VisualElement();
        configRow.style.flexDirection = FlexDirection.Row;
        configRow.style.marginTop = 10;
        configSection.Add(configRow);
        
        var battlesField = new IntegerField("Number of Battles:");
        battlesField.value = numBattles;
        battlesField.RegisterValueChangedCallback(evt => numBattles = evt.newValue);
        configRow.Add(battlesField);
        
        var enemySetupsField = new IntegerField("Number of Enemy Setups:");
        enemySetupsField.value = numEnemySetups;
        enemySetupsField.RegisterValueChangedCallback(evt => {
            numEnemySetups = evt.newValue;
            UpdateEnemySetups();
        });
        configRow.Add(enemySetupsField);
    }

    private void CreateHeroSelectionSection(VisualElement container)
    {
        var heroSection = new Box();
        heroSection.style.marginBottom = 10;
        container.Add(heroSection);
        
        var titleLabel = new Label("Character Selection (max 5)");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 16;
        heroSection.Add(titleLabel);
        
        heroSelectionView = new ScrollView();
        heroSelectionView.style.height = 150;
        heroSection.Add(heroSelectionView);
        
        var selectedLabel = new Label("Selected Characters:");
        selectedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        selectedLabel.style.marginTop = 10;
        heroSection.Add(selectedLabel);
        
        // Important: Create a named container for selected heroes that will persist
        var selectedHeroesBox = new Box();
        selectedHeroesBox.name = "SelectedHeroesBox"; // Name it so we can find it later
        selectedHeroesBox.style.minHeight = 50;
        heroSection.Add(selectedHeroesBox);
        
        // Create the container for selected heroes inside the box
        var selectedHeroesContainer = new VisualElement();
        selectedHeroesContainer.name = "SelectedHeroesContainer";
        selectedHeroesContainer.style.flexDirection = FlexDirection.Row;
        selectedHeroesContainer.style.marginTop = 5;
        selectedHeroesBox.Add(selectedHeroesContainer);
    }

    private void CreateEnemySetupSection(VisualElement container)
    {
        var enemySection = new Box();
        enemySection.style.marginBottom = 10;
        container.Add(enemySection);
        
        var titleLabel = new Label("Enemy Setup Configuration");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 16;
        enemySection.Add(titleLabel);
        
        // Default parameters
        var defaultParamsRow = new VisualElement();
        defaultParamsRow.style.flexDirection = FlexDirection.Row;
        defaultParamsRow.style.marginTop = 5;
        enemySection.Add(defaultParamsRow);
        
        var levelField = new IntegerField("Enemy Level:");
        levelField.value = enemyLevel;
        levelField.RegisterValueChangedCallback(evt => enemyLevel = evt.newValue);
        defaultParamsRow.Add(levelField);
        
        var skillField = new IntegerField("Skill Level:");
        skillField.value = enemySkillLevel;
        skillField.RegisterValueChangedCallback(evt => enemySkillLevel = evt.newValue);
        defaultParamsRow.Add(skillField);
        
        var starsField = new IntegerField("Stars:");
        starsField.value = enemyStars;
        starsField.RegisterValueChangedCallback(evt => enemyStars = evt.newValue);
        defaultParamsRow.Add(starsField);
        
        enemySetupView = new ScrollView();
        enemySetupView.style.height = 150;
        enemySection.Add(enemySetupView);
    }

    private void CreateSimulationSection(VisualElement container)
    {
        var simulationSection = new VisualElement();
        simulationSection.style.marginBottom = 10;
        container.Add(simulationSection);
        
        var runButton = new Button(() => RunSimulation());
        runButton.text = "Run Battle Simulation";
        runButton.style.height = 30;
        simulationSection.Add(runButton);
    }

    private void CreateResultsSection(VisualElement container)
    {
        var resultsSection = new Box();
        container.Add(resultsSection);
        
        var titleLabel = new Label("Simulation Results");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 16;
        resultsSection.Add(titleLabel);
        
        resultsView = new ScrollView();
        resultsView.style.height = 200;
        resultsSection.Add(resultsView);
    }

    private void LoadHeroData()
    {
        if (string.IsNullOrEmpty(heroDataPath) || !File.Exists(heroDataPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid CSV file", "OK");
            return;
        }
        
        try
        {
            allCharacters.Clear();
            selectedCharacters.Clear();
            
            string[] lines = File.ReadAllLines(heroDataPath);
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                
                // Ensure we have all required columns (20 fields in the specified schema)
                if (values.Length >= 20) 
                {
                    // Parse boolean from string (check if "true", "yes", "1", etc.)
                    bool isEnemy = false;
                    if (!string.IsNullOrEmpty(values[1]))
                    {
                        string enemyValue = values[1].Trim().ToLower();
                        isEnemy = enemyValue == "true" || enemyValue == "yes" || enemyValue == "1";
                    }
                    
        Character character = new Character
        {
            Id = int.Parse(values[0]),
            IsEnemy = isEnemy,
            Name = values[2],
            Type = values[3],
            Role = values[4],
            Faction = values[5],
            Troop = values[6],
            TroopCount = ParseIntSafe(values[7]),
            ATK = ParseFloatSafe(values[8]),   // ATK in spreadsheet
            HP = ParseFloatSafe(values[9]),   // HP in spreadsheet
            Range = ParseFloatSafe(values[10]),
            ATKTime = ParseFloatSafe(values[11]),  // ATKTime in spreadsheet
            CritRating = ParseFloatSafe(values[12]),
            Accuracy = ParseFloatSafe(values[13]),
            Dodge = ParseFloatSafe(values[14]),
            DPS = ParseFloatSafe(values[15]),
            MovementSpeed = ParseFloatSafe(values[16]),
            Level = ParseIntSafe(values[17]),
            Star = ParseIntSafe(values[18]),
            Power = ParseIntSafe(values[19])
        };
                    
                    // Only add non-enemy characters (heroes) to the selectable list
                    if (!character.IsEnemy)
                    {
                        allCharacters.Add(character);
                    }
                }
            }
            
            UpdateHeroSelectionUI();
            UpdateEnemySetups();
            isLoaded = true;
            
            EditorUtility.DisplayDialog("Success", $"Loaded {allCharacters.Count} characters from the CSV file", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading character data: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to load character data: {ex.Message}", "OK");
        }
    }
    
    // Helper methods for safe parsing
    private int ParseIntSafe(string value)
    {
        int result = 0;
        int.TryParse(value, out result);
        return result;
    }
    
    private float ParseFloatSafe(string value)
    {
        float result = 0f;
        float.TryParse(value, out result);
        return result;
    }

    private void UpdateHeroSelectionUI()
    {
        heroSelectionView.Clear();
        
        foreach (var character in allCharacters)
        {
            var characterRow = new VisualElement();
            characterRow.style.flexDirection = FlexDirection.Row;
            characterRow.style.marginBottom = 5;
            
            var checkbox = new Toggle();
            checkbox.value = selectedCharacters.Contains(character);
            checkbox.RegisterValueChangedCallback(evt => {
                if (evt.newValue)
                {
                    if (selectedCharacters.Count < 5)
                    {
                        selectedCharacters.Add(character);
                    }
                    else
                    {
                        checkbox.value = false;
                        EditorUtility.DisplayDialog("Selection Limit", "You can select a maximum of 5 characters", "OK");
                    }
                }
                else
                {
                    selectedCharacters.Remove(character);
                }
                
                UpdateSelectedHeroesDisplay();
            });
            characterRow.Add(checkbox);
            
            var nameLabel = new Label($"{character.Name} ({character.Type})");
            nameLabel.style.width = 150;
            characterRow.Add(nameLabel);
            
            var statsLabel = new Label($"{character.Role} | Lvl: {character.Level} | ★: {character.Star} | Power: {character.Power}");
            characterRow.Add(statsLabel);
            
            var detailedStatsLabel = new Label($"HP: {character.HP}, ATK: {character.ATK}, DPS: {character.DPS}");
            detailedStatsLabel.style.marginLeft = 10;
            characterRow.Add(detailedStatsLabel);
            
            heroSelectionView.Add(characterRow);
        }
        
        UpdateSelectedHeroesDisplay();
    }

    private void UpdateSelectedHeroesDisplay()
    {
        // Find the parent container for selected heroes display
        VisualElement parent = rootVisualElement.Q<Box>("SelectedHeroesBox");
        if (parent == null)
        {
            Debug.LogWarning("Could not find 'SelectedHeroesBox' container. Creating a fallback container.");
            return; // Exit early to prevent errors
        }
        
        // Clear existing displayed heroes
        var existingContainer = parent.Q<VisualElement>("SelectedHeroesContainer");
        if (existingContainer != null)
        {
            existingContainer.Clear(); // Clear children instead of removing the container
        }
        else
        {
            // Create the container if it doesn't exist
            existingContainer = new VisualElement();
            existingContainer.name = "SelectedHeroesContainer";
            existingContainer.style.flexDirection = FlexDirection.Row;
            existingContainer.style.marginTop = 5;
            parent.Add(existingContainer);
        }
        
        // Add selected characters to the container
        foreach (var character in selectedCharacters)
        {
            var characterBox = new Box();
            characterBox.style.marginRight = 10;
            characterBox.style.paddingLeft = 5;
            characterBox.style.paddingRight = 5;
            
            var nameLabel = new Label(character.Name);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            characterBox.Add(nameLabel);
            
            var typeRoleLabel = new Label($"{character.Type} - {character.Role}");
            characterBox.Add(typeRoleLabel);
            
            var statsLabel = new Label($"Lvl {character.Level} | ★{character.Star} | {character.Power} PWR");
            characterBox.Add(statsLabel);
            
            existingContainer.Add(characterBox);
        }
    }

    private void UpdateEnemySetups()
    {
        enemySetups.Clear();
        for (int i = 0; i < numEnemySetups; i++)
        {
            enemySetups.Add(new EnemySetup
            {
                Id = i + 1,
                Level = enemyLevel,
                SkillLevel = enemySkillLevel,
                Stars = enemyStars,
                Difficulty = 1.0f + (i * 0.25f)
            });
        }
        
        UpdateEnemySetupUI();
    }

    private void UpdateEnemySetupUI()
    {
        enemySetupView.Clear();
        
        foreach (var setup in enemySetups)
        {
            var setupBox = new Box();
            setupBox.style.marginBottom = 10;
            
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            setupBox.Add(titleRow);
            
            var titleLabel = new Label($"Enemy Setup #{setup.Id}");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(titleLabel);
            
            var paramsRow = new VisualElement();
            paramsRow.style.flexDirection = FlexDirection.Row;
            paramsRow.style.marginTop = 5;
            setupBox.Add(paramsRow);
            
            var levelField = new IntegerField("Level:");
            levelField.value = setup.Level;
            levelField.RegisterValueChangedCallback(evt => setup.Level = evt.newValue);
            paramsRow.Add(levelField);
            
            var skillField = new IntegerField("Skill Level:");
            skillField.value = setup.SkillLevel;
            skillField.RegisterValueChangedCallback(evt => setup.SkillLevel = evt.newValue);
            paramsRow.Add(skillField);
            
            var starsField = new IntegerField("Stars:");
            starsField.value = setup.Stars;
            starsField.RegisterValueChangedCallback(evt => setup.Stars = evt.newValue);
            paramsRow.Add(starsField);
            
            var difficultyField = new FloatField("Difficulty Multiplier:");
            difficultyField.value = setup.Difficulty;
            difficultyField.RegisterValueChangedCallback(evt => setup.Difficulty = evt.newValue);
            paramsRow.Add(difficultyField);
            
            enemySetupView.Add(setupBox);
        }
    }

    private void RunSimulation()
    {
        if (!isLoaded)
        {
            EditorUtility.DisplayDialog("Error", "Please load character data first", "OK");
            return;
        }
        
        if (selectedCharacters.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select at least one character", "OK");
            return;
        }
        
        if (enemySetups.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please configure at least one enemy setup", "OK");
            return;
        }
        
        // Initialize results array - just one row for the team's combined results against each enemy setup
        winRates = new float[1, enemySetups.Count];
        
        // Show progress bar
        EditorUtility.DisplayProgressBar("Running Simulations", "Calculating battle outcomes...", 0f);
        
        // Simulate team battles against each enemy setup
        for (int enemyIdx = 0; enemyIdx < enemySetups.Count; enemyIdx++)
        {
            int wins = 0;
            
            // Run the specified number of battle simulations
            for (int battle = 0; battle < numBattles; battle++)
            {
                bool teamWins = SimulateTeamBattle(selectedCharacters, enemySetups[enemyIdx]);
                if (teamWins) wins++;
                
                // Update progress
                float progress = (enemyIdx * numBattles + battle) / 
                                (float)(enemySetups.Count * numBattles);
                EditorUtility.DisplayProgressBar("Running Simulations", 
                    $"Team vs Enemy Setup {enemyIdx+1}/{enemySetups.Count}, Battle {battle+1}/{numBattles}", 
                    progress);
            }
            
            // Calculate win rate
            winRates[0, enemyIdx] = (float)wins / numBattles * 100f;
        }
        
        EditorUtility.ClearProgressBar();
        
        DisplayResults();
    }
    
    private bool SimulateTeamBattle(List<Character> team, EnemySetup enemySetup)
    {
        // Mock team battle simulation that returns random results
        // This is a placeholder for actual battle logic to be implemented later
        
        // Calculate a weighted random outcome based on the team's combined power and enemy difficulty
        float teamStrength = 0;
        foreach (var character in team)
        {
            // Add each character's contribution to team strength
            teamStrength += character.Power * (1.0f + (character.Level * 0.05f) + (character.Star * 0.1f));
        }
        
        // Adjust team strength based on team size (diminishing returns for large teams)
        if (team.Count > 1)
        {
            teamStrength *= (1.0f + (0.8f * (team.Count - 1) / 4.0f));
        }
        
        float enemyStrength = 1000 * enemySetup.Level * enemySetup.Stars * enemySetup.Difficulty;
        
        // Calculate win probability (between 0.1 and 0.9)
        float winProbability = Mathf.Clamp(teamStrength / (teamStrength + enemyStrength), 0.1f, 0.9f);
        
        // Random outcome
        return UnityEngine.Random.value < winProbability;
        
        /*
        // TODO: Replace with actual team battle simulation logic later
        // The actual battle simulation would implement your game's combat rules
        // and consider team composition, synergies, etc.
        */
    }

    private void DisplayResults()
    {
        resultsView.Clear();
        
        var resultsTable = new Box();
        resultsView.Add(resultsTable);
        
        // Header row
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        resultsTable.Add(headerRow);
        
        var cornerCell = new Label("Team \\ Enemy");
        cornerCell.style.width = 150;
        cornerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerRow.Add(cornerCell);
        
        for (int i = 0; i < enemySetups.Count; i++)
        {
            var headerCell = new Label($"Setup #{enemySetups[i].Id}");
            headerCell.style.width = 80;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.unityTextAlign = TextAnchor.MiddleCenter;
            headerRow.Add(headerCell);
        }
        
        // Data row (just one row for the team)
        var dataRow = new VisualElement();
        dataRow.style.flexDirection = FlexDirection.Row;
        resultsTable.Add(dataRow);
        
        var teamCell = new Label("Selected Team");
        teamCell.style.width = 150;
        teamCell.style.unityFontStyleAndWeight = FontStyle.Bold;
        dataRow.Add(teamCell);
        
        for (int enemyIdx = 0; enemyIdx < enemySetups.Count; enemyIdx++)
        {
            var rateCell = new Box();
            rateCell.style.width = 80;
            rateCell.style.height = 30;
            rateCell.style.marginRight = 2;
            rateCell.style.marginBottom = 2;
            
            float winRate = winRates[0, enemyIdx];
            var rateLabel = new Label($"{winRate:F1}%");
            rateLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Color coding based on win rate
            if (winRate >= 80)
            {
                rateCell.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green
                rateLabel.style.color = Color.white;
            }
            else if (winRate >= 50)
            {
                rateCell.style.backgroundColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
            }
            else if (winRate >= 30)
            {
                rateCell.style.backgroundColor = new Color(0.8f, 0.5f, 0.2f); // Orange
            }
            else
            {
                rateCell.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red
                rateLabel.style.color = Color.white;
            }
            
            rateCell.Add(rateLabel);
            dataRow.Add(rateCell);
        }
        
        // Team composition summary
        var teamSummary = new Box();
        teamSummary.style.marginTop = 10;
        resultsView.Add(teamSummary);
        
        var teamSummaryTitle = new Label("Team Composition");
        teamSummaryTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        teamSummary.Add(teamSummaryTitle);
        
        var teamList = new VisualElement();
        teamSummary.Add(teamList);
        
        foreach (var character in selectedCharacters)
        {
            var characterEntry = new Label($"• {character.Name} (Lvl {character.Level}, ★{character.Star}, {character.Power} PWR)");
            teamList.Add(characterEntry);
        }
        
        // Add CSV export button
        var exportButton = new Button(() => ExportResultsToCSV());
        exportButton.text = "Export Results to CSV";
        exportButton.style.marginTop = 10;
        resultsView.Add(exportButton);
    }

    private void ExportResultsToCSV()
    {
        string path = EditorUtility.SaveFilePanel("Save Results", "", "TeamBattleSimulationResults.csv", "csv");
        if (string.IsNullOrEmpty(path))
            return;
            
        try
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                // Write header row with enemy setup info
                writer.Write("Team vs Enemy Setup");
                for (int i = 0; i < enemySetups.Count; i++)
                {
                    writer.Write($",Setup #{enemySetups[i].Id} (Level: {enemySetups[i].Level}, Stars: {enemySetups[i].Stars}, Difficulty: {enemySetups[i].Difficulty})");
                }
                writer.WriteLine();
                
                // Write win rates row
                writer.Write("Win Rate (%)");
                for (int enemyIdx = 0; enemyIdx < enemySetups.Count; enemyIdx++)
                {
                    writer.Write($",{winRates[0, enemyIdx]:F1}");
                }
                writer.WriteLine();
                
                // Add blank line
                writer.WriteLine();
                
                // Write team composition
                writer.WriteLine("Team Composition:");
                foreach (var character in selectedCharacters)
                {
                    writer.WriteLine($"{character.Name},Level: {character.Level},Stars: {character.Star},Power: {character.Power},Type: {character.Type},Role: {character.Role}");
                }
            }
            
            EditorUtility.DisplayDialog("Export Complete", "Team battle results have been exported to CSV successfully", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error exporting results: {ex.Message}");
            EditorUtility.DisplayDialog("Export Error", $"Failed to export results: {ex.Message}", "OK");
        }
    }
}

/// <summary>
/// Represents a character with stats based on specified CSV format
/// </summary>
[Serializable]
public class Character
{
    public int Id;
    public bool IsEnemy;
    public string Name;
    public string Type;
    public string Role;
    public string Faction;
    public string Troop;
    public int TroopCount;
    public float ATK;  // ATK in spreadsheet
    public float HP;  // HP in spreadsheet
    public float Range;
    public float ATKTime;  // ATKTime in spreadsheet
    public float CritRating;
    public float Accuracy;
    public float Dodge;
    public float DPS;
    public float MovementSpeed;
    public int Level;
    public int Star;
    public int Power;
}

/// <summary>
/// Represents an enemy configuration for battle simulation
/// </summary>
[Serializable]
public class EnemySetup
{
    public int Id;
    public int Level;
    public int SkillLevel;
    public int Stars;
    public float Difficulty;
}